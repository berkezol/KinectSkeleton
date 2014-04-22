using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;


namespace KinectSkeleton
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Initial commit for gitHub KinectSkeleton repository
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {

        #region localVariables
        private  KinectSensor sensor;
        private WriteableBitmap colorBitmap;
        private byte[] colorPixels;
        private Skeleton[] skeletonData;
        DrawingGroup drawingGroup;
        private DrawingImage imageSource;
        private const float renderWidth = 640.0f;
        private const float renderHeight = 480.0f;

        #endregion
 
        public MainWindow()
        {
            InitializeComponent();
        }


        //Jedi push
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach(KinectSensor potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }
            if (this.sensor != null)
            {
                ///rgp camera picture to image1
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(
                    this.sensor.ColorStream.FrameWidth, 
                    this.sensor.ColorStream.FrameHeight, 
                    96.0, 
                    96.0, 
                    PixelFormats.Bgr32, 
                    null);
                this.image1.Source = this.colorBitmap;

                //skeleton tracking to image2
                this.drawingGroup = new DrawingGroup();
                this.imageSource = new DrawingImage(this.drawingGroup);
                image2.Source = this.imageSource;

                this.sensor.SkeletonStream.Enable();
                this.sensor.ColorFrameReady += sensor_ColorFrameReady;
                this.sensor.SkeletonFrameReady += sensor_SkeletonFrameReady;

                try
                {
                    sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
            else 
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
            }
        }
        private void sensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
           // Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, renderWidth, renderHeight));

                //skeletonData[] nálam
                if (skeletonData.Length != 0)
                {
                    foreach (Skeleton skel in skeletonData)
                    {
                        //RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            Brushes.Blue,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            10,
                            10);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, renderWidth, renderHeight));
            }
        }

        void sensor2_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) 
            {
                if (skeletonFrame != null)
                {
                    skeletonData = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                }
                if (skeletonData == null) 
                {
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(
                        new Rect(
                            0.0, 0.0, 
                            renderWidth, 
                            renderHeight));
                    return;
                }
                using(DrawingContext drawingContext = this.drawingGroup.Open())
                {
                    drawingContext.DrawRectangle(Brushes.Black,null,new Rect(0,0,renderWidth, renderHeight));
                        
                    foreach(Skeleton skeleton in skeletonData) 
                    {
                        if (skeleton.TrackingState == SkeletonTrackingState.NotTracked) 
                        {
                            this.DrawBonesAndJoints(skeleton, drawingContext);  
                        }
                        else if (skeleton.TrackingState == SkeletonTrackingState.PositionOnly) 
                        {
                            drawingContext.DrawEllipse(
                                Brushes.Blue,
                                null,
                                this.SkeletonPointToScreen(skeleton.Position),
                                10, 10);
                            
                        }
                    }
                }                           
            }
        }

        private Point SkeletonPointToScreen(SkeletonPoint skeletonPoint)
        {
            //throw new NotImplementedException();
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                skeletonPoint, 
                DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// itt rajzolom a csontokat meg a kapcsolatokat
        /// </summary>
        /// <param name="skeletonFrame"></param>
        /// <param name="drawingContext"></param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = Brushes.Yellow;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = Brushes.Red;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), 3, 3);
                }
            }

            //throw new NotImplementedException();
        }

        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1) 
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = new Pen(Brushes.Gray, 1);
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = new Pen(Brushes.Green, 6);
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
 
        }

        void sensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            // throw new NotImplementedException();
            using (ColorImageFrame cololFrame = e.OpenColorImageFrame()) 
            {
                if(cololFrame != null)
                {
                    cololFrame.CopyPixelDataTo(this.colorPixels);
                    this.colorBitmap.WritePixels(
                        new Int32Rect(
                            0, 0, this.colorBitmap.PixelWidth, 
                            this.colorBitmap.PixelHeight),
                            this.colorPixels, 
                            this.colorBitmap.PixelWidth * sizeof(int),0);
                }
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (this.sensor != null) { sensor.Stop(); }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.sensor != null) { sensor.Stop(); }
        }
    }
}

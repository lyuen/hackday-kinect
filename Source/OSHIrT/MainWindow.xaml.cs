using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;
using Coding4Fun.Kinect.Wpf.Controls;
using com.google.zxing;
using com.google.zxing.common;
using com.google.zxing.qrcode;
using OSHIrT.src;

namespace OSHIrT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Product> tshirts;
        private int shirtIndex = 0;

        private Canvas activeCanvas;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];

        //DCAPI
        DcapiClient dcapiClient;
        ProductService productService;
        CartService cartService;
        CheckoutService checkoutService;

        //QRCode
        private DispatcherTimer qrCodeTimer;
        private QRCodeReader qrCodeReader = new QRCodeReader();
        private Hashtable hint;
        private Boolean doneDetectingCode = true;

        //Bounding Box variables
        private double MinDistanceFromCamera = 1.75d;
        private double BoundsDepth = 1.00d;
        private double BoundsWidth = 1.00d;

        //Background Workers
        private BackgroundWorker checkoutBgWorker;

        public MainWindow()
        {
            dcapiClient = new DcapiClientImpl("http://10.10.3.215:8080/dcapi");
            productService = new ProductServiceImpl(dcapiClient);
            cartService = new CartServiceImpl(dcapiClient);
            checkoutService = new CheckoutServiceImpl(dcapiClient);

            //populate catalog products
            tshirts = productService.getTshirtProducts();

            //tshirts[0] = "All your base are belong to us";
            //tshirts[1] = "Bananas";
            //tshirts[2] = "Coffee Tree";

            InitializeComponent();
            initializeQRCodeProperties();
            initializeStoreButtons();
            initializeCheckoutButtons();
            initializeOrderReceiptButtons();
            switchCanvas(storeCanvas);
         
        }

        private void switchCanvas(Canvas canvas)
        {
            // Already active, do nothing
            if (canvas == activeCanvas)
            {
                return;
            }

            // Hide the current canvas
            if (activeCanvas != null)
            {
                activeCanvas.Visibility = Visibility.Hidden;
            }
            // Make the target visible, remember it
            canvas.Visibility = Visibility.Visible;
            activeCanvas = canvas;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensorChooser1.KinectSensorChanged += new DependencyPropertyChangedEventHandler(kinectSensorChooser1_KinectSensorChanged);
        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldValue;
            Stop_Kinect(oldSensor);

            KinectSensor newSensor = (KinectSensor)e.NewValue;

            if (newSensor == null)
            {
                return;
            }

            //Need these for skeleton tracking
            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.0f,
                Prediction = 1.0f,
                JitterRadius = 1.0f,
                MaxDeviationRadius = 0.5f
            };

            newSensor.SkeletonStream.Enable(parameters);

            newSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            newSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(_sensor_AllFramesReady);
            try
            {
                newSensor.Start();
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Stop_Kinect(kinectSensorChooser1.Kinect);
        }

        //Kinect Methods

        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            //Get a skeleton
            Skeleton first = GetFirstSkeleton(e);

            if (doneDetectingCode) {
                qrCodeTimer.Stop();
            }

            if (first == null || !GetUserIsInRange(first.Joints[JointType.Spine]))
            {
                //cursorImage.Visibility = System.Windows.Visibility.Hidden;
                //cursorLeftImage.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
            //else
            //{
                //cursorImage.Visibility = System.Windows.Visibility.Visible;
                //cursorLeftImage.Visibility = System.Windows.Visibility.Visible;
            //}
            loadImageFromCurrentIndex();


            //Cursor position
            ScaleCursorPosition(cursorImage, first.Joints[JointType.HandRight]);
            ScaleCursorPosition(cursorLeftImage, first.Joints[JointType.HandLeft]);
            ScaleShirtPosition(tshirtImage, first.Joints[JointType.Spine]);
            GetCameraPoint(first, e);

            CheckButton(cart_button, cursorImage);
            CheckButton(checkout_button, cursorImage);
            CheckButton(left_arrow_button, cursorLeftImage);
            CheckButton(right_arrow_button, cursorLeftImage);
            CheckButton(confirm_order_button, cursorImage);
            CheckButton(cancel_order_button, cursorImage);
            CheckButton(close_receipt_button, cursorImage);
        }

        void Stop_Kinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                if (sensor.AudioSource != null)
                {
                    sensor.AudioSource.Stop();
                }
            }
        }

        private Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                {
                    return null;
                }

                skeletonFrame.CopySkeletonDataTo(allSkeletons);

                Skeleton first = (from s in allSkeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();

                return first;
            }
        }

        private void ScaleCursorPosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 

            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(640, 480, 1f, 1f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);
        }

        private void ScaleShirtPosition(FrameworkElement element, Joint joint)
        {
            //convert the value to X/Y
            //Joint scaledJoint = joint.ScaleTo(1280, 720); 

            //convert & scale (.3 = means 1/3 of joint distance)
            Joint scaledJoint = joint.ScaleTo(640, 480, 1f, 1f);

            Canvas.SetLeft(element, scaledJoint.Position.X);
            Canvas.SetTop(element, scaledJoint.Position.Y);
        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null ||
                    kinectSensorChooser1.Kinect == null)
                {
                    return;
                }

                DepthImagePoint rightHandDepthPoint = depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);

                ColorImagePoint rightHandColorPoint = depth.MapToColorImagePoint(rightHandDepthPoint.X, rightHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                DepthImagePoint leftHandDepthPoint = depth.MapFromSkeletonPoint(first.Joints[JointType.HandLeft].Position);

                ColorImagePoint leftHandColorPoint = depth.MapToColorImagePoint(leftHandDepthPoint.X, leftHandDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                

                DepthImagePoint spineDepthPoint = depth.MapFromSkeletonPoint(first.Joints[JointType.Spine].Position);

                ColorImagePoint spineColorPoint = depth.MapToColorImagePoint(spineDepthPoint.X, spineDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                
                CameraCursorPosition(cursorImage, rightHandColorPoint);
                CameraCursorPosition(cursorLeftImage, leftHandColorPoint);
                CameraShirtPosition(tshirtImage, spineColorPoint);
            }

        }

        private void CameraCursorPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X);
            Canvas.SetTop(element, point.Y - element.Height / 2);

        }

        private void CameraShirtPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner
            Canvas.SetLeft(element, point.X - element.Width / 2 + 25);
            Canvas.SetTop(element, point.Y - element.Height / 2 - 25);

        }

        private void CheckButton(HoverButton button, System.Windows.Controls.Image cursor)
        {
            if (IsItemMidpointInContainer(button, cursor))
            {
                button.Hovering();
            }
            else
            {
                button.Release();
            }
        }

        public bool GetUserIsInRange(Joint torsoJoint)
        {
            return torsoJoint.Position.Z > this.MinDistanceFromCamera &
                torsoJoint.Position.Z < (this.MinDistanceFromCamera + this.BoundsDepth)
                & torsoJoint.Position.X > -this.BoundsWidth / 2 &
                torsoJoint.Position.X < this.BoundsWidth / 2;
        }

        // Kinect Button Logic

        public static bool IsItemMidpointInContainer(FrameworkElement container, FrameworkElement target)
        {
            if (!container.IsVisible)
            {
                return false;
            }

            var containerTopLeft = container.PointToScreen(new System.Windows.Point());
            var itemTopLeft = target.PointToScreen(new System.Windows.Point());

            var _topBoundary = containerTopLeft.Y;
            var _bottomBoundary = _topBoundary + container.ActualHeight;
            var _leftBoundary = containerTopLeft.X;
            var _rightBoundary = _leftBoundary + container.ActualWidth;

            //use midpoint of item (width or height divided by 2)
            var _itemLeft = itemTopLeft.X + (target.ActualWidth / 2);
            var _itemTop = itemTopLeft.Y + (target.ActualHeight / 2);

            if (_itemTop < _topBoundary || _bottomBoundary < _itemTop)
            {
                //Midpoint of target is outside of top or bottom
                return false;
            }

            if (_itemLeft < _leftBoundary || _rightBoundary < _itemLeft)
            {
                //Midpoint of target is outside of left or right
                return false;
            }

            return true;
        }

        //QRCode

        private void initializeQRCodeProperties()
        {
            //Initialize QR variables
            qrCodeTimer = new DispatcherTimer();
            qrCodeTimer.Tick += new EventHandler(timer_Tick);
            qrCodeTimer.Interval = new TimeSpan(0, 0, 1);
            hint = new Hashtable();
            hint.Add(DecodeHintType.POSSIBLE_FORMATS, BarcodeFormat.QR_CODE);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            detectQRCode();
        }

        private void detectQRCode()
        {
            Bitmap captureData = retrieveQRCodeImage();

            RGBLuminanceSource source = new RGBLuminanceSource(captureData, captureData.Width, captureData.Height);
            BinaryBitmap img = new BinaryBitmap(new GlobalHistogramBinarizer(source));
            Result result = null;
            try
            {
                //QR success
                result = qrCodeReader.decode(img, hint);
            }
            catch { }

            if (result == null)
            {
                //QR fail
            }
            else
            {
                //QR Success
                String decodedString = result.Text;
                String[] splitString = decodedString.Split(':');
                dcapiClient.login(splitString[0], splitString[1], "mobee");
                addCurrentProduct();
                doneDetectingCode = true;
                switchCanvas(storeCanvas);
            }
        }

        private Bitmap retrieveQRCodeImage()
        {
            RenderTargetBitmap canvasScreen = new RenderTargetBitmap((int)colorWindow.ActualWidth, (int)colorWindow.ActualHeight, 96d, 96d, PixelFormats.Default);
            canvasScreen.Render(colorWindow);
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(canvasScreen));

            MemoryStream stream = new MemoryStream();
            //FileStream fs = File.Open("screenshot.bmp", FileMode.OpenOrCreate);
            //encoder.Save(fs);
            //fs.Close();
            encoder.Save(stream);

            Bitmap captureData = new Bitmap(stream);

            //Kinect captures the mirrored image, so need to flip the QR Code image.
            captureData.RotateFlip(RotateFlipType.RotateNoneFlipX);

            return captureData;
        }
       
        //Store Code
        private void initializeStoreButtons()
        {
            cart_button.Click += new RoutedEventHandler(cart_button_hover);
            checkout_button.Click += new RoutedEventHandler(checkout_button_hover);
            left_arrow_button.Click += new RoutedEventHandler(left_arrow_button_hover);
            right_arrow_button.Click += new RoutedEventHandler(right_arrow_button_hover);
        }

        private void checkout_button_hover(object sender, RoutedEventArgs e)
        {
            cursorLeftImage.Visibility = System.Windows.Visibility.Hidden;
            colorWindow.Visibility = System.Windows.Visibility.Hidden;
            switchCanvas(confirmOrderCanvas);
        }

        private void cart_button_hover(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(dcapiClient.getAuthToken()))
            {
                switchCanvas(loginCanvas);
                doneDetectingCode = false;
                qrCodeTimer.Start();
            }
            else
            {
               addCurrentProduct();
            }
        }

        private void addCurrentProduct()
        {
            Product product = tshirts[shirtIndex];
            cartService.addToCart("mobee", product.itemUri, 1);
            // cartItemQty.Content = (int) (cartItemQty.Content) + 1;
            //String cartImageUri = "/OSHIrT;component/Images/" + cart + ".png";
            //cart_button.ImageSource = new BitmapImage(new Uri(cartImageUri, UriKind.RelativeOrAbsolute));
        }

        private void left_arrow_button_hover(object sender, RoutedEventArgs e)
        {
            shirtIndex--;
            if (shirtIndex < 0)
            {
                shirtIndex = tshirts.Count - 1;
            }
            loadImageFromCurrentIndex();
            switchCanvas(storeCanvas);
        }

        private void right_arrow_button_hover(object sender, RoutedEventArgs e)
        {
            shirtIndex++;
            if (shirtIndex >= tshirts.Count)
            {
                shirtIndex = 0;
            }
            loadImageFromCurrentIndex();
            switchCanvas(storeCanvas);
        }

        private void loadImageFromCurrentIndex()
        {
            Product tshirtProduct = tshirts[shirtIndex];
            String imageName = tshirtProduct.name.Replace(" ", String.Empty);
            ProductPrice productPrice = tshirtProduct.purchase_price;
            String price = productPrice.display;
            tshirtPriceLabel.Content = price;
            String tshirtImageUri = "/OSHIrT;component/Images/" + imageName + ".png";
            tshirtImage.Source = new BitmapImage(new Uri(tshirtImageUri, UriKind.RelativeOrAbsolute));
        }

        //Login Code

        //Checkout Code
        private void initializeCheckoutButtons()
        {
            confirm_order_button.Click += new RoutedEventHandler(confirm_order_button_hover);
            cancel_order_button.Click += new RoutedEventHandler(cancel_order_button_hover);

            checkoutBgWorker = new BackgroundWorker();
            checkoutBgWorker.WorkerReportsProgress = true;
            checkoutBgWorker.WorkerSupportsCancellation = true;
            checkoutBgWorker.DoWork += new DoWorkEventHandler(checkoutBgWorker_DoWork);
            checkoutBgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(checkoutBgWorker_WorkCompleted);
        }

        private void checkoutBgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Purchase purchase = checkoutService.createOrder("mobee");
            e.Result = purchase;
            
        }

        private void checkoutBgWorker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Purchase purchase = (Purchase) e.Result;
            purchaseNumberLabel.Content = "Purchase Number: " + purchase.purchaseNumber;
            subTotalLabel.Content = "Subtotal: " + purchase.total.display;
            taxLabel.Content = "Tax: " + purchase.tax.display;
            var total = purchase.total.amount + purchase.tax.amount;
            totalLabel.Content = "Total: $" + total.ToString();
            cursorImage.Visibility = System.Windows.Visibility.Visible;
            switchCanvas(orderReceiptCanvas);   
        }

        private void confirm_order_button_hover(object sender, RoutedEventArgs e)
        {
            switchCanvas(processingCanvas);
            cursorImage.Visibility = System.Windows.Visibility.Hidden;
            checkoutBgWorker.RunWorkerAsync();
        }

        private void cancel_order_button_hover(object sender, RoutedEventArgs e)
        {
            colorWindow.Visibility = System.Windows.Visibility.Visible;
            cursorLeftImage.Visibility = System.Windows.Visibility.Visible;
            switchCanvas(storeCanvas);
        }


        //Order Receipt Code
        private void initializeOrderReceiptButtons()
        {
            close_receipt_button.Click += new RoutedEventHandler(close_receipt_button_hover);
        }

        private void close_receipt_button_hover(object sender, RoutedEventArgs e)
        {
            colorWindow.Visibility = System.Windows.Visibility.Visible;
            cursorLeftImage.Visibility = System.Windows.Visibility.Visible;
            dcapiClient.setAuthToken(String.Empty);
            switchCanvas(storeCanvas);
        }
                
    }
}

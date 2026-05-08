using Emgu.CV;
using System;
using System.Windows.Forms;
using Emgu.CV.Structure;
using System.Drawing;
using System.Threading;
using System.Collections.Specialized;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.IO.Ports;
using System.Threading.Tasks;


namespace CS_HW_2_Emma_Harrison
{
    public partial class Form1 : Form
    {
        VideoCapture _capture;
        Thread _captureThread;
        SerialPort _serialPort;// = new SerialPort("COM4", 9600);

        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            //create the capture object and processing thread
            _capture = new VideoCapture(0); //0 -> device webcam 
            _captureThread = new Thread(DisplayWebcam);
            _serialPort = new SerialPort("COM20", 9600); //change com port as needed for ATmega Serial Port (determined using Device Manager)
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Open();

            _captureThread.Start();
        }

        private void DisplayWebcam()
        {
            int red_start = 0;
            bool already_moving = false;

            while (_capture.IsOpened)
            {
                //frame maintenance
                Mat frame = _capture.QueryFrame();

                //initialize variables for value counting

                //white pixel counting
                int wpcol1 = 0; //far left
                int wpcol2 = 0; //second from left
                int wpmid = 0; //third from left
                int wpcol4 = 0; //middle
                int wpcol5 = 0; //third from right
                int wpcol6 = 0; //second from right
                int wpcol7 = 0; //far right

                //count red pixels
                int red = 0;

                //count yellow pixels
                int yellowPixels = 0;
                int yellowPixelsMiddle = 0;

                int ypcol1 = 0;
                int ypcol2 = 0;
                int ypcol3 = 0;
                int ypcol4 = 0;
                int ypcol5 = 0;

                //flag for red stop logic
                bool flag = false;


                //resize to PictureBox aspect ratio
                int newHeight = (frame.Size.Height * pictureBox1.Size.Width) / frame.Size.Width;
                Size newSize = new Size(pictureBox1.Size.Width, newHeight);
                CvInvoke.Resize(frame, frame, newSize);

                //grayscaling and binary thresholding
                Mat grayscale = new Mat();
                CvInvoke.CvtColor(frame, grayscale, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                var mean = CvInvoke.Mean(grayscale);

                Mat binary_thresh = new Mat();
                CvInvoke.Threshold(grayscale, binary_thresh, 175, 255, Emgu.CV.CvEnum.ThresholdType.Binary);

                //hsv thresholding
                //red hsv mat object
                Mat hsv = new Mat();
                CvInvoke.CvtColor(frame, hsv, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);
                Image<Hsv, byte> redhsvImage = hsv.ToImage<Hsv, byte>();
                //175,255

                //yellow hsv mat object
                Mat hsvYellow = new Mat();
                CvInvoke.CvtColor(frame, hsvYellow, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);
                Image<Hsv, byte> yellowHSVimg = hsvYellow.ToImage<Hsv, byte>();

                //initialize images for binary thresholding columns
                Image<Gray, byte> img = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> img2 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col1 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col2 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col3 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col4 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col5 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col6 = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> col7 = binary_thresh.ToImage<Gray, byte>();

                //erode and dilate to reduce noise and reinforce lane lines
                img = img.Erode(3).Dilate(1);
                img2 = img2.Erode(3).Dilate(1);
                col1 = col1.Erode(3).Dilate(1);
                col2 = col2.Erode(3).Dilate(1);

                col3 = col3.Erode(3).Dilate(1);
                col4 = col4.Erode(3).Dilate(1);
                col5 = col5.Erode(3).Dilate(1);
                col6 = col6.Erode(3).Dilate(1);
                col7 = col7.Erode(3).Dilate(1);

                //count the white pixels in each column
                wpcol1 = img.CountNonzero()[0];
                wpcol2 = img2.CountNonzero()[0];
                wpmid = col3.CountNonzero()[0];
                wpcol4 = col4.CountNonzero()[0];
                wpcol5 = col5.CountNonzero()[0];
                wpcol6 = col6.CountNonzero()[0];
                wpcol7 = col7.CountNonzero()[0];

                //tune red HSV filter
                Hsv redlowerLimit = new Hsv(162, 76, 102);//325, 30, 40);
                Hsv redupperLimit = new Hsv(179, 178, 166);//360, 70, 65);
                //345.5, 51, 47

                //establish region of interest and pixel couting for red HSV mask
                redhsvImage.ROI = new Rectangle(0, 0, frame.Width, frame.Height);
                Image<Gray, byte> redmask = redhsvImage.InRange(redlowerLimit, redupperLimit);
                redhsvImage.ROI = new Rectangle(0, frame.Height - frame.Height / 2, frame.Width, frame.Height);
                Image<Gray, byte> countRed = redhsvImage.InRange(new Hsv(0, 150, 100), new Hsv(25, 255, 255));
                red = redmask.CountNonzero()[0];
                CvInvoke.PutText(frame, $"Red: {red}", new Point(10, 70), FontFace.HersheySimplex, 1.2, new MCvScalar(255, 0, 255), 2);

                //tune yellow HSV filter
                Hsv lowerLimit = new Hsv(24, 43, 122);
                Hsv upperLimit = new Hsv(30, 255, 255);

                //establish yellow hsv masks
                Image<Gray, byte> mask = yellowHSVimg.InRange(lowerLimit, upperLimit);
                yellowPixelsMiddle = mask.CountNonzero()[0];

                Image<Gray, byte> ycol1 = yellowHSVimg.InRange(lowerLimit, upperLimit);
                Image<Gray, byte> ycol2 = yellowHSVimg.InRange(lowerLimit, upperLimit);
                Image<Gray, byte> ycol3 = yellowHSVimg.InRange(lowerLimit, upperLimit);
                Image<Gray, byte> ycol4 = yellowHSVimg.InRange(lowerLimit, upperLimit);
                Image<Gray, byte> ycol5 = yellowHSVimg.InRange(lowerLimit, upperLimit);

                //establish yellow ROIs for pixel counting
                ycol1.ROI = new Rectangle(0, frame.Height - frame.Height / 2, frame.Width / 5, frame.Height);
                ycol2.ROI = new Rectangle((frame.Width) / 5, frame.Height - frame.Height / 2, frame.Width / 5, frame.Height);
                ycol3.ROI = new Rectangle(2 * (frame.Width) / 5, frame.Height - frame.Height / 2, frame.Width / 5, frame.Height);
                ycol4.ROI = new Rectangle(3 * (frame.Width) / 5, frame.Height - frame.Height / 2, frame.Width / 5, frame.Height);
                ycol5.ROI = new Rectangle(4 * (frame.Width) / 5, frame.Height - frame.Height / 2, frame.Width / 5, frame.Height);
                
                //count yellow pixels in whole image
                yellowHSVimg.ROI = new Rectangle(0, 0, frame.Width, frame.Height);
                yellowPixels = yellowHSVimg.CountNonzero()[0];

                //count yellow pixels in each ROI
                ypcol1 = ycol1.CountNonzero()[0];
                ypcol2 = ycol2.CountNonzero()[0];
                ypcol3 = ycol3.CountNonzero()[0];
                ypcol4 = ycol4.CountNonzero()[0];
                ypcol5 = ycol5.CountNonzero()[0];

                //draw yellow ROIs on camera input
                CvInvoke.Rectangle(frame, ycol1.ROI, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(frame, ycol2.ROI, new MCvScalar(0, 255, 0), 2);
                CvInvoke.Rectangle(frame, ycol3.ROI, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(frame, ycol4.ROI, new MCvScalar(0, 255, 0), 2);
                CvInvoke.Rectangle(frame, ycol5.ROI, new MCvScalar(0, 0, 255), 2);

                //label each column for easy debugging
                CvInvoke.PutText(frame, "col1", new Point(0, frame.Height - frame.Height / 2), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);
                CvInvoke.PutText(frame, "col2", new Point((frame.Width) / 5, frame.Height - frame.Height / 2), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);
                CvInvoke.PutText(frame, "col3", new Point(2 * (frame.Width) / 5, frame.Height - frame.Height / 2), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);
                CvInvoke.PutText(frame, "col4", new Point(3 * (frame.Width) / 5, frame.Height - frame.Height / 2), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);
                CvInvoke.PutText(frame, "col5", new Point(4 * (frame.Width) / 5, frame.Height - frame.Height / 2), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);

                //draw ROI rectangles on white input (if using white line logic - not implemented here
                CvInvoke.Rectangle(mask, img.ROI, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(mask, img2.ROI, new MCvScalar(0, 255, 0), 2);
                CvInvoke.Rectangle(mask, col3.ROI, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(mask, col4.ROI, new MCvScalar(0, 255, 0), 2);
                CvInvoke.Rectangle(mask, col5.ROI, new MCvScalar(0, 0, 255), 2);

                //draw red mask ROI on image frame
                CvInvoke.Rectangle(frame, redhsvImage.ROI, new MCvScalar(255, 0, 255), 2);
                int decision = 0;

                if (red > 100000) //if there is a prominent red line in the image
                {
                    flag = true; //mark you have seen the red line
                    decision = 0; //send stop command to atmega
                }
                else
                {
                    //decision
                    // 0 -> STOP
                    // 1 -> FORWARD
                    // 2 -> TURN LEFT
                    // 3 -> TURN RIGHT
                    // 4 -> HARD RIGHT
                    // 5 -> HARD LEFT 
                    // | col5 | col4 | col3 | col2 | col1 |
                    if (yellowPixels < 500)
                    {
                        //decision = 0; //STOP! No line! //did not use logic, would cause robot to unexpectedly stop
                    }
                    else if (ypcol3 > ypcol4 && ypcol3 > ypcol2) //if there are more yellow pixels in the middle than either of the neighboring columns
                    {
                        decision = 1; //FORWARD
                    }
                    else if (ypcol4 > 150) //otherwise if there are notaable yellow pixels in the 4th column (left of center)
                    {
                        decision = 3; //turn a bit right
                    }
                    else if (ypcol2 > 150) //otherwise if there are notable yellow pixels in the 2nd column (right of center)
                    {
                        decision = 2; //turn a bit left
                    }
                    else if (ypcol1 > 150) //otherwise if there are notable yellow pixels in the first column (far right of center)
                    {
                        decision = 5; //turn hard left
                    }
                    else if (ypcol5 > 150) //otherwise if there are notable yellow pixels in the last column (far left of center)
                    {
                        decision = 4; //turn hard right
                    }
                    else
                    {
                        //decision = 8; //ignore default case -> caused errors in logic
                    }

                }

                if (flag) //if you have seen the red line already
                {
                    decision = 0; //e-stop - must restart to continue
                }
                _serialPort.Write(decision.ToString()); //write decision string to serial port
                Thread.Sleep(50); //wait before completing next decision
                
                //format display of decision value
                CvInvoke.PutText(frame, decision.ToString(), new Point(frame.Width / 2 - 80, 100), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);

                //display the image in the PictureBox
                Bitmap bmp = frame.ToBitmap(); //frame bitmap
                Bitmap binaryBmp = binary_thresh.ToBitmap();  //binary thresholded bitmap
                Bitmap yellowMask = mask.ToBitmap(); //yellow hsv mask bitmap
                Bitmap redMask = redmask.ToBitmap();  //red hsv bitmap
                Mat combinedMask = new Mat(); //create new mat object for combined mask
                CvInvoke.BitwiseOr(mask, redmask, combinedMask); //combine yellow and red masks into one mask for display purposes
                Bitmap comboMask = combinedMask.ToBitmap(); //create combined mask bitmap
                pictureBox2.Invoke(new Action(() =>
                {
                    pictureBox2.Image = bmp; //display frame bitmap in windows form
                }));

                pictureBox3.Invoke(new Action(() =>
                {
                    pictureBox3.Image = comboMask;//binaryBmp; //display combined bitmap in windows form
                }));
                
                //cleanup ROIs
                img.ROI = Rectangle.Empty;
                img2.ROI = Rectangle.Empty;
                col3.ROI = Rectangle.Empty;
                col4.ROI = Rectangle.Empty;
                col5.ROI = Rectangle.Empty;

                ycol1.ROI = Rectangle.Empty;
                ycol2.ROI = Rectangle.Empty;
                ycol3.ROI = Rectangle.Empty;
                ycol4.ROI = Rectangle.Empty;
                ycol5.ROI = Rectangle.Empty;

            }
            _serialPort.Close(); //clean up serial port
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //terminate the image processing thread to avoid orphaned processes
            //_captureThread.Abort();
            if (_captureThread != null && _captureThread.IsAlive)
            {
                _captureThread.Join();
            }

            _capture?.Dispose();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e) //handle exceptions from serial communication debugging
        {
            try
            {
                string data = _serialPort.ReadExisting();
                Console.WriteLine("MCU: " + data);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serial read error: " + ex.Message);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        /*private void Form1_Load_1(object sender, EventArgs e)
        {

        }*/
    }
}

using Emgu.CV;
using System;
using System.Windows.Forms;
using Emgu.CV.Structure;
using System.Drawing;
using System.Threading;
using System.Collections.Specialized;
using Emgu.CV.CvEnum;
using System.IO.Ports;
//using System.IO.Ports.SerialPort;


namespace CS_HW_2_Emma_Harrison
{
    public partial class Form1 : Form
    {
        VideoCapture _capture;
        Thread _captureThread;
        SerialPort _serialPort;// = new SerialPort("COM4", 9600);

        //private object meanLbl;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            //create the capture object and processing thread
            _capture = new VideoCapture(0); //0 -> device webcam 
            _captureThread = new Thread(DisplayWebcam);
            _captureThread.Start();
            _serialPort = new SerialPort("COM20", 9600);
            
            //_serialPort.DataBits = 8;
            //_serialPort.Parity = Parity.None;
            //_serialPort.StopBits = StopBits.One;
            //_serialPort.Open();
        }

        private void DisplayWebcam()
        {
            int red_start = 0;
            bool already_moving = false;
            _serialPort.DataBits = 8;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Open();
            
            while (_capture.IsOpened)
            {
                //frame maintenance
                Mat frame = _capture.QueryFrame();
                int whitePixelsLeftQuarter = 0;
                int whitePixelsRightQuarter = 0;
                int redPixelsBottomThird = 0;

                int lMotorValue = 0;
                int rMotorValue = 0;

                //_serialPort = new SerialPort();
                //SerialPort _serialPort = new SerialPort("COM4", 9600);
                //_serialPort.DataBits = 8;
                //_serialPort.Parity = Parity.None;
                //_serialPort.StopBits = StopBits.One;
                //_serialPort.Open();

                //resize to PictureBox aspect ratio
                int newHeight = (frame.Size.Height * pictureBox1.Size.Width) / frame.Size.Width;
                Size newSize = new Size(pictureBox1.Size.Width, newHeight);
                CvInvoke.Resize(frame, frame, newSize);
               
                //grayscaling and binary thresholding
                Mat grayscale = new Mat();
                CvInvoke.CvtColor(frame, grayscale, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                var mean = CvInvoke.Mean(grayscale);
                //Invoke(new Action(() =>
                //{
                 //   meanLbl.Text = $"Mean: {mean.V0}";
                //}));

                Mat binary_thresh = new Mat();
                CvInvoke.Threshold(grayscale, binary_thresh, 150, 255, Emgu.CV.CvEnum.ThresholdType.Binary);

                Mat hsv = new Mat();
                CvInvoke.CvtColor(frame, hsv, Emgu.CV.CvEnum.ColorConversion.Bgr2Hsv);
                Image<Hsv, byte> hsvImage = hsv.ToImage<Hsv, byte>();
                //175,255

                Image<Gray, byte> img = binary_thresh.ToImage<Gray, byte>();
                Image<Gray, byte> img2 = binary_thresh.ToImage<Gray, byte>();
                img = img.Erode(2).Dilate(1);
                img2 = img2.Erode(2).Dilate(1);

                img.ROI = new Rectangle(0, frame.Height - frame.Height / 4, frame.Width / 2, frame.Height / 4);

                whitePixelsLeftQuarter = img.CountNonzero()[0];
                Console.WriteLine($"Left ROI: {whitePixelsLeftQuarter}");
                CvInvoke.PutText(frame, $"L: {whitePixelsLeftQuarter}", new Point(10, 30), FontFace.HersheySimplex, 1.2, new MCvScalar(0, 0, 255), 2);
                // img.ROI = Rectangle.Empty;

                img2.ROI = new Rectangle(frame.Width / 2, frame.Height - frame.Height / 4, frame.Width / 2, frame.Height / 4);
                whitePixelsRightQuarter = img2.CountNonzero()[0];
                Console.WriteLine($"Right ROI: {whitePixelsRightQuarter}");
                CvInvoke.PutText(frame, $"R: {whitePixelsRightQuarter}", new Point(frame.Width / 2 + 10, 30), FontFace.HersheySimplex, 1.2, new MCvScalar(0, 255, 0), 2);
                // img2.ROI = Rectangle.Empty;

                hsvImage.ROI = new Rectangle(0, frame.Height - frame.Height / 3, frame.Width, frame.Height / 3);
                redPixelsBottomThird = hsvImage.InRange(new Hsv(0, 150, 100), new Hsv(25, 255, 255)).CountNonzero()[0];
                CvInvoke.PutText(frame, $"Red: {redPixelsBottomThird}", new Point(10, 70),FontFace.HersheySimplex, 1.2, new MCvScalar(255, 0, 255), 2);

                CvInvoke.Rectangle(frame, img.ROI, new MCvScalar(0, 0, 255), 2);
                CvInvoke.Rectangle(frame, img2.ROI, new MCvScalar(0, 255, 0), 2);
                CvInvoke.Rectangle(frame, hsvImage.ROI, new MCvScalar(255,0, 255), 2);
                int decision;

                if (redPixelsBottomThird > 10000)
                {
                    if (red_start < 45)
                    {
                        decision = 1;
                            //"START";
                        red_start++;
                        
                    }
                    else
                    {
                        decision = 0;
                            //"STOP";
                        if(already_moving)
                        {
                            decision = 0; //STOP, you reached the end!
                            CvInvoke.PutText(frame, "STOP", new Point(frame.Width / 2, frame.Height / 2), FontFace.HersheySimplex, 25, new MCvScalar(0, 0, 255), 3);
                        }
                    }

                    
                }
                else if (whitePixelsLeftQuarter > whitePixelsRightQuarter + 500)
                {
                    decision = 1;//3;
                    already_moving = true;
                        //"TURN LEFT";
                }
                else if (whitePixelsRightQuarter > whitePixelsLeftQuarter + 500)
                {
                    decision = 1;//2;
                        //"TURN RIGHT";
                }
                else
                {
                    decision = 1;
                        //"FORWARD";
                }
                //update
                //0 -> STOP
                //1 -> FORWARD
                //2 -> TURN RIGHT
                //3 -> TURN LEFT
                //string decisionStr;
                switch(decision)
                {
                    case 0:
                        //STOP equal l and r
                        lMotorValue = 0;
                        rMotorValue = 0;
                        break;
                    case 1:
                        //supply full to l and r
                        lMotorValue = 1024;
                        rMotorValue = 1024;
                        break;
                    case 2:
                        //Supply less to right motor, more to left
                        lMotorValue = 1024;
                        rMotorValue = 750;
                        break;
                    case 3:
                        //supply less to left motor, more to right
                        lMotorValue = 750;
                        rMotorValue = 1024;
                        break;
                    default:
                        lMotorValue = 0;
                        rMotorValue = 0;
                        break;

                }
                string decisionStr;
                decisionStr = $"L: {lMotorValue}, R: {rMotorValue}";
                _serialPort.Write(decision.ToString());

                CvInvoke.PutText(frame, decision.ToString(), new Point(frame.Width / 2 - 80, 100), FontFace.HersheySimplex, 4, new MCvScalar(255, 0, 0), 3);
                //CvInvoke.PutText(frame, lMotorValue.ToString(), new Point(frame.Width / 3 - 80, 100), FontFace.HersheySimplex, 1.5, new MCvScalar(255, 0, 0), 3);
                //display the image in the PictureBox
                Bitmap bmp = frame.ToBitmap();
                Bitmap binaryBmp = binary_thresh.ToBitmap();
                pictureBox2.Invoke(new Action(() =>
                {
                    pictureBox2.Image = bmp;
                }));

                pictureBox3.Invoke(new Action(() =>
                {
                    pictureBox3.Image = binaryBmp;
                }));
                // pictureBox1.Image = frame.ToBitmap();
                img.ROI = Rectangle.Empty;
                img2.ROI = Rectangle.Empty;
                //grayscaling and binary thresholding
                //CvInvoke.Threshold(frame, frame, mean.V0, 255,
                  //  Emgu.CV.CvEnum.ThresholdType.Binary);



            }
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

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        /*private void Form1_Load_1(object sender, EventArgs e)
        {

        }*/
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

//// //// //// //// //// //// ////  특징점 추출 라이브러리  //// //// //// //// //// //// //// //// //// 
using OpenCvSharp;
using OpenCvSharp.Features2D;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Diagnostics;
//// //// //// //// //// //// ////  특징점 추출 라이브러리  //// //// //// //// //// //// //// //// //// 
namespace LiveCAM
{

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        SerialPort sp = new SerialPort();
        TCPIP.TCPIPConfiguration TcpCfg = new TCPIP.TCPIPConfiguration();
        UDP.UDPConfiguration UdpCfg = new UDP.UDPConfiguration();
        SerialComport.SerialPortConfiguration SerialCfg;
        Tasks.Scheduler TaskRecvSocket;
        SomeModel sm = new SomeModel();
        Utility.LogManager log = new Utility.LogManager();

        ///  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###
        // imageNumber 초기화
        int imageNumber = 0;
        // 파일 크기 제한 (30KB)
        const int fileSizeLimitBytes = 20 * 1024; // 20KB
        const int fileSizeLimitBytes_d = 200 * 1024; // 200KB
        ///  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###  ###

        bool IsRecvSocket = true;
        bool IsGrayColor = true;
        float BatteryVoltage = 0.0f;
        public MainWindow()
        {
            InitializeComponent();

            //TaskRecvSocket = new Tasks.Scheduler(RecvSocket1);
            //TaskRecvSocket = new Tasks.Scheduler(RecvUDP);


            SerialCfg = new SerialComport.SerialPortConfiguration(sp);
            //ComportReceiver = new SerialComport.Receiver(sp);

            sp.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

            for (int i = 0; i < 64; i++) cbo_Quality.Items.Add(i.ToString());
            cbo_Quality.SelectedIndex = 10;

            cbo_SpecialEffect.Items.Add("No Effect");
            cbo_SpecialEffect.Items.Add("Negative");
            cbo_SpecialEffect.Items.Add("Grayscale");
            cbo_SpecialEffect.Items.Add("Red Tint");
            cbo_SpecialEffect.Items.Add("Green Tint");
            cbo_SpecialEffect.Items.Add("Blue Tint");
            cbo_SpecialEffect.Items.Add("Sepia");
            cbo_SpecialEffect.SelectedIndex = 2;

            cbo_framesize.Items.Add("SIZE_96x96");
            cbo_framesize.Items.Add("SIZE_QQVGA_160x120");
            cbo_framesize.Items.Add("SIZE_QCIF_176x144");
            cbo_framesize.Items.Add("SIZE_HQVGA_240x176");
            cbo_framesize.Items.Add("SIZE_240X240");
            cbo_framesize.Items.Add("SIZE_QVGA_320x240");
            cbo_framesize.Items.Add("SIZE_CIF_400x296");
            cbo_framesize.Items.Add("SIZE_HVGA_480x320");
            cbo_framesize.Items.Add("SIZE_VGA_640x480");
            cbo_framesize.Items.Add("SIZE_SVGA_800x600");
            cbo_framesize.Items.Add("SIZE_XGA_1024x768");


            cbo_framesize.Items.Add("SIZE_HD_1280x720");
            cbo_framesize.Items.Add("SIZE_SXGA_1280x1024");
            cbo_framesize.Items.Add("SIZE_UXGA_1600x1200");
            cbo_framesize.Items.Add("SIZE_FHD_1920x1080");

            //cbo_framesize.Items.Add("FRAMESIZE_P_HD");
            //cbo_framesize.Items.Add("FRAMESIZE_P_3MP");
            //cbo_framesize.Items.Add("FRAMESIZE_QXGA");
            //cbo_framesize.Items.Add("FRAMESIZE_QHD");
            //cbo_framesize.Items.Add("FRAMESIZE_WQXGA");
            //cbo_framesize.Items.Add("FRAMESIZE_P_FHD");
            //cbo_framesize.Items.Add("FRAMESIZE_QSXGA");
            //cbo_framesize.Items.Add("FRAMESIZE_INVALID");

            for (int i = 0; i <= 30; i++)
            {
                cbo_CamDelayTime.Items.Add(string.Format("{0} ms", i * 100));
            }

            sm.img_sp = "image/Red ball.png";
            sm.img_tcp = "image/Red ball.png";

            this.DataContext = sm;
        }



        private int ResponseState = 0;
        private int ResponseCount = 0;
        private byte[] ResponseBuffer = new byte[1024];
        private void RecvResponse(byte[] buff)
        {
            try
            {
                for (int i = 0; i < buff.Length; i++)
                {
                    ResponseBuffer[ResponseCount] = buff[i];
                    ResponseCount++;

                    switch (ResponseState)
                    {
                        case 0:
                            ResponseState = (buff[i] == '\r') ? 1 : 0;
                            break;

                        case 1:
                            if (buff[i] == '\n')
                            {
                                byte[] temp = new byte[ResponseCount];
                                Buffer.BlockCopy(ResponseBuffer, 0, temp, 0, ResponseCount);
                                string[] str = Encoding.UTF8.GetString(temp).Split(new string[] { " ", ",", "\r\n" }, StringSplitOptions.RemoveEmptyEntries); ;

                                //for ( int j = 0; j < str.Length; j++ ) Console.Write(string.Format("{0} ", str[j]));
                                //Console.Write("\r\n");

                                if (str.Length >= 4)
                                {
                                    if (str[0] == "GET")
                                    {
                                        if (str[1] == "WIFI")
                                        {
                                            switch (str[2])
                                            {
                                                case "SSID": sm.SSID = str[3]; break;
                                                case "PW": sm.PW = str[3]; break;
                                                case "REMOTEIP": sm.RemoteIP = str[3]; break;
                                                case "REMOTEPORT": sm.RemotePort = Convert.ToInt16(str[3]); break;
                                                case "LOCALPORT": sm.LocalPort = Convert.ToInt16(str[3]); break;
                                            }
                                        }
                                        else if (str[1] == "CAM")
                                        {
                                            switch (str[2])
                                            {
                                                case "FRAMESIZE": sm.Framesize = Convert.ToInt16(str[3]); break;
                                                case "BRIGHTNESS": sm.Brightness = Convert.ToInt16(str[3]); break;
                                                case "CONTRAST": sm.Contrast = Convert.ToInt16(str[3]); break;
                                                case "SATURATION": sm.Saturation = Convert.ToInt16(str[3]); break;
                                                case "QUALITY": sm.JpegQuality = Convert.ToInt16(str[3]); break;
                                                case "VFLIP": sm.VFlip = Convert.ToInt16(str[3]); sm.strVFlip = (sm.VFlip == 1) ? "Enable" : "Disable"; break;
                                                case "HMIRROR": sm.HMirror = Convert.ToInt16(str[3]); sm.strHMirror = (sm.HMirror == 1) ? "Enable" : "Disable"; break;
                                                case "EFFECT": sm.SpecialEffect = Convert.ToInt16(str[3]); break;
                                                case "DELAYTIME": sm.CamDelayTime = Convert.ToInt16(str[3]); break;
                                                case "ONOFF": sm.IsCamOnOff = (Convert.ToInt16(str[3]) == 0) ? false : true; break;
                                            }
                                        }
                                        else if (str[1] == "BATTERY")
                                        {
                                            switch (str[2])
                                            {
                                                case "VOLTAGE": sm.BatteryVoltage = (Convert.ToSingle(str[3])); break;
                                            }
                                        }
                                    }
                                }


                                ResponseCount = 0;
                            }
                            ResponseState = 0;
                            break;
                    }

                    if (ResponseCount >= ResponseBuffer.Length) ResponseCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private byte[] StreamBuffer = new byte[1920 * 1080];
        private int state = 0;
        private int RecvCnt = 0;
        private byte IsEOI = 0; // End of image
        static WriteableBitmap wb;

        private void ShowCAM(byte[] buff)
        {
#if false
            int[] size = new int[] {
                        96*96,
                        160*120,
                        176*144,
                        240*176,
                        240*240,
                        320*240,
                        400*296,
                        480*320,
                        640*480,
                        800*600,
                        1024*768,
                        1280*720,
                        1280*1024,
                        1600*1200,            
                        1920*1080
                        };
            int[] W = new[] { 96,
                        160,
                        176,
                        240,
                        240,
                        320,
                        400,
                        480,
                        640,
                        800,
                        1024,
                        1280,
                        1280,
                        1600,
                        1920};
            int[] H = new[] {
                96,
                        120,
                        144,
                        176,
                        240,
                        240,
                        296,
                        320,
                        480,
                        600,
                        768,
                        720,
                        1024,
                        1200,
                        1080 };

            RecvResponse(buff);

                        Buffer.BlockCopy(buff, 0, StreamBuffer, count, buff.Length);
                        count = count + buff.Length;

            //if (count >= size[sm.Framesize])
            if ( count >= 307200)
                        {
                Console.WriteLine("Count : " + count);
                byte[] temp = new byte[307200];

                            Buffer.BlockCopy(StreamBuffer, 0, temp, 0, 307200);

                                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    try
                                    {


                                        var width = W[sm.Framesize];
                                        var height = H[sm.Framesize];
                                        var dpiY = 96d;//W[sm.Framesize];
                                        var dpiX = 96d;//H[sm.Framesize];
                                        var pixelFormat = PixelFormats.Gray8;
                                        var bytesPerPixel = (pixelFormat.BitsPerPixel + 7) / 8;// pixelFormat.BitsPerPixel / 8;//(pixelFormat.BitsPerPixel + 7) / 8; // 1 == in this example
                                        var stride = bytesPerPixel * width;
                                        var bitmap = BitmapSource.Create(width, height, dpiX, dpiY, pixelFormat, null, temp, stride);

                                        image.Source = bitmap;
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                    }
                                }));

                            count = 0;
                        }

#else

            RecvResponse(buff);

            for (int i = 0; i < buff.Length; i++)
            {
                StreamBuffer[RecvCnt] = buff[i];
                //++RecvCnt;
                switch (state)
                {
                    case 0:
                        //state = (buff[i] == 0xFF) ? 1 : 0;

                        if (RecvCnt == 0 && buff[i] == 0xFF)
                        {
                            ++RecvCnt;
                        }
                        else if (RecvCnt == 1 && buff[i] == 0xD8)
                        {
                            ++RecvCnt;
                            state = 1;
                        }
                        else
                        {
                            RecvCnt = 0;
                        }
                        break;



                    case 1:
                        ++RecvCnt;
                        state = (buff[i] == 0xFF) ? 2 : 1;
                        break;

                    case 2:
                        ++RecvCnt;
                        if (buff[i] == 0xD9)
                        {
                            byte[] temp = new byte[RecvCnt];
                            Buffer.BlockCopy(StreamBuffer, 0, temp, 0, RecvCnt);
                            Mat convMat;
                            Mat convMat2 = new Mat();

                            //if (IsGrayColor == true)
                            //{
                            //    convMat = Mat.FromImageData(temp, ImreadModes.Grayscale);
                            //    convMat = Cv2.ImDecode(temp, ImreadModes.Grayscale);
                            //    //convMat = Mat.FromImageData(temp, ImreadModes.Color);
                            //    //convMat = Cv2.ImDecode(temp, ImreadModes.Color);

                            //    //Cv2.CvtColor(convMat, convMat2, ColorConversionCodes.BGR2GRAY);
                            //}
                            //else
                            //{
                            convMat = Mat.FromImageData(temp, ImreadModes.Color);

                            //Cv2.CvtColor(convMat, convMat2, ColorConversionCodes.BGR2GRAY);
                            //}


                            if (convMat.Width != 0 && convMat.Height != 0)
                            {
                                //string txtFileName = $"./keypoint/key/{imageNumber}_k.txt";
                                string filename = $"./images/{imageNumber}.jpg";
                                //string demo_filename = "./images/" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".jpg";

                                //이미지 저장
                                convMat.SaveImage(filename);

                                // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## 특징 추출 ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## 특징 추출 ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                //// ORB or SIFT 디스크립터 추출기 초기화
                                //using (ORB orb = ORB.Create())
                                //{
                                //    // using (SIFT sift = SIFT.Create()){

                                //    Point[] keypoints;
                                //    using (Mat grayFrame = new Mat())
                                //    {
                                //        Cv2.CvtColor(convMat, grayFrame, ColorConversionCodes.BGR2GRAY);

                                //        // ORB
                                //        keypoints = orb.Detect(grayFrame);

                                //        // SIFT
                                //        // sift.DetectAndCompute(grayFrame, null, out keypoints, null);
                                //    }

                                //    // 텍스트 파일에 특징점 데이터 저장 +
                                //    if (keypoints.Length > 0)
                                //    {
                                //        //string imageName = $"image_{imageNumber}.jpg";
                                //        string txtFileName = "./keypoint/" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".txt";

                                //        using (StreamWriter sw = new StreamWriter(txtFileName, true))
                                //        {
                                //            foreach (KeyPoint keypoint in keypoints)
                                //            {
                                //                string data = $"{imageNumber}, {keypoint.Pt.X}, {keypoint.Pt.Y}, {keypoint.Size}, {keypoint.Angle}, {keypoint.Response}, {keypoint.Octave}";
                                //                sw.WriteLine(data);
                                //            }
                                //        }
                                //        // 이미지 번호 증가
                                //        imageNumber++;
                                //    }
                                //}
                                // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## 특징 추출 ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## 특징 추출 ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 


                                // @@  
                                // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## 특징 추출 ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## 특징 추출 ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                // ORB or SIFT 디스크립터 추출기 초기화
                                using (ORB orb = ORB.Create(500))
                                {
                                    // using (SIFT sift = SIFT.Create()){

                                    KeyPoint[] keypoints;
                                    using (Mat descriptors = new Mat())
                                    {
                                        using (Mat grayFrame = new Mat())
                                        {
                                            Cv2.CvtColor(convMat, grayFrame, ColorConversionCodes.BGR2GRAY);

                                            // ORB
                                            //keypoints, descriptors = orb.DetectAndCompute(grayFrame, null); //orb.Detect(grayFrame);

                                            //orb.DetectAndCompute(grayFrame, null, out keypoints, descriptors); //orb.Detect(grayFrame);
                                            keypoints = orb.Detect(grayFrame);
                                            orb.Compute(grayFrame, ref keypoints, descriptors);

                                            //// feature matching test part
                                            //if (imageNumber == 60) {
                                            //    string image40 = $"./images/40.jpg";
                                            //    string image60 = $"./images/{imageNumber}.jpg";

                                            //    // iamge 40
                                            //    Mat imageMat = new Mat(image40);
                                            //    Mat gray_40 = new Mat();
                                            //    Cv2.CvtColor(imageMat, gray_40, ColorConversionCodes.BGR2GRAY);
                                            //    KeyPoint[] keyPoints40;

                                            //    // iamge 60
                                            //    Mat imageMat60 = new Mat(image60);
                                            //    Mat gray_60 = new Mat();
                                            //    Cv2.CvtColor(imageMat60, gray_60, ColorConversionCodes.BGR2GRAY);


                                            //    Mat descriptors40 = new Mat();
                                            //    //orb.DetectAndCompute(gray_40, null, ref keyPoints40, descriptors40);

                                            //    keyPoints40 = orb.Detect(gray_40);
                                            //    orb.Compute(gray_40, ref keyPoints40, descriptors40);

                                            //    BFMatcher matcher = new BFMatcher();
                                            //    DMatch[] matches = matcher.Match(descriptors40, descriptors);

                                            //    // draw the matching result
                                            //    Mat resultImage = new Mat();
                                            //    Cv2.DrawMatches(imageMat, keyPoints40, imageMat60, keypoints, matches, resultImage);

                                            //    string result_Image = "./result/matching_image_4060.png";
                                            //    Cv2.ImWrite(result_Image, resultImage);
                                            //    Console.WriteLine("40-60 matches : "+ matches.Length);
                                            //}

                                            //// feature matching test part
                                            //if (imageNumber == 80)
                                            //{
                                            //    string image60 = $"./images/60.jpg";
                                            //    string image80 = $"./images/{imageNumber}.jpg";

                                            //    // iamge 60
                                            //    Mat imageMat = new Mat(image60);
                                            //    Mat gray_60 = new Mat();
                                            //    Cv2.CvtColor(imageMat, gray_60, ColorConversionCodes.BGR2GRAY);
                                            //    KeyPoint[] keyPoints60;

                                            //    // iamge 80
                                            //    Mat imageMat80 = new Mat(image80);
                                            //    Mat gray_80 = new Mat();
                                            //    Cv2.CvtColor(imageMat80, gray_80, ColorConversionCodes.BGR2GRAY);


                                            //    Mat descriptors60 = new Mat();
                                            //    //orb.DetectAndCompute(gray_40, null, ref keyPoints40, descriptors40);

                                            //    keyPoints60 = orb.Detect(gray_60);
                                            //    orb.Compute(gray_60, ref keyPoints60, descriptors60);

                                            //    BFMatcher matcher = new BFMatcher();
                                            //    DMatch[] matches = matcher.Match(descriptors60, descriptors);

                                            //    // draw the matching result
                                            //    Mat resultImage = new Mat();
                                            //    Cv2.DrawMatches(imageMat, keyPoints60, imageMat80, keypoints, matches, resultImage);

                                            //    string result_Image = "./result/matching_image_6080.png";
                                            //    Cv2.ImWrite(result_Image, resultImage);
                                            //    Console.WriteLine("60-80 matches : " + matches.Length);
                                            //}


                                            // SIFT
                                            // sift.DetectAndCompute(grayFrame, null, out keypoints, null);
                                        }


                                        // 텍스트 파일에 특징점 데이터 저장
                                        if (keypoints.Length > 0)
                                        {
                                            //int txt_door = 0;
                                            //int txt_door_d = 0;
                                            // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## [[[ 특징점 정보만 저장 ]]] ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                            //string imageName = $"image_{imageNumber}.jpg";
                                            // string txtFileName =  $"./keypoint/{imageNumber}_keypoints.txt"; //"keypoints.txt";

                                            // using (StreamWriter sw = new StreamWriter(txtFileName, true))
                                            // {
                                            //     foreach (KeyPoint keypoint in keypoints)
                                            //     {
                                            //         // $"ImageNumber: {imageNumber}, X: {keypoints[i].Pt.X}, Y: {keypoints[i].Pt.Y}, Size: {keypoints[i].Size}, Angle: {keypoints[i].Angle}, Response: {keypoints[i].Response}, Octave: {keypoints[i].Octave}";
                                            //         string data = $"{imageNumber}, {keypoint.Pt.X}, {keypoint.Pt.Y}, {keypoint.Size}, {keypoint.Angle}, {keypoint.Response}, {keypoint.Octave}";
                                            //         sw.WriteLine(data);
                                            //     }
                                            // }

                                            // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## [[[ 특징점 정보만 저장 ]]] ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 


                                            // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## <<< 디스크립터 정보 + 특징점 정보 >>> ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                            string txtFileName = $"./keypoint/key/{imageNumber}_k.txt";
                                            string txtFileName_d = $"./keypoint/des/{imageNumber}_d.txt";

                                            using (StreamWriter sw = new StreamWriter(txtFileName))
                                            {
                                                for (int a = 0; a < keypoints.Length; a++)
                                                {
                                                    // 특징점 정보
                                                    string keypointInfo = $"{keypoints[a].Pt.X},{keypoints[a].Pt.Y},{keypoints[a].Size},{keypoints[a].Angle},{keypoints[a].Response},{keypoints[a].Octave}";
                                                    sw.WriteLine(keypointInfo);

                                                }
                                                //// 디스크립터 정보
                                                using (StreamWriter sq = new StreamWriter(txtFileName_d))
                                                {
                                                    //for (int aa = 0; aa < descriptors.Rows; aa++){
                                                    //string descriptorLine = string.Join(", ", descriptors.Get<float>(aa, 0, descriptors.Cols));
                                                    //    //sq.WriteLine(descriptorLine);
                                                    //File.WriteAllText(txtFileName_d, descriptorLine);
                                                    //}
                                                    int cols = descriptors.Cols;
                                                    for (int aa = 0; aa < descriptors.Rows; aa++)
                                                    {
                                                        // 새로운 방식
                                                        byte[] des = new byte[cols];
                                                        Marshal.Copy(descriptors.Data + aa * cols, des, 0, cols);
                                                        string binaryDes = string.Join("", des.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
                                                        sq.WriteLine(binaryDes);

                                                        //// 기존 저장 방식 > 이상한 값으로 나옴
                                                        //string descriptorLine = string.Join(",", Enumerable.Range(0, descriptors.Cols).Select(j => descriptors.Get<int>(aa, j)));
                                                        ////string descriptorLine = string.Join(",", descriptors.Get<int>(aa, 0, descriptors.Cols));
                                                        //sq.WriteLine(descriptorLine);

                                                    }
                                                }
                                            }
                                            // ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## <<< 디스크립터 정보 + 특징점 정보 >>> ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## ## // 
                                        }
                                    }
                                    // 이미지 번호 증가
                                    imageNumber++;
                                }


                                //if(IsGrayColor == true)
                                //{
                                //    string filename2 = "./images2/received_image_" + DateTime.Now.ToString("yyyyMMdd_HHmmssfff") + ".jpg";

                                //    convMat2.SaveImage(filename2);
                                //}

                                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                                {
                                    try
                                    {
                                        image.Source = WriteableBitmapConverter.ToWriteableBitmap(convMat);
                                    }
                                    catch (Exception ex)
                                    {
                                        log.WriteLine(ex.ToString(), true, true);
                                        Console.WriteLine(ex.ToString());
                                    }
                                }));
                            }

                            sm.RecvSize = RecvCnt;
                            // Console.WriteLine("Size : " + sm.RecvSize);
                            log.WriteLine(String.Format("Size : {0}    BatteryVoltage : {1}", sm.RecvSize, sm.BatteryVoltage), true, true);
                            RecvCnt = 0;
                            state = 0;
                        }
                        else
                        {
                            state = 1;
                        }

                        // state = 0;

                        break;
                }

                if (RecvCnt >= StreamBuffer.Length)
                {
                    RecvCnt = 0;
                    state = 0;
                }
            }



#endif

        }

        private void RecvSocket()
        {
            try
            {
                byte[] buff;
                string msg;
                Socket socket;
                byte[] StreamBuffer = new byte[1920 * 1080];

                UDP.UdpObject udpObject = new UDP.UdpObject();

                while (IsRecvSocket)
                {
                    lock (TcpCfg.ServerQRecvBuf)
                    {
                        while (TcpCfg.ServerQRecvBuf.TryDequeue(out buff))
                        {
                            while (TcpCfg.ServerQEndPoint.TryDequeue(out socket)) ;
                            while (TcpCfg.ServerQMsg.TryDequeue(out msg)) ;
                            ShowCAM(buff);
                        }
                    }
                    /*
                    lock (TcpCfg.ClientQRecvBuf)
                    {
                        while(TcpCfg.ClientQRecvBuf.TryDequeue(out buff))
                        {
                            TcpCfg.ClientQEndPoint.TryDequeue(out socket);
                            TcpCfg.ClientQMsg.TryDequeue(out msg);
                            ShowCAM(buff);                            
                        }
                    }

                    lock (UdpCfg.QRecvBuf)
                    {
                        while (UdpCfg.QRecvBuf.TryDequeue(out udpObject))
                        {
                            if (udpObject.Buffer != null)
                            {
                                ShowCAM(udpObject.Buffer);
                            }
                        }
                    }                   
                    */
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Utility.ErrorMessage errMsg = new Utility.ErrorMessage();
                errMsg.Msg(ex);

                MessageBox.Show(errMsg.Msg(ex), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /*
        public Bitmap CopyDataToBitmap(int x, int y, byte[] data)
        {
            //Here create the Bitmap to the know height, width and format
            Bitmap bmp = new Bitmap(x, y, PixelFormat.Format16bppRgb555);


            //Create a BitmapData and Lock all pixels to be written 
            BitmapData bmpData = bmp.LockBits(
                                 new Rectangle(0, 0, bmp.Width, bmp.Height),
                                 ImageLockMode.ReadWrite, bmp.PixelFormat);


            //Copy the data from the byte array into BitmapData.Scan0 --> row값이 4의 배수가 아니면 이미지가 틀어짐 따라서 바로 byte array를 복사해서 사용이 불가
            //Marshal.Copy(data, 0, bmpData.Scan0, data.Length);

            //padding unused space
            int stride = bmpData.Stride;

            unsafe
            {
                byte* p = (byte*)(void*)bmpData.Scan0;

                //Format16bppRgb555 이기 때문에 *2
                int nOffset = stride - bmp.Width * 2;
                int nWidth = bmp.Width * 2;

                for (int h = 0; h < bmp.Height; h++)
                {
                    for (int w = 0; w < nWidth; w++)
                    {
                        p[0] = data[h * nWidth + w];
                        p++;
                    }
                    p = p + nOffset;
                }
                bmp.UnlockBits(bmpData);
            }

            //투명값 저장을 위해 16bit에서 32bit로 변환
            Bitmap newBmp = new Bitmap(bmp);

            Bitmap targetBmp = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format32bppArgb);

            //원하는 특정 pixel을 transparent로 변경
            targetBmp.MakeTransparent(Color.FromArgb(0, 231, 33));

            return targetBmp;
        }
        */

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            this.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sp.IsOpen == true)
            {
                sp.Write(string.Format("SET CAM QUALITY {0}\r\n", sm.JpegQuality));
            }
            else
            {
                if (TcpCfg.ServerIsConnected)
                {
                    TcpCfg.ServerStringSendBuf = string.Format("SET CAM QUALITY {0}\r\n", sm.JpegQuality);

                }

                if (TcpCfg.ClientIsConnected)
                {
                    TcpCfg.ClientStringSendBuf = string.Format("SET CAM QUALITY {0}\r\n", sm.JpegQuality);
                }
            }
        }

        private void DataReceivedHandler(object sender, EventArgs e)
        {
            StringBuilder strb_msg = new StringBuilder();
            byte[] buf = new byte[sp.BytesToRead];
            int LenREV = sp.Read(buf, 0, buf.Length);

            RecvResponse(buf);

            for (int i = 0; i < LenREV; i++)
            {
                strb_msg.Append(Convert.ToChar(buf[i]));
            }


            if (strb_msg.Length != 0)
            {
                Console.Write(string.Format("{0}", strb_msg));
            }

        }

        private void Cbo_SpecialEffect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sp.IsOpen == true)
            {
                sp.Write(string.Format("SET CAM EFFECT {0}\r\n", sm.SpecialEffect));
            }
            else
            {
                if (TcpCfg.ServerIsConnected)
                {
                    TcpCfg.ServerStringSendBuf = string.Format("SET CAM EFFECT {0}\r\n", sm.SpecialEffect);

                }

                if (TcpCfg.ClientIsConnected)
                {
                    TcpCfg.ClientStringSendBuf = string.Format("SET CAM EFFECT {0}\r\n", sm.SpecialEffect);
                }
            }
        }

        private void Cbo_framesize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sp.IsOpen == true)
            {
                sp.Write(string.Format("SET CAM FRAMESIZE {0}\r\n", sm.Framesize));
            }
            else
            {
                if (TcpCfg.ServerIsConnected)
                {
                    TcpCfg.ServerStringSendBuf = string.Format("SET CAM FRAMESIZE {0}\r\n", sm.Framesize);

                }

                if (TcpCfg.ClientIsConnected)
                {
                    TcpCfg.ClientStringSendBuf = string.Format("SET CAM FRAMESIZE {0}\r\n", sm.Framesize);
                }
            }
        }

        private void Chk_IsCamOnOff_Click(object sender, RoutedEventArgs e)
        {
            int value = (sm.IsCamOnOff == true) ? 1 : 0;
            if (sp.IsOpen == true)
            {
                sp.Write(string.Format("SET CAM ONOFF {0}\r\n", value));
            }
            else
            {
                if (TcpCfg.ServerIsConnected)
                {
                    TcpCfg.ServerStringSendBuf = string.Format("SET CAM ONOFF {0}\r\n", value);

                }

                if (TcpCfg.ClientIsConnected)
                {
                    TcpCfg.ClientStringSendBuf = string.Format("SET CAM ONOFF {0}\r\n", value);
                }
            }
        }

        private void CamDelayTime_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sp.IsOpen == true)
            {
                sp.Write(string.Format("SET CAM DELAYTIME {0}\r\n", sm.CamDelayTime));
            }
            else
            {
                if (TcpCfg.ServerIsConnected)
                {
                    TcpCfg.ServerStringSendBuf = string.Format("SET CAM DELAYTIME {0}\r\n", sm.CamDelayTime);

                }

                if (TcpCfg.ClientIsConnected)
                {
                    TcpCfg.ClientStringSendBuf = string.Format("SET CAM DELAYTIME {0}\r\n", sm.CamDelayTime);
                }
            }
        }

        private void LiveCamButton_Click(object sender, RoutedEventArgs e)
        {
            Utility.FormControls formControls = new Utility.FormControls();

            Button[] buttons = new Button[] { TcpSetup, VFlip, HMirror, Save, Read, SerialPortSetup, GetBatV };

            int idx = formControls.SelectControl(buttons, sender);

            string strCMD = null;

            switch (idx)
            {
                case 0:
                    TcpCfg.Owner = this;
                    TcpCfg.ShowDialog();
                    if (TcpCfg.ClientIsConnected || TcpCfg.ServerIsConnected)
                    {
                        RecvCnt = 0;
                        sm.RecvSize = 0;
                        TaskRecvSocket = new Tasks.Scheduler(RecvSocket);
                        sm.img_tcp = "image/green ball.png";
                    }
                    else
                    {
                        sm.img_tcp = "image/Red ball.png";
                    }
                    break;

                /*
                                case 1:
                                    UdpCfg.Owner = this;
                                    UdpCfg.ShowDialog();
                                    if (UdpCfg.IsStart)
                                    {
                                        sm.RecvSize = 0;
                                        TaskRecvSocket = new Tasks.Scheduler(RecvSocket);
                                    }
                                    break;
                */



                case 1:
                    if ((string)buttons[idx].Content == "Disable")
                    {
                        strCMD = "SET CAM VFLIP 1\r\n";
                        sm.strVFlip = "Enable";
                        sm.VFlip = 1;
                    }
                    else
                    {
                        strCMD = "SET CAM VFLIP 0\r\n";
                        sm.strVFlip = "Disable";
                        sm.VFlip = 0;
                    }
                    break;

                case 2:
                    if ((string)buttons[idx].Content == "Disable")
                    {
                        strCMD = "SET CAM HMIRROR 1\r\n";
                        sm.strHMirror = "Enable";
                        sm.HMirror = 1;
                    }
                    else
                    {
                        strCMD = "SET CAM HMIRROR 0\r\n";
                        sm.strHMirror = "Disable";
                        sm.HMirror = 0;
                    }
                    break;


                case 3:
                    strCMD = string.Format("SET WIFI SSID {0}\r\n", sm.SSID);
                    strCMD += string.Format("SET WIFI PW {0}\r\n", sm.PW);
                    strCMD += string.Format("SET WIFI REMOTEIP {0}\r\n", sm.RemoteIP);
                    strCMD += string.Format("SET WIFI REMOTEPORT {0}\r\n", sm.RemotePort);
                    strCMD += string.Format("SET WIFI LOCALPORT {0}\r\n", sm.LocalPort);
                    strCMD += "SET PARAM SAVE\r\n";
                    break;

                case 4:
                    strCMD = "GET PARAM\r\n";
                    break;

                case 5:
                    SerialCfg.Owner = this;
                    SerialCfg.ShowDialog();
                    if (sp.IsOpen)
                    {
                        sm.img_sp = "image/green ball.png";
                    }
                    else
                    {
                        sm.img_sp = "image/Red ball.png";
                    }
                    break;

                case 6:
                    strCMD = "GET BATTERY READ ON\r\n";
                    break;
            }

            if (strCMD != null)
            {
                Console.Write(string.Format("cli>. {0}", strCMD));

                if (sp.IsOpen == true)
                {
                    sp.Write(strCMD);
                }
                else
                {
                    if (TcpCfg.ClientIsConnected || TcpCfg.ServerIsConnected)
                    {
                        TcpCfg.ServerStringSendBuf = strCMD;
                    }
                }
            }
        }

        /*

        1. Mat -> System.Drawing.Bitmap
        Mat mat = new Mat("foobar.jpg", ImreadModes.Color);
        Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);

        2. System.Drawing.Bitmap -> Mat
        Bitmap bitmap = new Bitmap("foobar.jpg");
        Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);

        3. Mat -> byte[]
        Mat mat = new Mat("foobar.jpg", ImreadModes.Color);
        byte[] bytes1 = mat.ToBytes(".png");
        byte[] bytes2; Cv2.ImEncode(".jpg", mat, out bytes2);

        4. byte[] -> Mat
        byte[] imageData = System.IO.File.ReadAllBytes("foobar.jpg");
        Mat colorMat = Mat.FromImageData(imageData, ImreadModes.Color);
        Mat grayscaleMat = Mat.FromImageData(imageData, ImreadModes.GrayScale);
        Mat alt = Cv2.ImDecode(imageData, ImreadModes.GrayScale);

        */
    }

#if false

    public class BitmapHelper
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////// Field
        ////////////////////////////////////////////////////////////////////////////////////////// Private

#region Field

        /// <summary>
        /// 너비
        /// </summary>
        private int width;

        /// <summary>
        /// 높이
        /// </summary>
        private int height;

        /// <summary>
        /// 픽셀 배열
        /// </summary>
        private byte[] pixelArray;

        /// <summary>
        /// 스트라이드
        /// </summary>
        /// <remarks>해당 바이트 수이다.</remarks>
        private int stride;

#endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////// Constructor
        ////////////////////////////////////////////////////////////////////////////////////////// Public

#region 생성자 - BitmapHelper(width, height)

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="width">너비</param>
        /// <param name="height">높이</param>
        public BitmapHelper(int width, int height)
        {
            this.width = width;
            this.height = height;

            this.pixelArray = new byte[width * height * 4];

            this.stride = width * 4;
        }

#endregion

        //////////////////////////////////////////////////////////////////////////////////////////////////// Method
        ////////////////////////////////////////////////////////////////////////////////////////// Public

#region 픽셀 구하기 - GetPixel(x, y, red, green, blue, alpha)

        /// <summary>
        /// 픽셀 구하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="red">빨간색 채널</param>
        /// <param name="green">녹색 채널</param>
        /// <param name="blue">파란색 채널</param>
        /// <param name="alpha">알파 채널</param>
        public void GetPixel(int x, int y, out byte red, out byte green, out byte blue, out byte alpha)
        {
            int index = y * this.stride + x * 4;

            blue = this.pixelArray[index++];
            green = this.pixelArray[index++];
            red = this.pixelArray[index++];
            alpha = this.pixelArray[index];
        }

#endregion

#region 파란색 채널 구하기 - GetBlue(x, y)

        /// <summary>
        /// 파란색 채널 구하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <returns>파란색 채널</returns>
        public byte GetBlue(int x, int y)
        {
            return this.pixelArray[y * this.stride + x * 4];
        }

#endregion
#region 녹색 채널 구하기 - GetGreen(x, y)

        /// <summary>
        /// 녹색 채널 구하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <returns>녹색 채널</returns>
        public byte GetGreen(int x, int y)
        {
            return this.pixelArray[y * this.stride + x * 4 + 1];
        }

#endregion
#region 빨간색 채널 구하기 - GetRed(x, y)

        /// <summary>
        /// 빨간색 채널 구하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <returns>빨간색 채널</returns>
        public byte GetRed(int x, int y)
        {
            return this.pixelArray[y * this.stride + x * 4 + 2];
        }

#endregion
#region 알파 채널 구하기 - GetAlpha(x, y)

        /// <summary>
        /// 알파 채널 구하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <returns>알파 채널</returns>
        public byte GetAlpha(int x, int y)
        {
            return this.pixelArray[y * this.stride + x * 4 + 3];
        }

#endregion

#region 픽셀 설정하기 - SetPixel(x, y, red, green, blue, alpha)

        /// <summary>
        /// 픽셀 설정하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="red">빨간색 채널</param>
        /// <param name="green">녹색 채널</param>
        /// <param name="blue">파란색 채널</param>
        /// <param name="alpha">알파 채널</param>
        public void SetPixel(int x, int y, byte red, byte green, byte blue, byte alpha)
        {
            int index = y * this.stride + x * 4;

            this.pixelArray[index++] = blue;
            this.pixelArray[index++] = green;
            this.pixelArray[index++] = red;
            this.pixelArray[index++] = alpha;
        }

#endregion

#region 파란색 채널 설정하기 - SetBlue(x, y, blue)

        /// <summary>
        /// 파란색 채널 설정하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="blue">파란색 채널</param>
        public void SetBlue(int x, int y, byte blue)
        {
            this.pixelArray[y * this.stride + x * 4] = blue;
        }

#endregion
#region 녹색 채널 설정하기 - SetGreen(x, y, blue)

        /// <summary>
        /// 녹색 채널 설정하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="blue">녹색 채널</param>
        public void SetGreen(int x, int y, byte green)
        {
            this.pixelArray[y * this.stride + x * 4 + 1] = green;
        }

#endregion
#region 빨간색 채널 설정하기 - SetRed(x, y, blue)

        /// <summary>
        /// 빨간색 채널 설정하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="blue">빨간색 채널</param>
        public void SetRed(int x, int y, byte red)
        {
            this.pixelArray[y * this.stride + x * 4 + 2] = red;
        }

#endregion
#region 알파 채널 설정하기 - SetAlpha(x, y, blue)

        /// <summary>
        /// 알파 채널 설정하기
        /// </summary>
        /// <param name="x">X 좌표</param>
        /// <param name="y">Y 좌표</param>
        /// <param name="blue">알파 채널</param>
        public void SetAlpha(int x, int y, byte alpha)
        {
            this.pixelArray[y * this.stride + x * 4 + 3] = alpha;
        }

#endregion

#region 색상 설정하기 - SetColor(red, green, blue, alpha)

        /// <summary>
        /// 색상 설정하기
        /// </summary>
        /// <param name="red">빨간색 채널</param>
        /// <param name="green">녹색 채널</param>
        /// <param name="blue">파란색 채널</param>
        /// <param name="alpha">알파 채널</param>
        public void SetColor(byte red, byte green, byte blue, byte alpha)
        {
            int byteCount = this.width * this.height * 4;
            int index = 0;

            while (index < byteCount)
            {
                this.pixelArray[index++] = blue;
                this.pixelArray[index++] = green;
                this.pixelArray[index++] = red;
                this.pixelArray[index++] = alpha;
            }
        }

#endregion
#region 색상 설정하기 - SetColor(red, green, blue)

        /// <summary>
        /// 색상 설정하기
        /// </summary>
        /// <param name="red">빨간색 채널</param>
        /// <param name="green">녹색 채널</param>
        /// <param name="blue">파란색 채널</param>
        public void SetColor(byte red, byte green, byte blue)
        {
            SetColor(red, green, blue, 255);
        }

#endregion

#region 쓰기 가능 비트맵 구하기 - GetWriteableBitmap(dpiX, dpiY)

        /// <summary>
        /// 쓰기 가능 비트맵 구하기
        /// </summary>
        /// <param name="dpiX">DPU X</param>
        /// <param name="dpiY">DPU Y</param>
        /// <returns>쓰기 가능 비트맵</returns>
        public WriteableBitmap GetWriteableBitmap(double dpiX, double dpiY)
        {
            WriteableBitmap bitmap = new WriteableBitmap
            (
                this.width,
                this.height,
                dpiX,
                dpiY,
                PixelFormats.Bgra32,
                null
            );

            Int32Rect rectangle = new Int32Rect(0, 0, this.width, this.height);

            bitmap.WritePixels(rectangle, this.pixelArray, this.stride, 0);

            return bitmap;
        }

#endregion
    }
#endif
}

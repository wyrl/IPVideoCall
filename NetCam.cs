using Emgu.CV;
using Lib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPVideoCall
{
    class NetCam
    {
        private VideoCapture _capture;
        private Mat matImg;
        private TcpListener tcpListener;
        private Thread thCaptureListener;
        private TcpClient currentCamClient;
        private NetworkStream camNS;
        private bool isStopped;
        private ImageStream imageStream;
        private PictureBox pictureBox;
        private PictureBox previewPictureBox;
        public NetCam()
        {
            isStopped = true;
        }
        public void Start()
        {
            Console.WriteLine("Started NetCam Server");
            matImg = new Mat();
            tcpListener = new TcpListener(IPAddress.Any, 2527);
            thCaptureListener = new Thread(new ThreadStart(doListener));
            tcpListener.Start();
            thCaptureListener.Start();
            isStopped = false;
            imageStream = new ImageStream(200000, true);
            imageStream.ImageAvailable += imageStream_ImageAvailable;
            imageStream.Play();
        }

        public TcpClient Connect(String ip, int port)
        {
            currentCamClient = new TcpClient(ip, port);
            camNS = currentCamClient.GetStream();
            isStopped = false;
            matImg = new Mat();
            imageStream = new ImageStream(200000, true);
            imageStream.ImageAvailable += imageStream_ImageAvailable;
            imageStream.Play();
            Thread thRead = new Thread(new ThreadStart(() => {
                Console.WriteLine("@NetCam->Connect: Cam Connected to server...");
                doRead();
                Console.WriteLine("@NetCam->Connect: Cam Disconnected to server...");
            }));
            thRead.Start();
            return currentCamClient;
        }

        public bool IsStopped { get { return isStopped; } }
        public void OpenCamera(int camSourceIndex)
        {
            if (!isStopped)
            {
                try
                {
                    Console.WriteLine("Opened and Started Camera...");
                    _capture = new VideoCapture(camSourceIndex);
                    _capture.ImageGrabbed += _capture_ImageGrabbed;
                    _capture.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("@NetCam->OpenCamera: " + ex.Message);
                    MessageBox.Show("No device camera. Please select another cam.");
                }
            }
            else throw new MethodAccessException("Cannot open camera that has stopped.");
        }

        private void _capture_ImageGrabbed(object sender, EventArgs e)
        {
            if (_capture != null && _capture.IsOpened)
            {
                try
                {
                    _capture.Retrieve(matImg, 0);
                    //pictureBox1.Image = matImg;
                    Image img = matImg.Bitmap;
                    SendImage(img);
                    previewPictureBox.Image = img;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("@NetCam->_capture_ImageGrabbed Error: " + ex.Message);
                }
            }
        }
        public void CloseCamera()
        {
            if (!isStopped)
            {
                Console.WriteLine("Closed Camera");
                if (_capture != null)
                {
                    _capture.Dispose();
                    _capture = null;
                    pictureBox.Image = null;
                    previewPictureBox.Image = null;
                }
            }
            else throw new MethodAccessException("Cannot close camera that has stopped.");
        }


        private void imageStream_ImageAvailable(object sender, ImageStreamArgs e)
        {
            Image img = e.Image;
            img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pictureBox.Image = img;
        }

        private void doListener()
        {
            if(tcpListener != null)
            {
                Console.WriteLine("@NetCam->doListener run....");
                while (!isStopped)
                {
                    try
                    {
                        if(currentCamClient == null)
                        {
                            Console.WriteLine("@NetCam->doListener-> Waiting for Client Incoming....");
                            currentCamClient = tcpListener.AcceptTcpClient();
                            Console.WriteLine("@NetCam->Client has connected: " + currentCamClient.Client.RemoteEndPoint.ToString());
                            camNS = currentCamClient.GetStream();
                            
                            doRead();
                        }
                        Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("@NetCam->doListener->Listener Error: " + e);
                    }
                }
                Console.WriteLine("@NetCam->doListener stopped....");
            }
        }

        private void doRead()
        {
            Console.WriteLine("@NetRead->doRead run...");
            int lengthData = 0;
            byte[] dataImage = new byte[200000];
            try
            {
                if (camNS.CanRead)
                {
                    while ((lengthData = camNS.Read(dataImage, 0, dataImage.Length)) > 0 && !isStopped)
                    {
                        imageStream.Write(dataImage, 0, lengthData);
                        dataImage = new byte[200000];
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("@NetCam->doRead->Error: " + e);
            }
        }

        public void setPictureBox(PictureBox pictureBox)
        {
            this.pictureBox = pictureBox;
        }
        public void setPreviewPictureBox(PictureBox pictureBox)
        {
            this.previewPictureBox = pictureBox;
        }

        public void SendImage(Image img)
        {
            byte[] BufferBytes = ImageToByteArray(img);
            if (currentCamClient != null && currentCamClient.Connected)
            {
                if (camNS != null && camNS.CanWrite)
                {
                    try
                    {
                        camNS.Write(BufferBytes, 0, BufferBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        currentCamClient = null;
                        camNS = null;
                    }
                }
            }
        }

        private byte[] ImageToByteArray(Image img)
        {
            using (MemoryStream mStream = new MemoryStream())
            {
                mStream.WriteByte((int)0xFF);
                mStream.WriteByte((int)0xD8);
                mStream.Write(Encoding.ASCII.GetBytes("size"), 0, 4);
                byte[] lengthData = BitConverter.GetBytes((UInt32)0);
                mStream.Write(lengthData, 0, 4);
                img.Save(mStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                mStream.Seek(6, SeekOrigin.Begin);
                lengthData = BitConverter.GetBytes((UInt32)mStream.Length);
                mStream.Write(lengthData, 0, 4);
                mStream.Flush();
                mStream.Close();
                return mStream.ToArray();
            }
        }
        public void Stop()
        {
            if (!isStopped)
            {
                Console.WriteLine("Stopped NetCam Server...");
                CloseCamera();
                isStopped = true;
                if (tcpListener != null) tcpListener.Stop();
                if (imageStream != null) imageStream.Stop();
                if (currentCamClient != null) currentCamClient.Close();
                if (camNS != null) camNS.Close();

                tcpListener = null;
                imageStream = null;
                currentCamClient = null;
                camNS = null;
            }
        }
    }
}

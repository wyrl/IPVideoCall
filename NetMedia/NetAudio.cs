using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;
using NAudio.Wave;
using System.Net.Sockets;
using NAudio.CoreAudioApi;
using System.Windows.Forms;
using System.Threading;

namespace IPVideoCall.NetMedia
{
    class NetAudio : NetMedia
    {
        private WaveIn waveIn;
        private WaveOut waveOut;
        private BufferedWaveProvider waveProvider;
        private TcpListener audioListener;
        private TcpClient audioCurrentClient;
        private NetworkStream audioCurrentNS;

        private bool isStopped;

        private bool isOpenMic;
        private bool isOpenSound;

        public event EventHandler<ResultArgs> Result;
        public NetAudio()
        {
            isStopped = true;
        }

        public override void Start()
        {
            isStopped = false;

            audioListener = new TcpListener(System.Net.IPAddress.Any, 2528);
            audioListener.Start();
            Thread thListener = new Thread(new ThreadStart(doAudioListener));
            thListener.Start();

            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 25;
            waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            waveOut = new WaveOut();
            isOpenMic = false;
            isOpenSound = false;

            Console.WriteLine("Started NetAudio Server");
        }

        public override TcpClient Connect(String ip, int port)
        {
            audioCurrentClient = new TcpClient(ip, port);
            audioCurrentNS = audioCurrentClient.GetStream();
            isStopped = false;
            waveIn = new WaveIn();
            waveIn.BufferMilliseconds = 25;
            waveIn.WaveFormat = new WaveFormat(8000, 16, 1);
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveProvider = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            waveOut = new WaveOut();
            isOpenMic = false;
            isOpenSound = false;
            Thread thRead = new Thread(new ThreadStart(() => {
                Console.WriteLine("@NetAudio->Connect: Audio Connected to server");
                doRead();
                Console.WriteLine("@NetAudio->Connect: Audio Disconnected to server");
            }));
            thRead.Start();
            return audioCurrentClient;
        }

        public override bool IsStopped { get { return isStopped; } }


        private void doAudioListener()
        {
            while (!isStopped)
            {
                try
                {
                    audioCurrentClient = audioListener.AcceptTcpClient();
                    string ipaddress = audioCurrentClient.Client.RemoteEndPoint.ToString();
                    audioCurrentNS = audioCurrentClient.GetStream();
                    Console.WriteLine("Audio Client has connected({0})...", ipaddress);

                    doRead();

                    Console.WriteLine("Audio Client has disconnected({0})...", ipaddress);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("@NetAudio->doAudioListener: " + ex);
                }
            }
        }
        private void doRead()
        {
            byte[] dataBytes = new byte[200000];
            int lengthData = 0;

            //int top = Console.CursorTop;
            try
            {
                while ((lengthData = audioCurrentNS.Read(dataBytes, 0, dataBytes.Length)) > 0)
                {
                    if (isOpenSound)
                    {
                        int ms = (int)waveProvider.BufferedDuration.TotalMilliseconds;
                        Console.WriteLine("Audio Latency: " + ms);
                        if (Result != null) Result(this, new ResultArgs(ms));
                        if (ms >= 350) waveProvider.ClearBuffer();
                        else
                        {
                            if (waveProvider != null) waveProvider.AddSamples(dataBytes, 0, lengthData);
                        }
                    }
                    dataBytes = new byte[200000];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("@NetAudio->doAudioListener: " + ex);
            }
        }

        public void LoadWLoadWasapiDevicesCombo(ComboBox cb)
        {
            var deviceEnum = new MMDeviceEnumerator();
            var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

            cb.DataSource = devices;
            cb.DisplayMember = "FriendlyName";
        }
        public void OpenMic(int micSourceIndex)
        {
            if (!isOpenMic)
            {
                isOpenMic = true;
                waveIn.DeviceNumber = micSourceIndex;
                waveIn.StartRecording();
                Console.WriteLine("Opened Mic: " + micSourceIndex);
            }
        }
        public void CloseMic()
        {
            if (isOpenMic)
            {
                isOpenMic = false;
                waveIn.StopRecording();
                Console.WriteLine("Closed Mic");
            }
        }

        public void OpenSound()
        {
            if (!isOpenSound)
            {
                isOpenSound = true;
                waveOut.DesiredLatency = 150;
                waveOut.Init(waveProvider);
                waveOut.Play();
                Console.WriteLine("Opened Sound");
            }
        }
        public void CloseSound()
        {
            if (isOpenSound)
            {
                isOpenSound = false;
                waveOut.Stop();
                waveProvider.ClearBuffer();
                Console.WriteLine("Closed Sound");
            }
        }

        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (isOpenMic)
            {
                if (audioCurrentClient != null && audioCurrentClient.Connected)
                {
                    try
                    {
                        if (audioCurrentNS != null && audioCurrentNS.CanRead)
                            audioCurrentNS.Write(e.Buffer, 0, e.BytesRecorded);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("@NetAudio->waveIn_DataAvailable Error: " + ex.Message);
                    }
                }
            }
        }

        public override void Stop()
        {
            if (!isStopped)
            {
                isStopped = true;
                if (audioListener != null) audioListener.Stop();
                if (audioCurrentClient != null) audioCurrentClient.Close();
                if (audioCurrentNS != null) audioCurrentNS.Close();
                CloseMic();
                CloseSound();
                audioListener = null;
                waveIn = null;
                waveProvider = null;
                waveOut = null;

                Console.WriteLine("Stopped NetAudio Server...");
            }
        }
    }
}

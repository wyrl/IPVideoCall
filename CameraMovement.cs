using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
namespace IPVideoCall
{
    public class CameraMovement
    {
        public enum Type
        {
            Horizontal,
            Vertical,
            None
        }


        private string portName;
        private SerialPort serial;
        private TcpListener listener;
        private Thread thListener;
        private Boolean isStopped;
        private TcpClient currentClient;
        private NetworkStream currentNS;
        private int hDeg;
        private int vDeg;
        public event EventHandler<CameraMovementArgs> Response;
        public CameraMovement(string portName)
        {
            this.portName = portName;
            hDeg = 0;
            vDeg = 0;
        }
        public void Start()
        {
            listener = new TcpListener(IPAddress.Any, 2529);
            thListener = new Thread(new ThreadStart(doListener));
            listener.Start();
            thListener.Start();
            isStopped = false;
        }
        public void OpenSerial()
        {
            serial = new SerialPort(portName);
            serial.Open();
        }
        public void CloseSerial()
        {
            if (serial != null) serial.Close();
        }

        private void doListener()
        {
            Console.WriteLine("@CameraMovement->doListener run....");
            while(!isStopped)
            {
                try
                {
                    Console.WriteLine("@CameraMovement->doListener-> Waiting for Client Incoming....");
                    currentClient = listener.AcceptTcpClient();
                    Console.WriteLine("@CameraMovement->Client has connected: " + currentClient.Client.RemoteEndPoint.ToString());
                    currentNS = currentClient.GetStream();

                    doRead();
                }
                catch(Exception e)
                {
                    Console.WriteLine("@CameraMovement->doListener->Listener Error: " + e);
                }
            }
            Console.WriteLine("@CameraMovement->doListener stopped....");
        }

        private void doRead()
        {
            Console.WriteLine("@CameraMovement->doRead run...");
            int lengthData = 0;
            byte[] data = new byte[1024];
            string str1 = "";
            try
            {
                if (currentNS.CanRead)
                {
                    while ((lengthData = currentNS.Read(data, 0, data.Length)) > 0 && !isStopped)
                    {
                        //imageStream.Write(dataImage, 0, lengthData);
                        for (int i = 0; i < lengthData;i++ )
                        {
                            char ch = (char)data[i];
                            //Console.WriteLine(ch);
                            if (ch == ';')
                            {
                                Command(str1);
                                str1 = "";
                            }
                            else str1 += ch;
                        }
                            data = new byte[1024];
                    }
                    Console.WriteLine("@CameraMovement->doRead stopped...");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("@CameraMovement->doRead->Error: " + e);
            }
        }

        private void Command(string str1)
        {
            int deg = 0;
            string value;
            CameraMovement.Type type = CameraMovement.Type.None;
            
            if (str1.IndexOf("-movementH:") > -1)
            {
                type = CameraMovement.Type.Horizontal;
                value = str1.Split(':')[1];
                int.TryParse(value, out deg);
                setHorizontalDeg(deg);
            }
            else if (str1.IndexOf("-movementV:") > -1)
            {
                type = CameraMovement.Type.Vertical;
                value = str1.Split(':')[1];
                int.TryParse(value, out deg);
                setVerticalDeg(deg);
            }
            else return;

            if (Response != null) Response(this, new CameraMovementArgs(type, deg));

            Console.WriteLine("Command: " + str1);
        }

        public void setHorizontalDeg(int deg)
        {
            if(serial != null && serial.IsOpen)
            {
                byte[] a = new byte[4];
                a[0] = 0x00;                    //Begin for Command #
                a[1] = 0xFF;                    //End for Command #
                a[2] = (byte)deg;               //Begin for a value of Command
                a[3] = 0xFF;                    //End for a value of Command
                serial.Write(a, 0, a.Length);   //Send data to serial port for communication
                hDeg = deg;
            }
        }
        public void setVerticalDeg(int deg)
        {
            if(serial != null && serial.IsOpen)
            {
                byte[] a = new byte[4];
                a[0] = 0x01;                    //Begin for Command #
                a[1] = 0xFF;                    //End for Command #
                a[2] = (byte)deg;               //Begin for a value of Command
                a[3] = 0xFF;                    //End for a value of Command
                serial.Write(a, 0, a.Length);   //Send data to serial port for communication
                vDeg = deg;
            }
        }
        private int getHorizontalDeg()
        {
            return hDeg;
        }
        private int getVerticalDeg()
        {
            return vDeg;
        }
        public void Stop()
        {
            if (!isStopped)
            {
                isStopped = true;
                CloseSerial();
                if (listener != null) listener.Stop();
                if (currentClient != null) currentClient.Close();

                listener = null;
            }
        }
    }
}

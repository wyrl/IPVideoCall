using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.Util;
using Lib;
using System.Net.Sockets;
using System.Threading;
using System.Net;
namespace IPVideoCall
{
    public partial class frmChat : Form
    {
       
        private Chat chat;
        private NetCam netCam;
        private NetAudio netAudio;
        private CameraMovement movement;

        public frmChat()
        {
            InitializeComponent();
            this.Disposed += frmCam_Disposed;
            this.Load += frmCam_Load;
            this.KeyPress += frmCam_KeyPress;
            CvInvoke.UseOpenCL = false;

            movement = new CameraMovement("COM3");
            movement.Response += movement_Response;

            netCam = new NetCam();
            netCam.setPreviewPictureBox(pictureBox1);
            netCam.setPictureBox(pictureBox2);

            netAudio = new NetAudio();
            netAudio.Result += netAudio_Result;

            chat = new Chat(2524);
            chat.Receive += chat_Receive;
            chat.ClientConnected += chat_ClientConnected;
            chat.ClientDisconnected += chat_Disconnected;

        }

        void movement_Response(object sender, CameraMovementArgs e)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                if (e.Type == CameraMovement.Type.Horizontal && chkAdj.Checked) trackBar1.Value = e.Degree;
                if (e.Type == CameraMovement.Type.Vertical && chkAdj.Checked) trackBar2.Value = e.Degree;
            }));   
        }

        void netAudio_Result(object sender, ResultArgs e)
        {
            this.Invoke(new MethodInvoker(() => {
                lblLatency.Text = "Latency: " + e.AudioLatency.ToString();
            }));
        }

        void chat_ClientConnected(object sender, ConnectedArgs e)
        {
            if (IsServer)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    if (!Config.isIPConfirm)
                    {
                        Config.IPAddressConfirm = e.Address.ToString();
                        Config.isIPConfirm = true;
                        ControlEnabled(true);
                        netCam.Start();
                        netAudio.Start();
                        movement.Start();
                        pictureBox1.Show();
                        pictureBox2.Show();
                        //if (!netAudio.IsStopped) netAudio.OpenMic(0);
                        //if (!netAudio.IsStopped) netAudio.OpenSound();
                        //chat.Send("-com:ready");
                        lblMsg.Text = "Client has connected.";
                    }
                }));
            }
        }
        void chat_Disconnected(object sender, DisconnectedArgs e)
        {
            if (IsServer)
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    if (this.Visible)
                    {
                        Disconnect();
                        CreateServer();
                    }
                }));
            }
        }

        void chat_Receive(object sender, ReceiveArgs e)
        {
            this.Invoke(new MethodInvoker(() => {
                DateTime d = DateTime.Now;
                setMsg(d.ToShortDateString() + ": " + e.Msg);
            }));
        }

        void frmCam_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Keys)e.KeyChar == Keys.Enter)
            {
                Send(textBox1.Text);
            }
        }



        void frmCam_Load(object sender, EventArgs e)
        {
            //chkCam.Checked = chkMic.Checked = chkSound.Checked = chkAdj.Checked = true;
            netAudio.LoadWLoadWasapiDevicesCombo(comboBox2);
        }
        private void setMsg(String msg)
        {
            richTextBox1.AppendText(msg);
            richTextBox1.ScrollToCaret();
        }
        void frmCam_Disposed(object sender, EventArgs e)
        {
            Console.WriteLine("frmCam disposed...");
            chat.Stop();
            netCam.Stop();
            netAudio.Stop();
        }

        void ControlEnabled(bool enable)
        {
            tableLayoutPanel1.Enabled = enable;
            richTextBox1.Enabled = enable;
            textBox1.Enabled = enable;
            button1.Enabled = enable;
            chkCam.Enabled = enable;
            chkMic.Enabled = enable;
            chkAdj.Enabled = enable;
            chkSound.Enabled = enable;
        }

        private void chkCamAv_CheckedChanged(object sender, EventArgs e)
        {
            bool camchecked = chkCam.Checked;
            comboBox1.Enabled = camchecked;

            if (camchecked)
            {
                if(comboBox1.SelectedIndex > -1 && !netCam.IsStopped) netCam.OpenCamera(comboBox1.SelectedIndex);
            }
            else
            {
                if(!netCam.IsStopped) netCam.CloseCamera();
            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkCam.Checked && !netCam.IsStopped)
            {
                netCam.CloseCamera();
                netCam.OpenCamera(comboBox1.SelectedIndex);
            }
        }

        public void CreateServer()
        {
            lblMsg.Text = "Waiting for Client...";
            ControlEnabled(false);
            chat.StartServer();
        }
        public void Connect(String ip)
        {
            try
            {
                lblMsg.Text = "Connecting...";
                ControlEnabled(false);
                chat.Connect(ip, 2524);
                netCam.Connect(ip, 2527);
                netAudio.Connect(ip, 2528);
                ControlEnabled(true);
                pictureBox1.Show();
                pictureBox2.Show();
                lblMsg.Text = "";
            }
            catch(Exception ex)
            {
                MessageBox.Show("Try again later to connect. Make sure server is online.(" + ip + ")");
                Console.WriteLine("@frmCam->Connect Error: " + ex);
                this.Close();
            }
        }
        public void Disconnect()
        {
            Console.WriteLine("@frmCam->Disconnect()...");
            Config.IPAddressConfirm = "";
            Config.isIPConfirm = false;
            ControlEnabled(false);
            chkCam.Checked = false;
            chkMic.Checked = false;
            chkSound.Checked = false;
            netCam.Stop();
            netAudio.Stop();
            chat.Stop();
            movement.Stop();
            richTextBox1.Clear();
            pictureBox1.Hide();
            pictureBox2.Hide();
            pictureBox1.Image = null;
            pictureBox2.Image = null;
            lblMsg.Text = "Client has disconnected.";
        }


        private void Send(string msg)
        {
            DateTime d = DateTime.Now;
            setMsg(d.ToShortDateString() + "->you: " + msg + "\n");
            chat.Send(msg + "\n");
            textBox1.Clear();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chkMic.Checked && !netAudio.IsStopped)
            {
                netAudio.CloseMic();
                netAudio.OpenMic(comboBox2.SelectedIndex);
            }
        }

        private void chkMic_CheckedChanged(object sender, EventArgs e)
        {
            comboBox2.Enabled = chkMic.Checked;
            if (!netAudio.IsStopped)
            {
                if (chkMic.Checked) netAudio.OpenMic(comboBox2.SelectedIndex);
                else netAudio.CloseMic();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Send(textBox1.Text);
        }

        private void chkSound_CheckedChanged(object sender, EventArgs e)
        {
            if (!netAudio.IsStopped)
            {
                if (chkSound.Checked) netAudio.OpenSound();
                else netAudio.CloseSound();
            }
        }

        public bool IsServer { get; set; }

        private void chkAdj_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAdj.Checked)
            {
                try
                {
                    movement.OpenSerial();
                    trackbarsEnabled(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("@chkAdj_CheckedChanged Error: " + ex.Message);
                    MessageBox.Show("No found serial port device.");
                    chkAdj.Checked = false;
                    //trackbarsEnabled(false);
                }
            }
            else
            {
                movement.CloseSerial();
                trackbarsEnabled(false);
            }
        }

        private void trackbarsEnabled(bool enabled)
        {
            trackBar1.Enabled = enabled;
            trackBar2.Enabled = enabled;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            movement.setHorizontalDeg(trackBar1.Value);
            //Console.WriteLine("trackbac1: " + trackBar1.Value);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            movement.setVerticalDeg(trackBar2.Value);
            //Console.WriteLine("trackbac2: " + trackBar1.Value);
        }
    }
}

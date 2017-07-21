using Lib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IPVideoCall
{
    public partial class frmMain : Form
    {
        frmChat form_chat;
        public frmMain()
        {
            InitializeComponent();
            textBox1.KeyPress += textBox1_KeyPress;
            this.Disposed += frmMain_Disposed;
            form_chat = new frmChat();
            form_chat.FormClosing += cam_FormClosing;
        }

        void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if((Keys)e.KeyChar == Keys.Enter) Connect();
        }

        void cam_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            form_chat.Hide();
            form_chat.Disconnect();
            this.Show();
            Config.IPAddressConfirm = "";
            Config.isIPConfirm = false;
        }

        void frmMain_Disposed(object sender, EventArgs e)
        {
            if (form_chat != null) form_chat.Dispose();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            form_chat.Show();
            form_chat.IsServer = true;
            form_chat.CreateServer();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Connect();
        }
        private void Connect()
        {
            form_chat.Show();
            form_chat.IsServer = false;
            this.Hide();
            form_chat.Connect(textBox1.Text);
            textBox1.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace Cryptography
{
    public partial class Form2 : Form
    {
        //Form1 mainForm = new Form1();
        public Form2()
        {
            InitializeComponent();
        }
        
        private void button4_Click(object sender, EventArgs e)
        {
            string ip = textBox1.Text.Remove(0, textBox1.Text.LastIndexOf('/')+1);
            Socket socketToServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult resultOfConnect = socketToServer.BeginConnect(ip, 80, null, socketToServer);
            if (resultOfConnect.AsyncWaitHandle.WaitOne(40) == true)
            {
                socketToServer.Close();
            }
            else
            {
                this.DialogResult = DialogResult.No;
            }
        }
    }
}

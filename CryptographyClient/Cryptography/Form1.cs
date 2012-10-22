using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Security;
using System.Security.Cryptography;
using System.Net;
using System.Xml;
using System.IO;

namespace Cryptography
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Form2 loginForm = new Form2();
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            //Шифрование
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            //Расшифровка
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("Key");
            string plaintext = null;

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            return plaintext;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (richTextBox5.Text.Length > 0)
            {
                WebClient web = new WebClient();
                byte[] message = EncryptStringToBytes_Aes(richTextBox5.Text.Replace(';', ' '), Convert.FromBase64String(loginForm.richTextBox1.Text), Convert.FromBase64String(loginForm.richTextBox2.Text));
                byte[] answergByteFromServer = web.UploadData(loginForm.textBox1.Text, message);
                string decryptText = DecryptStringFromBytes_Aes(answergByteFromServer, Convert.FromBase64String(loginForm.richTextBox1.Text), Convert.FromBase64String(loginForm.richTextBox2.Text));
                string[] information = decryptText.Split(';');
                richTextBox6.Text = information[0];
                listBox1.Items.Clear();
                for (int i = 1; i < information.Length; i++)
                {
                    listBox1.Items.Add(information[i]);
                }
                richTextBox5.Text = "";
                richTextBox6.Select(richTextBox6.Text.Length - 1,1);
                richTextBox6.ScrollToCaret();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            WebClient web = new WebClient();
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            byte[] answergByteFromServer;
            string decryptText = "";
            try
            {
                answergByteFromServer = web.DownloadData(loginForm.textBox1.Text);
                decryptText = DecryptStringFromBytes_Aes(answergByteFromServer, Convert.FromBase64String(loginForm.richTextBox1.Text), Convert.FromBase64String(loginForm.richTextBox2.Text));
            }
            catch
            {
                timer1.Stop();
                button3.Enabled = false;
                MessageBox.Show("Ошибка соединения");
            }
            
            string [] information = decryptText.Split(';');
            richTextBox6.Text = information[0];
            
            listBox1.Items.Clear();
            for (int i = 1; i < information.Length; i++)
            {
                listBox1.Items.Add(information[i]);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.PerformClick();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            timer1.Stop();
            button3.Enabled = false;
            richTextBox6.Text = "";
            listBox1.Items.Clear();
            DialogResult result = loginForm.ShowDialog();
            timer1.Interval = decimal.ToInt32(loginForm.numericUpDown1.Value)*1000;
            if (result == DialogResult.OK)
            {
                timer1.Start();
                button3.Enabled = true;
            }
            else
            {
                MessageBox.Show("Введите адрес, параметры и попробуйте подключиться");
            }
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            panel2.Width = this.Size.Width - 140;
        }

        private void richTextBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                richTextBox5.Text = richTextBox5.Text.TrimEnd('\n');
                button3.PerformClick();
            }
        }
    }
}

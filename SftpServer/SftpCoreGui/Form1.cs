using MapDbContextLib.Context;
using SftpServerLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SftpCoreGui
{
    public partial class Form1 : Form
    {
        SftpLibApi _SftpApi = null;
        SftpAccount sftpAccountObject;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            Button Sender = sender as Button;
            if (Sender.Text == "Start")
            {
                _SftpApi = SftpLibApi.NewInstance();

                _SftpApi.Start("C:\\Users\\tom.puckett\\Desktop\\sftpPrivateKey.OpenSSH.pem");
                Sender.Text = "Stop";
            }
            else
            {
                Sender.Text = "Start";
                _SftpApi.Stop();
                _SftpApi = null;
            }
        }

        private void buttonStorePassword_Click(object sender, EventArgs e)
        {
            Button Sender = sender as Button;

            sftpAccountObject = new SftpAccount { Password = textPassword.Text };
            textHash.Text = sftpAccountObject.PasswordHash;
        }

        private void buttonVerifyPassword_Click(object sender, EventArgs e)
        {
            if (sftpAccountObject != null)
            {
                Button Sender = sender as Button;

                var result = sftpAccountObject.CheckPassword(textPassword.Text);
                MessageBox.Show(result.ToString());
            }
        }
    }
}

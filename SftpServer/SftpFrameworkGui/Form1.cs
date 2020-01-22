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

namespace SftpFrameworkGui
{
    public partial class Form1 : Form
    {
        SftpLibApi _SftpApi = null;

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
    }
}

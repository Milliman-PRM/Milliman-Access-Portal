/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: GUI form to manually drive SFTP library features
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.Extensions.Configuration;
using SftpServerLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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

            GlobalResources.LoadConfiguration();
            GlobalResources.InitializeSerilog(GlobalResources.ApplicationConfiguration);

            GlobalResources.MapDbConnectionString = GlobalResources.ApplicationConfiguration.GetConnectionString("DefaultConnection");
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            Button Sender = sender as Button;
            if (Sender.Text == "Start")
            {
                _SftpApi = SftpLibApi.NewInstance();

                using (var fileStream = new FileStream(textKeyfilePath.Text, FileMode.Open))
                {
                    using (BinaryReader keyStream = new BinaryReader(fileStream)) 
                    {
                        byte[] keyBytes = keyStream.ReadBytes((int)fileStream.Length);
                        _SftpApi.Start(keyBytes);
                    }
                }

                Sender.Text = "Stop";
            }
            else
            {
                if (_SftpApi != null)
                {
                    _SftpApi.Stop();
                    _SftpApi = null;
                }

                Sender.Text = "Start";
            }
        }

        private void buttonStorePassword_Click(object sender, EventArgs e)
        {
            Button Sender = sender as Button;

            // TODO fix the file drop id
            sftpAccountObject = new SftpAccount(Guid.Empty) { Password = textPassword.Text };
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

        private void buttonReportReportServerState_Click(object sender, EventArgs e)
        {
            if (_SftpApi != null)
            {
                ServerState state = _SftpApi.ReportState();

                MessageBox.Show($"Server Fingerprint is \"{state.Fingerprint}\"");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_SftpApi != null)
            {
                _SftpApi.Stop();
            }

            //base.OnFormClosing(e);
        }
    }
}

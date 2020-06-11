/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: GUI form to manually drive SFTP library features
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using MapDbContextLib.Context;
using Microsoft.AspNetCore.Identity;
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

            sftpAccountObject = new SftpAccount(Guid.Empty);

            buttonReportServerState.Enabled = false;
            buttonStorePassword.Enabled = false;
            buttonVerifyPassword.Enabled = false;
        }

        private void BtnStartStop_Click(object sender, EventArgs e)
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

                buttonReportServerState.Enabled = true;
                Sender.Text = "Stop";
            }
            else
            {
                if (_SftpApi != null)
                {
                    _SftpApi.Stop();
                    _SftpApi = null;
                }

                buttonReportServerState.Enabled = false;
                Sender.Text = "Start";
            }
        }

        private void ButtonHashPassword_Click(object sender, EventArgs e)
        {
            sftpAccountObject.Password = textPassword.Text;
            textHash.Text = sftpAccountObject.PasswordHash;
        }

        private void ButtonVerifyPassword_Click(object sender, EventArgs e)
        {
            if (sftpAccountObject != null)
            {
                Button Sender = sender as Button;

                var result = sftpAccountObject.CheckPassword(textPassword.Text);

                string msg = result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded
                    ? "Password matches hash"
                    : "Password does not match hash";

                MessageBox.Show(msg);
            }
        }

        private void ButtonReportReportServerState_Click(object sender, EventArgs e)
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

        private void textPassword_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textPassword.Text))
            {
                buttonStorePassword.Enabled = true;
            }
            else
            {
                buttonStorePassword.Enabled = false;
            }

            if (!string.IsNullOrEmpty(textPassword.Text) && !string.IsNullOrEmpty(textHash.Text))
            {
                buttonVerifyPassword.Enabled = true;
            }
            else
            {
                buttonVerifyPassword.Enabled = false;
            }
        }

        private void textHash_TextChanged(object sender, EventArgs e)
        {
            sftpAccountObject.PasswordHash = textHash.Text;

            if (!string.IsNullOrEmpty(textPassword.Text) && !string.IsNullOrEmpty(textHash.Text))
            {
                buttonVerifyPassword.Enabled = true;
            }
            else
            {
                buttonVerifyPassword.Enabled = false;
            }
        }

    }
}

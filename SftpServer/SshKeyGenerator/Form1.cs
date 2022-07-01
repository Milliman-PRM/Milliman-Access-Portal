/*
 * CODE OWNERS: Tom Puckett
 * OBJECTIVE: <What and WHY.>
 * DEVELOPER NOTES: <What future developers need to know.>
 */

using nsoftware.IPWorksSSH;
using System;
using System.Windows.Forms;

namespace SshKeyGenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            chkTranslateLineFeed.Checked = true;
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                int validityDays = (int)(DateTime.Today.AddYears((int)upDownExpirationYears.Value) - DateTime.Today).TotalDays;

                Certmgr mgr = new Certmgr();
                mgr.Config("CertValidityTime=" + validityDays.ToString());
                mgr.Config($"UseInternalSecurityAPI=true");
                mgr.CertStoreType = CertStoreTypes.cstPFXBlob;
                mgr.CreateCertificate(txtSubject.Text, (int)upDownSerialNumber.Value);

                txtOutput.Text = mgr.Cert.PrivateKey;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not generate new certificate: " + ex.Message,
                  "SshKeyGenerator", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCopyToClipboard_Click(object sender, EventArgs e)
        {
            if (chkTranslateLineFeed.Checked)
            {
                if (radioLF.Checked)
                {
                    Clipboard.SetText(txtOutput.Text.Replace("\r\n", @"\n"));
                }
                else if (radioCRLF.Checked)
                {
                    Clipboard.SetText(txtOutput.Text.Replace("\r\n", @"\r\n"));
                }
            }
            else
            {
                Clipboard.SetText(txtOutput.Text);
            }
        }

        private void chkTranslateLineFeed_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (checkBox.Checked)
            {
                radioLF.Enabled = true;
                radioCRLF.Enabled = true;
            }
            else
            {
                radioLF.Enabled = false;
                radioCRLF.Enabled = false;
            }
        }
    }
}

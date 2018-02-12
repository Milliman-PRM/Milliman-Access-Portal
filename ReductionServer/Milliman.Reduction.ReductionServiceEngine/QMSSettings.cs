using System;
using System.Security.Cryptography;
using System.Text;

namespace Milliman.Reduction.ReductionEngine
{
    [Serializable]
    public class QMSSettings
    {
        private string _encryptedPassword;
        private Rfc2898DeriveBytes Key;

        public string Name { get; set; }
        public string QMSURL { get; set; }
        public bool IsIntegratedSecurity { get; set; }
        public string UserName { get; set; }
        public string Password { get { return _encryptedPassword; } set { this.EncryptPassword(value); } }

        public QMSSettings()
        {
            byte[] Salt = Encoding.UTF8.GetBytes("Th1s1s4T3rr1bl3!DE@".ToCharArray());
            Key = new Rfc2898DeriveBytes("H3ck n0, this w1ll n3v3r work !@#$", Salt);
        }

        private void EncryptPassword(string password)
        {
            RijndaelManaged r = new RijndaelManaged();
            r.IV = Key.GetBytes(r.IV.Length);
            r.Key = Key.GetBytes(r.Key.Length);
            var encryptor = r.CreateEncryptor();
            var mem = new System.IO.MemoryStream();
            using (var cs = new CryptoStream(mem, encryptor, CryptoStreamMode.Write))
            {
                using (var w = new System.IO.StreamWriter(cs))
                {
                    w.Write(password);
                }
            }
            _encryptedPassword = Convert.ToBase64String(mem.ToArray());
        }

        internal string GetPassword()
        {
            if (string.IsNullOrEmpty(_encryptedPassword))
                return string.Empty;

            RijndaelManaged r = new RijndaelManaged();
            r.IV = Key.GetBytes(r.IV.Length);
            r.Key = Key.GetBytes(r.Key.Length);

            var decryptor = r.CreateDecryptor();
            var cipher = Convert.FromBase64String(this._encryptedPassword);

            using (var mem = new System.IO.MemoryStream(cipher))
            {
                using (var cs = new CryptoStream(mem, decryptor, CryptoStreamMode.Read))
                {
                    using (var reader = new System.IO.StreamReader(cs))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

    }
}

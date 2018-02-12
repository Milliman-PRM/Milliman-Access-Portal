using Sys = System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common {
    public static class Utilities {
        public static string ConvertStringToHex(string asciiString) {
            string hex = "";
            foreach( char c in asciiString ) {
                int tmp = c;
                hex += string.Format("{0:x2}", (uint) Sys.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }

        public static string ConvertHexToString(string HexValue) {
            string StrValue = "";
            while( HexValue.Length > 0 ) {
                StrValue += Sys.Convert.ToChar(Sys.Convert.ToUInt32(HexValue.Substring(0, 2), 16)).ToString();
                HexValue = HexValue.Substring(2, HexValue.Length - 2);
            }
            return StrValue;
        }

        public static byte[] StringToByteArray(string InputString) {
            byte[] bytes = Sys.Text.Encoding.UTF8.GetBytes(InputString);

            //byte[] bytes = new byte[InputString.Length * sizeof(char)];
            //Sys.Buffer.BlockCopy(InputString.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        public static string ByteArrayToString(byte[] bytes) {
            string result = Sys.Text.Encoding.UTF8.GetString(bytes);
            return result;
            //char[] chars = new char[bytes.Length / sizeof(char)];
            //Sys.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            //return new string(chars);
        }
        public static string CalculateMD5Hash(string input, bool isFileName = false) {
            byte[] inputBytes = null;
            if( isFileName == false ) {
                inputBytes = Sys.Text.Encoding.ASCII.GetBytes(input);
            } else {
                if( Sys.IO.File.Exists(input) )
                    inputBytes = Sys.IO.File.ReadAllBytes(input);
            }
            if( inputBytes.Length == 0 )
                return "";

            // step 1, calculate MD5 hash from input
            Sys.Security.Cryptography.MD5 md5 = Sys.Security.Cryptography.MD5.Create();

            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for( int i = 0; i < hash.Length; i++ ) {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static long GetDirectorySize(string p) {
            long b = 0;
            if( Sys.IO.Directory.Exists(p) ) {
                string[] a = Sys.IO.Directory.GetFiles(p, "*.*", Sys.IO.SearchOption.AllDirectories);

                foreach( string name in a ) {
                    Sys.IO.FileInfo info = new Sys.IO.FileInfo(name);
                    b += info.Length;
                }
            }
            return b;
        }

    }
}

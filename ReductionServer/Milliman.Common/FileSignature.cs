using Sys=System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Milliman.Common {
    public class FileSignature {
        const string Header = "HCINTEL";

        public bool IsSigned(string QualifiedFile) {
            if( Sys.IO.File.Exists(QualifiedFile) == false )
                return false;
            Sys.IO.FileInfo FI = new Sys.IO.FileInfo(QualifiedFile);

            //read the trailer to see how long the xml sig is
            byte[] SignatureLength = ReadSection(QualifiedFile, (int) FI.Length - 32, 32);
            string sSignatureLength = Milliman.Common.Utilities.ByteArrayToString(SignatureLength);

            return sSignatureLength.IndexOf(Header) != -1;
        }

        public bool SignFile(string QualifiedFile, string Signature) {
            if( Sys.IO.File.Exists(QualifiedFile) == false )
                return false;

            int SigLength = Signature.Length;
            string sSigLength = SigLength.ToString("D32");//always 32 chars
            //add the header
            sSigLength = Header + sSigLength.Substring(Header.Length);
            Signature += sSigLength;
            byte[] bSignature = Milliman.Common.Utilities.StringToByteArray(Signature);

            //remove old signature if present
            RemoveSignature(QualifiedFile);
            //check to see if file only contained signature
            Sys.IO.FileStream writeStream;
            try {
                writeStream = new Sys.IO.FileStream(QualifiedFile, Sys.IO.FileMode.Append, Sys.IO.FileAccess.Write);
                Sys.IO.BinaryWriter writeBinay = new Sys.IO.BinaryWriter(writeStream);
                writeBinay.Write(bSignature);
                writeBinay.Close();
            } catch( Sys.Exception ) {
                return false;
            }
            return true;
        }

        public byte[] ReadSection(string QualifiedFile, int FromPos, int Length) {
            if( Sys.IO.File.Exists(QualifiedFile) == false )
                return null;

            byte[] Data = new byte[Length];
            using( Sys.IO.FileStream sr = Sys.IO.File.OpenRead(QualifiedFile) ) {
                sr.Seek(FromPos, Sys.IO.SeekOrigin.Begin);

                sr.Read(Data, 0, Length);
            }
            return Data;
        }

        public string GetSignature(string QualifiedFile) {
            if( Sys.IO.File.Exists(QualifiedFile) == false )
                return "";
            Sys.IO.FileInfo FI = new Sys.IO.FileInfo(QualifiedFile);

            //read the trailer to see how long the xml sig is
            byte[] SignatureLength = ReadSection(QualifiedFile, (int) FI.Length - 32, 32);
            string sSignatureLength = Milliman.Common.Utilities.ByteArrayToString(SignatureLength);
            if( sSignatureLength.IndexOf(Header) == -1 )
                return "";  //not a valid signature
            int SigLength = Sys.Convert.ToInt32(sSignatureLength.Substring(Header.Length));

            byte[] bSignature = ReadSection(QualifiedFile, (int) FI.Length - (SigLength + 32), SigLength);

            return Milliman.Common.Utilities.ByteArrayToString(bSignature);
        }

        public bool RemoveSignature(string QualifiedFile) {
            if( Sys.IO.File.Exists(QualifiedFile) == false )
                return false;
            Sys.IO.FileInfo FI = new Sys.IO.FileInfo(QualifiedFile);

            //read the trailer to see how long the xml sig is
            byte[] SignatureLength = ReadSection(QualifiedFile, (int) FI.Length - 32, 32);
            string sSignatureLength = Milliman.Common.Utilities.ByteArrayToString(SignatureLength);
            if( sSignatureLength.IndexOf(Header) == -1 )
                return true;  //didn't really have a signature
            int SigLength = Sys.Convert.ToInt32(sSignatureLength.Substring(Header.Length));

            Sys.IO.FileStream fs = FI.Open(Sys.IO.FileMode.Open);
            fs.SetLength(Sys.Math.Max(0, FI.Length - (SigLength + 32))); //header is 32 bytes

            fs.Close();

            return true;
        }

    }
}

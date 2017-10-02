using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace APICon.Util
{
    public class ZipHelper
    {
        public static byte[] createZipBody(string name, string body, string sign)
        {
            /**/
            string docEntryName = Path.GetFileName(name);
            byte[] docBody = Encoding.GetEncoding("windows-1251").GetBytes(Utils.Base64Decode(body, "windows-1251"));
            string signEntryName = "6_" + Path.GetFileName(name).Replace(".xml", ".bin");
            byte[] signBody = Encoding.UTF8.GetBytes(sign);
            /**/
            byte[] zipByteArray = null;

            MemoryStream outputMemoryStream = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(outputMemoryStream);

            zipStream = addZipEntry(zipStream, docEntryName, docBody);
            zipStream = addZipEntry(zipStream, signEntryName, signBody);

            zipStream.IsStreamOwner = false;
            zipStream.Close();
            outputMemoryStream.Position = 0;
            zipByteArray = outputMemoryStream.ToArray();

            return zipByteArray;
        }
        public static byte[] createCondraZip(string xmlBody, string bodyName, string body, string signName, string sign)
        {
            byte[] zipByteArray = null;

            MemoryStream outputMemoryStream = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(outputMemoryStream);

            zipStream = addZipEntry(zipStream, "condra.xml", Encoding.UTF8.GetBytes(Utils.Base64Decode(xmlBody, "UTF-8")));
            zipStream = addZipEntry(zipStream, bodyName, Encoding.UTF8.GetBytes(Utils.Base64Decode(body, "UTF-8")));
            zipStream = addZipEntry(zipStream, signName, Encoding.UTF8.GetBytes(sign));

            zipStream.IsStreamOwner = false;
            zipStream.Close();
            outputMemoryStream.Position = 0;
            zipByteArray = outputMemoryStream.ToArray();

            return zipByteArray;
        }
        public static Dictionary<string, byte[]> unzip(byte[] zip)
        {
            Dictionary<string, byte[]> map = new Dictionary<string, byte[]>();

            ZipInputStream zis = new ZipInputStream(new MemoryStream(zip));
            ZipEntry ze = zis.GetNextEntry();
            while (ze != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];                    
                    StreamUtils.Copy(zis, ms, buffer);
                    map.Add(ze.Name, ms.ToArray());
                    ze = zis.GetNextEntry();
                }
            }
            zis.Close();
            return map;
        }
        private static ZipOutputStream addZipEntry(ZipOutputStream zipStream, string name, byte[] body)
        {
            MemoryStream inputMemoryStream = new MemoryStream(body);
            ZipEntry newZipEntry = new ZipEntry(Path.GetFileName(name));
            newZipEntry.Size = body.Length;
            zipStream.PutNextEntry(newZipEntry);
            StreamUtils.Copy(inputMemoryStream, zipStream, new byte[1024]);
            return zipStream;
        }
    }
}

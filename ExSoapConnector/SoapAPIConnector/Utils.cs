using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Security.Cryptography;

namespace APICon.Util
{
    public class Utils
    {
        public static string ToJson(object o)
        {
            return JsonConvert.SerializeObject(o);
        }
        /*public static object FromJson<Type>(string json)
        {
            return JsonConvert.DeserializeObject<Type>(json);
        }*/
        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T> (json);
        }
        public static string ToXml<T>(T obj)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T), "");
            using (StringWriter sw = new StringWriter())
            {
                serializer.Serialize(sw, obj);
                return sw.ToString();
            }
        }
        public static string ToXml<T>(T obj,string encodingName)
        {           
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            var encoding = Encoding.GetEncoding(encodingName);
            Stream str = new MemoryStream();
            using (StreamWriter sw = new StreamWriter(str, encoding))
            {
                serializer.Serialize(sw, obj);
                return StreamToString(str, encodingName);
            }
        }        
        public static T FromXml<T>(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (XmlReader reader=XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(xml))))
            {
                return (T)serializer.Deserialize(reader);
            }          
        }
        public static T FromXml<T>(string xml, string encodingName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (XmlReader reader = XmlReader.Create(new MemoryStream(Encoding.GetEncoding(encodingName).GetBytes(xml))))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

                
        /*
         for debugging streams
         */
        public static string StreamToString(Stream stream, string encodingName)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(encodingName)))
            {
                return reader.ReadToEnd();
            }
        }
        public static string Base64Encode(string plainText, string encodingName)
        {
            var plainTextBytes = Encoding.GetEncoding(encodingName).GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Encode(byte[] data, string encodingName)
        {
            var plainTextBytes = data;
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string baseText, string encodingName)
        {
            var base64textBytes = System.Convert.FromBase64String(baseText);
            return Encoding.GetEncoding(encodingName).GetString(base64textBytes);
        }
        public static byte[] Base64DecodeToBytes(string baseText, string encodingName)
        {
            var base64textBytes = System.Convert.FromBase64String(baseText);
            return base64textBytes;
        }
        public static string Base64DecodeToString(byte[] baseText, string encodingName)
        {            
            return Encoding.GetEncoding(encodingName).GetString(baseText);
        }
        public static string BytesToString(byte[] data, string encodingName)
        {
            return Encoding.GetEncoding(encodingName).GetString(data);
        }
        public static byte[] StringToBytes(string data, string encodingName)
        {
            return Encoding.GetEncoding(encodingName).GetBytes(data);
        }
        public static string GetMD5String(string input)
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] data = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (var b in data)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}

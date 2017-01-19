using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using System.IO;

namespace APICon.Util
{
    public class Utils
    {
        public static string ToJson(object o)
        {
            return null;
        }
        public static object FromJson<Type>(string json)
        {
            return null;
        }
        public static string ToXml<T>(T obj)
        {
            return null;
        }
        public static string ToXml<T>(T obj,string encodingName)
        {
            return null;
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
    }
}

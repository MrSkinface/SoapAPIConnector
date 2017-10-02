using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using APICon.Util;

namespace APICon.Condra
{
    [XmlRoot(ElementName = "CONDRA")]
    public class CondraXml
    {
        [XmlElement(ElementName = "HEAD")]
        public CondraHead head { set; get; }

        public string getFileName()
        {
            return head.content.fileName;
        }
        public string getSignName()
        {
            return head.content.signName;
        }
    }
    [XmlRoot]
    public class CondraHead
    {
        [XmlElement(ElementName = "CONTENT")]
        public CondraContent content { set; get; }
    }
    [XmlRoot]
    public class CondraContent
    {
        [XmlElement(ElementName = "FILENAME")]
        public string fileName { set; get; }
        [XmlElement(ElementName = "SIGNNAME")]
        public string signName { set; get; }
    }

    public class Condra
    {
        public static CondraXml toObj(string filePath)
        {
            
            byte[] b = File.ReadAllBytes(filePath);
            string xml = Encoding.UTF8.GetString(b);
            return Utils.FromXml<CondraXml>(xml);
        }
    }
}

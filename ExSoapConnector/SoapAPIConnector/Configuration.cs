using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace APICon.conf
{
    [XmlRoot(ElementName = "document")]
    public class Document
    {
        [XmlElement(ElementName = "doctype")]
        public string Doctype { get; set; }
        [XmlElement(ElementName = "localPath")]
        public string LocalPath { get; set; }
        [XmlElement(ElementName = "thumpprint")]
        public string Thumpprint { get; set; }
        [XmlAttribute(AttributeName = "needToBeSigned")]
        public string NeedToBeSigned { get; set; }

        override
        public string ToString()
        {
            return "\n\tDoctype" + Doctype +
                   "\n\tLocalPath" + LocalPath +
                   "\n\tThumpprint" + Thumpprint +
                   "\n\tNeedToBeSigned" + NeedToBeSigned;
        }
    }

    [XmlRoot(ElementName = "inbound")]
    public class Inbound
    {
        [XmlElement(ElementName = "document")]
        public List<Document> Document { get; set; }
        [XmlAttribute(AttributeName = "isArchive")]
        public string IsArchive { get; set; }

        override
        public string ToString()
        {
            return "\n\tDocument.ToString()" + Document.ToString() +
                   "\n\tIsArchive" + IsArchive;
        }
    }

    [XmlRoot(ElementName = "outbound")]
    public class Outbound
    {
        [XmlElement(ElementName = "document")]
        public List<Document> Document { get; set; }
        [XmlAttribute(AttributeName = "isArchive")]
        public string IsArchive { get; set; }

        override
        public string ToString()
        {
            return "\n\tDocument.ToString()" + Document.ToString() +
                   "\n\tIsArchive" + IsArchive;
        }
    }

    [XmlRoot(ElementName = "configuration")]
    public class Configuration
    {
        [XmlElement(ElementName = "login")]
        public string Login { get; set; }
        [XmlElement(ElementName = "soap_pass")]
        public string Soap_pass { get; set; }
        [XmlElement(ElementName = "api_pass")]
        public string Api_pass { get; set; }
        [XmlElement(ElementName = "thumpprint")]
        public string Thumpprint { get; set; }
        [XmlElement(ElementName = "inbound")]
        public Inbound Inbound { get; set; }
        [XmlElement(ElementName = "outbound")]
        public Outbound Outbound { get; set; }

        override
        public string ToString()
        {
            return "\n\tLogin" + Login +
                   "\n\tSoap_pass" + Soap_pass +
                   "\n\tApi_pass" + Api_pass +
                   "\n\tThumpprint" + Thumpprint +
                   "\n\tInbound.ToString()" + Inbound.ToString() +
                   "\n\tOutbound.ToString()" + Outbound.ToString();
        }
    }
}

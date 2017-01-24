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
        public List<string> LocalPath { get; set; }
        [XmlElement(ElementName = "ticketPath")]
        public List<string> TicketPath { get; set; }
        [XmlElement(ElementName = "thumpprint")]
        public string Thumpprint { get; set; }
        [XmlAttribute(AttributeName = "needToBeSigned")]
        public bool NeedToBeSigned { get; set; }                
    }

    [XmlRoot(ElementName = "inbound")]
    public class InboundOutbound
    {
        [XmlElement(ElementName = "defaultPath")]
        public string DefaultPath { get; set; }
        [XmlElement(ElementName = "defaultArchive")]
        public string DefaultArchive { get; set; }
        [XmlElement(ElementName = "document")]
        public List<Document> Document { get; set; }
        [XmlAttribute(AttributeName = "isArchive")]
        public bool IsArchive { get; set; }                
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
        [XmlElement(ElementName = "logFile")]
        public string LogFile { get; set; }
        [XmlElement(ElementName = "inbound")]
        public InboundOutbound Inbound { get; set; }
        [XmlElement(ElementName = "outbound")]
        public InboundOutbound Outbound { get; set; }       
    }
}

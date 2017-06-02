using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace APICon.conf
{
    [XmlRoot(ElementName = "proxy")]
    public class Proxy
    {
        [XmlAttribute(AttributeName = "enable")]
        public bool Enable { get; set; }
        [XmlElement(ElementName = "address")]
        public string address { get; set; }        
        [XmlElement(ElementName = "login")]
        public string login { get; set; }
        [XmlElement(ElementName = "password")]
        public string password { get; set; }
    }
    [XmlRoot(ElementName = "document")]
    public class Document
    {
        [XmlElement(ElementName = "doctype")]
        public string Doctype { get; set; }
        [XmlElement(ElementName = "localPath")]
        public List<string> LocalPath { get; set; }
        [XmlElement(ElementName = "localArchive")]
        public List<string> LocalArchive { get; set; }
        [XmlElement(ElementName = "ticketPath")]
        public List<string> TicketPath { get; set; }
        [XmlElement(ElementName = "thumpprint")]
        public string Thumpprint { get; set; }
        [XmlAttribute(AttributeName = "needToBeSigned")]
        public bool NeedToBeSigned { get; set; }
        [XmlAttribute(AttributeName = "ticketsGenerate")]
        public bool TicketsGenerate { get; set; }
    }

    [XmlRoot(ElementName = "inbound")]
    public class InboundOutbound
    {
        /**/
        [XmlAttribute(AttributeName = "mode")]
        public string mode { get; set; }
        [XmlElement(ElementName = "timeline")]
        public TicketsTimeLine timeline { get; set; }
        /**/
        [XmlElement(ElementName = "defaultPath")]
        public string DefaultPath { get; set; }
        [XmlElement(ElementName = "defaultArchive")]
        public string DefaultArchive { get; set; }
        [XmlElement(ElementName = "defaultError")]
        public string DefaultError { get; set; }
        [XmlElement(ElementName = "document")]
        public List<Document> Document { get; set; }
        [XmlAttribute(AttributeName = "enable")]
        public bool Enable { get; set; }
        [XmlAttribute(AttributeName = "isArchive")]
        public bool IsArchive { get; set; }
        [XmlAttribute(AttributeName = "useSubFoldersByDocType")]
        public bool SubFolders { get; set; }
    }
    /**/
    [XmlRoot(ElementName = "timeline")]
    public class TicketsTimeLine
    {
        [XmlElement(ElementName = "fromMinusDays")]
        public double fromMinusDays { get; set; }
        [XmlElement(ElementName = "toPlusDays")]
        public double toPlusDays { get; set; }
    }
    /**/

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
        [XmlElement(ElementName = "EDOTickets")]
        public InboundOutbound EDOTickets { get; set; }
        [XmlElement(ElementName = "proxy")]
        public Proxy proxy { get; set; }

        public Document GetCustomInboundSettings(string docType)
        {
            foreach (Document doc in this.Inbound.Document)
                if (doc.Doctype == docType)
                    return doc;
            return null;
        }
        public Document GetCustomOutboundSettings(string docType)
        {
            foreach (Document doc in this.Outbound.Document)
                if (doc.Doctype == docType)
                    return doc;
            return null;
        }
        public Document GetCustomEDOTicketSettings(string docType)
        {
            foreach (Document doc in this.EDOTickets.Document)
                if (doc.Doctype == docType)
                    return doc;
            return null;
        }
    }
}

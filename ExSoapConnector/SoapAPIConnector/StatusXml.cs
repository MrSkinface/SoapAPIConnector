using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace APICon.Status
{
    [XmlRoot(ElementName = "Status")]
    public class StatusXml
    {
        public StatusXml()
        {
            DateTime currentTime = DateTime.Now;
            this.Date = currentTime.ToString("yyyy-MM-dd");
            this.DateIn = currentTime.ToString("yyyy-MM-dd");
            this.TimeIn = currentTime.ToString("HH:mm:ss");
            this.DateOut = currentTime.ToString("yyyy-MM-dd");
            this.TimeOut = currentTime.ToString("HH:mm:ss");
            this.fileName = "STATUS_" + Guid.NewGuid().ToString() + ".xml";
        }

        [XmlIgnore]
        public string fileName { get; set; }
        [XmlElement(ElementName = "CustomerICID")]
        public string CustomerICID { get; set; }
        [XmlElement(ElementName = "EXiteICID")]
        public string EXiteICID { get; set; }
        [XmlElement(ElementName = "From")]
        public string From { get; set; }
        [XmlElement(ElementName = "To")]
        public string To { get; set; }
        [XmlElement(ElementName = "DeliveryPlace")]
        public string DeliveryPlace { get; set; }
        [XmlElement(ElementName = "Date")]
        public string Date { get; set; }
        [XmlElement(ElementName = "Status")]
        public string Status { get; set; }
        [XmlElement(ElementName = "Description")]
        public string Description { get; set; }
        [XmlElement(ElementName = "DateIn")]
        public string DateIn { get; set; }
        [XmlElement(ElementName = "TimeIn")]
        public string TimeIn { get; set; }
        [XmlElement(ElementName = "DateOut")]
        public string DateOut { get; set; }
        [XmlElement(ElementName = "TimeOut")]
        public string TimeOut { get; set; }
        [XmlElement(ElementName = "MessageClass")]
        public string MessageClass { get; set; }
        [XmlElement(ElementName = "StatusOnFileName")]
        public string StatusOnFileName { get; set; }

        /*
        * custom fields
        */
        [XmlElement(ElementName = "TotalLines")]
        public string TotalLines { get; set; }
        [XmlElement(ElementName = "TotalNetAmount")]
        public string TotalNetAmount { get; set; }
        [XmlElement(ElementName = "TotalTaxAmount")]
        public string TotalTaxAmount { get; set; }
        [XmlElement(ElementName = "TotalGrossAmount")]
        public string TotalGrossAmount { get; set; }

    }
}

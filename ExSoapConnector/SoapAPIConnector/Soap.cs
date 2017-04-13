using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Net;
using System.Xml;
using System.IO;
using APICon.Util;
using SoapAPIConnector;

namespace APICon.soap
{
    /*
    user's obj for every request 
    */
    [XmlRoot(ElementName = "user")]
    public class User
    {
        [XmlElement(ElementName = "login")]
        public string login { get; set; }
        [XmlElement(ElementName = "pass")]
        public string pass { get; set; }
    }    
    [XmlRoot(ElementName = "soap")]
    public class Request
    {
        [XmlElement(ElementName = "user")]
        public User user { get; set; }
    }    
    [XmlRoot(ElementName = "result")]
    public class Response
    {
        [XmlElement(ElementName = "errorCode")]
        public int errorCode { get; set; }
        [XmlElement(ElementName = "errorMessage")]
        public string errorMessage { get; set; }
    }

    /*
    getList() 
    */
    public class GetListRequest : Request
    {

    }   
    [XmlRoot(ElementName = "result")]
    public class GetListResponse : Response
    {
        [XmlElement(ElementName = "list")]
        public List<string> list { get; set; }
    }
    /*
    getDoc() 
    */
    public class GetDocRequest : Request
    {
        [XmlElement(ElementName = "fileName")]
        public string fileName { get; set; }
    }
    [XmlRoot(ElementName = "result")]
    public class GetDocResponse : Response
    {
        [XmlElement(ElementName = "content")]
        public string content { get; set; }
    }
    /*
    sendDoc() 
    */
    public class SendDocRequest : Request
    {
        [XmlElement(ElementName = "fileName")]
        public string fileName { get; set; }
        [XmlElement(ElementName = "content")]
        public string content { get; set; }
    }
    [XmlRoot(ElementName = "result")]
    public class SendDocResponse : Response
    {

    }
    /*
    archiveDoc() 
    */
    public class ArchiveDocRequest : Request
    {
        [XmlElement(ElementName = "fileName")]
        public string fileName { get; set; }
    }
    [XmlRoot(ElementName = "result")]
    public class ArchiveDocResponse : Response
    {

    }

    /*
    static class to dealing with soap 
    */
    public static class Soap
    {
        public static object GetList<Type>(GetListRequest req)
        {
            StringBuilder sb = new StringBuilder(
@"<soap:getList>           
            <user>
                <login>").
            Append(req.user.login).Append(@"</login><pass>").
            Append(req.user.pass).Append(@"</pass>
            </user>
</soap:getList>");           
            return Action<Type>(createRequestBody(sb.ToString()));
        }
        public static object GetDoc<Type>(GetDocRequest req)
        {
            StringBuilder sb = new StringBuilder(
@"<soap:getDoc>           
            <user>
                <login>").
            Append(req.user.login).Append(@"</login><pass>").
            Append(req.user.pass).Append(@"</pass></user>").
            Append(@"<fileName>").Append(req.fileName).Append(@"</fileName>
</soap:getDoc>");
            return Action<Type>(createRequestBody(sb.ToString()));              
        }
        public static object SendDoc<Type>(SendDocRequest req)
        {
            StringBuilder sb = new StringBuilder(
@"<soap:sendDoc>           
            <user>
                <login>").
            Append(req.user.login).Append(@"</login><pass>").
            Append(req.user.pass).Append(@"</pass></user>").
            Append(@"<fileName>").Append(req.fileName).Append(@"</fileName><content>").
            Append(req.content).Append(@"</content>
</soap:sendDoc>");
            return Action<Type>(createRequestBody(sb.ToString()));            
        }
        public static object ArchiveDoc<Type>(ArchiveDocRequest req)
        {
            StringBuilder sb = new StringBuilder(
@"<soap:archiveDoc>           
            <user>
                <login>").
            Append(req.user.login).Append(@"</login><pass>").
            Append(req.user.pass).Append(@"</pass></user>").
            Append(@"<fileName>").Append(req.fileName).Append(@"</fileName>
</soap:archiveDoc>");
            return Action<Type>(createRequestBody(sb.ToString()));           
        }

        private static string createRequestBody(string actionPart)
        {
            StringBuilder sb = new StringBuilder(
@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:soap=""http://soap.edi.exite.org/"">
    <soapenv:Header/>
    <soapenv:Body>");
            sb.Append(actionPart);
            sb.Append(
@"</soapenv:Body>
</soapenv:Envelope>");
            return sb.ToString();
        }
        
        private static object Action<Type>(string requestBody)
        {            
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(@"http://195.191.226.106:8080/soap");
            if (Program.conf.proxy.Enable)
            {               
                WebProxy proxy = new WebProxy();                
                Uri proxyUri = new Uri(Program.conf.proxy.address);
                proxy.Credentials = new NetworkCredential(Program.conf.proxy.login, Program.conf.proxy.password);
                webRequest.Proxy = proxy;
            }
            webRequest.Headers.Add(@"SOAP:Action");
            webRequest.ContentType = "text/xml; charset=UTF-8";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            XmlDocument bodyRequest = new XmlDocument();
            bodyRequest.LoadXml(requestBody);                 
            using (Stream stream = webRequest.GetRequestStream())
            {
                bodyRequest.Save(stream);
                WebResponse response = null;                 
                try
                {
                    response = webRequest.GetResponse();                    
                    XmlReader reader;
                    using (reader = XmlReader.Create(response.GetResponseStream()))
                    {
                        reader.ReadToFollowing("result");
                        reader = reader.ReadSubtree();
                        StringBuilder sb = new StringBuilder();
                        while (reader.Read())
                            sb.Append(reader.ReadOuterXml());
                        //Console.WriteLine(sb.ToString());
                        return Utils.FromXml<Type>(sb.ToString(), "UTF-8");
                    }
                }                
                finally
                {
                    stream.Close();
                    response.Close();
                }                
            }                      
        }
    }
}

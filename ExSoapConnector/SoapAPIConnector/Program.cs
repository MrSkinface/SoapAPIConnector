using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using APICon.Util;
using APICon.conf;
using APICon.soap;
using System.Xml;
using System.Xml.Serialization;
using System.Security.Cryptography;
using APICon.controller;
using APICon.logger;
using APICon.rest;

namespace SoapAPIConnector
{
    class Program
    {
        public static Configuration conf;
        private Controller controller;

        public Program()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("\t-allcerts\t-\tshow all certificates info");
            Console.WriteLine("\t-infocert\t-\tshow info for 2nd arg certificate by thumbprint");
            Console.WriteLine("\t-testcert\t-\ttesting sign methods for 2nd arg certificate by thumbprint");
            Console.WriteLine("\t-testrest\t-\ttesting http connection to web-services");
            Console.WriteLine("\t-testsoap\t-\ttesting http connection to soap-services");
        }
        public Program(String[] args)
        {
            Program.conf=GetAppConfiguration("configuration.xml");
            controller = new Controller();
            switch (args[0])
            {
                case "-allcerts":
                    // testing crypto etc.
                    testCrypto();
                    break;
                case "-infocert":
                    ExCert cert = null;
                    try
                    {
                        cert = controller.GetExCertificate(args[1]);
                        Console.WriteLine("certificate info:");
                        Console.WriteLine(cert.ToString());
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case "-testcert":
                    String sign = null;
                    String base64data = Utils.Base64DecodeToString(Encoding.GetEncoding("UTF-8").GetBytes("somedata"), "UTF-8");
                    try
                    {
                        sign = controller.Sign(args[1], base64data);
                        if (sign != null)
                            Console.WriteLine("signing O.K.");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case "-testrest":
                    AuthorizeResponse response = (AuthorizeResponse)Http.post<AuthorizeResponse>("https://api-service.edi.su/Api/Dixy/Index/Authorize", new AuthorizeRequest(conf.Login, conf.Api_pass));
                    if(response!=null)
                        Console.WriteLine("rest O.K.");
                    break;
                case "-testsoap":
                    GetListRequest req = new GetListRequest();
                    req.user = new User();
                    req.user.login = conf.Login;
                    req.user.pass = Utils.GetMD5String(conf.Soap_pass);
                    GetListResponse resp = (GetListResponse)Soap.GetList<GetListResponse>(req);
                    if (resp != null)
                        Console.WriteLine("soap O.K.");
                    break;
            }
            
            // testing tickets etc.
            //testTickets();
            //testConf();
        }
        public Program(Configuration conf)
        {
            Program.conf = conf;
            this.controller= new Controller(Program.conf);
            // TICKETS confirm (only UnRead events: mark as read after confirm)
            if (conf.EDOTickets.Enable)
            {
                if (conf.EDOTickets.mode == "unread")
                    processTicketsUnread();
                else if (conf.EDOTickets.mode == "timeline")
                    processTicketsTimeLine();
                else if (conf.EDOTickets.mode == "soap")
                    processTicketsSoap();
            }
            else
                Logger.log("tickets disabled in [configuration.xml]");
            // IN            
            if (conf.Inbound.Enable)
                processInbound();
            else
                Logger.log("inbound disabled in [configuration.xml]");
            // OUT            
            if (conf.Outbound.Enable)                
                processOutbound();
            else
                Logger.log("outbound disabled in [configuration.xml]");




            //debug();
        }

        /**/
        public void debug()
        {
            /*List<string> outArcPaths = new List<string>();
            foreach (Document confDoc in conf.Outbound.Document)
                foreach (string path in confDoc.LocalArchive)
                    outArcPaths.Add(path);
            foreach (string name in outArcPaths)
                Console.WriteLine(name);*/
        }
        /**/
        public void testCrypto()
        {
            foreach (ExCert cert in controller.GetCertificates())
            {
                Console.WriteLine("cert info :");
                Console.WriteLine(cert.ToString());
                Console.WriteLine("testing sign ...");
                String sign = null;
                String base64data = Utils.Base64DecodeToString(Encoding.GetEncoding("UTF-8").GetBytes("somedata"), "UTF-8");
                try
                {
                    sign = controller.Sign(cert.Thumbprint, base64data);
                    if (sign != null)
                        Console.WriteLine("signing O.K.");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        /**/
        public void processTicketsSoap()
        {
            List<string> names = controller.getList();

            List<string> docs = new List<string>();
            foreach (Document d in conf.EDOTickets.Document)
                docs.Add(d.Doctype);

            List<Event> evnts = new List<Event>();
            foreach (string name in names)
            {
                if (name.EndsWith(".xml"))
                    if (docs.Contains(name.Split('_')[0] + "_" + name.Split('_')[1]))
                    {
                        Event e = new Event();
                        e.document_id = name.Split('_')[5].Replace(".xml", "");
                        evnts.Add(e);
                    }
            }
            foreach (Event e in evnts)
                if (signAndConfirmEvent(e))
                {
                    foreach (string name in names)
                        if (name.Contains(e.document_id))
                            if (controller.archiveDoc(name))
                                Logger.log(name + " removed from server .");
                }
        }
        public void processTicketsTimeLine()
        {            
            Event[] events= controller.getIncomingEvents();            
            foreach (Event e in events)                
            if (signAndConfirmEvent(e))
                controller.MarkEventRead(e.event_id);
        }
        public void processTicketsUnread()
        {
            /**/
            List<string> docs = new List<string>();
            foreach (Document d in conf.EDOTickets.Document)
            {
                string value;
                if (Controller.ticketTypes.TryGetValue(d.Doctype, out value))
                    docs.Add(value);
            }            
            /**/

            int count = controller.getUnreadEvents().total_events_count;
            Event[] events;            
            for (int i=0;i<=(count / 100); i++)
            {                
                events = controller.getUnreadEvents().timeline;
                foreach (Event e in events)
                {
                    if(e.event_status.Contains("NEW") || e.event_status.Contains("ERROR") || e.event_status.Length<22)
                        controller.MarkEventRead(e.event_id);
                    else if(!e.need_reply_reciept)
                        controller.MarkEventRead(e.event_id);
                    else if(!docs.Contains(e.event_status.Substring(0, 22)))
                        controller.MarkEventRead(e.event_id);
                    else if (signAndConfirmEvent(e))
                            controller.MarkEventRead(e.event_id);
                }
                events = null;
            }
        }
        /**/




        public bool signAndConfirmEvent(Event e)
        {            
            try
            {
                //Console.WriteLine(e.document_id);
                GetContentResponse content = controller.getDocumentContent(e);
                if (content.intCode != 200)
                    content = controller.getUPDDocumentContent(e);
                string eventName = controller.GetIDFileFromTicket(content.body);
                string docType = eventName.Split('_')[0] + "_" + eventName.Split('_')[1];
                Document docSettings = conf.GetCustomEDOTicketSettings(docType);
                if (docSettings != null)
                {
                    string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                    Ticket ticket = controller.Ticket(thumbPrint, eventName);
                    if (ticket != null)
                    {
                        string body = Utils.Base64Encode(ticket.body, "windows-1251");
                        string sign = controller.Sign(thumbPrint, body);
                        if (controller.confirmEvent(e, body, sign))
                        {
                            /*
                                saving incoming ticket
                             */
                            saveTicket(docSettings.LocalPath, eventName + ".xml", Utils.Base64DecodeToBytes(content.body, "windows-1251"));
                            saveTicket(docSettings.LocalPath, eventName + ".bin", Utils.StringToBytes(content.sign, "UTF-8"));
                            /*
                                saving outgoing ticket
                             */
                            saveTicket(docSettings.TicketPath, ticket.fileName, ticket.body);
                            saveTicket(docSettings.TicketPath, ticket.fileName.Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8"));
                            
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {                
                Console.WriteLine(ex.StackTrace);
                Logger.log(ex.Message);
                return false;
            }
        }
        public void processInbound()
        {
            List<string> docs = new List<string>();
            foreach (Document d in conf.Inbound.Document)
                docs.Add(d.Doctype);

            List<string> inbound;
           
                inbound = controller.getList();
                foreach (string name in inbound)
                {
                if((docs.Contains(name.Split('_')[0])) ||(docs.Contains(name.Split('_')[0]+"_"+ name.Split('_')[1])))
                    try
                    {
                        byte[] docBody = controller.getDoc(name);
                        if (docBody != null)
                            if (saveDoc(name, docBody))
                                if(conf.Inbound.IsArchive)
                                    if (controller.archiveDoc(name))
                                        Logger.log(name + " removed from server .");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);//debug only
                        Logger.log(ex.Message);
                    }
                }            
        }
        public void processOutbound()
        {
            List<string> outbound;
            outbound = Directory.GetFiles(conf.Outbound.DefaultPath).ToList();
            foreach (Document confDoc in conf.Outbound.Document)
                foreach (string path in confDoc.LocalPath)
                    outbound.AddRange(Directory.GetFiles(path));            
                
                foreach (string name in outbound)
                {
                try
                {
                    string docType = GetDocType(Path.GetFileName(name));                    
                    Document docSettings = conf.GetCustomOutboundSettings(docType);
                    if (docSettings != null)
                        if (docSettings.NeedToBeSigned) // for signed docs
                        {
                            string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "windows-1251");
                            string sign = controller.Sign(thumbPrint, body);
                            if (docType.StartsWith("DP_"))
                            {         
                                if ((controller.sendDoc(Path.GetFileName(name), body)) 
                                    &&
                                    (controller.sendDoc(Path.GetFileName(name).Replace(".xml",".bin"), sign)))
                                {
                                    Logger.log(Path.GetFileName(name) + " sent successfully.");
                                    if (conf.Outbound.IsArchive)
                                    {
                                        if ((moveDocToArc(Path.GetFileName(name), (File.ReadAllBytes(name)), docSettings))
                                            &&
                                            (moveDocToArc(Path.GetFileName(name).Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8"), docSettings)))
                                            File.Delete(name);
                                    }
                                }
                            }
                            else
                            {
                                if (controller.sendDocApi(body, sign, docType))
                                {
                                    Logger.log(Path.GetFileName(name) + " sent successfully.");
                                    if (conf.Outbound.IsArchive)
                                    {
                                        if (moveDocToArc(Path.GetFileName(name), File.ReadAllBytes(name), docSettings))
                                            File.Delete(name);
                                        if (moveDocToArc(Path.GetFileName(name).Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8"), docSettings))
                                            File.Delete(name.Replace(".xml", ".bin"));
                                    }
                                }
                                else
                                {
                                    if (moveDocToError(Path.GetFileName(name), File.ReadAllBytes(name)))
                                        File.Delete(name);
                                    if (moveDocToError(Path.GetFileName(name).Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8")))
                                        File.Delete(name.Replace(".xml", ".bin"));
                                }
                            }

                        }
                        else // for simple docs
                        {
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "UTF-8");
                            if (controller.sendDoc(Path.GetFileName(name), body))
                            {
                                Logger.log(Path.GetFileName(name) + " sent successfully.");
                                if (conf.Outbound.IsArchive)
                                {
                                    if (moveDocToArc(Path.GetFileName(name), (File.ReadAllBytes(name)), docSettings))
                                        File.Delete(name);
                                }
                            }
                            else
                            {
                                if (moveDocToError(Path.GetFileName(name), (File.ReadAllBytes(name))))
                                    File.Delete(name);
                            }
                            
                        }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace);//debug only
                    Logger.log(ex.Message);
                }
            }
            
        }
        private bool saveDoc(string fileName,byte[]body)
        {
            try
            {
                string docType = GetDocType(fileName);
                //Console.WriteLine("fileName: " + fileName+ "; docType: "+ docType);                
                Document docSettings = conf.GetCustomInboundSettings(docType);
                StringBuilder sb = new StringBuilder(conf.Inbound.DefaultPath);
                //Console.WriteLine("docSettings: " + docSettings);
                if (docSettings != null)
                {
                    foreach (string path in docSettings.LocalPath)
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        File.WriteAllBytes(path + fileName, body);
                        Logger.log(fileName + " saved in " + path);
                    }
                }
                else
                {
                    if (conf.Inbound.SubFolders)
                        sb.Append(docType).Append("\\");
                    if (!Directory.Exists(sb.ToString()))
                        Directory.CreateDirectory(sb.ToString());
                    //Console.WriteLine("sb.ToString(): " + sb.ToString());
                    File.WriteAllBytes(sb.ToString() + fileName, body);
                    Logger.log(fileName + " saved in " + sb.ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
                return false;
            }              
        }
        private bool moveDocToArc(string fileName, byte[] body, Document doc)
        {
            if (doc.LocalArchive != null)
            {
                foreach (string path in doc.LocalArchive)
                {
                    StringBuilder sb = new StringBuilder(path);                    
                    if (!Directory.Exists(sb.ToString()))
                        Directory.CreateDirectory(sb.ToString());
                    try
                    {
                        File.WriteAllBytes(sb.Append(fileName).ToString(), body);
                        Logger.log(fileName + " moved to " + sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);//debug only
                        Logger.log(ex.Message);
                        return false;
                    }
                }
                return true;
            }
            else
                return moveDocToArc(fileName,body);
        }
        private bool moveDocToArc(string fileName, byte[] body)
        {           
            try
            {
                string docType = GetDocType(fileName);                
                StringBuilder sb = new StringBuilder(conf.Outbound.DefaultArchive);                
                if (conf.Outbound.SubFolders)
                    sb.Append(docType).Append("\\");
                if (!Directory.Exists(sb.ToString()))
                    Directory.CreateDirectory(sb.ToString());
                File.WriteAllBytes(sb.ToString() + fileName, body);
                Logger.log(fileName + " moved to " + sb.ToString());                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
                return false;
            }
        }
        private bool moveDocToError(string fileName, byte[] body)
        {
            try
            {
                string docType = GetDocType(fileName);
                StringBuilder sb = new StringBuilder(conf.Outbound.DefaultError);
                if (conf.Outbound.SubFolders)
                    sb.Append(docType).Append("\\");
                if (!Directory.Exists(sb.ToString()))
                    Directory.CreateDirectory(sb.ToString());
                File.WriteAllBytes(sb.ToString() + fileName, body);
                Logger.log(fileName + " moved to " + sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
                return false;
            }
        }
        private void saveTicket(List<string> ticketPath,string fileName, byte[] body)
        {
            foreach (string path in ticketPath)
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllBytes(path + fileName, body);
                Logger.log(fileName + " saved in " + path);
            }
        }
        private string GetDocType(string fileName)
        {
            List<string> checkEDOList = new List<string>(new string[] {"ON","DP"});
            var v = fileName.Split('_');
            if(checkEDOList.Contains(v[0]))
                return v[0]+"_"+v[1];
            return v[0];
        }

        static void Main(string[] args)
        {            
            if (args.Length == 0)
            {
                new Program();
            }
            else if (args[0].Contains("-"))
            {
                new Program(args);
            }
            else
            {
                Configuration conf = GetAppConfiguration(args[0]);
                Logger.loadConfig(conf);
                Logger.log("start");                
                new Program(conf);
                Logger.log("end");
            }            
        }
        public static Configuration GetAppConfiguration(string appArg)
        {
            string path = Path.GetFullPath(appArg);            
            byte[] xml = File.ReadAllBytes(path);
            return Utils.FromXml<Configuration>(Encoding.GetEncoding("UTF-8").GetString(xml));
        }
    }
}

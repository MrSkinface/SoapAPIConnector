using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using APICon.Util;
using APICon.conf;
using APICon.soap;
using APICon.controller;
using APICon.logger;
using APICon.rest;
using APICon.Condra;

namespace SoapAPIConnector
{
    public class Program
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
            Program.conf= DFSHelper.GetAppConfiguration("configuration.xml");
            Logger.loadConfig();
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
                case "-testrestFULLDEBUG":
                    AuthorizeRequest debugReq = new AuthorizeRequest(conf.Login, conf.Api_pass);
                    Console.WriteLine("request body:");
                    Console.WriteLine(Utils.ToJson(debugReq));
                    AuthorizeResponse debugResp = (AuthorizeResponse)Http2.post<AuthorizeResponse>("https://api-service.edi.su/Api/Dixy/Index/Authorize", debugReq);
                    Console.WriteLine("response body:");
                    Console.WriteLine(Utils.ToJson(debugResp));
                    if (debugResp != null)
                        Console.WriteLine("rest O.K.");
                    break;
                case "-testrest":
                    AuthorizeResponse response = (AuthorizeResponse)Http2.post<AuthorizeResponse>("https://api-service.edi.su/Api/Dixy/Index/Authorize", new AuthorizeRequest(conf.Login, conf.Api_pass));
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
            Logger.log("start processing tickets...");
            List <string> names = null;
            try
            {
                names = controller.getList();
                Logger.log("INFO: getFilesList returned ["+ names .Count + "] entries");
            }
            catch (Exception ex)
            {
                Logger.log("ERROR: tickets will NOT be procceed . Reason : " + ex.Message );               
                return;
            }

            List<string> docs = new List<string>();
            foreach (Document d in conf.EDOTickets.Document)
                docs.Add(d.Doctype);

            List<Event> evnts = new List<Event>();
            foreach (string name in names)
            {
                if (name.EndsWith(".xml") || name.EndsWith(".zip"))
                    if (docs.Contains(name.Split('_')[0] + "_" + name.Split('_')[1]))
                    {
                        Event e = new Event();
                        e.document_id = name.Split('_')[5].Replace(".xml", "").Replace(".zip", "");
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
                GetContentResponse content = controller.getUPDDocumentContent(e);
                if (content.intCode != 200)
                    content = controller.getDocumentContent(e);
                string eventName = controller.GetIDFileFromTicket(content.body);
                string docType = eventName.Split('_')[0] + "_" + eventName.Split('_')[1];
                Document docSettings = conf.GetCustomEDOTicketSettings(docType);
                if (docSettings != null)
                {
                    string signExt = ".bin";
                    if (docSettings.custom_sign_extension != null)
                        signExt = docSettings.custom_sign_extension;

                    if (!docSettings.TicketsGenerate)
                    {
                        /*
                                   just saving incoming ticket
                        */
                        DFSHelper.saveTicket(docSettings.LocalPath, eventName + ".xml", Utils.Base64DecodeToBytes(content.body, "windows-1251"));
                        DFSHelper.saveTicket(docSettings.LocalPath, eventName + signExt, Utils.StringToBytes(content.sign, "UTF-8"));
                    }
                    else
                    {
                        string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                        Ticket ticket = controller.Ticket(thumbPrint, eventName);
                        if (ticket != null)
                        {
                            string body = Utils.Base64Encode(ticket.body, "windows-1251");
                            string sign = controller.Sign(thumbPrint, body);
                            if (controller.confirmEvent(e, body, sign))
                            {                                
                                if (eventName.Contains(".xml"))
                                    eventName = eventName.Replace(".xml", string.Empty);
                                /*
                                    saving incoming ticket
                                 */
                                DFSHelper.saveTicket(docSettings.LocalPath, eventName + ".xml", Utils.Base64DecodeToBytes(content.body, "windows-1251"));
                                DFSHelper.saveTicket(docSettings.LocalPath, eventName + signExt, Utils.StringToBytes(content.sign, "UTF-8"));
                                /*
                                    saving outgoing ticket
                                 */
                                DFSHelper.saveTicket(docSettings.TicketPath, ticket.fileName, ticket.body);
                                DFSHelper.saveTicket(docSettings.TicketPath, ticket.fileName.Replace(".xml", signExt), Utils.StringToBytes(sign, "UTF-8"));

                            }
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
            Logger.log("start processing inbound...");
            List<string> docs = new List<string>();
            foreach (Document d in conf.Inbound.Document)
                docs.Add(d.Doctype);

            List<string> inbound = null;           
            try
            {
                inbound = controller.getList();
                Logger.log("INFO: getFilesList returned [" + inbound.Count + "] entries");
            }
            catch (Exception ex)
            {
                Logger.log("ERROR: inbound will NOT be procceed . Reason : " + ex.Message );                
                return;
            }
            foreach (string name in inbound)
            {                
                if (conf.Inbound.DownloadALL || (docs.Contains(name.Split('_')[0])) || (docs.Contains(name.Split('_')[0] + "_" + name.Split('_')[1])))
                {
                    try
                    {
                        byte[] docBody = controller.getDoc(name);
                        if (docBody != null)
                            if (DFSHelper.saveDoc(name, docBody))
                                if (conf.Inbound.IsArchive)
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
        }
        public void processOutbound()
        {
            Logger.log("start processing outbound...");
            List<string> outbound=null;            
            try
            {
                outbound = Directory.GetFiles(conf.Outbound.DefaultPath).ToList();
            }
            catch (Exception ex)
            {
                Logger.log("ERROR: outbound will NOT be procceed . Reason : " + ex.Message);                
                return;
            }     
            /*
             adding all others files from custom folders
             */
            outbound.AddRange(DFSHelper.GetOutFiles(conf.Outbound.Document));
            
            foreach (string name in outbound)
            {                
                try
                {
                    string docType = DFSHelper.GetDocType(Path.GetFileName(name));                    
                    Document docSettings = conf.GetCustomOutboundSettingsByPath(docType, name);                    
                    if (docSettings != null)
                        if (docSettings.NeedToBeSigned) // for signed docs
                        {
                            string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "windows-1251");
                            string sign = controller.Sign(thumbPrint, body);
                            if (docSettings.NeedToBeZipped) // trick with non-secure soap and xp (w/o support tls over 1.0)
                            {
                                zipAndProcessDoc(docSettings, name, body, sign);
                            }
                            else if (
                                (docType.StartsWith("DP_") || docType.StartsWith("ON_SCHFDOPPOK") || docType.StartsWith("ON_KORSCHFDOPPOK"))
                                &&
                                name.EndsWith(".xml")
                                )
                            {
                                if ((controller.sendDoc(Path.GetFileName(name), body))
                                    &&
                                    (controller.sendDoc(Path.GetFileName(name).Replace(".xml", ".bin"), sign)))
                                {
                                    Logger.log(Path.GetFileName(name) + " sent successfully.");
                                    if (conf.Outbound.IsArchive)
                                    {
                                        if ((DFSHelper.moveDocToArc(Path.GetFileName(name), (File.ReadAllBytes(name)), docSettings))
                                            &&
                                            (DFSHelper.moveDocToArc(Path.GetFileName(name).Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8"), docSettings)))
                                            File.Delete(name);
                                    }
                                }
                            }
                            else if (docType.Equals("CONDRA", StringComparison.OrdinalIgnoreCase))
                            {                                
                                var c = Condra.toObj(name);                                

                                string condraXmlBody = body;
                                string filePath = Path.GetDirectoryName(name) + Path.DirectorySeparatorChar + c.getFileName();

                                body = Utils.Base64Encode(File.ReadAllBytes(filePath), "UTF-8");                              
                                sign = controller.Sign(thumbPrint, body);

                                string condraName = "condra_" + Guid.NewGuid() + ".zip";
                                byte[] condra = ZipHelper.createCondraZip(condraXmlBody, c.getFileName(), body, c.getSignName(), sign);

                                if (controller.sendDoc(condraName, Utils.Base64Encode(condra, "UTF-8")))
                                {
                                    Logger.log(Path.GetFileName(condraName) + " sent successfully.");
                                    if (conf.Outbound.IsArchive)
                                    {
                                        if (DFSHelper.moveDocToArc(condraName, condra, docSettings))
                                        {
                                            File.Delete(name);
                                            File.Delete(filePath);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    controller.sendDocApi(body, sign, docType);
                                    Logger.log(Path.GetFileName(name) + " sent successfully.");
                                    if (conf.Outbound.IsArchive)
                                    {
                                        if (DFSHelper.moveDocToArc(Path.GetFileName(name), File.ReadAllBytes(name), docSettings))
                                            File.Delete(name);
                                        if (DFSHelper.moveDocToArc(Path.GetFileName(name).Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8"), docSettings))
                                            File.Delete(name.Replace(".xml", ".bin"));
                                    }
                                }
                                catch (Exception e)
                                {
                                    try
                                    {    
                                        handleSendException(new Exception(e.Message + " [ " + controller.GetIDFileFromTicket(body) + " ]"), name, sign);
                                    }
                                    catch (Exception ex)
                                    {
                                        handleSendException(new Exception("XML document not well formed"), name, sign);
                                    }                                    
                                }
                            }
                        }
                        else // for simple docs
                        {
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "UTF-8");                            
                            string remoteName = Path.GetFileName(name);
                            if (docSettings.remoteFileNamePrefix != null)
                                remoteName = docSettings.remoteFileNamePrefix + remoteName;                            
                            if (controller.sendDoc(remoteName, body))
                            {
                                Logger.log(remoteName + " sent successfully.");
                                if (conf.Outbound.IsArchive)
                                {
                                    if (DFSHelper.moveDocToArc(Path.GetFileName(name), (File.ReadAllBytes(name)), docSettings))
                                        File.Delete(name);
                                }
                            }
                            else
                            {
                                if (DFSHelper.moveDocToError(Path.GetFileName(name), (File.ReadAllBytes(name))))
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
        private void handleSendException(Exception e , string filePath, string signBody)
        {
            if (e.Message.Equals("Not find successful Aperak for document invoice"))
            {
                Logger.log("file ["+ filePath +"] will be waiting for next sending");
            }
            else
            {
                Logger.error("ERROR: " + e.Message + ", file [ " + filePath + " ]");
                if (DFSHelper.moveDocToError(Path.GetFileName(filePath), File.ReadAllBytes(filePath)))
                    File.Delete(filePath);
                if (DFSHelper.moveDocToError(Path.GetFileName(filePath).Replace(".xml", ".bin"), Utils.StringToBytes(signBody, "UTF-8")))
                    File.Delete(filePath.Replace(".xml", ".bin"));
            }            
        }
        /*SUPERKOSTYL'*/
        /*trick with non-secure soap and xp (w/o support tls over 1.0)*/
        private void zipAndProcessDoc(Document docSettings, string name, string body, string sign)
        {
            string zipName = Path.GetFileName(name).Replace(".xml", ".zip");
            byte[] zipBody = ZipHelper.createZipBody(name, body, sign);

            if (controller.sendDoc(zipName, Utils.Base64Encode(zipBody, "UTF-8")))
            {
                Logger.log(Path.GetFileName(name) + " sent successfully.");
                if (conf.Outbound.IsArchive)
                {
                    if (DFSHelper.moveDocToArc(zipName, zipBody, docSettings))
                        File.Delete(name);
                }
            }
        }        
        /**/
        
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
                Configuration conf = DFSHelper.GetAppConfiguration(args[0]);
                Logger.loadConfig(conf);
                Logger.log("start");                
                new Program(conf);
                Logger.log("end");
            }            
        }
        
    }
}

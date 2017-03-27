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
        private Configuration conf;
        private Controller controller;

        public Program()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("\t-allcerts\t-\tshow all certificates info");
            Console.WriteLine("\t-infocert\t-\tshow info for 2nd arg certificate by thumbprint");
            Console.WriteLine("\t-testcert\t-\ttesting sign methods for 2nd arg certificate by thumbprint");
        }
        public Program(String[] args)
        {
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
            }
            
            // testing tickets etc.
            //testTickets();
            //testConf();
        }
        public Program(Configuration conf)
        {
            this.conf = conf;
            this.controller= new Controller(conf);            
            // TICKETS confirm (from 60 days till now)
            processTickets();
            // IN
            processInbound();
            // OUT
            processOutbound();
        }
        
        /**/        
        public void testCrypto()
        {
            foreach (ExCert cert in controller.GetCertificates())
                Console.WriteLine(cert.ToString());
        }
        public void testTickets()
        {
            Console.WriteLine(controller.getIncomingEvents(conf).Length);
            foreach (Event e in controller.getIncomingEvents(conf))
                Console.WriteLine(e.ToString());
        }
        /**/




        public void processTickets()
        {
            Event[] eventsToConfirm;
            try
            {
                eventsToConfirm = controller.getIncomingEvents(conf);
                foreach (Event e in eventsToConfirm)
                {
                    ApiDocument apiDocInfo = controller.getDocInfoByEvent(e);
                    string docType = apiDocInfo.doc_type;
                    Document docSettings = conf.GetCustomEDOTicketSettings(docType);
                    if (docSettings != null)
                    {
                        string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                        Ticket ticket = controller.Ticket(thumbPrint, apiDocInfo.file_name);
                        if (ticket != null)
                        {                            
                            string body = Utils.Base64Encode(ticket.body, "windows-1251");
                            string sign = controller.Sign(thumbPrint, body);
                            if (controller.confirmEvent(e, body, sign))
                            {
                                /*
                                    saving incoming ticket
                                 */
                                saveTicket(docSettings.LocalPath, apiDocInfo.file_name +".xml", Utils.Base64DecodeToBytes(apiDocInfo.file_body, "windows-1251"));
                                saveTicket(docSettings.LocalPath, apiDocInfo.file_name + ".bin", Utils.StringToBytes(apiDocInfo.sign_body, "UTF-8"));
                                /*
                                    saving outgoing ticket
                                 */
                                saveTicket(docSettings.TicketPath, ticket.fileName, ticket.body);
                                saveTicket(docSettings.TicketPath, ticket.fileName.Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8")); 
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Logger.log(ex.Message);
            }
        }
        public void processInbound()
        {
            List<string> inbound;
            try
            {
                inbound = controller.getList();
                foreach (string name in inbound)
                {
                    byte[] docBody = controller.getDoc(name);
                    if (docBody != null)
                        if (saveDoc(name, docBody))
                            if(conf.Inbound.IsArchive)
                                if (controller.archiveDoc(name))
                                    Logger.log(name + " removed from server .");
                }              
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);                
            }
        }
        public void processOutbound()
        {
            string[] outbound;
            try
            {
                outbound = Directory.GetFiles(conf.Outbound.DefaultPath);
                foreach (string name in outbound)
                {
                    string docType = GetDocType(Path.GetFileName(name));                    
                    Document docSettings = conf.GetCustomOutboundSettings(docType);
                    if (docSettings != null)
                        if (docSettings.NeedToBeSigned) // for signed docs
                        {
                            string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "windows-1251");
                            string sign = controller.Sign(thumbPrint, body);                  
                            if (controller.sendDocApi(body, sign, docType))
                                Logger.log(Path.GetFileName(name) + " sent successfully.");
                            if (conf.Outbound.IsArchive)
                            {
                                if(moveDocToArc(Path.GetFileName(name), File.ReadAllBytes(name)))
                                    File.Delete(name);
                                if(moveDocToArc(Path.GetFileName(name).Replace(".xml", ".bin"), Utils.StringToBytes(sign, "UTF-8")))
                                    File.Delete(name.Replace(".xml", ".bin"));
                            }
                            
                        }
                        else // for simple docs
                        {
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "UTF-8");
                            if (controller.sendDoc(Path.GetFileName(name), body))
                                Logger.log(Path.GetFileName(name) + " sent successfully.");
                            if (conf.Outbound.IsArchive)
                            {                                    
                                if(moveDocToArc(Path.GetFileName(name), (File.ReadAllBytes(name))))
                                    File.Delete(name);
                            }
                            
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
            }
        }
        private bool saveDoc(string fileName,byte[]body)
        {
            try
            {
                string docType = GetDocType(fileName);
                Document docSettings = conf.GetCustomInboundSettings(docType);
                StringBuilder sb = new StringBuilder(conf.Inbound.DefaultPath);
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
        private bool moveDocToArc(string fileName, byte[] body)
        {
            try
            {
                string docType = GetDocType(fileName);
                Document docSettings = conf.GetCustomOutboundSettings(docType);
                StringBuilder sb = new StringBuilder(conf.Outbound.DefaultArchive);
                /*if (docSettings != null)
                {
                    foreach (string path in docSettings.LocalPath)
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        File.WriteAllBytes(path + fileName, body);
                        Logger.log(fileName + " moved to " + path);
                    }
                }
                else
                {
                    if (conf.Outbound.SubFolders)
                        sb.Append(docType).Append("\\");
                    if (!Directory.Exists(sb.ToString()))
                        Directory.CreateDirectory(sb.ToString());
                    File.WriteAllBytes(sb.ToString() + fileName, body);
                    Logger.log(fileName + " moved to " + sb.ToString());
                }*/
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

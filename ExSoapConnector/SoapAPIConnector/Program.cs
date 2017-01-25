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

        public Program(Configuration conf)
        {
            this.conf = conf;
            this.controller= new Controller(conf);
            // IN
            //processInbound();
            // OUT
            processOutbound();

            // testing crypto etc.
            //testCrypto();
            // testing tickets etc.
            //testTickets();
        }

        public void testCrypto()
        {
            foreach (ExCert cert in controller.GetCertificates())
                Console.WriteLine(cert.ToString());
        }
        public void testTickets()
        {            
        }






        public void processInbound()
        {
            List<string> inbound;
            try
            {
                inbound = controller.getList();
                foreach (string name in inbound)
                {
                    saveDoc(name, controller.getDoc(name));
                    /*
                     checking if we need to create ticket
                     */
                    string docType = GetDocType(name);
                    Document docSettings = conf.GetCustomInboundSettings(docType);
                    if(docSettings!=null)
                    if (docSettings.TicketsGenerate)
                    {
                            if (name.EndsWith(".xml"))
                            {
                                string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                                var ticket = controller.Ticket(thumbPrint, name);
                                if(ticket!=null)
                                    saveTicket(docSettings.TicketPath, ticket.First().Key, ticket.First().Value);
                            }                     
                    }
                }              
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.StackTrace);//debug only
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
                        if (docSettings.NeedToBeSigned)
                        {
                            string thumbPrint = docSettings.Thumpprint != null ? docSettings.Thumpprint : conf.Thumpprint;
                            string body = Utils.Base64Encode(File.ReadAllBytes(name), "windows-1251");
                            string sign = controller.Sign(thumbPrint, body);
                            controller.sendDoc(Path.GetFileName(name), body);                            
                            controller.sendDoc(Path.GetFileName(name).Replace(".xml",".bin"), sign);
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
            }
        }
        private void saveDoc(string fileName,byte[]body)
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
                    Logger.log(fileName+" saved in "+ path);
                }
            }
            else
            {
                if(conf.Inbound.SubFolders)
                    sb.Append(docType).Append("\\");
                if (!Directory.Exists(sb.ToString()))
                    Directory.CreateDirectory(sb.ToString());
                File.WriteAllBytes(sb.ToString() + fileName, body);
                Logger.log(fileName + " saved in " + sb.ToString());
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
            Configuration conf = GetAppConfiguration(args[0]);
            Logger.loadConfig(conf);
            Logger.log("start");
            new Program(conf);
            Logger.log("end");            
        }
        public static Configuration GetAppConfiguration(string appArg)
        {
            string path = Path.GetFullPath(appArg);
            byte[] xml = File.ReadAllBytes(path);
            return Utils.FromXml<Configuration>(Encoding.GetEncoding("UTF-8").GetString(xml));
        }
    }
}

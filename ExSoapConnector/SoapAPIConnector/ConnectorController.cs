using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using APICon.conf;
using APICon.soap;
using APICon.Util;
using APICon.rest;
using APICon.logger;
using SoapAPIConnector;

namespace APICon.controller
{    

    public static class Controller
    {          
        public static void init()
        {
            Logger.loadConfig();
            RestHelper.authorize(Program.conf.Login, Program.conf.Api_pass);
            Soap.Authorize(Program.conf.Login, Program.conf.Soap_pass);            
        }

        public static void ShowUsage()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine("\t-allcerts\t-\tshow all certificates info");            
            Console.WriteLine("\t-testcert\t-\ttesting sign methods for 2nd arg certificate by thumbprint");
            Console.WriteLine("\t-testrest\t-\ttesting http connection to web-services");
            Console.WriteLine("\t-testsoap\t-\ttesting http connection to soap-services");
        }

        public static void CertsList()
        {
            try
            {
                ExCert[] certs = GetCertificates();
                Console.WriteLine("CSP Store has [" + certs.Length + "] certificates");
                foreach (ExCert cert in certs)
                {
                    Console.WriteLine("cert info :");
                    Console.WriteLine(cert.ToString());                    
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public static void TestCertificate(string thumbPrint)
        {
            byte[] sign = null;
            byte[] content = Encoding.GetEncoding("UTF-8").GetBytes("somedata");
            try
            {
                sign = getSign(thumbPrint, content, "UTF-8");
                if (sign != null)
                    Console.WriteLine("signing O.K.");
            }
            catch (Exception e)
            {
                Console.WriteLine("signing fails: " + e.Message);
            }
        }

        public static void TestRestConnection()
        {
            RestHelper.authorize(Program.conf.Login, Program.conf.Api_pass);
            Console.WriteLine("rest O.K.");
            
        }

        public static void TestSoapConnection()
        {
            Soap.Authorize(Program.conf.Login, Program.conf.Soap_pass);
            Console.WriteLine("soap O.K.");
        }

        public static void archiveDoc(string name)
        {
            Soap.archiveDoc(name);
            Logger.log(name + " removed from server");
        }

        public static void archiveDocByID(string id)
        {
            string fileName = getFileNameByDocGUID(id);
            if (fileName == null)
                throw new Exception("No such ID was found [" + id + "]");
            archiveDoc(fileName);
        }

        private static string getFileNameByDocGUID(string docGUID)
        {
            foreach (string name in getList())
                if (name.Contains(docGUID))
                    return name;
            return null;
        }

        public static string[] GetCertificateNames()
        {
            throw new NotImplementedException();
        }

        public static byte[] getDoc(string name)
        {
            return Soap.getDoc(name);
        }
        public static byte[] getBinForDoc(string fileName)
        {
            return getDoc(fileName.Replace(".xml", ".bin"));
        }
        public static void archiveBinForDoc(string fileName)
        {
            archiveDoc(fileName.Replace(".xml", ".bin"));
        }
        public static void archiveDocAndSign(string fileName)
        {
            archiveDoc(fileName);
            archiveBinForDoc(fileName);
        }
        public static void archiveDFSFile(ExDFSFile file)
        {
            archiveDocAndSign(file.fileName);
        }

        public static ExCert GetExCertificate(string thumbprint)
        {
            return new ExCert(GetCertByThumbprint(thumbprint));
        }

        public static List<string> getList()
        {
            return Soap.getList().ToList();
        }

        public static List<string> getList(string filter)
        {
            List<string> list = new List<string>();
            foreach (string file in getList())
            {
                if (file.StartsWith(filter))
                {
                    list.Add(file);
                }
            }
            return list;
        }
        public static List<string> getList(string filter, string ending)
        {
            List<string> list = new List<string>();
            foreach (string file in getList(filter))
            {
                if (file.EndsWith(ending))
                {
                    list.Add(file);
                }
            }
            return list;
        }
        public static List<string> getList(List<string> filters)
        {
            List<string> list = new List<string>();
            foreach (string filter in filters)
            {
                list.AddRange(getList(filter));
            }
            return list;
        }

        public static List<string> getList(List<string> filters, string[] endings)
        {
            List<string> list = new List<string>();
            foreach (string ending in endings)
            {
                list.AddRange(getList(filters, ending));
            }
            return list;
        }

        public static List<string> getList(List<string> filters, string ending)
        {
            List<string> list = new List<string>();
            foreach (string file in getList(filters))
            {
                if (file.EndsWith(ending))
                {
                    list.Add(file);
                }
            }
            return list;
        }

        public static byte[] getSign(string thumbprint, byte[] content, string encoding)
        {
            string baseSign = getSign(thumbprint, Utils.Base64Encode(content, encoding));
            return Utils.Base64DecodeToBytes(baseSign, encoding);
        }

        public static string getSign(string thumbprint, string base64data)
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                CAPICOM.Certificate cert = GetCertByThumbprint(thumbprint);
                CAdESCOM.CPSigner signer = new CAdESCOM.CPSigner();
                signer.Certificate = cert;
                signer.TSAAddress = "http://cryptopro.ru/tsp/";
                CAdESCOM.CadesSignedData signedData = new CAdESCOM.CadesSignedData();
                signedData.ContentEncoding = CAdESCOM.CADESCOM_CONTENT_ENCODING_TYPE.CADESCOM_BASE64_TO_BINARY;
                signedData.Content = base64data;
                return signedData.SignCades(signer, CAdESCOM.CADESCOM_CADES_TYPE.CADESCOM_CADES_BES, true);
            }
            finally
            {
                store.Close();
            }
        }

        private static CAPICOM.Certificate GetCertByThumbprint(string thumbprint)
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                foreach (CAPICOM.Certificate cert in store.Certificates)
                {
                    if (cert.Thumbprint.Equals(thumbprint))
                    {
                        if (cert.HasPrivateKey())
                        {
                            return cert;
                        }
                        throw new Exception("Certificate [" + thumbprint + "] has no private key");
                    }
                }
                throw new Exception("No certificate was found by thumbprint [" + thumbprint + "]");
            }
            finally
            {
                store.Close();
            }            
        }

        public static Ticket getTicket(string thumbprint, string uuid)
        {
            ExSigner signer = getSignerForTicket(thumbprint);
            byte[] body = RestHelper.getTicket(signer, uuid);
            byte[] sign = getSign(thumbprint, body, "windows-1251");
            string xmlString = Utils.BytesToString(body, "windows-1251");
            string name = Utils.GetTextFromXml(xmlString, "/Файл[@*]/@ИдФайл", ".xml");
            return new Ticket(name, body, sign);
        }
        private static ExSigner getSignerForTicket(string thumbprint)
        {
            CAPICOM.Certificate cert = GetCertByThumbprint(thumbprint);
            ExCert exCert = GetExCertificate(thumbprint);
            return new ExSigner(exCert);
        }
        public static ExCert GetExCertificate(CAPICOM.Certificate cert)
        {
            return new ExCert(cert);
        }
        public static Ticket getTicket(ExDFSFile file)
        {
            string thumbprint = file.settings.Thumpprint != null ? file.settings.Thumpprint : Program.conf.Thumpprint;
            Ticket ticket =  getTicket(thumbprint, getUUID(file.fileName));
            ticket.sign = getSign(thumbprint, ticket.body, "windows-1251");
            return ticket;
        }
        private static string getUUID(string fileName)
        {
            return fileName.Split('_')[5].Split('.')[0];
        }
        

        public static void sendDoc(string fileName, byte[] body)
        {
            Soap.sendDoc(fileName, body);
            Logger.log(fileName + " sent");
        }

        public static ExDFSFile setBodyFromApi(ExDFSFile file)
        {
            string uuid = getUUID(file.fileName);
            GetContentResponse content = RestHelper.GetContentResponse(uuid);
            byte[] body = Convert.FromBase64String(content.body);
            byte[] sign = Utils.StringToBytes(content.sign, "windows-1251");
            file.body = body;
            file.sign = sign;
            return file;
        }

        public static ExDFSFile setZipBody(ExDFSFile file)
        {
            file.zipBody = getDoc(file.fileName);
            return file;
        }

        public static void sendDoc(string fileName, string base64data)
        {
            sendDoc(fileName, Convert.FromBase64String(base64data));
        }

        public static void sendDocApi(string content, string sign, string docType)
        {
            RestHelper.send(content, sign, docType);
        }

        public static void sendTicket(ExDFSFile file)
        {            
            sendTicket(file.ticket.body, file.ticket.sign, getUUID(file.fileName));
            Logger.log(file.fileName + " confirmed");
        }
        public static void sendTicket(byte[] content, byte[] sign, string docId)
        {
            RestHelper.sendTicket(content, sign, docId);
        }

        public static string getUPDBase64body(string varDocGuid)
        {
            return RestHelper.getUPDDocumentContent(varDocGuid).body;
        }

        public static void sendTicket(string base64content, string base64sign, string docId)
        {
            throw new NotImplementedException();
        }

        public static ExCert[] GetCertificates()
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                CAPICOM.ICertificates icerts = store.Certificates;
                ExCert[] certs = new ExCert[icerts.Count];
                int i = 0;
                foreach (CAPICOM.Certificate cert in store.Certificates)
                {
                    certs[i++] = new ExCert(cert);
                }
                return certs;
            }
            finally
            {
                store.Close();
            }
        }
    }

    public class Ticket
    {
        public string fileName { get; set; }
        public byte[] body { get; set; }
        public byte[] sign { get; set; }

        public Ticket() { }
        public Ticket(string fileName, byte[] body)
        {
            this.fileName = fileName;
            this.body = body;
        }
        public Ticket(string fileName, byte[] body, byte[] sign)
        {
            this.fileName = fileName;
            this.body = body;
            this.sign = sign;
        }
    }
}

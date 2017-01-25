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

namespace APICon.controller
{
    public interface IController
    {
        List<string> getList();
        bool sendDoc(string fileName, string base64data);
        bool archiveDoc(string name);
        byte[] getDoc(string name);
        Dictionary<string,byte[]> Ticket(string thumbprint,string uuid);
        string Sign(string thumbprint,string base64data);
        ExCert GetExCertificate(string thumbprint);
        string[] GetCertificateNames();        
    }

    public class Controller : IController
    {
        private Configuration conf;
        private string authToken;
        public Controller(Configuration conf)
        {
            this.conf = conf;
            this.authToken= authorize();
        }

        public bool archiveDoc(string name)
        {
            ArchiveDocRequest req = new ArchiveDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = name;
            ArchiveDocResponse resp = (ArchiveDocResponse)Soap.ArchiveDoc<ArchiveDocResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);
            return resp.errorCode == 0;
        }
        public byte[] getDoc(string name)
        {
            GetDocRequest req = new GetDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = name;
            GetDocResponse resp = (GetDocResponse)Soap.GetDoc<GetDocResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);
            return Utils.Base64DecodeToBytes(resp.content,"UTF-8");
        }
        public List<string> getList()
        {
            GetListRequest req = new GetListRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            GetListResponse resp = (GetListResponse)Soap.GetList<GetListResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);
            return resp.list;
        }
        public bool sendDoc(string fileName, string base64data)
        {
            SendDocRequest req = new SendDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = fileName;
            req.content = base64data;
            SendDocResponse resp = (SendDocResponse)Soap.SendDoc<SendDocResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);
            return resp.errorCode == 0;
        }        
        public string Sign(string thumbprint, string base64data)
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                CAPICOM.Certificate cert = GetCertByThumbprint(store, thumbprint);
                CAdESCOM.CPSigner signer = new CAdESCOM.CPSigner();
                signer.Certificate = cert;
                signer.TSAAddress = "http://cryptopro.ru/tsp/";               
                CAdESCOM.CadesSignedData signedData = new CAdESCOM.CadesSignedData();
                signedData.ContentEncoding = CAdESCOM.CADESCOM_CONTENT_ENCODING_TYPE.CADESCOM_BASE64_TO_BINARY;
                signedData.Content = base64data;
                try
                {
                    return signedData.SignCades(signer, CAdESCOM.CADESCOM_CADES_TYPE.CADESCOM_CADES_BES, true);
                }
                catch (Exception e)
                {
                    throw new Exception("Sign error", e);
                }
            }
            finally
            {
                store.Close();
            }
        }
        public Dictionary<string, byte[]> Ticket(string thumbprint, string fileName)
        {
            string uuid = fileName.Split('_')[5].Replace(".xml","");
            ExCert cert = GetExCertificate(thumbprint);
            ExSigner signer = new ExSigner(cert);
            CreateTicketResponse response = (CreateTicketResponse)Http.post<CreateTicketResponse>("https://api-service.edi.su/Api/Dixy/Ticket/Generate", new CreateTicketRequest(authToken, uuid, signer));
            if (response.intCode != 200)
            {
                Logger.log("for file ["+ fileName+"] :" + response.varMessage);
                return null;
            }
            var map = new Dictionary<string, byte[]>();
            string name=GetIDFileFromTicket(response.content);            
            byte[] body= Utils.Base64DecodeToBytes(response.content, "windows-1251");
            map.Add(name,body);
            return map;
        }
        /**/
        private string GetIDFileFromTicket(string ticketContent)
        {
            string xmlString = Utils.Base64Decode(ticketContent, "windows-1251");
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlString);
            return xml.SelectSingleNode("/Файл[@*]/@ИдФайл").InnerText+".xml";
        }
        private string authorize()
        {
            string login = conf.Login;
            string password = conf.Api_pass;
            AuthorizeResponse response = (AuthorizeResponse)Http.post<AuthorizeResponse>("https://api-service.edi.su/Api/Dixy/Index/Authorize", new AuthorizeRequest(login, password));
            return response.varToken;
        }
        /**/
        public ExCert GetExCertificate(string thumbprint)
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
                    if (cert.Thumbprint.Equals(thumbprint) && cert.HasPrivateKey()) return new ExCert(cert);
                }
                throw new Exception("No certificate was found by thumbprint ["+ thumbprint + "]");
            }
            finally
            {
                store.Close();
            }
        }
        private CAPICOM.Certificate GetCertByThumbprint(CAdESCOM.CPStore store, string thumbprint)
        {
            foreach (CAPICOM.Certificate cert in store.Certificates)
            {
                if (cert.Thumbprint.Equals(thumbprint) && cert.HasPrivateKey()) return cert;
            }
            throw new Exception("Certificate not found");
        }
        public string[] GetCertificateNames()
        {
            ExCert[] certs = GetCertificates();
            string[] result = new string[certs.Length];
            for (int i = 0; i < certs.Length; i++)
            {
                result[i] = certs[i].Name;
            }
            return result;
        }
        public ExCert[] GetCertificates()
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
}

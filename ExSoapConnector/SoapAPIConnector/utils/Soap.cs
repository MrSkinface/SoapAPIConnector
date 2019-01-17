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
using SoapAPIConnector.ServiceReference;
using System.ServiceModel;
using APICon.logger;

namespace APICon.soap
{

    public static class Soap
    {
        private static ediLogin soapAuth;

        public static void Authorize(string login, string pass)
        {
            ExiteWsClient client = configureClient();
            ediLogin user = new ediLogin();
            user.login = login;
            user.pass = Utils.GetMD5String(pass);
            getListRequest req = new getListRequest();
            req.user = user;
            getListResponse response = client.getList(req);            
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);
            soapAuth = user;
        }
        public static string[] getList()
        {
            ExiteWsClient client = configureClient();
            getListRequest req = new getListRequest();
            req.user = soapAuth;
            getListResponse response = client.getList(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);
            if (response.result.list == null)
                return new string[0];
            return response.result.list;
        }        
        public static byte[] getDoc(string fileName)
        {
            ExiteWsClient client = configureClient();
            getDocRequest req = new getDocRequest();
            req.user = soapAuth;
            req.fileName = fileName;
            getDocResponse response = client.getDoc(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);
            return response.result.content;
        }

        public static byte[] getDocuments(string[] fileName)
        {
            ExiteWsClient client = configureClient();
            getDocumentsRequest req = new getDocumentsRequest();
            req.user = soapAuth;
            req.fileName = fileName;
            getDocumentsResponse response = client.getDocuments(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);
            return response.result.content;
        }

        public static void archiveDoc(string fileName)
        {
            ExiteWsClient client = configureClient();
            archiveDocRequest req = new archiveDocRequest();
            req.user = soapAuth;
            req.fileName = fileName;
            archiveDocResponse response = client.archiveDoc(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);            
        }

        public static void archiveDocuments(string[] fileName)
        {
            ExiteWsClient client = configureClient();
            archiveDocumentsRequest req = new archiveDocumentsRequest();
            req.user = soapAuth;
            req.fileName = fileName;
            archiveDocumentsResponse response = client.archiveDocuments(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);
        }

        public static void sendDoc(string fileName, byte[] docBody)
        {
            ExiteWsClient client = configureClient();            
            sendDocRequest req = new sendDocRequest();
            req.user = soapAuth;
            req.fileName = fileName;
            req.content = docBody;
            sendDocResponse response = client.sendDoc(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);            
        }

        public static void uploadDoc(string fileName, byte[] docBody, string remoteFolder)
        {
            ExiteWsClient client = configureClient();
            uploadDocRequest req = new uploadDocRequest();
            req.user = soapAuth;
            req.fileName = fileName;
            req.content = docBody;
            req.remoteFolder = remoteFolder;
            uploadDocResponse response = client.uploadDoc(req);
            if (response.result == null || response.result.errorCode != 0)
                throw new Exception(response.result.errorMessage);
        }

        private static ExiteWsClient configureClient()
        {
            try
            {
                BasicHttpBinding binding = new BasicHttpBinding();
                if (Program.conf.IsSecure())
                {
                    binding.Security.Mode = BasicHttpSecurityMode.Transport;
                    binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                }
                binding.MaxReceivedMessageSize = Int32.MaxValue;
                EndpointAddress endpoint = new EndpointAddress(Program.conf.getTransportSchema() + "://" + Program.conf.getSoapEndpoint() + "/soap/exite.wsdl");
                ExiteWsClient client = new ExiteWsClient(binding, endpoint);
                return client;
            }
            catch (Exception e)
            {                
                Console.WriteLine(e.StackTrace);                
                throw e;
            }
        }
    }
}

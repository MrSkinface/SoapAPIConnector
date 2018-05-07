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
using SoapAPIConnector.ExiteSoapService;
using System.ServiceModel;
using APICon.logger;

namespace APICon.soap
{

    public static class Soap
    {
        private static ediLogin soapAuth;

        public static void Authorize(string login, string pass)
        {
            EdiServerClient client = configureClient();
            ediLogin user = new ediLogin();
            user.login = login;
            user.pass = Utils.GetMD5String(pass);
            ediFileList response = client.getList(user);
            if (response == null || response.errorCode != 0)
                throw new Exception(response.errorMessage);
            soapAuth = user;
        }
        public static string[] getList()
        {
            EdiServerClient client = configureClient();            
            ediFileList response = client.getList(soapAuth);
            if (response == null || response.errorCode != 0)
                throw new Exception(response.errorMessage);
            if (response.list == null)
                return new string[0];
            return response.list;
        }        
        public static byte[] getDoc(string fileName)
        {
            EdiServerClient client = configureClient();            
            ediFile response = client.getDoc(soapAuth, fileName);
            if (response == null || response.errorCode != 0)
                throw new Exception(response.errorMessage);
            return response.content;
        }
        
        public static void archiveDoc(string fileName)
        {
            EdiServerClient client = configureClient();            
            ediResponse response = client.archiveDoc(soapAuth, fileName);
            if (response == null || response.errorCode != 0)
                throw new Exception(response.errorMessage);            
        }       

        public static void sendDoc(string fileName, byte[] docBody)
        {
            EdiServerClient client = configureClient();            
            ediResponse response = client.sendDoc(soapAuth, fileName, docBody);
            if (response == null || response.errorCode != 0)
                throw new Exception(response.errorMessage);            
        }

        private static EdiServerClient configureClient()
        {
            try
            {
                BasicHttpBinding binding = new BasicHttpBinding();
                binding.Security.Mode = BasicHttpSecurityMode.Transport;
                binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
                binding.MaxReceivedMessageSize = Int32.MaxValue;
                /*EndpointAddress endpoint = new EndpointAddress("https://ru-soap.edi.su/soap/?wsdl");*/
                EndpointAddress endpoint = new EndpointAddress("https://soap.e-vo.ru/soap/?wsdl");
                EdiServerClient client = new EdiServerClient(binding, endpoint);
                return client;
            }
            catch (Exception e)
            {                
                Logger.log(e.StackTrace);
                throw e;
            }
        }
    }
}

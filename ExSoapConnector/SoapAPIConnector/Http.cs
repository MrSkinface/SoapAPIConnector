using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APICon.Util;
using System.Net;
using System.Security.Authentication;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using SoapAPIConnector;

namespace APICon.rest
{
    public static class Http
    {   
        public static object post<Type>(string url, object data)
        {
            return post<Type>(url, Utils.ToJson(data));
        }
        public static object post<Type>(string url, string data)
        {
            //Console.WriteLine(data);//debug only
            return post<Type>(url, Encoding.GetEncoding("UTF-8").GetBytes(data));
        }
        public static object post<Type>(string url, byte[] data)
        {            
            HttpWebRequest post = (HttpWebRequest)WebRequest.Create(url);
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, error) => true;
            
            if (Program.conf.proxy != null)
                if (Program.conf.proxy.Enable)
                {                
                    WebProxy proxy = new WebProxy();               
                    Uri proxyUri = new Uri(Program.conf.proxy.address);
                    proxy.Credentials = new NetworkCredential(Program.conf.proxy.login, Program.conf.proxy.password);
                    post.Proxy = proxy;
                }
            /*post.KeepAlive = false;
            post.ProtocolVersion = HttpVersion.Version10;*/
            post.Method = "POST";
            post.ContentType = "application/json; charset=utf-8";
            post.ContentLength = data.Length;
            using (Stream reqStream = post.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
                WebResponse response=null;
                try
                {
                    try
                    {
                        response = post.GetResponse();
                    }
                    catch (WebException e)
                    {
                        Console.WriteLine(e.StackTrace);
                        response = e.Response;
                    }
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    string responseFromServer = reader.ReadToEnd();
                    //Console.WriteLine(responseFromServer); //debug only          
                    return Utils.FromJson<Type>(responseFromServer);
                }
                finally
                {
                    reqStream.Close();
                    response.Close();
                }
            }            
        }
    }
    public class MyPolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true;
        }
    }
}

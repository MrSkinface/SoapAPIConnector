using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APICon.Util;
using System.Net;
using System.IO;

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
            HttpWebRequest post = WebRequest.CreateHttp(url);
            post.Method = "POST";
            post.ContentType = "application/json; charset=UTF-8";
            post.ContentLength = data.Length;
            post.GetRequestStream().Write(data, 0, data.Length);
            WebResponse response;
            try
            {
                response = post.GetResponse();
            }
            catch (WebException e)
            {                
                response = e.Response;
            }
            StreamReader reader = new StreamReader(response.GetResponseStream());            
            string responseFromServer = reader.ReadToEnd();
            //Console.WriteLine(responseFromServer); //debug only          
            return Utils.FromJson<Type>(responseFromServer);
        }
    }
}

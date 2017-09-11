using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.Util;
using APICon.logger;

namespace APICon.rest
{
    public static class Http2
    {
        public static object post<Type>(string url, object data)
        {
            var client = new RestClient(url);            
            var request = new RestRequest(Method.POST);
            request.AddJsonBody(data);               
            IRestResponse response = client.Execute(request);
            var content = response.Content;
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.log(
                    "ERROR: Server responded with: \nStatusCode [" + response.StatusCode + "]" +
                    "\nStatusDescription [" + response.StatusDescription + "]." +
                    "\nResponseUri [" + response.ResponseUri + "]." +
                    "\nContentLength [" + response.ContentLength + "]." +
                    "\nDebug: \nrequest body:\n" +
                    Utils.ToJson(data) +
                    "\nresponse body:\n" +
                    content
                    );
                //return null;
            }            
            return Utils.FromJson<Type>(content);                    
        }
    }
}

using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.Util;
using System.Net;
using APICon.logger;
using SoapAPIConnector;

namespace APICon.rest
{
    public static class Http
    {
        private static string url = Program.conf.getTransportSchema() + "://" + Program.conf.getRestEndpoint() + "/Api/V1/Edo/";

        public static object post<Type>(string url, object data)
        {
            var client = new RestClient(Http.url + url);
            var request = new RestRequest(Method.POST);
            request.AddJsonBody(data);
            IRestResponse response = client.Execute(request);
            var content = response.Content;
            debug(data,response);           
            return Utils.FromJson<Type>(content);                    
        }

        private static void debug(object jsonRequest, IRestResponse response)
        {
            if (Program.conf.debug)
            {
                Console.WriteLine(
                        "ERROR: Server responded with: \nStatusCode [" + response.StatusCode + "]" +
                        "\nStatusDescription [" + response.StatusDescription + "]." +
                        "\nResponseUri [" + response.ResponseUri + "]." +
                        "\nContentLength [" + response.ContentLength + "]." +
                        "\nDebug: \nrequest body:\n" +
                        Utils.ToJson(jsonRequest) +
                        "\nresponse body:\n" +
                        response.Content
                        );
            }
        }
    }
}

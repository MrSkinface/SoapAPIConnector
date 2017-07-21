using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.Util;

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
            Console.WriteLine(response.StatusDescription);
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.Server);
            Console.WriteLine(response.ResponseUri);
            Console.WriteLine(response.ResponseStatus);
            Console.WriteLine(response.RawBytes);
            Console.WriteLine(response.ErrorMessage);
            Console.WriteLine(response.ContentType);
            Console.WriteLine(response.ContentLength);
            Console.WriteLine(response.ContentEncoding);
            Console.WriteLine(response.Content);
            return Utils.FromJson<Type>(content);            
        }
    }
}

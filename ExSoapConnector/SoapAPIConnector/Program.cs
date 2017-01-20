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

namespace SoapAPIConnector
{
    class Program
    {
        static void Main(string[] args)
        {
            // testing conf loading

            /*string path = Path.GetFullPath(args[0]);
            Console.WriteLine(path);            
            byte[] xml = File.ReadAllBytes(path);            
            Configuration conf = Utils.FromXml<Configuration>(Encoding.GetEncoding("UTF-8").GetString(xml));
            Console.WriteLine(conf.ToString());*/

            // testing soap

            string login = "testru";
            string pass = "2c5348cadc34783815fa1cc8f5cebf67";
            GetListRequest req = new GetListRequest();
            req.user = new User();
            req.user.login = login;
            req.user.pass = pass;

            //Console.WriteLine(Utils.ToXml<GetListRequest>(req, "UTF-8"));

            GetListResponse resp=(GetListResponse)Soap.GetList<GetListResponse>(req);
            Console.WriteLine(resp.errorCode);
            foreach(string name in resp.list)
                Console.WriteLine(name);

           
        }
    }
}

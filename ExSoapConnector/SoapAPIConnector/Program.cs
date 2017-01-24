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
using System.Security.Cryptography;
using APICon.controller;

namespace SoapAPIConnector
{
    class Program
    {
        static void Main(string[] args)
        {
            // testing conf loading
            
            string path = Path.GetFullPath(args[0]);                      
            byte[] xml = File.ReadAllBytes(path);            
            Configuration conf = Utils.FromXml<Configuration>(Encoding.GetEncoding("UTF-8").GetString(xml));            

            // testing controller

            Controller controller = new Controller(conf);
            List<string> list;
            try
            {
                list = controller.getList();
                foreach (string name in list)
                    Console.WriteLine(name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            

        }
    }
}

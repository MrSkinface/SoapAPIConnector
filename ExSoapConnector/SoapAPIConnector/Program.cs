using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using APICon.Util;
using APICon.conf;

namespace SoapAPIConnector
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = Path.GetFullPath(args[0]);
            Console.WriteLine(path);
            //Console.WriteLine(Directory.GetCurrentDirectory());
            byte[] xml = File.ReadAllBytes(path);

            //Console.WriteLine(Encoding.UTF8.GetString(xml));
            Configuration conf = Utils.FromXml<Configuration>(Encoding.GetEncoding("UTF-8").GetString(xml));
            Console.WriteLine(conf.ToString());
        }
    }
}

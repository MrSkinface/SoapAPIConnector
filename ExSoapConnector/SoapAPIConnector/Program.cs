using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SoapAPIConnector
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(Path.GetFullPath(args[0]));
            Console.WriteLine(Directory.GetCurrentDirectory());
        }
    }
}

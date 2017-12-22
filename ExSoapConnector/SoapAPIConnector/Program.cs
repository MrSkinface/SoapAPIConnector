using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using APICon.Util;
using APICon.conf;
using APICon.soap;
using APICon.controller;
using APICon.logger;
using APICon.rest;
using APICon.Condra;
using System.Xml;

namespace SoapAPIConnector
{
    public class Program
    {
        public static Configuration conf;           

        public Program(string[] args)
        {
            conf = DFSHelper.GetAppConfiguration(args[0]);
            if (args.Length > 1)
            {
                help(args);
            }
            else
            {
                Controller.init();
                new InboundWorker();
                new OutboundWorker();
                new TicketsWorker();
            }
            
        }

        public static void help(string[] args)
        {
            switch (args[1])
            {
                case "-allcerts":
                    Controller.CertsList();
                    break;
                case "-testcert":
                    if (args.Length < 3 || args[2] == null)
                    {
                        throw new Exception("thumbprint null");
                    }
                    Controller.TestCertificate(args[2]);
                    break;
                case "-testrest":
                    Controller.TestRestConnection();
                    break;
                case "-testsoap":
                    Controller.TestSoapConnection();
                    break;
            }
        }

        public static void Main(string[] args)
        {           
            if ((args.Length == 0) || (args[0] == null))
            {
                Controller.ShowUsage();
                Environment.Exit(1);
            }
            new Program(args);
        }

    }
}

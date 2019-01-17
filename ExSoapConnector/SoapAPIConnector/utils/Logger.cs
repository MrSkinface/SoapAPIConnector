using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using APICon.conf;
using SoapAPIConnector;

namespace APICon.logger
{
    public static class Logger
    {
        public static string logPath { get; set; }

        public static void loadConfig()
        {
            logPath = Program.conf.LogFile;
        }
        
        public static void log(string message)
        {
            StringBuilder sb = new StringBuilder(logPath);
            checkIfExist();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd")).Append("_log.txt");
            string textToLog = "["+DateTime.Now.ToString()+ "]" + message +"\n";
            File.AppendAllText(sb.ToString(), textToLog);
            Console.Write(textToLog);
        }
        public static void error(string message)
        {
            StringBuilder sb = new StringBuilder(logPath);
            checkIfExist();
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd")).Append("_error.txt");
            string textToLog = "[" + DateTime.Now.ToString() + "]" + message + "\n";
            File.AppendAllText(sb.ToString(), textToLog);
            Console.Write(textToLog);
        }
        public static void error(Exception e)
        {
            error("[ERROR] " + e.Message, e);
        }
        public static void error(string message, Exception e)
        {
            Console.WriteLine(e.StackTrace);
            log("[ERROR] " + message);
            error(e.StackTrace);
        }
        public static void checkIfExist()
        {
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);
        }
    }
}

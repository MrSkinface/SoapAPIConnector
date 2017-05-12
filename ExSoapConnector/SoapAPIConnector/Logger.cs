using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using APICon.conf;

namespace APICon.logger
{
    public static class Logger
    {
        public static string logPath { get; set; }

        public static void loadConfig(Configuration conf)
        {
            logPath = conf.LogFile;
        }
        public static void log(string message)
        {
            StringBuilder sb = new StringBuilder(logPath);
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd")).Append("_log.txt");
            string textToLog = "["+DateTime.Now.ToString()+ "]" + message +"\n";
            File.AppendAllText(sb.ToString(), textToLog);
            Console.Write(textToLog);
        }
        public static void error(string message)
        {
            StringBuilder sb = new StringBuilder(logPath);
            sb.Append(DateTime.Now.ToString("yyyy-MM-dd")).Append("_error.txt");
            string textToLog = "[" + DateTime.Now.ToString() + "]" + message + "\n";
            File.AppendAllText(sb.ToString(), textToLog);
            Console.Write(textToLog);
        }
    }
}

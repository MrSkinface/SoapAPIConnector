using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using APICon.conf;
using APICon.logger;
using SoapAPIConnector;

namespace APICon.Util
{
    public class DFSHelper
    {
        public static Configuration GetAppConfiguration(string appArg)
        {
            string path = Path.GetFullPath(appArg);
            byte[] xml = File.ReadAllBytes(path);
            return Utils.FromXml<Configuration>(Encoding.GetEncoding("UTF-8").GetString(xml));
        }
        public static string GetDocType(string fileName)
        {
            List<string> checkEDOList = new List<string>(new string[] { "ON", "DP" });
            var v = fileName.Split('_');
            if (checkEDOList.Contains(v[0]))
                return v[0] + "_" + v[1];
            return v[0];
        }
        public static string[] GetOutFiles(List<Document>doc)
        {
            List<String> res = new List<string>();
            foreach (Document confDoc in doc)
                foreach (string path in confDoc.LocalPath)
                {
                    if (confDoc.Doctype.Equals("CONDRA", StringComparison.OrdinalIgnoreCase))
                    {
                        res.AddRange(Directory.GetFiles(path,"condra_*.xml"));
                    }
                    else
                    {
                        res.AddRange(Directory.GetFiles(path));
                    }
                }
            return res.ToArray();
        }
        public static bool saveDoc(string fileName, byte[] body)
        {
            try
            {
                string docType = GetDocType(fileName);                              
                Document docSettings = Program.conf.GetCustomInboundSettings(docType);
                StringBuilder sb = new StringBuilder(Program.conf.Inbound.DefaultPath);                
                if (docSettings != null)
                {
                    foreach (string path in docSettings.LocalPath)
                    {
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);                        
                        if (docType.Equals("condra", StringComparison.OrdinalIgnoreCase))
                        {
                            return saveCondra(docSettings, fileName, body, true);
                        }
                        else
                        {
                            File.WriteAllBytes(path + overrideName(fileName,docSettings), body);
                            Logger.log(overrideName(fileName, docSettings) + " saved in " + path);
                        }
                    }
                }
                else
                {
                    if (Program.conf.Inbound.SubFolders)
                        sb.Append(docType).Append("\\");
                    if (!Directory.Exists(sb.ToString()))
                        Directory.CreateDirectory(sb.ToString());                    
                    File.WriteAllBytes(sb.ToString() + fileName, body);
                    Logger.log(fileName + " saved in " + sb.ToString());
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
                return false;
            }
        }
        private static bool saveCondra(Document docSettings, string fileName, byte[] zip, bool unzip)
        {
            Dictionary<string, byte[]> map = ZipHelper.unzip(zip);
            foreach (string path in docSettings.LocalPath)
            {
                foreach (string entry in map.Keys)
                {                   
                    string name = entry.Equals("condra.xml", StringComparison.OrdinalIgnoreCase) ? fileName.Replace(".zip",".xml") : overrideName(entry, docSettings);
                    File.WriteAllBytes(path + name, map[entry]);
                    Logger.log(name + " saved in " + path);
                }
            }
            return true;
        }
        public static string overrideName(string fileName, Document setting)
        {
            string res = fileName;
            if (setting.custom_sign_extension == null)
                return res;
            res = res.Replace(res.Split('.')[res.Split('.').Length - 1], setting.custom_sign_extension);
            return res;
        }
        public static void saveTicket(List<string> ticketPath, string fileName, byte[] body)
        {
            foreach (string path in ticketPath)
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                File.WriteAllBytes(path + fileName, body);
                Logger.log(fileName + " saved in " + path);
            }
        }
        public static bool moveDocToError(string fileName, byte[] body)
        {
            try
            {
                string docType = GetDocType(fileName);
                StringBuilder sb = new StringBuilder(Program.conf.Outbound.DefaultError);
                if (Program.conf.Outbound.SubFolders)
                    sb.Append(docType).Append("\\");
                if (!Directory.Exists(sb.ToString()))
                    Directory.CreateDirectory(sb.ToString());
                File.WriteAllBytes(sb.ToString() + fileName, body);
                Logger.log(fileName + " moved to " + sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
                return false;
            }
        }
        public static bool moveDocToArc(string fileName, byte[] body, Document doc)
        {
            if (doc.LocalArchive.Count != 0)
            {
                foreach (string path in doc.LocalArchive)
                {
                    StringBuilder sb = new StringBuilder(path);
                    if (!Directory.Exists(sb.ToString()))
                        Directory.CreateDirectory(sb.ToString());
                    try
                    {
                        File.WriteAllBytes(sb.Append(fileName).ToString(), body);
                        Logger.log(fileName + " moved to " + sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.StackTrace);//debug only
                        Logger.log(ex.Message);
                        return false;
                    }
                }
                return true;
            }
            else
                return moveDocToArc(fileName, body);
        }
        public static bool moveDocToArc(string fileName, byte[] body)
        {
            try
            {
                string docType = GetDocType(fileName);
                StringBuilder sb = new StringBuilder(Program.conf.Outbound.DefaultArchive);
                if (Program.conf.Outbound.SubFolders)
                    sb.Append(docType).Append("\\");
                if (!Directory.Exists(sb.ToString()))
                    Directory.CreateDirectory(sb.ToString());
                File.WriteAllBytes(sb.ToString() + fileName, body);
                Logger.log(fileName + " moved to " + sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);//debug only
                Logger.log(ex.Message);
                return false;
            }
        }
    }
}

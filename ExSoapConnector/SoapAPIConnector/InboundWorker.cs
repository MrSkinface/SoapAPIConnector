using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.logger;
using APICon.conf;
using APICon.controller;
using APICon.Util;

namespace SoapAPIConnector
{
    public class InboundWorker
    {
        public InboundWorker()
        {
            if (isEnabled())
            {
                run();
            }            
        }

        private void run()
        {            
            Logger.log("start processing inbound ...");
            List<string> inbound = Controller.getList();
            foreach (string fileName in inbound)
            {
                try
                {
                    /*Console.WriteLine("fileName: " + fileName);                    
                    Console.WriteLine("isDownload(fileName): " + isDownload(fileName));*/

                    if (isDownload(fileName))
                    {
                        byte[] body = Controller.getDoc(fileName);
                        DFSHelper.saveDoc(fileName, body);
                        if (isArchive())
                            Controller.archiveDoc(fileName);                     
                    }
                }
                catch (Exception e)
                {
                    Logger.log(e.Message);
                }
            }
            Logger.log("inbound processed");
        }

        private bool isEnabled()
        {
            if (Program.conf.Inbound != null)
                return Program.conf.Inbound.Enable;
            return false;
        }

        private List<Document> getDocsSetting()
        {
            return Program.conf.Inbound.Document;
        }

        private List<string> getDocTypes()
        {
            List<string> types = new List<string>();
            foreach (Document doc in getDocsSetting())
            {
                types.Add(doc.Doctype);
            }
            return types;
        }

        private bool isDownload(string fileName)
        {
            List<string> types = getDocTypes();
            if (Program.conf.Inbound.DownloadALL)
                return true;
            if(types.Contains(fileName.Split('_')[0]))
                return true;
            if (types.Contains(fileName.Split('_')[0] + "_" + fileName.Split('_')[1]))
                return true;
            return false;
        }

        private bool isArchive()
        {
            return Program.conf.Inbound.IsArchive;
        }
    }
}

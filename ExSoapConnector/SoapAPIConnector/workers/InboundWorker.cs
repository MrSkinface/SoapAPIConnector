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
            try
            {
                List<string> inbound = Controller.getList();
                List<string> filesToProcess = getFilesToBeProcessed(inbound);                
                Logger.log(filesToProcess.Count + " files found");
                if (filesToProcess.Count > 0)
                {
                    Dictionary<string, byte[]> files = Controller.getDocuments(filesToProcess);
                    foreach (string fileName in files.Keys)
                    {
                        DFSHelper.saveDoc(fileName, files[fileName]);
                    }
                    if (isArchive())
                    {
                        Controller.archiveDocuments(files.Keys.ToList());
                    }
                }                
            }
            catch (Exception e)
            {
                Logger.error(e);
            }
            Logger.log("inbound processed");            
        }

        private List<string> getFilesToBeProcessed(List<string> allInbound)
        {
            List<string> toProcess = new List<string>();
            foreach (string fileName in allInbound)
            {
                if (isDownload(fileName))
                {
                    toProcess.Add(fileName);
                }
            }
            return toProcess;
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

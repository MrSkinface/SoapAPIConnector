using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.logger;
using APICon.controller;
using APICon.conf;
using APICon.Util;

namespace SoapAPIConnector
{
    public class TicketsWorker
    {
        public TicketsWorker()
        {
            if (isEnabled())
            {
                run();
            }                
        }

        private void run()
        {
            Logger.log("start processing tickets ...");
            try
            {
                List<ExDFSFile> files = getFiles();
                foreach (ExDFSFile f in files)
                {                    
                    ExDFSFile file = f;
                    try
                    {
                        file = getBody(file);
                        file = f.settings.TicketsGenerate ? getTicket(f) : f;                        
                        if (file.ticket != null)
                            Controller.sendTicket(file);                        
                        Controller.archiveDFSFile(file);
                        DFSHelper.saveDFSFile(file);
                        DFSHelper.saveStatus(file);
                        if (needContainer(file))
                            ContainerController.performChainContainer(file);
                    }
                    catch (Exception e)
                    {
                        handleTicketsException(e,file);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.log(e.Message);
            }
            Logger.log("tickets processed");
        }

        private void handleTicketsException(Exception e, ExDFSFile file)
        {
            Console.WriteLine(e.StackTrace);
            Logger.log("while processing [" + file.fileName + "] " + e.Message);
            if (e.Message.Contains("Document already queued"))
                Controller.archiveDFSFile(file);
            if (e.Message.Contains("tickets only for"))
            {
                file.ticket = null;
                Controller.archiveDFSFile(file);
                DFSHelper.saveDFSFile(file);
            }
        }

        private bool isEnabled()
        {
            if (Program.conf.EDOTickets != null)
                return Program.conf.EDOTickets.Enable;
            return false;
        }

        private List<ExDFSFile> getFiles()
        {
            List<ExDFSFile> files = new List<ExDFSFile>();
            foreach (string fileName in Controller.getList(getDocTypes(), new string[] { ".xml", ".zip" }))
            {
                try
                {
                    Document setting = getSetting(fileName);                    
                    files.Add(new ExDFSFile(null, setting, fileName, null, null));
                }
                catch (Exception e)
                {                    
                    Logger.log("while getting [" + fileName + "] : " + e.Message);
                }
            }
            return files;
        }

        private List<Document> getDocsSetting()
        {
            return Program.conf.EDOTickets.Document;
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

        private Document getSetting(string fileName)
        {
            return Program.conf.GetCustomEDOTicketSettings(getFileType(fileName));
        }

        private string getFileType(string fileName)
        {
            string[] parts = fileName.Split('_');
            return parts[0] + "_" + parts[1];
        }

        private ExDFSFile getTicket(ExDFSFile file)
        {
            if (isNotSupportedTicket(file))
                throw new Exception("Auto-generated tickets only for [СЧФДОП] are allowed .");
            file.ticket = Controller.getTicket(file);
            return file;
        }

        private bool isNotSupportedTicket(ExDFSFile file)
        {
            if (file.fileName.StartsWith("ON_SCHFDOPPR") || file.fileName.StartsWith("ON_KORSCHFDOPPR"))
            {
                if (!Utils.GetTextFromXml(Utils.BytesToString(file.body, "windows-1251"), "Файл/Документ/@Функция").Equals("СЧФДОП")
                    &&
                    !Utils.GetTextFromXml(Utils.BytesToString(file.body, "windows-1251"), "Файл/Документ/@Функция").Equals("КСЧФДИС"))
                {
                    return true;
                }                
            }
            return false;
        }

        private bool needContainer(ExDFSFile file)
        {
            if (Program.conf.EDOTickets.chainContainer == null)
                return false;
            if (!Program.conf.EDOTickets.chainContainer.enable)
                return false;
            /*
            if one of 3 final tickets => perform check for conf and do chainContainer
            */
            string[] s = { "DP_IZVPOL", "ON_SCHFDOPPOK", "DP_UVUTOCH", "ON_KORSCHFDOPPOK" };
            return s.Contains(getFileType(file.fileName));
        }

        private bool needBody(Document setting)
        {
            if (setting.LocalPath != null && setting.LocalPath.Count > 0)
                return true;
            if (Program.conf.EDOTickets.DefaultPath != null)
                return true;
            return false;
        }

        private ExDFSFile getBody(ExDFSFile file)
        {
            ExDFSFile f = file;
            if (needBody(f.settings))
            {
                f = Controller.setBodyFromApi(f);
                if (f.fileName.EndsWith(".zip"))
                {
                    f = Controller.setZipBody(f);
                }
            }
            return f;
        }
    }
}

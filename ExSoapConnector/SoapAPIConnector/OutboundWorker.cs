using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using APICon.logger;
using APICon.Util;
using APICon.controller;
using APICon.conf;

namespace SoapAPIConnector
{
    public class OutboundWorker
    {
        public OutboundWorker()
        {
            if (isEnabled())
            {
                run();
            }
        }

        private void run()
        {
            Logger.log("start processing outbound ...");
            List<ExDFSFile> files = getFiles();
            try
            {
                foreach (ExDFSFile f in files)
                {
                    ExDFSFile file = f;
                    try
                    {                        
                        if (file.settings == null)
                        {
                            Controller.sendDoc(file.fileName, file.body);
                        }
                        else
                        {
                            if (file.settings.NeedToBeSigned)
                            {
                                string thumbprint = file.settings.Thumpprint != null ? file.settings.Thumpprint : Program.conf.Thumpprint;
                                file = signFile(file, thumbprint);
                            }

                            if (file.sign != null)
                            {
                                string baseBody = Utils.Base64Encode(file.body, "windows-1251");
                                string baseSign = Utils.Base64Encode(file.sign, "windows-1251");
                                string docType = file.settings.Doctype;
                                Controller.sendDocApi(baseBody, baseSign, docType);
                            }
                            else
                            {
                                Controller.sendDoc(file.fileName, file.body);
                            }
                        }
                        if (isArchive())
                            DFSHelper.moveDocToArc(file);
                        DFSHelper.saveStatus(file, null);
                    }
                    catch (Exception e)
                    {
                        handleSendException(e, file);                        
                    }
                }
            }
            catch (Exception e)
            {
                Logger.log(e.Message);
            }
            Logger.log("outbound processed");
        }

        private bool isEnabled()
        {
            if (Program.conf.Outbound != null)
                return Program.conf.Outbound.Enable;
            return false;
        }

        private bool isArchive()
        {
            return Program.conf.Outbound.IsArchive;
        }

        private List<ExDFSFile> getFiles()
        {
            List<ExDFSFile> files = new List<ExDFSFile>();
            if (Program.conf.Outbound.DefaultPath != null)
            {
                DFSHelper.checkIfExist(Program.conf.Outbound.DefaultPath);
                foreach (string path in Directory.GetFiles(Program.conf.Outbound.DefaultPath))
                {
                    string name = Path.GetFileName(path);
                    byte[] body = File.ReadAllBytes(path);

                    Document settings = getDocSettings(name, path);

                    files.Add(new ExDFSFile(path, settings, name, body, null));
                }
            }
            foreach (Document setting in Program.conf.Outbound.Document)
            {
                foreach (string path in setting.LocalPath)
                {
                    DFSHelper.checkIfExist(path);
                    string name = Path.GetFileName(path);
                    byte[] body = File.ReadAllBytes(path);

                    files.Add(new ExDFSFile(path, setting, name, body, null));
                }
            }
            return files;
        }

        private ExDFSFile signFile(ExDFSFile file, string thumbprint)
        {
            file.sign = Controller.getSign(thumbprint, file.body, "windows-1251");
            return file;
        }

        private Document getDocSettings(string fileName, string path)
        {
            string docType = DFSHelper.GetDocType(fileName);
            return Program.conf.GetCustomOutboundSettingsByPath(docType, path);
        }

        private void handleSendException(Exception e, ExDFSFile file)
        {
            if (e.Message.Equals("Not find successful Aperak for document invoice"))
            {
                Logger.log("file [" + file.fileName + "] will be waiting for next sending");
            }
            else if (e.Message.Contains("Not authorized"))
            {
                Logger.log("ERROR: " + e.Message + ", file [" + file.fileName + "] will be waiting for next sending");
            }
            else
            {
                DFSHelper.saveStatus(file, e.Message);                
                Logger.error("ERROR: " + e.Message + ", file [ " + file.fileName + " ]");
                DFSHelper.moveDocToError(file);
            }
        }
    }
}

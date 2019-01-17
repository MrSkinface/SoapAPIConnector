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
            HashSet<ExDFSFile> files = getFiles();
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
                                if (isSpecFolder(file.settings))
                                {
                                    Controller.uploadDoc(file.fileName, file.body, file.settings.remoteFolder);
                                }
                                else
                                {
                                    Controller.sendDoc(file.fileName, file.body);
                                }                                
                            }
                        }
                        if (isArchive())
                            DFSHelper.moveDocToArc(file);
                        DFSHelper.saveStatus(file, null);
                    }
                    catch (Exception e)
                    {
                        Logger.error(e);
                        handleSendException(e, file);                        
                    }
                }
            }
            catch (Exception e)
            {
                Logger.error(e);
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
            if (Program.conf.Outbound != null)
                return Program.conf.Outbound.IsArchive;
            return false;
        }

        private bool isSpecFolder(Document settings)
        {
            return settings != null && settings.remoteFolder != null && settings.remoteFolder.Length != 0;
        }

        private HashSet<ExDFSFile> getFiles()
        {
            HashSet<string> paths = new HashSet<string>();
            HashSet<ExDFSFile> files = new HashSet<ExDFSFile>();
            /*
            * getting default path files
            */
            if (Program.conf.Outbound.DefaultPath != null)
            {
                DFSHelper.checkIfExist(Program.conf.Outbound.DefaultPath);
                foreach (string path in Directory.GetFiles(Program.conf.Outbound.DefaultPath))
                {
                    paths.Add(path);                    
                }
            }
            /*
            * getting custom path files
            */
            foreach (Document setting in Program.conf.Outbound.Document)
            {
                foreach (string path in setting.LocalPath)
                {
                    DFSHelper.checkIfExist(path);
                    foreach (string docPath in Directory.GetFiles(path))
                    {
                        paths.Add(docPath);                       
                    }
                }
            }
            /*
            * getting ExDFSFiles from unique paths
            */
            foreach (string path in paths)
            {
                string name = Path.GetFileName(path);
                byte[] body = File.ReadAllBytes(path);
                Document settings = getDocSettings(name, path);
                files.Add(new ExDFSFile(path, settings, name, body, null));
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
            else if (e.Message.Contains("invalid entry CRC"))
            {
                Logger.log("ERROR: " + e.Message + ", file [" + file.fileName + "] will be waiting for next sending");
            }
            else if (e.Message.Contains("Unexpected character encountered"))
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

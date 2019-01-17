using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.Util;
using APICon.rest;
using SoapAPIConnector;

namespace APICon.controller
{
    public static class ContainerController
    {

        public static void performChainContainer(ExDFSFile file)
        {
            Dictionary<string, byte[]> additionalTickets = new Dictionary<string, byte[]>();
            if (file.ticket != null)
            {
                additionalTickets.Add(file.ticket.fileName, file.ticket.body);
                additionalTickets.Add(file.ticket.fileName.Replace(".xml", GetSignExtensionForContainer()), GetSignEncodedBodyForContainer(file.ticket.sign));
            }
            performChainContainer(getUUID(file.fileName), additionalTickets);
        }
        public static void performChainContainer(ExDFSFile file, Dictionary<string, byte[]> additionalTicketsToBeChained)
        {
            performChainContainer(getUUID(file.fileName), additionalTicketsToBeChained);
        }
        public static void performChainContainer(string document_id)
        {
            performChainContainer(document_id, new Dictionary<string, byte[]>());
        }

        public static void performChainContainer(string document_id, Dictionary<string, byte[]> additionalTicketsToBeChained)
        {
            Container.ChainContainer container = new Container.ChainContainer();

            Event[] e = RestHelper.getAllBindedEventsInChain(document_id);
            foreach (Event ev in e)
            {
                //Console.WriteLine(ev.ToString());

                GetContentResponse contentResp = RestHelper.GetContentResponse(ev.document_id);
                string name = Utils.GetTextFromXml(Utils.Base64Decode(contentResp.body, "windows-1251"), "Файл[@*]/@ИдФайл");
                container.AddEntry(name + ".xml", Utils.Base64DecodeToBytes(contentResp.body, "windows-1251"));
                container.AddEntry(name + GetSignExtensionForContainer(), GetSignEncodedBodyForContainer(contentResp.sign));
                if (ev.event_status.StartsWith("УПД"))
                {
                    byte[] pdf = Utils.Base64DecodeToBytes(RestHelper.GetPdf(ev.document_id), "UTF-8");
                    container.AddEntry(name + ".pdf", pdf);
                    container.docFunction = Utils.GetTextFromXml(Utils.Base64Decode(contentResp.body, "windows-1251"), "Файл/Документ/@Функция");
                    string[] s = { "КСЧФ", "КСЧФДИС", "ДИС" };
                    container.docNumber = Utils.GetTextFromXml(Utils.Base64Decode(contentResp.body, "windows-1251"), s.Contains(container.docFunction) ? "Файл/Документ/СвКСчФ/@НомерКСчФ" : "Файл/Документ/СвСчФакт/@НомерСчФ");
                    container.docDate = Utils.GetTextFromXml(Utils.Base64Decode(contentResp.body, "windows-1251"), s.Contains(container.docFunction) ? "Файл/Документ/СвКСчФ/@ДатаКСчФ" : "Файл/Документ/СвСчФакт/@ДатаСчФ");
                    container.SetContainerName();
                }
            }
            foreach (string key in additionalTicketsToBeChained.Keys)
                container.AddEntry(key, additionalTicketsToBeChained[key]);
            DFSHelper.saveContainer(container);
        }

        private static string GetSignExtensionForContainer()
        {
            string extension = ".bin";
            if (Program.conf.EDOTickets.chainContainer.signExtension != null)
                extension = Program.conf.EDOTickets.chainContainer.signExtension;
            return extension;
        }

        private static byte[] GetSignEncodedBodyForContainer(byte[] signBody)
        {
            return GetSignEncodedBodyForContainer(Convert.ToBase64String(signBody));
        }
        private static byte[] GetSignEncodedBodyForContainer(string base64sign)
        {
            byte[] signBody;
            if (Program.conf.EDOTickets.chainContainer.codeBase)
                signBody = Utils.StringToBytes(base64sign, "UTF-8");
            else
                signBody = Utils.Base64DecodeToBytes(base64sign, "UTF-8");
            return signBody;
        }

        private static string getUUID(string fileName)
        {
            return fileName.Split('_')[5].Split('.')[0];
        }
    }
}

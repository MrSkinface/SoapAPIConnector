﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Threading.Tasks;
using APICon.conf;
using APICon.soap;
using APICon.Util;
using APICon.rest;
using APICon.logger;

namespace APICon.controller
{
    public interface IController
    {
        List<string> getList();
        bool sendDoc(string fileName, string base64data);
        bool sendDocApi(string content, string sign, string docType);
        bool archiveDoc(string name);
        byte[] getDoc(string name);
        Ticket Ticket(string thumbprint,string uuid);
        bool sendTicket(string content, string sign, string docId);
        string Sign(string thumbprint,string base64data);
        ExCert GetExCertificate(string thumbprint);
        string[] GetCertificateNames();        
    }

    public class Controller : IController
    {
        public static Dictionary<string, string> ticketTypes = new Dictionary<string, string>();        

        private Configuration conf;
        private string authToken;
        public Controller() { }
        public Controller(Configuration conf)
        {
            this.conf = conf;
            this.authToken= authorize();

            ticketTypes.Add("DP_PDPOL", "ПодтверждениеДатыПоступления".Substring(0, 22));
            ticketTypes.Add("DP_PDOTPR", "ПодтверждениеДатыОтправки".Substring(0, 22));
            ticketTypes.Add("DP_UVUTOCH", "УведомлениеОбУточнении".Substring(0, 22));
            ticketTypes.Add("DP_IZVPOL", "ИзвещениеПолученияУПДПокупатель".Substring(0, 22));
            ticketTypes.Add("ON_SCHFDOPPOK", "ИнформацияПокупателяRECIEVED".Substring(0, 22));
        }

        public bool archiveDoc(string name)
        {
            ArchiveDocRequest req = new ArchiveDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = name;
            ArchiveDocResponse resp = (ArchiveDocResponse)Soap.ArchiveDoc<ArchiveDocResponse>(req);
            if (resp.errorCode != 0)
            {
                Logger.log("ERROR: " + resp.errorMessage);
                throw new Exception(resp.errorMessage);
            }                
            return resp.errorCode == 0;
        }
        public byte[] getDoc(string name)
        {
            GetDocRequest req = new GetDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = name;
            GetDocResponse resp = (GetDocResponse)Soap.GetDoc<GetDocResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);
            return Utils.Base64DecodeToBytes(resp.content,"UTF-8");
        }
        public List<string> getList()
        {
            GetListRequest req = new GetListRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            GetListResponse resp = (GetListResponse)Soap.GetList<GetListResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);            
            return resp.list;
        }
        public bool sendDoc(string fileName, string base64data)
        {
            SendDocRequest req = new SendDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = fileName;
            req.content = base64data;
            SendDocResponse resp = (SendDocResponse)Soap.SendDoc<SendDocResponse>(req);
            if (resp.errorCode != 0)
            {                
                Logger.log("ERROR: " + resp.errorMessage);
                throw new Exception(resp.errorMessage);
            }
            return resp.errorCode == 0;
        }
        public bool sendDocApi(string content, string sign, string docType)
        {
            APICon.rest.Request req = null;
            if (docType.StartsWith("ON_SCHF")|| docType.StartsWith("ON_KORSCHF"))
                req = new DocumentUPDSendRequest(authToken, content, sign, docType);
            else
                req = new DocumentSendRequest(authToken, content, sign, docType);            
            DocumentSendResponse response = (DocumentSendResponse)Http2.post<DocumentSendResponse>("https://api-service.edi.su/Api/Dixy/Document/Send", req);
            if (response.intCode == 200)
                return true;
            throw new Exception(response.varMessage);
        }        
        public string Sign(string thumbprint, string base64data)
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                CAPICOM.Certificate cert = GetCertByThumbprint(store, thumbprint);
                CAdESCOM.CPSigner signer = new CAdESCOM.CPSigner();
                signer.Certificate = cert;
                signer.TSAAddress = "http://cryptopro.ru/tsp/";               
                CAdESCOM.CadesSignedData signedData = new CAdESCOM.CadesSignedData();
                signedData.ContentEncoding = CAdESCOM.CADESCOM_CONTENT_ENCODING_TYPE.CADESCOM_BASE64_TO_BINARY;
                signedData.Content = base64data;
                try
                {
                    return signedData.SignCades(signer, CAdESCOM.CADESCOM_CADES_TYPE.CADESCOM_CADES_BES, true);
                }
                catch (Exception e)
                {
                    throw new Exception("Sign error", e);
                }
            }
            finally
            {
                store.Close();
            }
        }
        public Ticket Ticket(string thumbprint, string fileName)
        {
            CreateTicketResponse response = null;
            try
            {
                string uuid = fileName.Split('_')[5].Replace(".xml", "");
                if (fileName.StartsWith("DP_OTORG1_") || fileName.StartsWith("DP_OTORG2_"))
                {
                    return TicketTorg(thumbprint, fileName);
                }
                ExCert cert = GetExCertificate(thumbprint);
                ExSigner signer = new ExSigner(cert);
                response = (CreateTicketResponse)Http2.post<CreateTicketResponse>("https://api-service.edi.su/Api/Dixy/Ticket/Generate", new CreateTicketRequest(authToken, uuid, signer));
                if (response.intCode != 200)
                {
                    Logger.log("for file [" + fileName + "] :" + response.varMessage);
                    return null;
                }
                string name = GetIDFileFromTicket(response.content);
                byte[] body = Utils.Base64DecodeToBytes(response.content, "windows-1251");
                return new Ticket(name, body);
            }
            catch (Exception ex)
            {
                Logger.log("for file [" + fileName + "] :" + response.varMessage);
                Logger.log(ex.Message);
                return null;
            }
        }
        public Ticket TicketTorg(string thumbprint, string fileName)
        {
            CreateAnswerResponse response = null;
            try
            {
                string uuid = fileName.Split('_')[5].Replace(".xml", "");
                ExCert cert = GetExCertificate(thumbprint);
                ExSigner signer = new ExSigner(cert);
                CreateAnswerRequest req = new CreateAnswerRequest(authToken, uuid, new AnswerData(signer));
                response = (CreateAnswerResponse)Http2.post<CreateAnswerResponse>("https://api-service.edi.su/Api/Dixy/Ticket/Generate", req);
                if (response.intCode != 200)
                {
                    Logger.log("for file [" + fileName + "] :" + response.varMessage);
                    return null;
                }
                string name = GetIDFileFromTicket(response.content);
                byte[] body = Utils.Base64DecodeToBytes(response.content, "windows-1251");
                return new Ticket(name, body);
            }
            catch (Exception ex)
            {
                Logger.log("for file [" + fileName + "] :" + response.varMessage);
                Logger.log(ex.Message);
                return null;
            }
        }
        /**/

        public string GetIDFileFromTicket(string ticketContent, Event e)
        {
            if (e.soapFileName.Contains("DP_OTORG1_") || e.soapFileName.Contains("DP_OTORG2_"))
            {
                return GetIDFileFromTicket(ticketContent, "UTF-8");
            }
            else
            {
                return GetIDFileFromTicket(ticketContent, "windows-1251");
            }                
        }
        public string GetIDFileFromTicket(string ticketContent)
        {
            return GetIDFileFromTicket(ticketContent, "windows-1251");
        }
        public string GetIDFileFromTicket(string ticketContent, string encoding)
        {
            try
            {
                string xmlString = Utils.Base64Decode(ticketContent, encoding);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(xmlString);
                return xml.SelectSingleNode("/Файл[@*]/@ИдФайл").InnerText + ".xml";
            }
            catch (Exception e)
            {
                Logger.error(e.StackTrace);
                throw e;
            }
        }
        private string authorize()
        {
            string login = conf.Login;
            string password = conf.Api_pass;
            AuthorizeResponse response;
            try
            {
                response = (AuthorizeResponse)Http2.post<AuthorizeResponse>("https://api-service.edi.su/Api/Dixy/Index/Authorize", new AuthorizeRequest(login, password));
                return response.varToken;
            }
            catch (Exception ex)
            {
                Logger.log("ERROR: api auth fails. Api funcs will NOT be able . Reason : " + ex.Message);
                return null;
            }            
        }
        /**/
        public ExCert GetExCertificate(string thumbprint)
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                CAPICOM.ICertificates icerts = store.Certificates;
                ExCert[] certs = new ExCert[icerts.Count];
                int i = 0;
                foreach (CAPICOM.Certificate cert in store.Certificates)
                {
                    if (cert.Thumbprint.Equals(thumbprint) && cert.HasPrivateKey()) return new ExCert(cert);
                }
                throw new Exception("No certificate was found by thumbprint ["+ thumbprint + "]");
            }
            finally
            {
                store.Close();
            }
        }
        private CAPICOM.Certificate GetCertByThumbprint(CAdESCOM.CPStore store, string thumbprint)
        {
            foreach (CAPICOM.Certificate cert in store.Certificates)
            {
                if (cert.Thumbprint.Equals(thumbprint) && cert.HasPrivateKey()) return cert;
            }
            throw new Exception("No certificate was found by thumbprint [" + thumbprint + "]");
        }
        public string[] GetCertificateNames()
        {
            ExCert[] certs = GetCertificates();
            string[] result = new string[certs.Length];
            for (int i = 0; i < certs.Length; i++)
            {
                result[i] = certs[i].Name;
            }
            return result;
        }
        public ExCert[] GetCertificates()
        {
            CAdESCOM.CPStore store = new CAdESCOM.CPStore();
            store.Open();
            try
            {
                CAPICOM.ICertificates icerts = store.Certificates;
                ExCert[] certs = new ExCert[icerts.Count];
                int i = 0;
                foreach (CAPICOM.Certificate cert in store.Certificates)
                {
                    certs[i++] = new ExCert(cert);
                }
                return certs;
            }
            finally
            {
                store.Close();
            }
        }
        public bool sendTicket(string content, string sign, string docId)
        {
            EnqueueTicketRequest req = null;
            EnqueueTicketResponse enqueueResponse = null;
            try
            {
                req = new EnqueueTicketRequest(authToken, docId, content, sign);
                enqueueResponse = (EnqueueTicketResponse)Http2.post<EnqueueTicketResponse>("https://api-service.edi.su/Api/Dixy/Ticket/Enqueue", req);
                if (enqueueResponse.intCode == 200)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Logger.log("error while processing [" + docId + "]");
                Logger.log(enqueueResponse.varMessage);
                Logger.log(ex.Message);
                return false;
            }
        }              
        
        public ApiDocument getDocInfoByEvent(Event e)
        {
            GetDocInfoRequest req = null;
            GetDocInfoResponse resp = null;
            GetContentResponse content = null;
            try
            {
                req = new GetDocInfoRequest(authToken, e.document_id);                
                resp = (GetDocInfoResponse)Http2.post<GetDocInfoResponse>("https://api-service.edi.su/Api/Dixy/TimeLine/GetDocData", req);                
                if (resp.intCode == 200)
                {
                    content = getDocumentContent(e);
                    resp.document.file_body = content.body;
                    resp.document.sign_body = content.sign;
                    return resp.document;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.log("error while processing [" + e.document_id + "]");
                Logger.log(resp.varMessage);
                Logger.log(ex.Message);
                return null;
            }
        }
        
        public bool confirmEvent(Event e, string body, string sign)
        {
            EnqueueTicketResponse enqueueResponse=null;
            try
            {
                EnqueueTicketRequest req = new EnqueueTicketRequest(authToken, e.document_id, body, sign);
                enqueueResponse = (EnqueueTicketResponse)Http2.post<EnqueueTicketResponse>("https://api-service.edi.su/Api/Dixy/Ticket/Enqueue", req);
                if (enqueueResponse.intCode == 200)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Logger.log("error while processing ["+ e.document_id+"]");
                Logger.log(enqueueResponse.varMessage);
                Logger.log(ex.Message);
                return false;
            }
        }

        public GetContentResponse GetContent(Event e)
        {
            GetContentResponse response = null;
            if (e.soapFileName.Contains("DP_OTORG"))
            {
                response = getDocumentContent(e);
                if (response.intCode != 200)
                {
                    response = getUPDDocumentContent(e);
                }
            }
            else
            {
                response = getUPDDocumentContent(e);
                if (response.intCode != 200)
                {
                    response = getDocumentContent(e);
                }
            }
            return response;
        }

        public GetContentResponse getDocumentContent(Event e)
        {
            GetContentResponse response = null;
            try
            {
                response = (GetContentResponse)Http2.post<GetContentResponse>("https://api-service.edi.su/Api/Dixy/Content/GetBoth", new GetContentRequest(authToken, e.document_id));
            }
            catch (Exception ex)
            {
                Logger.log("error while processing [" + e.document_id + "]");
                Logger.log(response.varMessage);
                Logger.log(ex.Message);
            }
            return response;
        }
        public GetContentResponse getUPDDocumentContent(Event e)
        {            
            return getUPDDocumentContent(e.document_id);
        }
        public GetContentResponse getUPDDocumentContent(string docGUID)
        {
            GetUPDContentResponse response = null;
            GetContentResponse contResp = null;
            try
            {
                response = (GetUPDContentResponse)Http2.post<GetUPDContentResponse>("https://api-service.edi.su/Api/Dixy/Content/GetDocWithSignContent", new GetContentRequest(authToken, docGUID));
                contResp = new GetContentResponse();
                contResp.intCode = response.intCode;
                contResp.varMessage = response.varMessage;
                contResp.body = response.body;
                contResp.sign = response.sign[0].body;

            }
            catch (Exception ex)
            {
                Logger.log("error while processing [" + docGUID + "]");
                Logger.log(response.varMessage);
                Logger.log(ex.Message);
            }
            return contResp;
        }
        public Event[] getAllBindedEventsInChain(string varDocGuid)
        {
            GetTimeLineRequest req = new GetTimeLineRequest(authToken, varDocGuid, true);
            GetTimeLineResponse resp = (GetTimeLineResponse)Http2.post<GetTimeLineResponse>("https://api-service.edi.su/Api/Dixy/TimeLine/GetTimeLine", req);
            if (resp.intCode != 200)
                throw new Exception(resp.varMessage);
            return resp.timeline;
        }
        /*  pdf print form      */
        public string GetPdf(string id)
        {
            string mode = "SENT";
            GetPdfRequest req = new GetPdfRequest(authToken, id, mode);
            GetPdfResponse resp;
            try
            {
                resp = (GetPdfResponse)Http2.post<GetPdfResponse>("https://api-service.edi.su/Api/Dixy/PrintForm/Generate", req);
                if (resp.intCode != 200)
                    throw new Exception(resp.varMessage);
            }
            catch (Exception e)
            {
                mode = "RECIEVED";
                req = new GetPdfRequest(authToken, id, mode);
                resp = (GetPdfResponse)Http2.post<GetPdfResponse>("https://api-service.edi.su/Api/Dixy/PrintForm/Generate", req);                
            }
            if (resp.intCode != 200)
                throw new Exception(resp.varMessage);
            return resp.form;
        }
    }
    public class Ticket
    {
        public string fileName { get; set; }
        public byte[] body { get; set; }

        public Ticket() { }
        public Ticket(string fileName, byte[] body)
        {
            this.fileName = fileName;
            this.body = body;
        }
    }
}

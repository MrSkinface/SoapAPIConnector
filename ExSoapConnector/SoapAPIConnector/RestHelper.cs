using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.logger;
using APICon.controller;
using APICon.Util;

namespace APICon.rest
{
    public static class RestHelper
    {
        private static string authToken;

        public static void authorize(string login, string password)
        {
            try
            {
                AuthorizeRequest req = new AuthorizeRequest(login, password);
                AuthorizeResponse response = (AuthorizeResponse)Http.post<AuthorizeResponse>("Index/Authorize", req);
                if (response.varToken == null)
                    throw new Exception("API authorization error. " + response.varMessage);
                authToken = response.varToken;
            }
            catch (Exception ex)
            {                
                Logger.log(ex.Message);                
            }
        }
        public static void send(string body, string sign, string doc_type)
        {
            Request req = null;
            if (doc_type.StartsWith("ON_SCHFDOPPR") || doc_type.StartsWith("ON_KORSCHFDOPPR"))
                req = new DocumentUPDSendRequest(authToken, body, sign, doc_type);
            else
                req = new DocumentSendRequest(authToken, body, sign, doc_type);
            DocumentSendResponse response = (DocumentSendResponse)Http.post<DocumentSendResponse>("Document/Send", req);
            if (response.intCode != 200)
                throw new Exception(response.varMessage);

        }
        public static byte[] getTicket(ExSigner signer, string uuid)
        {
            CreateTicketRequest req = new CreateTicketRequest(authToken, uuid, signer);
            CreateTicketResponse response = (CreateTicketResponse)Http.post<CreateTicketResponse>("Ticket/Generate", req);
            return Utils.Base64DecodeToBytes(response.content, "windows-1251");
        }
        public static void sendTicket(byte[] content, byte[] sign, string docId)
        {            
            string base64content = Convert.ToBase64String(content);
            string base64sign = Convert.ToBase64String(sign);
            sendTicket(base64content, base64sign, docId);
        }
        public static void sendTicket(string base64content, string base64sign, string docId)
        {
            EnqueueTicketRequest req = new EnqueueTicketRequest(authToken, docId, base64content, base64sign);
            EnqueueTicketResponse resp = (EnqueueTicketResponse)Http.post<EnqueueTicketResponse>("Ticket/Enqueue", req);
            if (resp.intCode != 200)
                throw new Exception(resp.varMessage);
        }

        /**/
        public static GetUPDContentResponse getUPDDocumentContent(string docGUID)
        {
            GetContentRequest req = new GetContentRequest(authToken, docGUID);
            GetUPDContentResponse resp = (GetUPDContentResponse)Http.post<GetUPDContentResponse>("Content/GetDocWithSignContent", req);
            if (resp.intCode != 200)
                throw new Exception(resp.varMessage);
            return resp;
        }
        public static GetContentResponse GetContentResponse(string docGUID)
        { 
            GetContentRequest req = new GetContentRequest(authToken, docGUID);
            GetUPDContentResponse response = (GetUPDContentResponse)Http.post<GetUPDContentResponse>("Content/GetDocWithSignContent", req);
            GetContentResponse contResp = new GetContentResponse();
            contResp.intCode = response.intCode;
            contResp.varMessage = response.varMessage;
            contResp.body = response.body;
            contResp.sign = response.sign[0].body;
            return contResp;
        }

        public static Event[] getAllBindedEventsInChain(string varDocGuid)
        {
            GetTimeLineRequest req = new GetTimeLineRequest(authToken, varDocGuid, true);
            GetTimeLineResponse resp = (GetTimeLineResponse)Http.post<GetTimeLineResponse>("TimeLine/GetTimeLine", req);
            if (resp.intCode != 200)
                throw new Exception(resp.varMessage);
            return resp.timeline;
        }

        public static string GetPdf(string id)
        {
            string mode = "SENT";
            GetPdfRequest req = new GetPdfRequest(authToken, id, mode);
            GetPdfResponse resp;
            try
            {
                resp = (GetPdfResponse)Http.post<GetPdfResponse>("PrintForm/Generate", req);
                if (resp.intCode != 200)
                    throw new Exception(resp.varMessage);
            }
            catch (Exception e)
            {
                mode = "RECIEVED";
                req = new GetPdfRequest(authToken, id, mode);
                resp = (GetPdfResponse)Http.post<GetPdfResponse>("PrintForm/Generate", req);
            }
            if (resp.intCode != 200)
                throw new Exception(resp.varMessage);
            return resp.form;
        }        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace APICon.rest
{
    /*
     objects for exCertificat, significant , reception_info
    */
    public class ExCert
    {
        public string Thumbprint { get; }
        public string Name { get; }
        public string SubjectName { get; }
        public DateTime ValidFromDate { get; }
        public DateTime ValidToDate { get; }
        public bool IsValid { get; }

        public ExCert(CAPICOM.Certificate cert)
        {
            Thumbprint = cert.Thumbprint;
            Name = cert.GetInfo(CAPICOM.CAPICOM_CERT_INFO_TYPE.CAPICOM_CERT_INFO_SUBJECT_DNS_NAME);
            SubjectName = cert.SubjectName;
            ValidFromDate = cert.ValidFromDate;
            ValidToDate = cert.ValidToDate;
            IsValid = cert.IsValid().Result;
        }
        override
        public string ToString()
        {
            return Name + "\n\tдействителен: " + ValidFromDate + "-" + ValidToDate
                + "\n\tвалиден: " + IsValid
                + "\n\tотпечаток: " + Thumbprint;
        }
    }
    public class ExSigner
    {
        public ExCert cert;
        public string givenName;
        public string surName;
        public string orgPosition;
        public string inn;
        public ExSigner() { }
        public ExSigner(ExCert cert)
        {
            this.cert = cert;
            var map = getTokenizedValues(cert.SubjectName);
            map.TryGetValue("G", out givenName);
            map.TryGetValue("SN", out surName);
            map.TryGetValue("T", out orgPosition);
            map.TryGetValue("ИНН", out inn);
        }
        public ExSigner setFirstName(string value)
        {
            this.givenName = value;
            return this;
        }
        public ExSigner setSurName(string value)
        {
            this.surName = value;
            return this;
        }
        public ExSigner setOrgPosition(string value)
        {
            this.orgPosition = value;
            return this;
        }
        public ExSigner setInn(string value)
        {
            this.inn = value;
            return this;
        }
        private Dictionary<string, string> getTokenizedValues(string subjectName)
        {
            var map = new Dictionary<string, string>();
            foreach (string part in subjectName.Split(','))
            {
                if (part.Contains("="))
                {
                    string[] parts = part.Split('=');
                    map.Add(parts[0].TrimStart(' '), parts[1].TrimStart('0'));
                }                
            }
            return map;
        }
        override
        public string ToString()
        {
            return
                "\n\tgivenName: " + givenName
                + "\n\tsurName: " + surName
                + "\n\torgPosition: " + orgPosition
                + "\n\tinn: " + inn;
        }
    }
    public class RecieptInformation
    {
        public RecieptInformation() { }
        public RecieptInformation
            (string receptionDate,
            string receptionistFirstName,
            string receptionistSurName,
            string receptionistPatronymic,
            string receptionistPosition)
        {
            this.receptionDate = receptionDate;
            this.receptionistFirstName = receptionistFirstName;
            this.receptionistSurName = receptionistSurName;
            this.receptionistPatronymic = receptionistPatronymic;
            this.receptionistPosition = receptionistPosition;
        }
        public RecieptInformation setReceptionDate(string value)
        {
            this.receptionDate = value;
            return this;
        }
        public RecieptInformation setReceptionistFirstName(string value)
        {
            this.receptionistFirstName = value;
            return this;
        }
        public RecieptInformation setReceptionistSurName(string value)
        {
            this.receptionistSurName = value;
            return this;
        }
        public RecieptInformation setReceptionistPatronymic(string value)
        {
            this.receptionistPatronymic = value;
            return this;
        }
        public RecieptInformation setReceptionistPosition(string value)
        {
            this.receptionistPosition = value;
            return this;
        }
        public string receptionDate { get; set; }
        public string receptionistFirstName { get; set; }
        public string receptionistSurName { get; set; }
        public string receptionistPatronymic { get; set; }
        public string receptionistPosition { get; set; }
    }
    /*
     basic request / response
    */
    public class Request
    {
        public string varToken { get; set; }
        public override string ToString()
        {
            return base.ToString();
        }
    }
    public class Response
    {
        public int intCode { get; set; }
        public string varMessage { get; set; }
        public override string ToString()
        {
            return base.ToString() + ",\nintCode=" + intCode + "\nvarMessage=" + varMessage;
        }
    }
    /*
     authorization request / response
    */
    public class AuthorizeRequest
    {
        public AuthorizeRequest() { }
        public AuthorizeRequest(string varLogin, string varPassword)
        {
            this.varLogin = varLogin;
            this.varPassword = varPassword;
        }
        public string varLogin { get; set; }
        public string varPassword { get; set; }
    }
    public class AuthorizeResponse : Response
    {
        public string varToken { get; set; }
        public override string ToString()
        {
            return base.ToString() + ",\nvarToken=" + varToken;
        }
    }
    /*
     get content (both) request / response
    */
    public class GetContentRequest : Request
    {
        public GetContentRequest() { }
        public GetContentRequest(string authToken, string identifier)
        {
            this.identifier = identifier;
            base.varToken = authToken;
        }
        public string identifier { get; set; }
    }
    public class GetContentResponse : Response
    {
        public string sign { get; set; }
        public string body { get; set; }
    }
    public class GetUPDContentResponse : Response
    {
        public List<UPDSign> sign { get; set; }
        public string body { get; set; }
    }
    /*
     create tickets request / response
    */
    public class CreateTicketRequest : Request
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string comment { get; set; }
        public string identifier { get; set; }
        public string signer_fname { get; set; }
        public string signer_inn { get; set; }
        public string signer_position { get; set; }
        public string signer_sname { get; set; }
        public CreateTicketRequest() { }
        public CreateTicketRequest(string authToken, string identifier, ExSigner signer)
        {
            this.identifier = identifier;
            base.varToken = authToken;
            this.signer_fname = signer.givenName;
            this.signer_sname = signer.surName;
            this.signer_position = signer.orgPosition;
            this.signer_inn = signer.inn;
        }
        public CreateTicketRequest(string authToken, string uvutochComment, string identifier, ExSigner signer)
        {
            this.identifier = identifier;
            this.comment = uvutochComment;
            base.varToken = authToken;
            this.signer_fname = signer.givenName;
            this.signer_sname = signer.surName;
            this.signer_position = signer.orgPosition;
            this.signer_inn = signer.inn;
        }
    }
    public class CreateTicketResponse : Response
    {
        public string content { get; set; }
    }
    /*
     enqueue tickets request / response
    */
    public class EnqueueTicketRequest : Request
    {
        public string identifier { get; set; }
        public string xml { get; set; }
        public string sign { get; set; }
        public EnqueueTicketRequest() { }
        public EnqueueTicketRequest(string authToken, string identifier, string xml, string sign)
        {
            this.xml = xml;
            this.sign = sign;
            this.identifier = identifier;
            base.varToken = authToken;
        }
    }
    public class EnqueueTicketResponse : Response
    {
    }
    /*
     create answers (ptorg12 / zaktprm) request / response
    */
    public class CreateAnswerRequest : Request
    {
        public CreateAnswerRequest() { }
        public CreateAnswerRequest(string authToken, string identifier, AnswerData answer_data)
        {
            base.varToken = authToken;
            this.identifier = identifier;
            this.answer_data = answer_data;
        }
        public AnswerData answer_data { get; set; }
        public string identifier { get; set; }
    }
    public class AnswerData
    {
        public AnswerData() { }
        public AnswerData(ExSigner signer, RecieptInformation recieptInfo)
        {
            string[] name = signer.givenName.Split(' ');
            if (name.Length > 1)
            {
                this.signer_fname = name[0];
                this.signer_patronymic = name[1];
            }
            else
            {
                this.signer_fname = signer.givenName;
                this.signer_patronymic = signer.givenName;
            }
            this.signer_sname = signer.surName;
            this.signer_position = signer.orgPosition;
            this.signer_inn = signer.inn;
            this.rec_date = recieptInfo.receptionDate;
            this.rec_fname = recieptInfo.receptionistFirstName;
            this.rec_patronymic = recieptInfo.receptionistPatronymic;
            this.rec_position = recieptInfo.receptionistPosition;
            this.rec_sname = recieptInfo.receptionistSurName;
        }
        public string rec_date { get; set; }
        public string rec_fname { get; set; }
        public string rec_patronymic { get; set; }
        public string rec_position { get; set; }
        public string rec_sname { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_number { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_date { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_company { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_position { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_sname { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_fname { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string procuratory_patronymic { get; set; }
        public string signer_fname { get; set; }
        public string signer_inn { get; set; }
        public string signer_position { get; set; }
        public string signer_sname { get; set; }
        public string signer_patronymic { get; set; }
    }
    public class CreateAnswerResponse : Response
    {
        public string content { get; set; }
    }
    /*
     enqueue answers (ptorg12 / zaktprm) request / response
    */
    public class EnqueueAnswerRequest : Request
    {
        public EnqueueAnswerRequest() { }
        public EnqueueAnswerRequest(string authToken, string identifier, string xml, string sign)
        {
            this.xml = xml;
            this.sign = sign;
            this.identifier = identifier;
            base.varToken = authToken;
        }
        public string identifier { get; set; }
        public string sign { get; set; }
        public string xml { get; set; }
    }
    public class EnqueueAnswerResponse : Response
    {
    }
    /*
     send document (sfakt,otorg12,iaktprm) request / response
    */
    public class DocumentSendRequest : Request
    {
        public DocumentSendRequest() { }
        public DocumentSendRequest(string authToken, string body, string sign, string doc_type)
        {
            base.varToken = authToken;
            this.body = body;
            this.sign = sign;
            this.doc_type = doc_type;
        }
        public string body { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string comment { get; set; }
        public string doc_type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string identifier { get; set; }
        public string sign { get; set; }
    }
    public class DocumentUPDSendRequest : Request
    {
        public DocumentUPDSendRequest() { }
        public DocumentUPDSendRequest(string authToken, string body, string sign, string doc_type)
        {
            base.varToken = authToken;
            this.body = body;
            this.sign = new List<UPDSign>();
            this.sign.Add(new UPDSign(sign));
            this.doc_type = doc_type;
        }
        public string body { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string comment { get; set; }
        public string doc_type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string identifier { get; set; }
        public List<UPDSign> sign { get; set; }
    }
    public class UPDSign
    {
        public string type { get; set; }
        public string body { get; set; }

        public UPDSign() { }
        public UPDSign(string sign)
        {
            this.type = "1";
            this.body = sign;
        }
    }
    public class DocumentSendResponse : Response
    {
    }
    /*
        for scanning tickets need to be signed/answered 
    */
    public class GetTimeLineRequest : Request
    {
        public GetTimeLineRequest() { }
        public GetTimeLineRequest(string authToken, string timefrom, string timeto)
        {
            this.timefrom = timefrom;
            this.timeto = timeto;
            base.varToken = authToken;
        }
        public GetTimeLineRequest(string authToken, string exchangeid)
        {
            this.exchangeid = exchangeid;
            base.varToken = authToken;
        }
        public GetTimeLineRequest(string authToken, string timefrom, string timeto, string mode)
        {
            this.timefrom = timefrom;
            this.timeto = timeto;
            this.mode = mode;
            base.varToken = authToken;
        }
        public GetTimeLineRequest(string authToken)
        {
            this.mode = "ESF_UPD";
            base.varToken = authToken;
        }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string timefrom { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string mode { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string timeto { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string exchangeid { get; set; }
    }
    public class GetUnreadTimeLineResponse : GetTimeLineResponse
    {
        public int total_events_count { get; set; }
    }
    public class GetTimeLineResponse : Response
    {
        public Event[] timeline { get; set; }
    }
    public class Event
    {
        public string document_id { get; set; }
        public string event_date { get; set; }
        public string event_id { get; set; }
        public string event_status { get; set; }
        public string exchangeid { get; set; }
        public bool need_reply_reciept { get; set; }
        public string recipient_id { get; set; }
        public string sender_id { get; set; }
        override
        public string ToString()
        {
            return
                "\n\tdocument_id: " + document_id
                + "\n\tevent_date: " + event_date
                + "\n\tevent_id: " + event_id
                + "\n\tevent_status: " + event_status
                + "\n\texchangeid: " + exchangeid
                + "\n\tneed_reply_reciept: " + need_reply_reciept
                + "\n\trecipient_id: " + recipient_id
                + "\n\tsender_id: " + sender_id;
        }
    }
    public class MarkEventReadRequest : Request
    {
        public string event_id { get; set; }
        public MarkEventReadRequest() { }
        public MarkEventReadRequest(string authToken, string event_id)
        {
            this.event_id = event_id;
            base.varToken = authToken;
        }
    }
    public class MarkEventReadResponse : Response { }
    public class GetDocInfoRequest : Request
    {
        public string identifier { get; set; }

        public GetDocInfoRequest() { }
        public GetDocInfoRequest(string authToken, string identifier)
        {
            this.identifier = identifier;
            base.varToken = authToken;
        }
    }
    public class GetDocInfoResponse : Response
    {
        public ApiDocument document { get; set; }
    }
    public class ApiDocument
    {
        public string doc_type { get; set; }
        public string document_id { get; set; }
        public string event_date { get; set; }
        public string event_direction { get; set; }
        public string file_name { get; set; }
        public string recipient_id { get; set; }
        public string sender_id { get; set; }
        public string sender_internal_id { get; set; }
        public string file_body { get; set; }
        public string sign_body { get; set; }
    }
}

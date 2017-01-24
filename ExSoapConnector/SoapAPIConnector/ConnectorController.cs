using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using APICon.conf;
using APICon.soap;
using APICon.Util;

namespace APICon.controller
{
    public interface IController
    {
        List<string> getList();
        bool sendDoc(string localPath);
        bool archiveDoc(string name);
        byte[] getDoc(string name);
        string Ticket(string thumbprint,string uuid);
        string Sign(string thumbprint,string base64data);
    }

    public class Controller : IController
    {
        private Configuration conf;
        public Controller(Configuration conf)
        {
            this.conf = conf;
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
                throw new Exception(resp.errorMessage);
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

        public bool sendDoc(string localPath)
        {
            SendDocRequest req = new SendDocRequest();
            req.user = new User();
            req.user.login = conf.Login;
            req.user.pass = Utils.GetMD5String(conf.Soap_pass);
            req.fileName = Path.GetFileName(localPath);
            req.content = Utils.BytesToString(File.ReadAllBytes(localPath),"UTF-8");
            SendDocResponse resp = (SendDocResponse)Soap.SendDoc<SendDocResponse>(req);
            if (resp.errorCode != 0)
                throw new Exception(resp.errorMessage);
            return resp.errorCode == 0;
        }

        public string Sign(string thumbprint, string base64data)
        {
            throw new NotImplementedException();
        }

        public string Ticket(string thumbprint, string uuid)
        {
            throw new NotImplementedException();
        }
    }
}

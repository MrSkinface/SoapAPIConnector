using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.conf;
using APICon.controller;

namespace APICon.Util
{
    public class ExDFSFile
    {
        public Document settings;
        public string fullPath;

        public string fileName;
        public byte[] body;
        public byte[] sign;
        public byte[] zipBody;

        /**/
        public Ticket ticket;

        public ExDFSFile() { }

        public ExDFSFile(string fileName, byte[] body)
        {
            this.fileName = fileName;
            this.body = body;
            sign = null;
        }

        public ExDFSFile(string fileName, byte[] body, byte[] sign)
        {
            this.fileName = fileName;
            this.body = body;
            this.sign = sign;
        }

        public ExDFSFile(string fullPath, string fileName, byte[] body, byte[] sign)
        {
            this.fullPath = fullPath;
            this.fileName = fileName;
            this.body = body;
            this.sign = sign;
        }

        public ExDFSFile(string fullPath, Document settings, string fileName, byte[] body, byte[] sign)
        {
            this.settings = settings;
            this.fullPath = fullPath;
            this.fileName = fileName;
            this.body = body;
            this.sign = sign;
        }        
    }
}

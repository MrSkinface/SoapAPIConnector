using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using APICon.rest;
using APICon.logger;
using APICon.controller;

namespace APICon.Container
{
    public class ChainContainer
    {
        public string name { set; get; }

        public string docFunction { set; get; }
        public string docNumber { set; get; }
        public string docDate { set; get; }

        public Dictionary<string, byte[]> containedEntries { set; get; }

        public ChainContainer()
        {
            containedEntries = new Dictionary<string, byte[]>();           
        }

        public void AddEntry(string name, byte[] body)
        {
            try
            {
                containedEntries.Add(name, body);                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Logger.log(e.Message);
            }
        }
        public void SetContainerName()
        {
            try
            {
                this.name = this.docFunction+"_"+this.docNumber+"_"+this.docDate+".zip";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                Logger.log(e.Message);
            }
        }
        override
        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[ChainContainer = ")
                .Append("\n\tname = ").Append(name)
                .Append("\n\tdocFunction = ").Append(docFunction)
                .Append("\n\tdocNumber = ").Append(docNumber)
                .Append("\n\tdocDate = ").Append(docDate)
                .Append("]");
            return sb.ToString();
        }
    }
}

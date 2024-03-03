using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RFID_Program
{
    public class TagPack
    {
        public string Epc;
        public string Port;
        public string F;
        public string Phase;
        public string RSS;
        
        public TagPack(string epc, string port, string f, string phase, string rss)
        {
            this.Epc = epc;
            this.Port = port;
            this.F = f;
            this.Phase = phase;
            this.RSS = rss;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Resources.API_Classes
{
    public class Lstspecility
    {
        public string strSPECID { get; set; }
        public string strSPECDESC { get; set; }
    }

    public class RootGetSpecility
    {
        public List<Lstspecility> lstspecility { get; set; }
        public int intFlag { get; set; }
        public object strMessage { get; set; }
        public object strError { get; set; }
    }
}

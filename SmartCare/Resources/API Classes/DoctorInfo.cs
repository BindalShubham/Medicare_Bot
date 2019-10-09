using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Resources.API_Classes
{
    public class DoctorInfo
    {
        public int id { get; set; }
        public string strDOCID { get; set; }
        public string strHospitalId { get; set; }
        public string strTITLE { get; set; }
        public string strDOCNAME { get; set; }
        public string strDESIGNATION { get; set; }
        public string strSPECID { get; set; }
        public string strSPECIALITYDESC { get; set; }
        public string strLANGUAGES { get; set; }
        public string dtSERVICESTDT { get; set; }
        public string strABOUT { get; set; }
        public string strWorkingHours { get; set; }
        public int intFLAG { get; set; }
        public object strTITLEDOCNAME { get; set; }
        public object strServUrl { get; set; }
        public object strCode { get; set; }
        public object strDesc { get; set; }
        public object lstHospitalMaster { get; set; }
        public int intFlag { get; set; }
        public object strMessage { get; set; }
        public object strError { get; set; }
    }
}

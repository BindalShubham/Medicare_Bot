using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Resources.API_Classes
{
    public class DoctorBookInput
    {
        public string strHospitalId { get; set; }
        public string strDOCID { get; set; }
        public string strSPECID { get; set; }
        public string strDOCNAME { get; set; }
        public string strSPECIALITYDESC { get; set; }
        public string strTIMESLOT { get; set; }
        public DateTime dtAPPDATE { get; set; }
        public string intAPPSTATUS { get; set; }
        public string intPATTITLE { get; set; }
        public string strPATFIRSTNAME { get; set; }
        public string strPATMIDNAME { get; set; }
        public string strPATLASTNAME { get; set; }
        public string intPATGENDER { get; set; }
        public string strMOBILENO { get; set; }
        public string strHOMEPHONE { get; set; }
        public string strEMAILID { get; set; }
        public string intHAVEUHID { get; set; }
        public string strUHID { get; set; }
        public string strCityId { get; set; }
        public string strREMARKS { get; set; }
    }
}

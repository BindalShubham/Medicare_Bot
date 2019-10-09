using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Resources.API_Classes
{
    public class DoctorTimeOutput
    {
        public string strDOCID { get; set; }
        public string strHospitalId { get; set; }
        public string dtSelectedDay { get; set; }
        public List<ObjDoctorTimeSlot> objDoctorTimeSlots { get; set; }
        public string intFlag { get; set; }
        public string strMessage { get; set; }
        public object strError { get; set; }
    }
    public class ObjDoctorTimeSlot
    {
        public string intSlotId { get; set; }
        public string strSlotDesc { get; set; }
        public string intFALLIN { get; set; }
        public string strShowSlot { get; set; }
    }
}

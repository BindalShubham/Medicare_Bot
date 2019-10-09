using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Resources.API_Classes
{
    public class DoctorTimeInput
    {
        public string strHospitalId { get; set; }
        public string strDoctorid { get; set; }
        public string dtSelectedDay { get; set; }
    }
}

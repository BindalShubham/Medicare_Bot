using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BasicBot.Dialogs.Medic
{
    public class MedicState
    {
        public string Complaint { get; set; }

        public string ComplaintCategory { get; set; }

        public string TimeSpan { get; set; }

        public string OtherSymptoms { get; set; }

        public string OtherSymptoms2 { get; set; }

        public string DoctorID { get; set; }

        public string HospitalID { get; set; }

        public DateTime DoctorApointmentDate { get; set; }

        public DateTime DoctorApointmentTime { get; set; }

    }
}

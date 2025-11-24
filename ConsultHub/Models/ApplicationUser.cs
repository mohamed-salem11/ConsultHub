using Microsoft.AspNetCore.Identity;

namespace ConsultHub.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; }
        public string? Bio { get; set; }
        public string? Photo { get; set; }
        public string? Specialization { get; set; }
        public bool IsConsultant { get; set; } = false;
        public bool IsConsultantRequestPending { get; set; } = false;
        public virtual List<Consultation>? Consultations { get; set; } 
        public virtual List<Booking>? Bookings { get; set; }
   
    }
 
}

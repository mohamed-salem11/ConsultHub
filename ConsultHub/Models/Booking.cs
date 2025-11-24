namespace ConsultHub.Models
{
    public class Booking
    {

        public int Id { get; set; }
        public DateTime BookedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ProblemDescription { get; set; }
        public int? Rating { get; set; }          
        public string ApplicationUserId { get; set; }      
        public virtual ApplicationUser ApplicationUser { get; set; }
        public int ConsultationId { get; set; }      
        public virtual Consultation Consultation { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

    }
}

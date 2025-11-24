using System.ComponentModel.DataAnnotations.Schema;

namespace ConsultHub.Models
{
    public class Consultation
    {

        public int Id { get; set; }
        public string Title { get; set; }

        public string Description { get; set; }

        public string CoverImageUrl { get; set; }
        public int Price { get; set; }

        public double? TotalRating { get; set; } = 0;

        public int TotalVotes { get; set; } = 0;

        [NotMapped]
        public double AverageRating => TotalVotes > 0 ? (double)TotalRating / TotalVotes : 0;
        public int NumberOfClients { get; set; } = 0;
        public ConsultationStatus Status { get; set; } = ConsultationStatus.Draft;
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public string ApplicationUserId { get; set; }     
        public virtual ApplicationUser ApplicationUser { get; set; }

        public virtual List<Booking>? Bookings { get; set; }
    }
}

















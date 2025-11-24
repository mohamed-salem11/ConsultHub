namespace ConsultHub.Models
{
    public class Category
    {

        public int Id { get; set; }
        public string Name { get; set; }         
        public string CoverImageUrl { get; set; }

        public virtual List<Consultation> Consultations { get; set; }
    }
}

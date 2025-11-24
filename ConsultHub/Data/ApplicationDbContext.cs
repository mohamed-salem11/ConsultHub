using ConsultHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ConsultHub.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Consultation> Consultations { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = "1",
                Name = "Admin",
                NormalizedName = "ADMIN"
            });


            builder.Entity<Consultation>()
         .HasOne(c => c.ApplicationUser)
         .WithMany(u => u.Consultations)
         .HasForeignKey(c => c.ApplicationUserId)
         .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(e => e.ApplicationUser)
                .WithMany(u => u.Bookings)
                .HasForeignKey(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(e => e.Consultation)
                .WithMany(c => c.Bookings)
                .HasForeignKey(e => e.ConsultationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}


















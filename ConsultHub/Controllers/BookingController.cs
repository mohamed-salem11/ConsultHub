using ConsultHub.Data;
using ConsultHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace ConsultHub.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public BookingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;

            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

     
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var bookings = await _context.Bookings
                .Include(b => b.Consultation)
                    .ThenInclude(c => c.ApplicationUser)
                .Include(b => b.Consultation.Category)
                .Where(b => b.ApplicationUserId == user.Id)
                .ToListAsync();

            return View(bookings);
        }

 
        [HttpPost]
        public async Task<IActionResult> BookConsultation(int consultationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var consultation = await _context.Consultations
                .Include(c => c.ApplicationUser)
                .FirstOrDefaultAsync(c => c.Id == consultationId);

            if (consultation == null) return NotFound();

            var domain = $"{Request.Scheme}://{Request.Host}";
            var imageUrl = consultation.CoverImageUrl;
            if (!string.IsNullOrEmpty(imageUrl) && !imageUrl.StartsWith("http"))
                imageUrl = $"{domain}{consultation.CoverImageUrl}";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "egp",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = consultation.Title,
                        Description = consultation.Description?.Length > 500
                            ? consultation.Description.Substring(0, 500)
                            : consultation.Description,
                        Images = string.IsNullOrEmpty(imageUrl) ? null : new List<string> { imageUrl }
                    },
                    UnitAmount = (long)(consultation.Price * 100),
                },
                Quantity = 1,
            },
        },
                Mode = "payment",
                SuccessUrl = $"{domain}/Booking/PaymentSuccess?consultationId={consultationId}",
                CancelUrl = $"{domain}/Consultation/Details/{consultationId}",
                Metadata = new Dictionary<string, string>
        {
            { "consultationId", consultationId.ToString() },
            { "userId", user.Id }
        }
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Redirect(session.Url);
        }


        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int consultationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var consultation = await _context.Consultations.FindAsync(consultationId);
            if (consultation == null) return NotFound();

            var booking = new Booking
            {
                ConsultationId = consultationId,
                ApplicationUserId = user.Id,
                BookedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            consultation.NumberOfClients = consultation.NumberOfClients + 1;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Consultation booked successfully.";
            return RedirectToAction("MyConsultations");
        }


        [HttpPost]
        public async Task<IActionResult> AddRating(int consultationId, int rating)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.ConsultationId == consultationId && b.ApplicationUserId == user.Id);

            if (booking == null)
            {
                TempData["Error"] = "You must book the consultation before rating.";
                return RedirectToAction("Details", "Consultation", new { id = consultationId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("Details", "Consultation", new { id = consultationId });
            }

            var consultation = await _context.Consultations.FindAsync(consultationId);
            if (consultation == null) return NotFound();

            if (booking.Rating.HasValue)
            {
                consultation.TotalRating = (consultation.TotalRating ?? 0) - booking.Rating.Value + rating;
                booking.Rating = rating;
            }
            else
            {
                consultation.TotalRating = (consultation.TotalRating ?? 0) + rating;
                consultation.TotalVotes = consultation.TotalVotes  + 1;
                booking.Rating = rating;
            }

            await _context.SaveChangesAsync();

            TempData["Message"] = "Your rating has been submitted.";
            return RedirectToAction("Details", "Consultation", new { id = consultationId });
        }

       
        [HttpPost]
        public async Task<IActionResult> RemoveRating(int consultationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.ConsultationId == consultationId && b.ApplicationUserId == user.Id);

            if (booking == null || !booking.Rating.HasValue)
            {
                TempData["Error"] = "No rating found to remove.";
                return RedirectToAction("Details", "Consultation", new { id = consultationId });
            }

            var consultation = await _context.Consultations.FindAsync(consultationId);
            if (consultation == null) return NotFound();

            consultation.TotalRating = (consultation.TotalRating ?? 0) - booking.Rating.Value;
            consultation.TotalVotes = Math.Max(0, consultation.TotalVotes - 1);

            booking.Rating = null;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Your rating has been removed.";
            return RedirectToAction("Details", "Consultation", new { id = consultationId });
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmCompletion(int bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();
            if (booking.ApplicationUserId != user.Id) return Unauthorized();

            booking.Status = BookingStatus.Completed;
            booking.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Consultation marked as completed.";
            return RedirectToAction("MyBookings");
        }

        [HttpPost]
        public async Task<IActionResult> ReportProblem(int bookingId, string problemDescription)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();
            if (booking.ApplicationUserId != user.Id) return Unauthorized();

            booking.Status = BookingStatus.Disputed;
            booking.ProblemDescription = problemDescription;  

            await _context.SaveChangesAsync();
            TempData["Error"] = "Problem reported. Admin will review.";
            return RedirectToAction("MyBookings");
        }


    }
}



















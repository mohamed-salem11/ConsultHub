using ConsultHub.Data;
using ConsultHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultHub.Controllers
{
    public class ConsultantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ConsultantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Profile(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var consultant = await _userManager.Users
                .Include(u => u.Consultations)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsConsultant);

            if (consultant == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            ViewBag.IsOwner = currentUser != null && currentUser.Id == consultant.Id;

            return View("Profile", consultant);
        }


        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsConsultant)
                return Forbid();

            var consultant = await _userManager.Users
                .Include(u => u.Consultations)
                .FirstOrDefaultAsync(u => u.Id == user.Id);


            ViewBag.IsOwner = true;
            return View("Profile", consultant);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsConsultant)
                return Forbid();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(ApplicationUser model, IFormFile PhotoFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsConsultant)
                return Forbid();


            user.Bio = model.Bio;
            user.Specialization = model.Specialization;
            user.IsConsultantRequestPending = true;

            if (PhotoFile != null && PhotoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(PhotoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await PhotoFile.CopyToAsync(fileStream);
                }

                user.Photo = "/uploads/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Something went wrong while Updating Profile.");
                return View(model);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("MyProfile");
        }
        [HttpGet]
        public async Task<IActionResult> MyBookedConsultations()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.IsConsultant)
                return RedirectToAction("Index", "Home");

            var bookings = await _context.Bookings
                .Include(b => b.ApplicationUser)
                .Include(b => b.Consultation)
                .Where(b => b.Consultation.ApplicationUserId == user.Id)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> StartWorking(int bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Consultation)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking?.Consultation.ApplicationUserId != user.Id)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("MyBookedConsultations");
            }

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "Booking is not in pending status.";
                return RedirectToAction("MyBookedConsultations");
            }

            booking.Status = BookingStatus.InProgress;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Booking status updated to In Progress.";
            return RedirectToAction("MyBookedConsultations");
        }

        [HttpPost]
        public async Task<IActionResult> RequestCompletion(int bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            var booking = await _context.Bookings
                .Include(b => b.Consultation)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

  
            if (booking.Consultation.ApplicationUserId != user.Id)
                return Unauthorized();

            booking.Status = BookingStatus.AwaitingConfirmation;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Completion request sent to client.";
            return RedirectToAction("MyBookedConsultations");
        }


    }
}













using ConsultHub.Data;
using ConsultHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsultHub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> ConsultantRequests()
        {
            var requests = await _userManager.Users
                .Where(u => u.IsConsultantRequestPending && !u.IsConsultant)
                .ToListAsync();

            return View(requests);
        }

  
        public async Task<IActionResult> ApproveConsultant(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsConsultant = true;
            user.IsConsultantRequestPending = false;
            await _userManager.UpdateAsync(user);

            TempData["Message"] = "Consultant approved successfully.";
            return RedirectToAction("ConsultantRequests");
        }

  
        public async Task<IActionResult> RejectConsultant(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.IsConsultantRequestPending = false;
            await _userManager.UpdateAsync(user);

            TempData["Message"] = "Consultant request rejected.";
            return RedirectToAction("ConsultantRequests");
        }


        public async Task<IActionResult> PendingConsultations()
        {
            var consultations = await _context.Consultations
                .Include(c => c.ApplicationUser)
                .Include(c => c.Category)
                .Where(c => c.Status == ConsultationStatus.Pending)
                .ToListAsync();

            return View(consultations);
        }


        public async Task<IActionResult> ApproveConsultation(int id)
        {
            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation == null) return NotFound();

            consultation.Status = ConsultationStatus.Approved;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Consultation approved successfully.";
            return RedirectToAction("PendingConsultations");
        }


        public async Task<IActionResult> RejectConsultation(int id)
        {
            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation == null) return NotFound();

            consultation.Status = ConsultationStatus.Rejected;
            _context.Update(consultation);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Consultation rejected successfully.";
            return RedirectToAction("PendingConsultations");
        }

        [HttpGet]
        public async Task<IActionResult> Disputes()
        {
            var disputes = await _context.Bookings
                .Include(b => b.ApplicationUser)  
                .Include(b => b.Consultation)
                    .ThenInclude(c => c.ApplicationUser)  
                .Where(b => b.Status == BookingStatus.Disputed)
                .ToListAsync();

            return View(disputes);
        }
        [HttpPost]
        public async Task<IActionResult> ResolveDispute(int bookingId, bool approveCompletion)
        {
            var booking = await _context.Bookings
                .Include(b => b.ApplicationUser)
                .Include(b => b.Consultation)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null) return NotFound();

            if (approveCompletion)
            {
                booking.Status = BookingStatus.Completed;
                booking.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                booking.Status = BookingStatus.Disputed; 
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Dispute resolved.";

        
            return RedirectToAction("Disputes");
        }


    }
}





















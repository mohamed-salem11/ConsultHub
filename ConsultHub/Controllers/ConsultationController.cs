using ConsultHub.Data;
using ConsultHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ConsultHub.Controllers
{
    public class ConsultationController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _usermanager;

        public ConsultationController(ApplicationDbContext context, UserManager<ApplicationUser> usermanager)
        {
            _context = context;
            _usermanager = usermanager;
        }

        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Consultations
                .Include(c => c.ApplicationUser)
                .Include(c => c.Category)
                .Where(c => c.Status ==  ConsultationStatus.Approved);

            return View(await applicationDbContext.ToListAsync());
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultation = await _context.Consultations
                .Include(c => c.ApplicationUser)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (consultation == null)
            {
                return NotFound();
            }

            return View(consultation);
        }


        [HttpGet]
        public async Task<IActionResult> ConsultationsByCategory(int id)
        {
            var consultations = await _context.Consultations
         .Include(c => c.ApplicationUser)
         .Include(c => c.Category)
         .Where(c => c.CategoryId == id && c.Status == ConsultationStatus.Approved).ToListAsync();



            return View(consultations);
        }

        public IActionResult SearchPage()
        {
            return View();
        }
        public IActionResult Search(string query)
        {
            if (string.IsNullOrEmpty(query))
                return RedirectToAction("Index");

            var consultations = _context.Consultations
                .Include(c => c.Category)
                .Include(c => c.ApplicationUser)
                .Include(c => c.Bookings)
                .Where(c =>
                    c.Title.Contains(query) ||
                    c.Description.Contains(query) ||
                    c.Category.Name.Contains(query) ||
                    c.ApplicationUser.FullName.Contains(query)
                )
                .ToList();

            return View("SearchResults", consultations);
        }

        [HttpGet]
        public IActionResult Create()
        {

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Consultation consultation, IFormFile imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0)
                {
                    ModelState.AddModelError("imageFile", "Please upload an image.");
                    ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", consultation.CategoryId);
                    return View(consultation);
                }

                string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                string[] allowedMimeTypes = { "image/jpeg", "image/jpg", "image/png" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension) || !allowedMimeTypes.Contains(imageFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError("imageFile", "Only JPG, JPEG, PNG files are allowed.");
                    ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", consultation.CategoryId);
                    return View(consultation);
                }

                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var fileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                consultation.CoverImageUrl = $"/uploads/{fileName}";

                var user = await _usermanager.GetUserAsync(User);
                consultation.ApplicationUserId = user.Id;

                consultation.TotalRating = 0;
                consultation.TotalVotes = 0;
                consultation.NumberOfClients = 0;
                consultation.Status = ConsultationStatus.Draft;

                _context.Add(consultation);
                await _context.SaveChangesAsync();

                return RedirectToAction("MyProfile", "Consultant", new { id = consultation.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error: {ex.Message}");
                ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", consultation.CategoryId);
                return View(consultation);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReview(int ConsultationId)
        {
            var user = await _usermanager.GetUserAsync(User);

            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.Id == ConsultationId && c.ApplicationUserId == user.Id);

            if (consultation == null)
                return Forbid();

            consultation.Status = ConsultationStatus.Pending;
            _context.Update(consultation);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Review request submitted successfully ";

            return RedirectToAction("Profile", "Consultant", new { id = user.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation == null)
            {
                return NotFound();
            }

            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", consultation.CategoryId);

            return View(consultation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Consultation updatedConsultation, IFormFile CoverImageFile)
        {
            if (id != updatedConsultation.Id)
            {
                return NotFound();
            }

            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation == null)
            {
                return NotFound();
            }


            consultation.Title = updatedConsultation.Title;
            consultation.Description = updatedConsultation.Description;
            consultation.Price = updatedConsultation.Price;
            consultation.CategoryId = updatedConsultation.CategoryId;


            if (CoverImageFile != null && CoverImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(CoverImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await CoverImageFile.CopyToAsync(stream);
                }

                consultation.CoverImageUrl = "/uploads/" + uniqueFileName;
            }

            _context.Update(consultation);
            await _context.SaveChangesAsync();


            return RedirectToAction("MyProfile", "Consultant");
        }



        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var consultation = await _context.Consultations
                .Include(c => c.ApplicationUser)
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (consultation == null)
            {
                return NotFound();
            }

            return View(consultation);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var consultation = await _context.Consultations.FindAsync(id);
            if (consultation != null)
            {
                _context.Consultations.Remove(consultation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

      
    }
}


















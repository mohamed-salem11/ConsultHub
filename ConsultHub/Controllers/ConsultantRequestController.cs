using ConsultHub.Data;
using ConsultHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ConsultHub.Controllers
{
    public class ConsultantRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _usermanager;

        public ConsultantRequestController(ApplicationDbContext context, UserManager<ApplicationUser> usermanager)
        {
            _context = context;
            _usermanager = usermanager;
        }

        [HttpGet]
        [Authorize]
        public IActionResult ConfirmJoin()
        {
            return View();
        }


        [HttpGet]
        [Authorize]
        public IActionResult SubmitRequest()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitRequest(ApplicationUser model, IFormFile PhotoFile)
        {
            var user = await _usermanager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

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

            var result = await _usermanager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Something went wrong while submitting your request.");
                return View(model);
            }
            await _context.SaveChangesAsync();
            TempData["Message"] = "Your Consultant request has been submitted and is pending admin approval.";
            return RedirectToAction("Index", "Category");

        }
    }
}

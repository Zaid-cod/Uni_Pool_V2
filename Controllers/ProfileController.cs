using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniPool01.Data;
using System.Threading.Tasks;
using System.IO;
using System;

namespace UniPool01.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProfileController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Helper methods
        private bool IsLoggedIn() => HttpContext.Session.GetString("UserEmail") != null;
        private string CurrentUserEmail() => HttpContext.Session.GetString("UserEmail") ?? "";

        // GET: /Profile/Edit
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var email = CurrentUserEmail();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(user);
        }
        // Add this method to Controllers/ProfileController.cs

        // GET: /Profile/ViewReviews
        [HttpGet]
        public async Task<IActionResult> ViewReviews()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var userEmail = CurrentUserEmail();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get all reviews for this user as a driver
            var reviews = await _db.Reviews
                .Include(r => r.Reviewer)
                .Include(r => r.Ride)
                .Where(r => r.DriverId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Calculate average rating
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            ViewBag.AverageRating = Math.Round(averageRating, 1);
            ViewBag.TotalReviews = reviews.Count;

            return View(reviews);
        }

        // POST: /Profile/UploadCarImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCarImage(IFormFile carImage)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var email = CurrentUserEmail();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Edit");
            }

            if (carImage != null && carImage.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(carImage.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Only image files (JPG, PNG, GIF) are allowed.";
                    return RedirectToAction("Edit");
                }

                // Validate file size (max 5MB)
                if (carImage.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "File size must be less than 5MB.";
                    return RedirectToAction("Edit");
                }

                try
                {
                    // Generate unique filename
                    var fileName = $"{user.Id}_{Guid.NewGuid()}{extension}";
                    var carsFolder = Path.Combine(_env.WebRootPath, "images", "cars");

                    // Ensure directory exists
                    Directory.CreateDirectory(carsFolder);

                    var filePath = Path.Combine(carsFolder, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await carImage.CopyToAsync(stream);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(user.CarImagePath))
                    {
                        var oldPath = Path.Combine(carsFolder, user.CarImagePath);
                        if (System.IO.File.Exists(oldPath))
                        {
                            System.IO.File.Delete(oldPath);
                        }
                    }

                    // Update user record
                    user.CarImagePath = fileName;
                    await _db.SaveChangesAsync();

                    TempData["Message"] = "Car image uploaded successfully!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error uploading image: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "Please select an image file.";
            }

            return RedirectToAction("Edit");
        }

        // POST: /Profile/RemoveCarImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCarImage()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var email = CurrentUserEmail();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null && !string.IsNullOrEmpty(user.CarImagePath))
            {
                // Delete file
                var filePath = Path.Combine(_env.WebRootPath, "images", "cars", user.CarImagePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                // Update database
                user.CarImagePath = null;
                await _db.SaveChangesAsync();

                TempData["Message"] = "Car image removed successfully.";
            }

            return RedirectToAction("Edit");
        }
    }
}
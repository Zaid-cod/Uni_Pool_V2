using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniPool01.Data;
using UniPool01.Models;
using System.Threading.Tasks;
using System.Linq;

namespace UniPool01.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReviewController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Helper methods
        private bool IsLoggedIn() => HttpContext.Session.GetString("UserEmail") != null;
        private string CurrentUserEmail() => HttpContext.Session.GetString("UserEmail") ?? "";

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int rideId, int rating, string comment)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var userEmail = CurrentUserEmail();
            var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (passenger == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("MyBookings", "Ride");
            }

            var ride = await _db.Rides.FindAsync(rideId);
            if (ride == null)
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("MyBookings", "Ride");
            }

            var driver = await _db.Users.FirstOrDefaultAsync(u => u.Email == ride.DriverEmail);
            if (driver == null)
            {
                TempData["Error"] = "Driver not found.";
                return RedirectToAction("MyBookings", "Ride");
            }

            // Prevent duplicate reviews
            var exists = await _db.Reviews.AnyAsync(r => r.RideId == rideId && r.ReviewerId == passenger.Id);
            if (exists)
            {
                TempData["Error"] = "You've already reviewed this ride.";
                return RedirectToAction("MyBookings", "Ride");
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("MyBookings", "Ride");
            }

            // Create review
            var review = new Review
            {
                RideId = rideId,
                ReviewerId = passenger.Id,
                DriverId = driver.Id,
                Rating = rating,
                Comment = comment ?? ""
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Review submitted successfully!";
            return RedirectToAction("MyBookings", "Ride");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniPool01.Data;
using UniPool01.Filters;
using System.Threading.Tasks;

namespace UniPool01.Controllers
{
    [AdminAuthorization]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.Users = await _db.Users.OrderBy(u => u.Id).ToListAsync();
            ViewBag.Rides = await _db.Rides
                .Include(r => r.Bookings)
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            ViewBag.TotalUsers = await _db.Users.CountAsync();
            ViewBag.TotalRides = await _db.Rides.CountAsync();
            ViewBag.TotalBookings = await _db.Bookings.CountAsync();

            return View();
        }

        // POST: /Admin/DeleteUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _db.Users.FindAsync(id);

            if (user != null)
            {
                // Prevent deleting admin account
                if (user.Role == "Admin")
                {
                    TempData["Error"] = "Cannot delete admin account.";
                    return RedirectToAction("Dashboard");
                }

                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
                TempData["Message"] = "User deleted successfully.";
            }
            else
            {
                TempData["Error"] = "User not found.";
            }

            return RedirectToAction("Dashboard");
        }

        // POST: /Admin/DeleteRide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRide(int id)
        {
            var ride = await _db.Rides.FindAsync(id);

            if (ride != null)
            {
                _db.Rides.Remove(ride);
                await _db.SaveChangesAsync();
                TempData["Message"] = "Ride deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Ride not found.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}
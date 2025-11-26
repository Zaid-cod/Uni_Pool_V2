using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using UniPool01.Models;
using System.Linq;
using System;
using UniPool01.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace UniPool01.Controllers
{
    public class RideController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RideController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Helper methods
        private bool IsLoggedIn() => HttpContext.Session.GetString("UserEmail") != null;
        private string CurrentUserEmail() => HttpContext.Session.GetString("UserEmail") ?? "";
        private string CurrentUserName() => HttpContext.Session.GetString("UserName") ?? "";

        public IActionResult Find()
        {
            return RedirectToAction("Index");
        }

        public IActionResult Offer()
        {
            return RedirectToAction("Add");
        }

        // GET: /Ride/Index
        public async Task<IActionResult> Index(string from, string to, string mode)
        {
            var list = _db.Rides.AsQueryable();

            if (!string.IsNullOrWhiteSpace(from))
                list = list.Where(r => r.Departure.ToLower().Contains(from.ToLower()));

            if (!string.IsNullOrWhiteSpace(to))
                list = list.Where(r => r.Destination.ToLower().Contains(to.ToLower()));

            if (!string.IsNullOrWhiteSpace(mode))
                list = list.Where(r => r.ModeOfTransport.ToLower() == mode.ToLower());

            var rides = await list.OrderBy(r => r.DepartureTime).ToListAsync();

            // Load driver info and calculate ratings
            foreach (var ride in rides)
            {
                if (!string.IsNullOrEmpty(ride.DriverEmail))
                {
                    var driver = await _db.Users.FirstOrDefaultAsync(u => u.Email == ride.DriverEmail);
                    if (driver != null)
                    {
                        ride.Driver = driver;

                        // Calculate driver's average rating
                        var driverReviews = await _db.Reviews
                            .Where(r => r.DriverId == driver.Id)
                            .ToListAsync();

                        if (driverReviews.Any())
                        {
                            ViewData[$"DriverRating_{driver.Id}"] = Math.Round(driverReviews.Average(r => r.Rating), 1);
                            ViewData[$"DriverReviewCount_{driver.Id}"] = driverReviews.Count;
                        }
                    }
                }
            }

            return View(rides);
        }

        // GET: /Ride/Add
        [HttpGet]
        public IActionResult Add()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var model = new Ride
            {
                DepartureTime = DateTime.Now.AddHours(1),
                ModeOfTransport = "Car",
                AvailableSeats = 1
            };
            return View(model);
        }

        // POST: /Ride/Add - ENHANCED WITH VALIDATION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Ride newRide)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            // Custom validation
            if (newRide.AvailableSeats < 1 || newRide.AvailableSeats > 50)
            {
                ModelState.AddModelError("AvailableSeats", "Seats must be between 1 and 50.");
            }

            if (newRide.DepartureTime <= DateTime.Now)
            {
                ModelState.AddModelError("DepartureTime", "Departure time must be in the future.");
            }

            if (!ModelState.IsValid)
            {
                return View(newRide);
            }

            // Strip seconds from datetime
            newRide.DepartureTime = new DateTime(
                newRide.DepartureTime.Year,
                newRide.DepartureTime.Month,
                newRide.DepartureTime.Day,
                newRide.DepartureTime.Hour,
                newRide.DepartureTime.Minute,
                0
            );

            newRide.DriverEmail = CurrentUserEmail();
            newRide.DriverName = CurrentUserName();

            _db.Rides.Add(newRide);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Ride posted successfully! 🎉";
            return RedirectToAction("Index");
        }

        // POST: /Ride/Join/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var ride = await _db.Rides.FindAsync(id);
            if (ride == null)
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("Index");
            }

            var userEmail = CurrentUserEmail();

            if (string.Equals(ride.DriverEmail, userEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "You cannot book your own ride.";
                return RedirectToAction("Index");
            }

            var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if (passenger == null)
            {
                TempData["Error"] = "User profile not found.";
                return RedirectToAction("Index");
            }

            var alreadyBooked = await _db.Bookings.AnyAsync(b => b.RideId == id && b.PassengerId == passenger.Id);
            if (alreadyBooked)
            {
                TempData["Error"] = "You have already booked this ride.";
                return RedirectToAction("Index");
            }

            if (ride.AvailableSeats <= 0)
            {
                TempData["Error"] = "No seats available.";
                return RedirectToAction("Index");
            }

            Booking newBooking = new Booking
            {
                RideId = id,
                PassengerId = passenger.Id,
                BookingStatus = "Confirmed"
            };
            _db.Bookings.Add(newBooking);

            ride.AvailableSeats--;
            _db.Rides.Update(ride);

            await _db.SaveChangesAsync();
            TempData["Message"] = "Successfully joined the ride! 🎉";
            return RedirectToAction("MyBookings");
        }

        // POST: /Ride/CancelBooking/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("MyBookings");
            }

            var ride = await _db.Rides.FindAsync(booking.RideId);
            if (ride != null)
            {
                ride.AvailableSeats++;
                _db.Rides.Update(ride);
            }

            _db.Bookings.Remove(booking);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Booking cancelled successfully.";
            return RedirectToAction("MyBookings");
        }

        // GET: /Ride/Manage/5
        public async Task<IActionResult> Manage(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var userEmail = CurrentUserEmail().ToLower();

            var ride = await _db.Rides
                .Include(r => r.Bookings)
                    .ThenInclude(b => b.Passenger)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null) return RedirectToAction("Index");

            if (ride.DriverEmail.ToLower() != userEmail)
                return RedirectToAction("Index");

            return View(ride);
        }

        // GET: /Ride/MyBookings - ENHANCED WITH DRIVER INFO
        public async Task<IActionResult> MyBookings()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var userEmail = CurrentUserEmail();
            var passenger = await _db.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (passenger == null) return RedirectToAction("Login", "Account");

            var myBookings = await _db.Bookings
                .Include(b => b.Ride)
                .Where(b => b.PassengerId == passenger.Id)
                .OrderByDescending(b => b.Ride.DepartureTime)
                .ToListAsync();

            // Load driver info for each ride
            foreach (var booking in myBookings)
            {
                if (!string.IsNullOrEmpty(booking.Ride.DriverEmail))
                {
                    var driver = await _db.Users.FirstOrDefaultAsync(u => u.Email == booking.Ride.DriverEmail);
                    if (driver != null)
                    {
                        booking.Ride.Driver = driver;
                    }
                }
            }

            // Get reviewed ride IDs
            var reviewedRides = await _db.Reviews
                .Where(r => r.ReviewerId == passenger.Id)
                .Select(r => r.RideId)
                .ToListAsync();

            ViewBag.ReviewedRides = reviewedRides;

            return View(myBookings);
        }

        // GET: /Ride/MyOffered
        public async Task<IActionResult> MyOffered()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var userEmail = CurrentUserEmail().ToLower();
            var mine = await _db.Rides
                .Include(r => r.Bookings)
                .Where(r => r.DriverEmail.ToLower() == userEmail)
                .OrderByDescending(r => r.DepartureTime)
                .ToListAsync();

            return View(mine);
        }

        // POST: /Ride/MarkComplete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkComplete(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var ride = await _db.Rides.FindAsync(id);
            var userEmail = CurrentUserEmail();

            if (ride == null || ride.DriverEmail.ToLower() != userEmail.ToLower())
            {
                TempData["Error"] = "Unauthorized or ride not found.";
                return RedirectToAction("MyOffered");
            }

            ride.IsCompleted = true;
            await _db.SaveChangesAsync();

            TempData["Message"] = "Ride marked as complete! 🎉";
            return RedirectToAction("MyOffered");
        }

        // POST: /Ride/DeleteRide
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteRide(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            var userEmail = CurrentUserEmail().ToLower();
            var ride = await _db.Rides
                .Include(r => r.Bookings)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (ride == null)
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("MyOffered");
            }

            if (ride.DriverEmail.ToLower() != userEmail)
            {
                TempData["Error"] = "You are not authorized to delete this ride.";
                return RedirectToAction("MyOffered");
            }

            if (ride.Bookings != null && ride.Bookings.Any())
            {
                TempData["Error"] = $"Cannot delete ride with {ride.Bookings.Count} passenger(s). Please ask them to cancel first.";
                return RedirectToAction("Manage", new { id = ride.Id });
            }

            _db.Rides.Remove(ride);
            await _db.SaveChangesAsync();

            TempData["Message"] = "Ride deleted successfully.";
            return RedirectToAction("MyOffered");
        }
    }
}
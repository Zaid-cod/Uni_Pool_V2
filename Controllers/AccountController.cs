using UniPool01.Models;
using UniPool01.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

using System.Linq;




namespace UniPool01.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model)
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                ViewBag.Error = "Email and password are required.";
                return View(model);
            }

            // Check if user already exists
            if (await _db.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower()))
            {
                ViewBag.Error = "A user with that email already exists.";
                return View(model);
            }

            // Add new user to database
            _db.Users.Add(new User
            {
                FullName = model.FullName ?? "",
                Email = model.Email.Trim(),
                Password = model.Password, // NOTE: Should be hashed in production
                Role = model.Email.ToLower() == "admin@unipool.com" ? "Admin" : "Student"
            });

            await _db.SaveChangesAsync();

            TempData["Message"] = "Registration successful. You may log in now.";
            return RedirectToAction("Login");
        }
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["Message"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            // Validate credentials
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == Email.ToLower() && u.Password == Password);

            if (user == null)
            {
                ViewBag.Error = "Invalid credentials.";
                return View();
            }

            // Store session data
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role); // Store role for admin check

            TempData["Message"] = "Login successful!";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout

    }

}

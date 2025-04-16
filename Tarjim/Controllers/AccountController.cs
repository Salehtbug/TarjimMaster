using Microsoft.AspNetCore.Mvc;
using Tarjim.Models;
using Tarjim.ViewModels;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text;

namespace Tarjim.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AccountController(IWebHostEnvironment env)
        {
            _context = new MyDbContext();
            _env = env;
        }

        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }


        
        public ActionResult RegisterStep1()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterStep1(RegisterStep1ViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "هذا البريد مستخدم مسبقاً.");
                return View(model);
            }

            TempData["Email"] = model.Email;
            TempData["Password"] = model.Password;
            TempData["AccountType"] = model.AccountType;

            return RedirectToAction("RegisterStep2");
        }

        [HttpGet]
        public ActionResult RegisterStep2()
        {
            if (TempData["Email"] == null) return RedirectToAction("RegisterStep1");
            TempData.Keep();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegisterStep2(RegisterStep2ViewModel model)
        {
            //if (!ModelState.IsValid)
            //{
            //    TempData.Keep();
            //    return View(model);
            //}

            var user = new User
            {
                Name = model.Name,
                Email = TempData["Email"] as string,
                PasswordHash = HashPassword(TempData["Password"] as string),
                AccountType = TempData["AccountType"] as string,
                Location = model.City,
                CreatedAt = DateTime.UtcNow
            };

            
            if (model.Avatar != null && model.Avatar.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(model.Avatar.FileName).ToLower();

                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("Avatar", "الرجاء اختيار صورة بصيغة PNG أو JPG");
                    TempData.Keep();
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    model.Avatar.CopyTo(stream);
                }

                user.AvatarUrl = "/uploads/" + fileName;
            }

            var role = _context.Roles.FirstOrDefault(r => r.RoleName == "Client");
            if (role != null)
            {
                user.Roles = new List<Role> { role };
            }
          

                _context.Users.Add(user);
            _context.SaveChanges();

            TempData.Clear();
            TempData["Success"] = "تم إنشاء الحساب بنجاح، يمكنك تسجيل الدخول الآن.";
            return RedirectToAction("Login");

        }

        public ActionResult ChooseRole()
        {
            return View();
        }

        public ActionResult LoginTranslator()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LoginTranslator(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var hashedPassword = HashPassword(model.Password);

            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == hashedPassword);

            if (user == null || !user.Roles.Any(r => r.RoleName == "Translator"))
            {
                ModelState.AddModelError("", "بيانات الدخول غير صحيحة أو الحساب ليس مترجماً.");
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", "Translator");

            return RedirectToAction("Dashboard", "Translator");
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var hashedPassword = HashPassword(model.Password);

            var user = _context.Users
                .Include(u => u.Roles)
                .FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == hashedPassword);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
                return View(model);
            }

            var role = user.Roles.FirstOrDefault()?.RoleName ?? "Unknown";

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", role);

            switch (role)
            {
                case "Admin":
                    return RedirectToAction("Dashboard", "Admin");
                case "Translator":
                    return RedirectToAction("Dashboard", "Translator");
                case "Client":
                    return RedirectToAction("Dashboard", "Client");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

        public ActionResult TranslatorRegisterStep1()
        {
            return View();
        }


    }
}

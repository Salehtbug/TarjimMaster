using Microsoft.AspNetCore.Mvc;
using Tarjim.Models;
using Tarjim.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace Tarjim.Controllers
{
    public class AuthController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AuthController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }


        public IActionResult ChooseRole()
        {
            return View();
        }

        public IActionResult SelectRole(string role)
        {
            switch (role.ToLower())
            {
                case "client":

                    return RedirectToAction("Login", "Auth");
                case "translator":

                    return RedirectToAction("LoginTranslator", "Auth");
                default:
                    return RedirectToAction("ChooseRole");
            }
        }

        // دوال إنشاء الحساب بخطوتين
        public IActionResult RegisterStep1()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegisterStep1(RegisterStep1ViewModel model)
        {
            if (ModelState.IsValid)
            {
                // التحقق مما إذا كان البريد الإلكتروني مستخدماً
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل.");
                    return View(model);
                }

                // تخزين بيانات الخطوة الأولى في TempData
                TempData["Step1_Email"] = model.Email;
                TempData["Step1_Password"] = model.Password;
                TempData["Step1_AccountType"] = model.AccountType;

                // الانتقال إلى الخطوة الثانية
                return RedirectToAction("RegisterStep2");
            }

            return View(model);
        }

        public IActionResult RegisterStep2()
        {
            // التحقق من وجود بيانات الخطوة الأولى
            if (TempData["Step1_Email"] == null || TempData["Step1_Password"] == null)
            {
                return RedirectToAction("RegisterStep1");
            }

            var model = new RegisterStep2ViewModel
            {
                Email = TempData["Step1_Email"].ToString(),
                Password = TempData["Step1_Password"].ToString(),
                AccountType = TempData["Step1_AccountType"]?.ToString() ?? "Personal"
            };

            // إعادة تخزين البيانات في TempData لأنها تُقرأ مرة واحدة فقط
            TempData.Keep("Step1_Email");
            TempData.Keep("Step1_Password");
            TempData.Keep("Step1_AccountType");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterStep2(RegisterStep2ViewModel model)
        {
            // التحقق من وجود بيانات الخطوة الأولى
            if (TempData["Step1_Email"] == null || TempData["Step1_Password"] == null)
            {
                return RedirectToAction("RegisterStep1");
            }

            model.Email = TempData["Step1_Email"].ToString();
            model.Password = TempData["Step1_Password"].ToString();
            model.AccountType = TempData["Step1_AccountType"]?.ToString() ?? "Personal";

            if (ModelState.IsValid)
            {
                // التحقق مرة أخرى مما إذا كان البريد الإلكتروني مستخدماً
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل.");
                    return View(model);
                }

                // إنشاء موقع من البلد والمدينة
                string location = !string.IsNullOrEmpty(model.City) && !string.IsNullOrEmpty(model.Country)
                    ? $"{model.City}, {model.Country}"
                    : (!string.IsNullOrEmpty(model.City) ? model.City : model.Country);

                // إنشاء حساب مستخدم جديد
                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    AccountType = model.AccountType,
                    Location = location,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // معالجة الصورة الشخصية إذا تم تحميلها
                if (model.Avatar != null && model.Avatar.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.Avatar.FileName;
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Avatar.CopyToAsync(fileStream);
                    }

                    user.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // إضافة دور العميل
                var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Client");
                if (clientRole != null)
                {
                    if (user.Roles == null)
                    {
                        user.Roles = new List<Role>();
                    }
                    user.Roles.Add(clientRole);
                    await _context.SaveChangesAsync();
                }

                // إضافة إعدادات المستخدم
                _context.UserSettings.Add(new UserSetting
                {
                    UserId = user.UserId,
                    NotificationEmail = true,
                    NotificationSystem = true,
                    LanguagePreference = "ar",
                    Theme = "light"
                });
                await _context.SaveChangesAsync();

                // إضافة سجل النشاط
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.UserId,
                    ActivityType = "Register",
                    Description = "تم إنشاء حساب عميل جديد",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                await LoginUserAfterRegistration(user);

                TempData.Clear();

                TempData["SuccessMessage"] = "تم إنشاء الحساب بنجاح. يرجى تسجيل الدخول الآن.";

                return RedirectToAction("Login", "Auth");
            }

            // إعادة تخزين البيانات في TempData لأنها تُقرأ مرة واحدة فقط
            TempData.Keep("Step1_Email");
            TempData.Keep("Step1_Password");
            TempData.Keep("Step1_AccountType");

            return View(model);
        }

        // إعادة توجيه من طريقة التسجيل القديمة إلى الطريقة الجديدة
        public IActionResult RegisterClient()
        {
            return RedirectToAction("RegisterStep1");
        }

        // طرق تسجيل الدخول
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && VerifyPassword(user.PasswordHash, model.Password))
                {
                    // عكس المنطق هنا - تحقق إذا كان الحساب غير نشط
                    if (user.IsActive != true)
                    {
                        ModelState.AddModelError(string.Empty, "هذا الحساب غير نشط.");
                        return View(model);
                    }

                    var userWithRoles = await _context.Users
                        .Include(u => u.Roles)
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                    var roleNames = new List<string>();
                    if (userWithRoles?.Roles != null)
                    {
                        foreach (var role in userWithRoles.Roles)
                        {
                            roleNames.Add(role.RoleName);
                        }
                    }

                    // التحقق من أن المستخدم هو عميل
                    if (!roleNames.Contains("Client"))
                    {
                        ModelState.AddModelError(string.Empty, "هذا الحساب ليس مرتبطاً بعميل. يرجى استخدام صفحة تسجيل دخول المترجمين.");
                        return View(model);
                    }

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

                    foreach (var roleName in roleNames)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleName));
                    }

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
                        });

                    user.LastLogin = DateTime.UtcNow;
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = user.UserId,
                        ActivityType = "Login",
                        Description = "تم تسجيل دخول عميل بنجاح",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    // توجيه العميل إلى صفحته الرئيسية
                    return RedirectToAction("Index", "Client");
                }

                ModelState.AddModelError(string.Empty, "محاولة تسجيل دخول غير صالحة.");
            }

            return View(model);
        }

        // دالة لعرض صفحة تسجيل دخول المترجم
        public IActionResult LoginTranslator(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // دالة لمعالجة تسجيل دخول المترجم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginTranslator(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // التحقق من بيانات المدير أولاً
                if (model.Email == "Tarjim@info.com" && model.Password == "123")
                {
                    // البحث عن المستخدم المدير
                    var adminUser = await _context.Users
                        .Include(u => u.Roles)
                        .FirstOrDefaultAsync(u => u.Email == "Tarjim@info.com");

                    // إنشاء حساب المدير إذا لم يكن موجودًا
                    if (adminUser == null)
                    {
                        adminUser = new User
                        {
                            Name = "مدير النظام",
                            Email = "Tarjim@info.com",
                            PasswordHash = HashPassword("123"),
                            AccountType = "Admin",
                            IsActive = true,
                            IsVerified = true,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Users.Add(adminUser);
                        await _context.SaveChangesAsync();
                    }

                    // التحقق من وجود دور المدير للمستخدم
                    bool hasAdminRole = false;
                    if (adminUser.Roles != null)
                    {
                        hasAdminRole = adminUser.Roles.Any(r => r.RoleName == "Admin");
                    }

                    // إضافة دور المدير إذا لم يكن موجودًا
                    if (!hasAdminRole)
                    {
                        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");

                        // إنشاء دور المدير إذا لم يكن موجودًا
                        if (adminRole == null)
                        {
                            adminRole = new Role { RoleName = "Admin" };
                            _context.Roles.Add(adminRole);
                            await _context.SaveChangesAsync();
                        }

                        // إضافة الدور للمستخدم
                        if (adminUser.Roles == null)
                        {
                            adminUser.Roles = new List<Role>();
                        }
                        adminUser.Roles.Add(adminRole);
                        await _context.SaveChangesAsync();
                    }

                    // إضافة دور المترجم أيضًا إذا لم يكن موجودًا (اختياري)
                    bool hasTranslatorRole = false;
                    if (adminUser.Roles != null)
                    {
                        hasTranslatorRole = adminUser.Roles.Any(r => r.RoleName == "Translator");
                    }

                    if (!hasTranslatorRole)
                    {
                        var translatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Translator");
                        if (translatorRole != null)
                        {
                            adminUser.Roles.Add(translatorRole);
                            await _context.SaveChangesAsync();
                        }
                    }

                    // إنشاء إعدادات المستخدم إذا لم تكن موجودة
                    var userSettings = await _context.UserSettings.FirstOrDefaultAsync(us => us.UserId == adminUser.UserId);
                    if (userSettings == null)
                    {
                        _context.UserSettings.Add(new UserSetting
                        {
                            UserId = adminUser.UserId,
                            NotificationEmail = true,
                            NotificationSystem = true,
                            LanguagePreference = "ar",
                            Theme = "light"
                        });
                        await _context.SaveChangesAsync();
                    }

                    // إنشاء المطالبات للمدير
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, adminUser.UserId.ToString()),
                new Claim(ClaimTypes.Name, adminUser.Name),
                new Claim(ClaimTypes.Email, adminUser.Email),
                new Claim(ClaimTypes.Role, "Admin")
            };

                    if (hasTranslatorRole)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Translator"));
                    }

                    // إنشاء هوية المطالبات وتسجيل الدخول
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
                        });

                    // تحديث وقت آخر تسجيل دخول
                    adminUser.LastLogin = DateTime.UtcNow;
                    _context.Update(adminUser);

                    // إضافة سجل النشاط
                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = adminUser.UserId,
                        ActivityType = "Login",
                        Description = "تم تسجيل دخول مدير النظام بنجاح",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    // توجيه المدير إلى لوحة التحكم الخاصة به
                    return RedirectToAction("Index", "Admin");
                }

                // الكود الأصلي لتسجيل دخول المترجم
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && VerifyPassword(user.PasswordHash, model.Password))
                {
                    // التحقق من حالة الحساب
                    if (user.IsActive != true)
                    {
                        ModelState.AddModelError(string.Empty, "هذا الحساب غير نشط.");
                        return View(model);
                    }

                    var userWithRoles = await _context.Users
                        .Include(u => u.Roles)
                        .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                    var roleNames = new List<string>();
                    if (userWithRoles?.Roles != null)
                    {
                        foreach (var role in userWithRoles.Roles)
                        {
                            roleNames.Add(role.RoleName);
                        }
                    }

                    // التحقق من أن المستخدم هو مترجم
                    if (!roleNames.Contains("Translator"))
                    {
                        ModelState.AddModelError(string.Empty, "هذا الحساب ليس مرتبطاً بمترجم. يرجى استخدام صفحة تسجيل دخول العملاء.");
                        return View(model);
                    }

                    // إنشاء المطالبات (claims)
                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

                    foreach (var roleName in roleNames)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleName));
                    }

                    // إنشاء هوية المطالبات
                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    // تسجيل دخول المستخدم
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe,
                            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
                        });

                    // تحديث وقت آخر تسجيل دخول
                    user.LastLogin = DateTime.UtcNow;
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    // إضافة سجل النشاط
                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = user.UserId,
                        ActivityType = "Login",
                        Description = "تم تسجيل دخول مترجم بنجاح",
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    // توجيه المترجم إلى الصفحة الرئيسية الخاصة به
                    return RedirectToAction("Index", "Translator");
                }

                ModelState.AddModelError(string.Empty, "محاولة تسجيل دخول غير صالحة.");
            }

            return View(model);
        }


        public IActionResult RegisterTranslator()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterTranslator(TranslatorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {

                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل.");
                    return View(model);
                }


                var user = new User
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = HashPassword(model.Password),
                    AccountType = "Personal",
                    Location = model.Location,
                    Bio = model.Bio,
                    IsActive = true,
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();


                var translatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Translator");
                if (translatorRole != null)
                {
                    user.Roles = new List<Role> { translatorRole };
                    await _context.SaveChangesAsync();
                }



                _context.UserSettings.Add(new UserSetting
                {
                    UserId = user.UserId,
                    NotificationEmail = true,
                    NotificationSystem = true,
                    LanguagePreference = "ar",
                    Theme = "light"
                });
                await _context.SaveChangesAsync();


                if (!string.IsNullOrEmpty(model.Education))
                {
                    _context.TranslatorCvs.Add(new TranslatorCv
                    {
                        TranslatorId = user.UserId,
                        Type = "Education",
                        Value = model.Education
                    });
                }

                if (!string.IsNullOrEmpty(model.Experience))
                {
                    _context.TranslatorCvs.Add(new TranslatorCv
                    {
                        TranslatorId = user.UserId,
                        Type = "Experience",
                        Value = model.Experience
                    });
                }

                await _context.SaveChangesAsync();


                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.UserId,
                    ActivityType = "Register",
                    Description = "تم إنشاء حساب مترجم جديد",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();


                await LoginUserAfterRegistration(user);

                return RedirectToAction("Index", "Translator");
            }

            return View(model);
        }


        public async Task<IActionResult> Logout()
        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "Logout",
                    Description = "تم تسجيل الخروج",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }




        private bool VerifyPassword(string hashedPassword, string password)
        {

            return hashedPassword == HashPassword(password);
        }


        private string HashPassword(string password)
        {

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        }

        private async Task LoginUserAfterRegistration(User user)
        {

            var userWithRoles = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == user.UserId);


            var roleNames = new List<string>();
            if (userWithRoles?.Roles != null)
            {
                foreach (var role in userWithRoles.Roles)
                {
                    roleNames.Add(role.RoleName);
                }
            }


            /*
            var roleNames = await _context.Set<UserRole>()
                .Where(ur => ur.UserId == user.UserId)
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.RoleId,
                    (ur, r) => r.RoleName)
                .ToListAsync();
            */

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roleNames)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));
        }


        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
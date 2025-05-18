using Microsoft.AspNetCore.Mvc;
using Tarjim.Models;
using Tarjim.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Tarjim.Controllers
{
    public class TranslatorRegistrationController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TranslatorRegistrationController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            // إعادة توجيه إلى الخطوة الأولى
            return RedirectToAction("Step1");
        }

        #region Step1 - المعلومات الشخصية

        public IActionResult Step1()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1(TranslatorRegisterStep1ViewModel model)
        {
            if (ModelState.IsValid)
            {
                // التحقق من عدم وجود بريد إلكتروني مكرر
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم بالفعل");
                    return View(model);
                }

                // حفظ البيانات في TempData للانتقال بين الخطوات
                TempData["Step1_Name"] = model.Name;
                TempData["Step1_Email"] = model.Email;
                TempData["Step1_Password"] = model.Password;
                TempData["Step1_Bio"] = model.Bio;

                // معالجة الصورة إذا تم تحميلها
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

                    TempData["Step1_AvatarUrl"] = "/uploads/avatars/" + uniqueFileName;
                }

                // الانتقال إلى الخطوة التالية
                return RedirectToAction("Step2");
            }

            return View(model);
        }
        #endregion

        #region Step2 - الموقع

        public IActionResult Step2()
        {
            // التحقق من وجود بيانات الخطوة الأولى
            if (TempData["Step1_Email"] == null || TempData["Step1_Password"] == null)
            {
                return RedirectToAction("Step1");
            }

            // الاحتفاظ بالبيانات في TempData للاستخدام لاحقاً
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step1_")).ToList())
            {
                TempData.Keep(key);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step2(TranslatorRegisterStep2ViewModel model)
        {
            // التحقق من وجود بيانات الخطوة الأولى
            if (TempData["Step1_Email"] == null || TempData["Step1_Password"] == null)
            {
                return RedirectToAction("Step1");
            }

            if (ModelState.IsValid)
            {
                // حفظ بيانات الموقع
                TempData["Step2_Country"] = model.Country;
                TempData["Step2_City"] = model.City;

                // الاحتفاظ ببيانات الخطوة الأولى
                foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step1_")).ToList())
                {
                    TempData.Keep(key);
                }

                // الانتقال إلى الخطوة التالية
                return RedirectToAction("Step3");
            }

            // الاحتفاظ ببيانات الخطوة الأولى
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step1_")).ToList())
            {
                TempData.Keep(key);
            }

            return View(model);
        }
        #endregion

        #region Step3 - الوثائق والتعليم

        public IActionResult Step3()
        {
            // التحقق من وجود بيانات الخطوات السابقة
            if (TempData["Step1_Email"] == null || TempData["Step2_Country"] == null)
            {
                return RedirectToAction("Step1");
            }

            // الاحتفاظ ببيانات الخطوات السابقة
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Keep(key);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step3(TranslatorRegisterStep3ViewModel model)
        {
            // التحقق من وجود بيانات الخطوات السابقة
            if (TempData["Step1_Email"] == null || TempData["Step2_Country"] == null)
            {
                return RedirectToAction("Step1");
            }

            if (ModelState.IsValid)
            {
                // حفظ بيانات التعليم
                TempData["Step3_University"] = model.University;
                TempData["Step3_Major"] = model.Major;
                TempData["Step3_FieldOfStudy"] = model.FieldOfStudy;

                // معالجة ملفات الأعمال السابقة إذا تم تحميلها
                if (model.WorkSample != null && model.WorkSample.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.WorkSample.FileName;
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "samples");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.WorkSample.CopyToAsync(fileStream);
                    }

                    TempData["Step3_WorkSampleUrl"] = "/uploads/samples/" + uniqueFileName;
                }

                // معالجة ملف عضوية جمعية المترجمين إذا تم تحميله
                if (model.MembershipDocument != null && model.MembershipDocument.Length > 0)
                {
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.MembershipDocument.FileName;
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "membership");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.MembershipDocument.CopyToAsync(fileStream);
                    }

                    TempData["Step3_MembershipUrl"] = "/uploads/membership/" + uniqueFileName;
                }

                // الاحتفاظ ببيانات الخطوات السابقة
                foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step1_") || k.StartsWith("Step2_")).ToList())
                {
                    TempData.Keep(key);
                }

                // الانتقال إلى الخطوة التالية
                return RedirectToAction("Step4");
            }

            // الاحتفاظ ببيانات الخطوات السابقة
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Keep(key);
            }

            return View(model);
        }
        #endregion

        #region Step4 - الشهادات

        public IActionResult Step4()
        {
            // التحقق من وجود بيانات الخطوات السابقة
            if (TempData["Step1_Email"] == null)
            {
                return RedirectToAction("Step1");
            }

            // الاحتفاظ ببيانات الخطوات السابقة
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Keep(key);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step4(TranslatorRegisterStep4ViewModel model)
        {
            // التحقق من وجود بيانات الخطوات السابقة
            if (TempData["Step1_Email"] == null)
            {
                return RedirectToAction("Step1");
            }

            if (ModelState.IsValid)
            {
                // حفظ بيانات الشهادات
                TempData["Step4_CertificateName"] = model.CertificateName;
                TempData["Step4_Institution"] = model.Institution;
                TempData["Step4_IssueMonth"] = model.IssueMonth;
                TempData["Step4_IssueYear"] = model.IssueYear;

                // الاحتفاظ ببيانات الخطوات السابقة
                foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step1_") ||
                                                            k.StartsWith("Step2_") ||
                                                            k.StartsWith("Step3_")).ToList())
                {
                    TempData.Keep(key);
                }

                // الانتقال إلى الخطوة التالية
                return RedirectToAction("Step5");
            }

            // الاحتفاظ ببيانات الخطوات السابقة
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Keep(key);
            }

            return View(model);
        }
        #endregion

        #region Step5 - المهارات

        public IActionResult Step5()
        {
            // التحقق من وجود بيانات الخطوات السابقة
            if (TempData["Step1_Email"] == null)
            {
                return RedirectToAction("Step1");
            }

            // الاحتفاظ ببيانات الخطوات السابقة
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Keep(key);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step5(TranslatorRegisterStep5ViewModel model)
        {
            // التحقق من وجود بيانات الخطوات السابقة  
            if (TempData["Step1_Email"] == null)
            {
                return RedirectToAction("Step1");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // استرجاع البيانات من جميع الخطوات  
                    string name = TempData["Step1_Name"]?.ToString() ?? string.Empty;
                    string email = TempData["Step1_Email"]?.ToString() ?? string.Empty;
                    string password = TempData["Step1_Password"]?.ToString() ?? string.Empty;
                    string bio = TempData["Step1_Bio"]?.ToString();
                    string avatarUrl = TempData["Step1_AvatarUrl"]?.ToString();

                    string country = TempData["Step2_Country"]?.ToString();
                    string city = TempData["Step2_City"]?.ToString();

                    string university = TempData["Step3_University"]?.ToString();
                    string major = TempData["Step3_Major"]?.ToString();
                    string fieldOfStudy = TempData["Step3_FieldOfStudy"]?.ToString();
                    string workSampleUrl = TempData["Step3_WorkSampleUrl"]?.ToString();
                    string membershipUrl = TempData["Step3_MembershipUrl"]?.ToString();

                    string certificateName = TempData["Step4_CertificateName"]?.ToString();
                    string institution = TempData["Step4_Institution"]?.ToString();
                    string issueMonth = TempData["Step4_IssueMonth"]?.ToString();
                    string issueYear = TempData["Step4_IssueYear"]?.ToString();

                    // إنشاء الموقع (الدولة، المدينة)  
                    string location = !string.IsNullOrEmpty(city) && !string.IsNullOrEmpty(country)
                        ? $"{city}, {country}"
                        : (!string.IsNullOrEmpty(city) ? city : country);

                    // إنشاء حساب المستخدم المترجم  
                    var user = new User
                    {
                        Name = name,
                        Email = email,
                        PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)),
                        AccountType = "Personal",
                        Location = location,
                        Bio = bio,
                        AvatarUrl = avatarUrl,
                        IsActive = true,
                        IsVerified = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // إضافة دور المترجم  
                    var translatorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Translator");
                    if (translatorRole != null)
                    {
                        if (user.Roles == null)
                        {
                            user.Roles = new List<Role>();
                        }
                        user.Roles.Add(translatorRole);
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

                    // إضافة معلومات التعليم للمترجم  
                    if (!string.IsNullOrEmpty(university) || !string.IsNullOrEmpty(major))
                    {
                        _context.TranslatorCvs.Add(new TranslatorCv
                        {
                            TranslatorId = user.UserId,
                            Type = "Education",
                            Value = $"{university} - {major} - {fieldOfStudy}"
                        });
                    }

                    // إضافة معلومات الشهادات  
                    if (!string.IsNullOrEmpty(certificateName))
                    {
                        _context.TranslatorCvs.Add(new TranslatorCv
                        {
                            TranslatorId = user.UserId,
                            Type = "Certification",
                            Value = $"{certificateName} - {institution} - {issueMonth}/{issueYear}"
                        });
                    }

                    // تعديل جزء من دالة Step5 في الكنترولر

                    if (model.SelectedServiceTypes != null && model.SelectedServiceTypes.Count > 0)
                    {
                        foreach (var serviceTypeId in model.SelectedServiceTypes)
                        {
                            string serviceTypeName = GetServiceTypeName(serviceTypeId);
                            var serviceSkill = await GetOrCreateSkill($"خدمة: {serviceTypeName}");
                            if (serviceSkill != null)
                            {
                                if (user.Skills == null)
                                {
                                    user.Skills = new List<Skill>();
                                }
                                user.Skills.Add(serviceSkill);
                            }
                        }
                    }

                    if (model.SelectedSpecializations != null && model.SelectedSpecializations.Count > 0)
                    {
                        foreach (var specializationId in model.SelectedSpecializations)
                        {
                            string specializationName = GetSpecializationName(specializationId);
                            var specializationSkill = await GetOrCreateSkill($"تخصص: {specializationName}");
                            if (specializationSkill != null)
                            {
                                if (user.Skills == null)
                                {
                                    user.Skills = new List<Skill>();
                                }
                                user.Skills.Add(specializationSkill);
                            }
                        }
                    }

                    if (model.SourceLanguages != null && model.SourceLanguages.Count > 0)
                    {
                        List<string> sourceLanguageNames = new List<string>();
                        foreach (var langId in model.SourceLanguages)
                        {
                            string langName = GetLanguageName(langId);
                            sourceLanguageNames.Add(langName);
                        }

                        var sourceLangs = string.Join(", ", sourceLanguageNames);

                        _context.TranslatorCvs.Add(new TranslatorCv
                        {
                            TranslatorId = user.UserId,
                            Type = "SourceLanguages",
                            Value = sourceLangs
                        });
                    }

                    if (model.TargetLanguages != null && model.TargetLanguages.Count > 0)
                    {
                        List<string> targetLanguageNames = new List<string>();
                        foreach (var langId in model.TargetLanguages)
                        {
                            string langName = GetLanguageName(langId);
                            targetLanguageNames.Add(langName);
                        }

                        var targetLangs = string.Join(", ", targetLanguageNames);

                        _context.TranslatorCvs.Add(new TranslatorCv
                        {
                            TranslatorId = user.UserId,
                            Type = "TargetLanguages",
                            Value = targetLangs
                        });
                    }

                    if (model.AvailableDays != null && model.AvailableDays.Count > 0)
                    {
                        List<string> dayNames = new List<string>();
                        foreach (var dayId in model.AvailableDays)
                        {
                            string dayName = GetDayName(dayId);
                            dayNames.Add(dayName);
                        }

                        var availableDays = string.Join(", ", dayNames);

                        _context.TranslatorCvs.Add(new TranslatorCv
                        {
                            TranslatorId = user.UserId,
                            Type = "Availability",
                            Value = availableDays
                        });
                    }

                    // إضافة سجل النشاط  
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

                   
                    //await LoginUserAfterRegistration(user);

                    TempData.Clear();

                   
                    TempData["SuccessMessage"] = "تم إنشاء حسابك بنجاح. يمكنك الآن تسجيل الدخول باستخدام بياناتك.";

                     
                    return RedirectToAction("LoginTranslator", "Auth");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "حدث خطأ أثناء إنشاء الحساب: " + ex.Message);

                    foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
                    {
                        TempData.Keep(key);
                    }

                    return View(model);
                }
            }

            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Keep(key);
            }

            return View(model);
        }
        #endregion

        #region Helper Methods
        
        private async Task<Skill> GetOrCreateSkill(string skillName)
        {
            var skill = await _context.Skills.FirstOrDefaultAsync(s => s.SkillName == skillName);
            if (skill == null)
            {
                skill = new Skill { SkillName = skillName };
                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();
            }
            return skill;
        }

        // الحصول على اسم نوع الخدمة
        private string GetServiceTypeName(int id)
        {
            return id switch
            {
                1 => "ترجمة نصية",
                2 => "ترجمة فورية",
                3 => "تدقيق لغوي",
                4 => "تحرير محتوى",
                5 => "تعريب",
                6 => "تفريغ صوتي",
                7 => "توطين",
                _ => "خدمة أخرى"
            };
        }

        // الحصول على اسم التخصص
        private string GetSpecializationName(int id)
        {
            return id switch
            {
                1 => "قانوني",
                2 => "طبي",
                3 => "تقني",
                4 => "أدبي",
                5 => "تسويقي",
                6 => "اقتصادي",
                7 => "تعليمي",
                8 => "إعلامي",
                9 => "ديني",
                _ => "تخصص آخر"
            };
        }

        // الحصول على اسم اللغة
        private string GetLanguageName(int id)
        {
            return id switch
            {
                1 => "العربية",
                2 => "الإنجليزية",
                3 => "الفرنسية",
                4 => "الإسبانية",
                5 => "الألمانية",
                6 => "الإيطالية",
                7 => "البرتغالية",
                8 => "الروسية",
                9 => "الصينية",
                10 => "اليابانية",
                11 => "الكورية",
                12 => "التركية",
                _ => "لغة أخرى"
            };
        }

        // الحصول على اسم اليوم
        private string GetDayName(int id)
        {
            return id switch
            {
                1 => "السبت",
                2 => "الأحد",
                3 => "الإثنين",
                4 => "الثلاثاء",
                5 => "الأربعاء",
                6 => "الخميس",
                7 => "الجمعة",
                _ => "يوم آخر"
            };
        }
        #endregion

        //// دالة مساعدة لتسجيل الدخول تلقائياً بعد التسجيل
        //private async Task LoginUserAfterRegistration(User user)
        //{
        //    var userWithRoles = await _context.Users
        //        .Include(u => u.Roles)
        //        .FirstOrDefaultAsync(u => u.UserId == user.UserId);

        //    var roleNames = new List<string>();
        //    if (userWithRoles?.Roles != null)
        //    {
        //        foreach (var role in userWithRoles.Roles)
        //        {
        //            roleNames.Add(role.RoleName);
        //        }
        //    }

        //    var claims = new List<Claim>
        //    {
        //        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        //        new Claim(ClaimTypes.Name, user.Name),
        //        new Claim(ClaimTypes.Email, user.Email)
        //    };

        //    foreach (var role in roleNames)
        //    {
        //        claims.Add(new Claim(ClaimTypes.Role, role));
        //    }

        //    var claimsIdentity = new ClaimsIdentity(
        //        claims, CookieAuthenticationDefaults.AuthenticationScheme);

        //    await HttpContext.SignInAsync(
        //        CookieAuthenticationDefaults.AuthenticationScheme,
        //        new ClaimsPrincipal(claimsIdentity));
        //}
    }
}
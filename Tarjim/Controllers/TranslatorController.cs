using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Tarjim.Models;
using Tarjim.ViewModels;

namespace Tarjim.Controllers
{
    [Authorize(Roles = "Translator")]
    public class TranslatorController : Controller
    {
        private readonly MyDbContext _context;

        public TranslatorController(MyDbContext context)
        {
            _context = context;
        }

        // لوحة تحكم المترجم
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // المشاريع المتاحة - أضف هذا الجزء
            var availableProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .Where(p => p.Status == "Open") // مشاريع مفتوحة
                .OrderByDescending(p => p.CreatedAt)
                .Take(5) // عرض أحدث 5 مشاريع فقط في الصفحة الرئيسية
                .ToListAsync();

            // المشاريع قيد التنفيذ
            var activeProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .Where(p => p.AssignedTranslatorId == userIdInt &&
                        (p.Status == "Assigned" || p.Status == "In Progress"))
                .OrderByDescending(p => p.Deadline)
                .ToListAsync();

            // المشاريع المكتملة مؤخراً
            var completedProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .Where(p => p.AssignedTranslatorId == userIdInt && p.Status == "Completed")
                .OrderByDescending(p => p.UpdatedAt)
                .Take(5)
                .ToListAsync();

            // العروض قيد الانتظار
            var pendingOffers = await _context.ProjectOffers
                .Include(o => o.Project)
                .ThenInclude(p => p.SourceLanguage)
                .Include(o => o.Project.TargetLanguage)
                .Include(o => o.Project.Category)
                .Where(o => o.TranslatorId == userIdInt && o.OfferStatus == "Pending")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // الإشعارات الغير مقروءة
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userIdInt && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            // جلب معلومات المترجم ومهاراته وخبراته
            var translatorUser = await _context.Users
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            var translatorCv = await _context.TranslatorCvs
                .Where(cv => cv.TranslatorId == userIdInt)
                .ToListAsync();

            // جلب إجمالي التقييمات
            var totalRating = await _context.Reviews
                .Where(r => r.TranslatorId == userIdInt)
                .Select(r => (double?)r.Rating)
                .AverageAsync() ?? 0;

            var totalReviews = await _context.Reviews
                .Where(r => r.TranslatorId == userIdInt)
                .CountAsync();

            // إنشاء نموذج لوحة التحكم
            var viewModel = new TranslatorDashboardViewModel
            {
                AvailableProjects = availableProjects, // أضف هذا السطر
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                PendingOffers = pendingOffers,
                Unreadnotifications = unreadNotifications,
                Translator = translatorUser,
                TranslatorCv = translatorCv,
                TotalRating = totalRating,
                TotalReviews = totalReviews
            };

            return View(viewModel);
        }

        // عرض مشروع وعمل عليه
        public async Task<IActionResult> WorkOnProject(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.AssignedTranslatorId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من أن المشروع مسند أو قيد التنفيذ
            if (project.Status != "Assigned" && project.Status != "In Progress")
            {
                TempData["ErrorMessage"] = "لا يمكنك العمل على هذا المشروع في الحالة الحالية.";
                return RedirectToAction("Index");
            }

            // جلب ملفات المشروع الأصلية
            var originalFiles = await _context.ProjectFiles
                .Where(f => f.ProjectId == id && f.IsOriginal == true)
                .ToListAsync();

            // جلب ملفات المشروع المترجمة
            var translatedFiles = await _context.ProjectFiles
                .Where(f => f.ProjectId == id && f.IsOriginal == false)
                .ToListAsync();

            // جلب متطلبات المشروع
            var requirements = await _context.ProjectRequirements
                .Where(r => r.ProjectId == id)
                .ToListAsync();

            var viewModel = new WorkOnProjectViewModel
            {
                Project = project,
                OriginalFiles = originalFiles,
                TranslatedFiles = translatedFiles,
                Requirements = requirements
            };

            return View(viewModel);
        }

        // تحديث حالة المشروع إلى قيد التنفيذ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartWorking(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.AssignedTranslatorId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من أن المشروع مسند
            if (project.Status != "Assigned")
            {
                TempData["ErrorMessage"] = "لا يمكن تحديث حالة هذا المشروع.";
                return RedirectToAction("WorkOnProject", new { id = project.ProjectId });
            }

            // تحديث حالة المشروع
            project.Status = "In Progress";
            project.UpdatedAt = DateTime.UtcNow;
            _context.Update(project);

            // إضافة نشاط بدء العمل
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "StartWorking",
                EntityType = "Project",
                EntityId = project.ProjectId,
                Description = "تم بدء العمل على المشروع: " + project.Title,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });

            // إرسال إشعار للعميل
            _context.Notifications.Add(new Notification
            {
                UserId = project.ClientId,
                Type = "project_started",
                Message = "بدأ المترجم العمل على المشروع: " + project.Title,
                RelatedId = project.ProjectId,
                RelatedType = "project",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تحديث حالة المشروع إلى قيد التنفيذ.";
            return RedirectToAction("WorkOnProject", new { id = project.ProjectId });
        }

        // رفع ملف مترجم
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadTranslatedFile(IFormFile file, int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.AssignedTranslatorId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من أن المشروع مسند أو قيد التنفيذ
            if (project.Status != "Assigned" && project.Status != "In Progress")
            {
                TempData["ErrorMessage"] = "لا يمكنك رفع ملفات لهذا المشروع في الحالة الحالية.";
                return RedirectToAction("WorkOnProject", new { id = projectId });
            }

            if (file != null && file.Length > 0)
            {
                // التأكد من وجود مجلد التحميلات
                var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }

                // حفظ الملف على السيرفر
                var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsDirectory, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // إضافة معلومات الملف لقاعدة البيانات
                _context.ProjectFiles.Add(new ProjectFile
                {
                    ProjectId = projectId,
                    FileName = file.FileName,
                    FilePath = "/uploads/" + fileName,
                    FileSize = (int)file.Length,
                    FileType = file.ContentType ?? "application/octet-stream",
                    IsOriginal = false, // ملف مترجم
                    UploadedBy = userIdInt,
                    UploadedAt = DateTime.UtcNow
                });

                // إضافة نشاط رفع ملف
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "UploadTranslatedFile",
                    EntityType = "Project",
                    EntityId = projectId,
                    Description = "تم رفع ملف مترجم للمشروع: " + project.Title,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });

                // إرسال إشعار للعميل
                _context.Notifications.Add(new Notification
                {
                    UserId = project.ClientId,
                    Type = "file_uploaded",
                    Message = "تم رفع ملف مترجم للمشروع: " + project.Title,
                    RelatedId = projectId,
                    RelatedType = "project",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم رفع الملف بنجاح.";
            }

            return RedirectToAction("WorkOnProject", new { id = projectId });
        }


        // عرض جميع المشاريع المتاحة
        public async Task<IActionResult> AvailableProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // المشاريع المتاحة
            var availableProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .Where(p => p.Status == "Open") // مشاريع مفتوحة
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveSection = "available";
            return View(availableProjects);
        }

        // عرض مشاريع المترجم الحالية
        public async Task<IActionResult> MyProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // المشاريع قيد التنفيذ
            var activeProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .Where(p => p.AssignedTranslatorId == userIdInt &&
                        (p.Status == "Assigned" || p.Status == "In Progress"))
                .OrderByDescending(p => p.Deadline)
                .ToListAsync();

            ViewBag.ActiveSection = "in-progress";
            return View(activeProjects);
        }

        // عرض المشاريع المكتملة
        public async Task<IActionResult> CompletedProjects()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // المشاريع المكتملة
            var completedProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.Client)
                .Where(p => p.AssignedTranslatorId == userIdInt && p.Status == "Completed")
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();

            ViewBag.ActiveSection = "completed";
            return View(completedProjects);
        }

        // إكمال المشروع
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProject(int id, string completionNotes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.AssignedTranslatorId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من أن المشروع قيد التنفيذ
            if (project.Status != "In Progress")
            {
                TempData["ErrorMessage"] = "لا يمكن إكمال هذا المشروع في الحالة الحالية.";
                return RedirectToAction("WorkOnProject", new { id = project.ProjectId });
            }

            // التحقق من وجود ملفات مترجمة
            var translatedFilesCount = await _context.ProjectFiles
                .Where(f => f.ProjectId == id && f.IsOriginal == false)
                .CountAsync();

            if (translatedFilesCount == 0)
            {
                TempData["ErrorMessage"] = "يجب رفع ملف مترجم واحد على الأقل قبل إكمال المشروع.";
                return RedirectToAction("WorkOnProject", new { id = project.ProjectId });
            }

            // تحديث حالة المشروع
            project.Status = "Completed";
            project.UpdatedAt = DateTime.UtcNow;
            _context.Update(project);

            // إضافة ملاحظات الإكمال
            if (!string.IsNullOrEmpty(completionNotes))
            {
                _context.SystemMessages.Add(new SystemMessage
                {
                    ProjectId = project.ProjectId,
                    SenderId = userIdInt,
                    Message = "ملاحظات إكمال المشروع: " + completionNotes,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // إضافة نشاط إكمال المشروع
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "CompleteProject",
                EntityType = "Project",
                EntityId = project.ProjectId,
                Description = "تم إكمال العمل على المشروع: " + project.Title,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });

            // إرسال إشعار للعميل
            _context.Notifications.Add(new Notification
            {
                UserId = project.ClientId,
                Type = "project_completed",
                Message = "تم إكمال العمل على المشروع: " + project.Title,
                RelatedId = project.ProjectId,
                RelatedType = "project",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إكمال المشروع بنجاح.";
            return RedirectToAction("Index");
        }

        // إدارة الملف الشخصي
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // جلب معلومات المترجم
            var translatorUser = await _context.Users
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            if (translatorUser == null)
            {
                return NotFound();
            }

            // جلب السيرة الذاتية
            var cvItems = await _context.TranslatorCvs
                .Where(cv => cv.TranslatorId == userIdInt)
                .ToListAsync();

            // جلب المهارات المتاحة
            var allSkills = await _context.Skills.ToListAsync();

            // جلب التقييمات
            var reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Project)
                .Where(r => r.TranslatorId == userIdInt)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // تأكد من أن translatorUser.Skills ليس null
            var selectedSkillIds = new List<int>();
            if (translatorUser.Skills != null)
            {
                selectedSkillIds = translatorUser.Skills.Select(s => s.SkillId).ToList();
            }

            var viewModel = new TranslatorProfileViewModel
            {
                Translator = translatorUser,
                CvItems = cvItems,
                AllSkills = allSkills,
                SelectedSkillIds = selectedSkillIds,
                Reviews = reviews
            };

            return View(viewModel);
        }

        // تحديث الملف الشخصي
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateTranslatorProfileViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                var translatorUser = await _context.Users
                    .Include(u => u.Skills)
                    .FirstOrDefaultAsync(u => u.UserId == userIdInt);

                if (translatorUser == null)
                {
                    return NotFound();
                }

                // تحديث معلومات المترجم
                translatorUser.Name = model.Name;
                translatorUser.Bio = model.Bio;
                translatorUser.Location = model.Location;
                translatorUser.UpdatedAt = DateTime.UtcNow;

                // تحديث المهارات
                // أولاً: تأكد من وجود مجموعة المهارات
                if (translatorUser.Skills == null)
                {
                    translatorUser.Skills = new List<Skill>();
                }
                else
                {
                    // إزالة كل المهارات الحالية
                    translatorUser.Skills.Clear();
                }

                // ثانياً: إضافة المهارات المختارة
                if (model.SelectedSkillIds != null && model.SelectedSkillIds.Count > 0)
                {
                    foreach (var skillId in model.SelectedSkillIds)
                    {
                        var skill = await _context.Skills.FindAsync(skillId);
                        if (skill != null)
                        {
                            translatorUser.Skills.Add(skill);
                        }
                    }
                }

                _context.Update(translatorUser);

                // تحديث الصورة الشخصية
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    // التأكد من وجود مجلد الصور الشخصية
                    var profilesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/profiles");
                    if (!Directory.Exists(profilesDirectory))
                    {
                        Directory.CreateDirectory(profilesDirectory);
                    }

                    var fileName = Guid.NewGuid().ToString() + "_" + model.ProfileImage.FileName;
                    var filePath = Path.Combine(profilesDirectory, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImage.CopyToAsync(stream);
                    }

                    translatorUser.AvatarUrl = "/uploads/profiles/" + fileName;
                }

                // تحديث السيرة الذاتية
                // أولاً: حذف السيرة الذاتية الحالية
                var currentCvItems = await _context.TranslatorCvs
                    .Where(cv => cv.TranslatorId == userIdInt)
                    .ToListAsync();

                _context.TranslatorCvs.RemoveRange(currentCvItems);

                // ثانياً: إضافة عناصر السيرة الذاتية الجديدة

                // التعليم
                if (!string.IsNullOrEmpty(model.Education))
                {
                    _context.TranslatorCvs.Add(new TranslatorCv
                    {
                        TranslatorId = userIdInt,
                        Type = "Education",
                        Value = model.Education
                    });
                }

                // الخبرة
                if (!string.IsNullOrEmpty(model.Experience))
                {
                    _context.TranslatorCvs.Add(new TranslatorCv
                    {
                        TranslatorId = userIdInt,
                        Type = "Experience",
                        Value = model.Experience
                    });
                }

                // الشهادات
                if (!string.IsNullOrEmpty(model.Certifications))
                {
                    _context.TranslatorCvs.Add(new TranslatorCv
                    {
                        TranslatorId = userIdInt,
                        Type = "Certification",
                        Value = model.Certifications
                    });
                }

                // إضافة نشاط تحديث الملف الشخصي
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "UpdateProfile",
                    EntityType = "User",
                    EntityId = userIdInt,
                    Description = "تم تحديث الملف الشخصي",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم تحديث الملف الشخصي بنجاح.";
                return RedirectToAction("Profile");
            }

            // إذا كان هناك خطأ في النموذج، إعادة تحميل البيانات
            var allSkills = await _context.Skills.ToListAsync();
            var cvItems = await _context.TranslatorCvs
                .Where(cv => cv.TranslatorId == userIdInt)
                .ToListAsync();

            var translatorForView = await _context.Users
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            var reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Project)
                .Where(r => r.TranslatorId == userIdInt)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new TranslatorProfileViewModel
            {
                Translator = translatorForView,
                CvItems = cvItems,
                AllSkills = allSkills,
                SelectedSkillIds = model.SelectedSkillIds ?? new List<int>(),
                Reviews = reviews
            };

            return View("Profile", viewModel);
        }

        // عرض الإشعارات
        public async Task<IActionResult> Notifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userIdInt)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // تحديث حالة الإشعارات إلى مقروءة
            foreach (var notification in notifications.Where(n => n.IsRead == false))
            {
                notification.IsRead = true;
                _context.Update(notification);
            }

            await _context.SaveChangesAsync();

            return View(notifications);
        }

        // طريقة لتحميل عدد الإشعارات غير المقروءة
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationsCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var count = await _context.Notifications
                .Where(n => n.UserId == userIdInt && n.IsRead == false)
                .CountAsync();

            return Json(count);
        }

        // طريقة لتحميل الإشعارات الجزئية
        [HttpGet]
        public async Task<IActionResult> GetNotificationsPartial()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userIdInt)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            // تحديث حالة الإشعارات إلى مقروءة
            foreach (var notification in notifications.Where(n => n.IsRead == false))
            {
                notification.IsRead = true;
                _context.Update(notification);
            }

            await _context.SaveChangesAsync();

            return PartialView("_NotificationsPartial", notifications);
        }
    }
}
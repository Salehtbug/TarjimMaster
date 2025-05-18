using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tarjim.Models;
using Tarjim.ViewModels;

namespace Tarjim.Controllers
{
    public class ProjectController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProjectController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // عرض صفحة المشاريع الرئيسية
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // إذا كان المستخدم عميلاً - عرض المشاريع الخاصة به
            if (User.IsInRole("Client"))
            {
                var clientProjects = await _context.Projects
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .Include(p => p.AssignedTranslator)
                    .Where(p => p.ClientId == userIdInt)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return View("ClientProjects", clientProjects);
            }
            // إذا كان المستخدم مترجماً - عرض المشاريع المتاحة والمُسندة إليه
            else if (User.IsInRole("Translator"))
            {
                var availableProjects = await _context.Projects
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .Where(p => p.Status == "Open")
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var assignedProjects = await _context.Projects
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .Include(p => p.Client)
                    .Where(p => p.AssignedTranslatorId == userIdInt &&
                           (p.Status == "Assigned" || p.Status == "In Progress"))
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var viewModel = new TranslatorProjectsViewModel
                {
                    AvailableProjects = availableProjects,
                    AssignedProjects = assignedProjects
                };

                return View("TranslatorProjects", viewModel);
            }
            // إذا كان المستخدم أدمن - عرض جميع المشاريع
            else if (User.IsInRole("Admin"))
            {
                var allProjects = await _context.Projects
                    .Include(p => p.Client)
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .Include(p => p.AssignedTranslator)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                return View("AdminProjects", allProjects);
            }

            return RedirectToAction("Index", "Home");
        }

        // عرض تفاصيل مشروع
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Client)
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من الصلاحيات
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            bool canAccess = false;
            if (User.IsInRole("Admin"))
            {
                canAccess = true;
            }
            else if (User.IsInRole("Client") && project.ClientId == userIdInt)
            {
                canAccess = true;
            }
            else if (User.IsInRole("Translator") &&
                    (project.Status == "Open" || project.AssignedTranslatorId == userIdInt))
            {
                canAccess = true;
            }

            if (!canAccess)
            {
                return Forbid();
            }

            // جلب الملفات المرتبطة بالمشروع
            var projectFiles = await _context.ProjectFiles
                .Where(f => f.ProjectId == id)
                .ToListAsync();

            // جلب العروض المقدمة على المشروع (للعميل والأدمن فقط)
            var projectOffers = new List<ProjectOffer>();
            if (User.IsInRole("Client") || User.IsInRole("Admin"))
            {
                projectOffers = await _context.ProjectOffers
                    .Include(o => o.Translator)
                    .Where(o => o.ProjectId == id)
                    .ToListAsync();
            }
            // للمترجم - فقط عرضه الشخصي
            else if (User.IsInRole("Translator"))
            {
                projectOffers = await _context.ProjectOffers
                    .Include(o => o.Translator) // إضافة هذا السطر
                    .Where(o => o.ProjectId == id && o.TranslatorId == userIdInt)
                    .ToListAsync();
            }

            // جلب متطلبات المشروع
            var projectRequirements = await _context.ProjectRequirements
                .Where(r => r.ProjectId == id)
                .ToListAsync();

            var viewModel = new ProjectDetailsViewModel
            {
                Project = project,
                ProjectFiles = projectFiles,
                ProjectOffers = projectOffers,
                ProjectRequirements = projectRequirements
            };

            return View(viewModel);
        }

        // عرض نموذج إنشاء مشروع جديد
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Create()
        {
            // جلب اللغات والفئات لعرضها في النموذج
            var languages = await _context.Languages.ToListAsync();
            var categories = await _context.TranslationCategories.ToListAsync();

            var viewModel = new CreateProjectViewModel
            {
                Languages = languages,
                Categories = categories
            };

            return View(viewModel);
        }

        // معالجة إنشاء مشروع جديد
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // إنشاء مشروع جديد
                var project = new Project
                {
                    ClientId = userIdInt,
                    Title = model.Title,
                    Description = model.Description,
                    SourceLanguageId = model.SourceLanguageId,
                    TargetLanguageId = model.TargetLanguageId,
                    CategoryId = model.CategoryId,
                    PageCount = model.PageCount,
                    Budget = model.Budget,
                    Deadline = model.Deadline,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // إضافة متطلبات المشروع
                if (model.Requirements != null && model.Requirements.Count > 0)
                {
                    foreach (var req in model.Requirements)
                    {
                        _context.ProjectRequirements.Add(new ProjectRequirement
                        {
                            ProjectId = project.ProjectId,
                            RequirementType = req.Type,
                            RequirementLabel = req.Label,
                            RequirementValue = req.Value,
                            IsRequired = req.IsRequired
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // معالجة الملفات المرفقة
                if (model.Files != null && model.Files.Count > 0)
                {
                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0)
                        {
                            // حفظ الملف على السيرفر
                            var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // إضافة معلومات الملف لقاعدة البيانات
                            _context.ProjectFiles.Add(new ProjectFile
                            {
                                ProjectId = project.ProjectId,
                                FileName = file.FileName,
                                FilePath = "/uploads/" + fileName,
                                FileSize = (int)file.Length,
                                FileType = file.ContentType,
                                IsOriginal = true,
                                UploadedBy = userIdInt,
                                UploadedAt = DateTime.UtcNow
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // إضافة نشاط إنشاء مشروع
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "CreateProject",
                    EntityType = "Project",
                    EntityId = project.ProjectId,
                    Description = "تم إنشاء مشروع جديد: " + project.Title,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // إرسال إشعارات للمترجمين (يتم تنفيذه تلقائياً عبر trigger في قاعدة البيانات)

                return RedirectToAction(nameof(Details), new { id = project.ProjectId });
            }

            // إذا كان هناك خطأ في النموذج، إعادة تحميل اللغات والفئات
            model.Languages = await _context.Languages.ToListAsync();
            model.Categories = await _context.TranslationCategories.ToListAsync();

            return View(model);
        }

        // عرض نموذج تعديل مشروع
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id && p.ClientId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من حالة المشروع - يمكن تعديل المشروع فقط إذا كان مفتوحاً
            if (project.Status != "Open")
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل المشروع بعد تعيين مترجم له.";
                return RedirectToAction(nameof(Details), new { id = project.ProjectId });
            }

            // جلب اللغات والفئات
            var languages = await _context.Languages.ToListAsync();
            var categories = await _context.TranslationCategories.ToListAsync();

            // جلب متطلبات المشروع
            var requirements = await _context.ProjectRequirements
                .Where(r => r.ProjectId == id)
                .Select(r => new RequirementViewModel
                {
                    Id = r.RequirementId,
                    Type = r.RequirementType,
                    Label = r.RequirementLabel,
                    Value = r.RequirementValue,
                    IsRequired = r.IsRequired
                })
                .ToListAsync();

            var viewModel = new EditProjectViewModel
            {
                ProjectId = project.ProjectId,
                Title = project.Title,
                Description = project.Description,
                SourceLanguageId = project.SourceLanguageId ?? 0,
                TargetLanguageId = project.TargetLanguageId ?? 0,
                CategoryId = project.CategoryId,
                PageCount = project.PageCount,
                Budget = project.Budget,
                Deadline = project.Deadline,
                Languages = languages,
                Categories = categories,
                Requirements = requirements
            };

            return View(viewModel);
        }

        // معالجة تعديل مشروع
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId && p.ClientId == userIdInt);

                if (project == null)
                {
                    return NotFound();
                }

                // التحقق من حالة المشروع - يمكن تعديل المشروع فقط إذا كان مفتوحاً
                if (project.Status != "Open")
                {
                    TempData["ErrorMessage"] = "لا يمكن تعديل المشروع بعد تعيين مترجم له.";
                    return RedirectToAction(nameof(Details), new { id = project.ProjectId });
                }

                // تحديث بيانات المشروع
                project.Title = model.Title;
                project.Description = model.Description;
                project.SourceLanguageId = model.SourceLanguageId;
                project.TargetLanguageId = model.TargetLanguageId;
                project.CategoryId = model.CategoryId;
                project.PageCount = model.PageCount;
                project.Budget = model.Budget;
                project.Deadline = model.Deadline;
                project.UpdatedAt = DateTime.UtcNow;

                _context.Update(project);
                await _context.SaveChangesAsync();

                // تحديث متطلبات المشروع

                // أولاً حذف المتطلبات الحالية
                var currentRequirements = await _context.ProjectRequirements
                    .Where(r => r.ProjectId == project.ProjectId)
                    .ToListAsync();

                _context.ProjectRequirements.RemoveRange(currentRequirements);
                await _context.SaveChangesAsync();

                // إضافة المتطلبات الجديدة
                if (model.Requirements != null && model.Requirements.Count > 0)
                {
                    foreach (var req in model.Requirements)
                    {
                        _context.ProjectRequirements.Add(new ProjectRequirement
                        {
                            ProjectId = project.ProjectId,
                            RequirementType = req.Type,
                            RequirementLabel = req.Label,
                            RequirementValue = req.Value,
                            IsRequired = req.IsRequired
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // معالجة الملفات المرفقة الجديدة
                if (model.NewFiles != null && model.NewFiles.Count > 0)
                {
                    foreach (var file in model.NewFiles)
                    {
                        if (file.Length > 0)
                        {
                            // حفظ الملف على السيرفر
                            var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // إضافة معلومات الملف لقاعدة البيانات
                            _context.ProjectFiles.Add(new ProjectFile
                            {
                                ProjectId = project.ProjectId,
                                FileName = file.FileName,
                                FilePath = "/uploads/" + fileName,
                                FileSize = (int)file.Length,
                                FileType = file.ContentType,
                                IsOriginal = true,
                                UploadedBy = userIdInt,
                                UploadedAt = DateTime.UtcNow
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                // إضافة نشاط تعديل مشروع
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "EditProject",
                    EntityType = "Project",
                    EntityId = project.ProjectId,
                    Description = "تم تعديل المشروع: " + project.Title,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = project.ProjectId });
            }

            // إذا كان هناك خطأ في النموذج، إعادة تحميل اللغات والفئات
            model.Languages = await _context.Languages.ToListAsync();
            model.Categories = await _context.TranslationCategories.ToListAsync();

            return View(model);
        }

        // حذف مشروع
        [HttpPost]
        [Authorize(Roles = "Client,Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من الصلاحية
            bool canDelete = false;
            if (User.IsInRole("Admin"))
            {
                canDelete = true;
            }
            else if (User.IsInRole("Client") && project.ClientId == userIdInt)
            {
                // العميل يمكنه حذف المشروع فقط إذا كان مفتوحاً
                canDelete = project.Status == "Open";
            }

            if (!canDelete)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف المشروع في الحالة الحالية.";
                return RedirectToAction(nameof(Details), new { id = project.ProjectId });
            }

            // حذف الملفات المرتبطة بالمشروع
            var projectFiles = await _context.ProjectFiles
                .Where(f => f.ProjectId == id)
                .ToListAsync();

            // حذف الملفات الفعلية من السيرفر
            foreach (var file in projectFiles)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", file.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.ProjectFiles.RemoveRange(projectFiles);

            // حذف متطلبات المشروع
            var projectRequirements = await _context.ProjectRequirements
                .Where(r => r.ProjectId == id)
                .ToListAsync();

            _context.ProjectRequirements.RemoveRange(projectRequirements);

            // حذف العروض المقدمة
            var projectOffers = await _context.ProjectOffers
                .Where(o => o.ProjectId == id)
                .ToListAsync();

            _context.ProjectOffers.RemoveRange(projectOffers);

            // حذف المشروع
            _context.Projects.Remove(project);

            // إضافة نشاط حذف مشروع
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "DeleteProject",
                EntityType = "Project",
                EntityId = project.ProjectId,
                Description = "تم حذف المشروع: " + project.Title,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var file = await _context.ProjectFiles
                    .Include(f => f.Project)
                    .FirstOrDefaultAsync(f => f.FileId == id);

                if (file == null)
                {
                    return NotFound("الملف غير موجود");
                }

                // التحقق من الصلاحية
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
                {
                    return RedirectToAction("Login", "Auth");
                }

                bool canAccess = false;
                if (User.IsInRole("Admin"))
                {
                    canAccess = true;
                }
                else if (User.IsInRole("Client") && file.Project.ClientId == userIdInt)
                {
                    canAccess = true;
                }
                else if (User.IsInRole("Translator") &&
                        (file.Project.Status == "Open" || file.Project.AssignedTranslatorId == userIdInt))
                {
                    canAccess = true;
                }

                if (!canAccess)
                {
                    return Forbid();
                }

                // تنزيل الملف - تعديل هذا الجزء
                var webRootPath = _webHostEnvironment.WebRootPath;
                var filePath = Path.Combine(webRootPath, file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (!System.IO.File.Exists(filePath))
                {
                    // محاولة البحث عن الملف في نفس المجلد بغض النظر عن الاسم المحدد
                    var directory = Path.GetDirectoryName(filePath);
                    var fileName = Path.GetFileName(filePath);

                    if (Directory.Exists(directory))
                    {
                        var files = Directory.GetFiles(directory);
                        var matchingFile = files.FirstOrDefault(f =>
                            Path.GetFileNameWithoutExtension(f).Contains(Path.GetFileNameWithoutExtension(fileName).Split('_').Last()) ||
                            f.EndsWith(Path.GetExtension(fileName)));

                        if (matchingFile != null)
                        {
                            filePath = matchingFile;
                        }
                        else
                        {
                            return NotFound($"الملف غير موجود في المجلد: {directory}");
                        }
                    }
                    else
                    {
                        return NotFound($"المجلد غير موجود: {directory}");
                    }
                }

                // إضافة نشاط تنزيل ملف
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "DownloadFile",
                    EntityType = "File",
                    EntityId = file.FileId,
                    Description = "تم تنزيل الملف: " + file.FileName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                var contentType = file.FileType ?? "application/octet-stream";
                var fileBytes = System.IO.File.ReadAllBytes(filePath);

                // التأكد من استخدام الاسم الأصلي للملف بدلاً من الاسم المخزن في النظام
                return File(fileBytes, contentType, file.FileName);
            }
            catch (Exception ex)
            {
                // عرض تفاصيل الخطأ للتصحيح
                return StatusCode(500, $"حدث خطأ: {ex.Message} - {ex.StackTrace}");
            }
        }
    }
}
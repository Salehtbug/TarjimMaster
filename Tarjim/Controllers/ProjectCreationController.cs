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
    [Authorize(Roles = "Client")]
    public class ProjectCreationController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProjectCreationController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // الخطوة الأولى: معلومات المشروع
        public async Task<IActionResult> Step1()
        {
            // جلب قوائم اللغات والتصنيفات لملء القوائم المنسدلة
            var languages = await _context.Languages.ToListAsync();
            var categories = await _context.TranslationCategories.ToListAsync();

            var viewModel = new CreateProjectViewModel
            {
                Languages = languages,
                Categories = categories
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step1(CreateProjectViewModel model)
        {
            if (ModelState.IsValid)
            {
                // حفظ بيانات الخطوة الأولى في التيمب داتا
                TempData["Step1_Title"] = model.Title;
                TempData["Step1_Description"] = model.Description;
                TempData["Step1_SourceLanguageId"] = model.SourceLanguageId;
                TempData["Step1_TargetLanguageId"] = model.TargetLanguageId;
                TempData["Step1_CategoryId"] = model.CategoryId;
                TempData["Step1_PageCount"] = model.PageCount;
                TempData["Step1_Budget"] = model.Budget?.ToString(); // تحويل إلى string
                TempData["Step1_Deadline"] = model.Deadline?.ToString("yyyy-MM-dd");

                // معالجة الملفات إذا تم تحميلها
                if (model.Files != null && model.Files.Count > 0)
                {
                    // حفظ الملفات مؤقتاً
                    List<string> tempFilePaths = new List<string>();
                    List<string> tempFileNames = new List<string>();

                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "temp");

                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            tempFilePaths.Add("/uploads/temp/" + uniqueFileName);
                            tempFileNames.Add(file.FileName);
                        }
                    }

                    if (tempFilePaths.Count > 0)
                    {
                        TempData["Step1_FilePaths"] = string.Join("|", tempFilePaths);
                        TempData["Step1_FileNames"] = string.Join("|", tempFileNames);
                    }
                }

                // حفظ المتطلبات الخاصة
                if (model.Requirements != null && model.Requirements.Count > 0)
                {
                    var requirementsJson = System.Text.Json.JsonSerializer.Serialize(model.Requirements);
                    TempData["Step1_Requirements"] = requirementsJson;
                }

                // الانتقال إلى صفحة التأكيد
                return RedirectToAction("Confirmation");
            }

            // إذا كان هناك خطأ في النموذج، إعادة تحميل اللغات والفئات
            model.Languages = await _context.Languages.ToListAsync();
            model.Categories = await _context.TranslationCategories.ToListAsync();

            return View(model);
        }

        // صفحة التأكيد
        public async Task<IActionResult> Confirmation()
        {
            // التحقق من وجود بيانات الخطوة الأولى
            if (TempData["Step1_Title"] == null)
            {
                return RedirectToAction("Step1");
            }

            // الاحتفاظ ببيانات الخطوة الأولى
            PreserveStepOneData();

            // استرجاع أسماء اللغات والفئات
            string sourceLangName = string.Empty;
            string targetLangName = string.Empty;
            string categoryName = string.Empty;

            if (TempData["Step1_SourceLanguageId"] != null && int.TryParse(TempData["Step1_SourceLanguageId"].ToString(), out int sourceId))
            {
                var sourceLanguage = await _context.Languages.FindAsync(sourceId);
                if (sourceLanguage != null)
                {
                    sourceLangName = sourceLanguage.LanguageName;
                }
            }

            if (TempData["Step1_TargetLanguageId"] != null && int.TryParse(TempData["Step1_TargetLanguageId"].ToString(), out int targetId))
            {
                var targetLanguage = await _context.Languages.FindAsync(targetId);
                if (targetLanguage != null)
                {
                    targetLangName = targetLanguage.LanguageName;
                }
            }

            if (TempData["Step1_CategoryId"] != null && int.TryParse(TempData["Step1_CategoryId"].ToString(), out int catId))
            {
                var category = await _context.TranslationCategories.FindAsync(catId);
                if (category != null)
                {
                    categoryName = category.CategoryName;
                }
            }

            // إنشاء قائمة بأسماء الملفات المرفقة
            var uploadedFilePaths = new List<string>();
            var uploadedFileNames = new List<string>();

            if (TempData["Step1_FilePaths"] != null)
            {
                uploadedFilePaths = TempData["Step1_FilePaths"].ToString().Split('|').ToList();
            }

            if (TempData["Step1_FileNames"] != null)
            {
                uploadedFileNames = TempData["Step1_FileNames"].ToString().Split('|').ToList();
            }

            // استرجاع المتطلبات الخاصة
            var requirements = new List<RequirementViewModel>();
            if (TempData["Step1_Requirements"] != null)
            {
                try
                {
                    requirements = System.Text.Json.JsonSerializer.Deserialize<List<RequirementViewModel>>(
                        TempData["Step1_Requirements"].ToString());
                }
                catch { /* يتم تجاهل الخطأ إذا حدث أثناء استرجاع المتطلبات */ }
            }

            // إنشاء نموذج ملخص للتأكيد
            var confirmationViewModel = new ProjectConfirmationViewModel
            {
                Title = TempData["Step1_Title"]?.ToString(),
                Description = TempData["Step1_Description"]?.ToString(),
                SourceLanguageId = TempData["Step1_SourceLanguageId"] != null ?
            Convert.ToInt32(TempData["Step1_SourceLanguageId"]) : 0,
                TargetLanguageId = TempData["Step1_TargetLanguageId"] != null ?
            Convert.ToInt32(TempData["Step1_TargetLanguageId"]) : 0,
                CategoryId = TempData["Step1_CategoryId"] != null ?
            Convert.ToInt32(TempData["Step1_CategoryId"]) : null,
                PageCount = TempData["Step1_PageCount"] != null ?
            Convert.ToInt32(TempData["Step1_PageCount"]) : null,
                Budget = !string.IsNullOrEmpty(TempData["Step1_Budget"]?.ToString()) ?
            decimal.Parse(TempData["Step1_Budget"].ToString()) : null,
                Deadline = !string.IsNullOrEmpty(TempData["Step1_Deadline"]?.ToString()) ?
            DateTime.Parse(TempData["Step1_Deadline"].ToString()) : null,
                SourceLanguageName = sourceLangName,
                TargetLanguageName = targetLangName,
                CategoryName = categoryName,
                UploadedFilePaths = uploadedFilePaths,
                UploadedFileNames = uploadedFileNames,
                Requirements = requirements
            };

            // الاحتفاظ ببيانات الخطوة الأولى مرة أخرى
            PreserveStepOneData();

            return View(confirmationViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitProject()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // إنشاء كائن المشروع من البيانات المخزنة
                var project = new Project
                {
                    ClientId = userIdInt,
                    Title = TempData["Step1_Title"]?.ToString(),
                    Description = TempData["Step1_Description"]?.ToString(),
                    SourceLanguageId = Convert.ToInt32(TempData["Step1_SourceLanguageId"]),
                    TargetLanguageId = Convert.ToInt32(TempData["Step1_TargetLanguageId"]),
                    CategoryId = !string.IsNullOrEmpty(TempData["Step1_CategoryId"]?.ToString()) ?
         Convert.ToInt32(TempData["Step1_CategoryId"]) : null,
                    PageCount = !string.IsNullOrEmpty(TempData["Step1_PageCount"]?.ToString()) ?
         Convert.ToInt32(TempData["Step1_PageCount"]) : null,
                    Budget = !string.IsNullOrEmpty(TempData["Step1_Budget"]?.ToString()) ?
         decimal.Parse(TempData["Step1_Budget"].ToString()) : null,
                    Deadline = !string.IsNullOrEmpty(TempData["Step1_Deadline"]?.ToString()) ?
         DateTime.Parse(TempData["Step1_Deadline"].ToString()) : null,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                // إضافة الملفات المرفقة
                if (TempData["Step1_FilePaths"] != null)
                {
                    string[] filePaths = TempData["Step1_FilePaths"].ToString().Split('|');
                    string[] fileNames = TempData["Step1_FileNames"]?.ToString().Split('|') ??
                                        filePaths.Select(p => Path.GetFileName(p)).ToArray();

                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        string tempPath = filePaths[i];
                        string fileName = i < fileNames.Length ? fileNames[i] : Path.GetFileName(tempPath);

                        if (!string.IsNullOrEmpty(tempPath))
                        {
                            // نقل الملف من المجلد المؤقت إلى المجلد الدائم
                            string tempFileName = Path.GetFileName(tempPath.Replace("/uploads/temp/", ""));
                            string tempFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "temp", tempFileName);
                            string newFileName = Guid.NewGuid().ToString() + "_" + fileName;
                            string targetPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", newFileName);

                            if (System.IO.File.Exists(tempFilePath))
                            {
                                System.IO.File.Copy(tempFilePath, targetPath);
                                // لا نحذف الملف المؤقت الآن لتجنب الخطأ إذا لم تكتمل العملية
                            }

                            // إضافة معلومات الملف لقاعدة البيانات
                            _context.ProjectFiles.Add(new ProjectFile
                            {
                                ProjectId = project.ProjectId,
                                FileName = fileName,
                                FilePath = "/uploads/" + newFileName,
                                FileSize = (int)new FileInfo(targetPath).Length,
                                FileType = GetFileType(fileName),
                                IsOriginal = true,
                                UploadedBy = userIdInt,
                                UploadedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                // إضافة متطلبات المشروع
                if (TempData["Step1_Requirements"] != null)
                {
                    var requirementsJson = TempData["Step1_Requirements"].ToString();
                    var requirements = System.Text.Json.JsonSerializer.Deserialize<List<RequirementViewModel>>(requirementsJson);

                    foreach (var req in requirements)
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
                }

                // إضافة سجل النشاط
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

                // حذف الملفات المؤقتة بعد الانتهاء
                if (TempData["Step1_FilePaths"] != null)
                {
                    string[] filePaths = TempData["Step1_FilePaths"].ToString().Split('|');
                    foreach (var tempPath in filePaths)
                    {
                        if (!string.IsNullOrEmpty(tempPath))
                        {
                            string tempFileName = Path.GetFileName(tempPath.Replace("/uploads/temp/", ""));
                            string tempFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "temp", tempFileName);
                            if (System.IO.File.Exists(tempFilePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(tempFilePath);
                                }
                                catch { /* تجاهل الخطأ إذا لم يمكن حذف الملف */ }
                            }
                        }
                    }
                }

                // حذف البيانات المؤقتة
                ClearTempData();

                // عرض رسالة نجاح
                TempData["SuccessMessage"] = "تم إنشاء المشروع بنجاح وسيتم إشعار المترجمين به";

                // إعادة توجيه إلى صفحة تفاصيل المشروع
                return RedirectToAction("Details", "Project", new { id = project.ProjectId });
            }
            catch (Exception ex)
            {
                // يمكن تسجيل الاستثناء هنا
                TempData["ErrorMessage"] = "حدث خطأ أثناء إنشاء المشروع: " + ex.Message;

                // الاحتفاظ ببيانات الخطوات السابقة
                PreserveStepOneData();

                return RedirectToAction("Step1");
            }
        }

        // طريقة مساعدة للاحتفاظ ببيانات الخطوة الأولى
        private void PreserveStepOneData()
        {
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step1_")).ToList())
            {
                TempData.Keep(key);
            }
        }

        // حذف البيانات المؤقتة
        private void ClearTempData()
        {
            foreach (var key in TempData.Keys.Where(k => k.StartsWith("Step")).ToList())
            {
                TempData.Remove(key);
            }
        }

        // تحديد نوع الملف
        private string GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream", // نوع افتراضي
            };
        }
    }
}
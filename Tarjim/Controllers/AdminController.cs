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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly MyDbContext _context;

        public AdminController(MyDbContext context)
        {
            _context = context;
        }

      
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

           
            var viewModel = new AdminDashboardViewModel();

            
            viewModel.TotalUsers = await _context.Users.CountAsync();
            viewModel.TotalClients = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.RoleName == "Client"))
                .CountAsync();
            viewModel.TotalTranslators = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.RoleName == "Translator"))
                .CountAsync();
            viewModel.ActiveTranslators = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.RoleName == "Translator") && u.IsActive == true)
                .CountAsync();
            viewModel.ActiveClients = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.RoleName == "Client") && u.IsActive == true)
                .CountAsync();

            
            viewModel.TotalProjects = await _context.Projects.CountAsync();
            viewModel.OpenProjects = await _context.Projects.CountAsync(p => p.Status == "Open");
            viewModel.InProgressProjects = await _context.Projects.CountAsync(p => p.Status == "Assigned" || p.Status == "In Progress");
            viewModel.CompletedProjects = await _context.Projects.CountAsync(p => p.Status == "Completed" || p.Status == "Received");
            viewModel.CanceledProjects = await _context.Projects.CountAsync(p => p.Status == "Canceled");

            
            var payments = await _context.Payments.ToListAsync();
            viewModel.TotalRevenue = payments.Sum(p => p.Amount);
            viewModel.PlatformProfit = payments.Sum(p => p.PlatformFee ?? 0);
            viewModel.PendingPayments = await _context.Projects
                .Where(p => p.Status == "Completed" && !_context.Payments.Any(pay => pay.ProjectId == p.ProjectId))
                .SumAsync(p => p.Budget ?? 0);
            viewModel.CompletedPayments = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount);

          
            viewModel.AverageTranslatorRating = await _context.Users
     .Where(u => u.Roles.Any(r => r.RoleName == "Translator"))
     .AverageAsync(u => (double)(u.Rating ?? 0));
            viewModel.TopTranslators = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.RoleName == "Translator"))
                .OrderByDescending(u => u.Rating)
                .Take(5)
                .ToListAsync();

        
            viewModel.TopClients = await _context.Users
                .Include(u => u.Roles)
                .Where(u => u.Roles.Any(r => r.RoleName == "Client"))
                .OrderByDescending(u => u.ProjectClients.Count)
                .Take(5)
                .ToListAsync();

            
            viewModel.TopCategories = await _context.Projects
                .Where(p => p.CategoryId != null)
                .GroupBy(p => p.CategoryId)
                .Select(g => new CategoryStatistics
                {
                    CategoryId = g.Key.Value,
                    CategoryName = g.First().Category.CategoryName,
                    ProjectCount = g.Count(),
                    TotalRevenue = g.Sum(p => p.Budget ?? 0)
                })
                .OrderByDescending(c => c.ProjectCount)
                .Take(5)
                .ToListAsync();

            
            viewModel.TopLanguages = await _context.Languages
                .Select(l => new LanguageStatistics
                {
                    LanguageId = l.LanguageId,
                    LanguageName = l.LanguageName,
                    AsSourceCount = _context.Projects.Count(p => p.SourceLanguageId == l.LanguageId),
                    AsTargetCount = _context.Projects.Count(p => p.TargetLanguageId == l.LanguageId)
                })
                .OrderByDescending(l => l.AsSourceCount + l.AsTargetCount)
                .Take(5)
                .ToListAsync();

            viewModel.RecentActivities = await _context.ActivityLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();
            viewModel.RecentProjects = await _context.Projects
                .Include(p => p.Client)
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();
            viewModel.RecentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            
            viewModel.PendingInstantRequests = await _context.InstantTranslatorRequests
                .CountAsync(r => r.Status == "Pending");
            viewModel.RecentInstantRequests = await _context.InstantTranslatorRequests
                .Include(r => r.Client)
                .Include(r => r.SourceLanguage)
                .Include(r => r.TargetLanguage)
                .Include(r => r.AssignedTranslator)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .Select(r => new InstantTranslatorRequestViewModel
                {
                    RequestId = r.RequestId,
                    ClientId = r.ClientId,
                    ClientName = r.Client.Name,
                    SourceLanguageId = r.SourceLanguageId,
                    SourceLanguageName = r.SourceLanguage.LanguageName,
                    TargetLanguageId = r.TargetLanguageId,
                    TargetLanguageName = r.TargetLanguage.LanguageName,
                    ServiceType = r.ServiceType,
                    AppointmentDate = r.AppointmentDate,
                    Duration = r.Duration,
                    Subject = r.Subject,
                    Notes = r.Notes,
                    Status = r.Status,
                    AssignedTranslatorId = r.AssignedTranslatorId,
                    AssignedTranslatorName = r.AssignedTranslator != null ? r.AssignedTranslator.Name : null,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return View(viewModel);
        }

        public async Task<IActionResult> Users(string role = null, bool? active = null)
        {
            var query = _context.Users
                .Include(u => u.Roles)
                .AsQueryable();

          
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Roles.Any(r => r.RoleName == role));
            }

            
            if (active.HasValue)
            {
                query = query.Where(u => u.IsActive == active.Value);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            ViewBag.Role = role;
            ViewBag.Active = active;

            return View(users);
        }

        
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

          
            if (user.Roles.Any(r => r.RoleName == "Translator"))
            {
                ViewBag.Skills = await _context.Skills
                    .Where(s => s.Translators.Any(t => t.UserId == id))
                    .ToListAsync();

                ViewBag.CV = await _context.TranslatorCvs
                    .Where(cv => cv.TranslatorId == id)
                    .ToListAsync();

                ViewBag.Reviews = await _context.Reviews
                    .Include(r => r.Client)
                    .Include(r => r.Project)
                    .Where(r => r.TranslatorId == id)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                ViewBag.CompletedProjects = await _context.Projects
                    .CountAsync(p => p.AssignedTranslatorId == id &&
                              (p.Status == "Completed" || p.Status == "Received"));

                ViewBag.TotalEarnings = await _context.Payments
                    .Where(p => p.TranslatorId == id)
                    .SumAsync(p => p.Amount - (p.PlatformFee ?? 0));
            }
            else if (user.Roles.Any(r => r.RoleName == "Client"))
            {
                ViewBag.TotalProjects = await _context.Projects
                    .CountAsync(p => p.ClientId == id);

                ViewBag.CompletedProjects = await _context.Projects
                    .CountAsync(p => p.ClientId == id &&
                              (p.Status == "Completed" || p.Status == "Received"));

                ViewBag.TotalSpent = await _context.Payments
                    .Where(p => p.ClientId == id)
                    .SumAsync(p => p.Amount);
            }

          
            ViewBag.RecentActivity = await _context.ActivityLogs
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(user);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(int id, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Update(user);
            await _context.SaveChangesAsync();

            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = isActive ? "ActivateUser" : "DeactivateUser",
                    EntityType = "User",
                    EntityId = id,
                    Description = isActive ?
                        $"تم تفعيل حساب المستخدم: {user.Name}" :
                        $"تم تعطيل حساب المستخدم: {user.Name}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = isActive ?
                "تم تفعيل حساب المستخدم بنجاح" :
                "تم تعطيل حساب المستخدم بنجاح";

            return RedirectToAction(nameof(UserDetails), new { id });
        }

        
        public async Task<IActionResult> Projects(string status = null)
        {
            var query = _context.Projects
                .Include(p => p.Client)
                .Include(p => p.AssignedTranslator)
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            var projects = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Status = status;

            return View(projects);
        }

       
        public async Task<IActionResult> ProjectDetails(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Client)
                .Include(p => p.AssignedTranslator)
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProjectId == id);

            if (project == null)
            {
                return NotFound();
            }

         
            var files = await _context.ProjectFiles
                .Where(f => f.ProjectId == id)
                .ToListAsync();

            var offers = await _context.ProjectOffers
                .Include(o => o.Translator)
                .Where(o => o.ProjectId == id)
                .ToListAsync();

            
            var requirements = await _context.ProjectRequirements
                .Where(r => r.ProjectId == id)
                .ToListAsync();

          
            var reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Translator)
                .Where(r => r.ProjectId == id)
                .ToListAsync();

          
            var payments = await _context.Payments
                .Where(p => p.ProjectId == id)
                .ToListAsync();

         
            var systemMessages = await _context.SystemMessages
                .Include(m => m.Sender)
                .Where(m => m.ProjectId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Files = files;
            ViewBag.Offers = offers;
            ViewBag.Requirements = requirements;
            ViewBag.Reviews = reviews;
            ViewBag.Payments = payments;
            ViewBag.SystemMessages = systemMessages;

            return View(project);
        }

  
        public async Task<IActionResult> InstantRequests(string status = null)
        {
            var query = _context.InstantTranslatorRequests
                .Include(r => r.Client)
                .Include(r => r.SourceLanguage)
                .Include(r => r.TargetLanguage)
                .Include(r => r.AssignedTranslator)
                .AsQueryable();

          
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Status = status;

            return View(requests);
        }

      
        public async Task<IActionResult> InstantRequestDetails(int id)
        {
            var request = await _context.InstantTranslatorRequests
                .Include(r => r.Client)
                .Include(r => r.SourceLanguage)
                .Include(r => r.TargetLanguage)
                .Include(r => r.AssignedTranslator)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (request == null)
            {
                return NotFound();
            }

     
            var availableTranslators = await _context.Users
                .Include(u => u.Roles)
                .Include(u => u.Skills)
                .Where(u => u.Roles.Any(r => r.RoleName == "Translator") &&
                           u.IsActive == true)
                .OrderByDescending(u => u.Rating)
                .ToListAsync();

            ViewBag.AvailableTranslators = availableTranslators;

            return View(request);
        }

       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTranslator(int requestId, int translatorId)
        {
            var request = await _context.InstantTranslatorRequests.FindAsync(requestId);

            if (request == null)
            {
                return NotFound();
            }

            var translator = await _context.Users.FindAsync(translatorId);

            if (translator == null)
            {
                TempData["ErrorMessage"] = "المترجم غير موجود";
                return RedirectToAction(nameof(InstantRequestDetails), new { id = requestId });
            }

          
            request.AssignedTranslatorId = translatorId;
            request.Status = "Approved";
            request.UpdatedAt = DateTime.UtcNow;

            _context.Update(request);

           
            _context.Notifications.Add(new Notification
            {
                UserId = request.ClientId,
                Type = "instant_request_approved",
                Message = $"تم قبول طلبك للترجمة الفورية وتعيين المترجم {translator.Name}",
                RelatedId = request.RequestId,
                RelatedType = "instant_request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

     
            _context.Notifications.Add(new Notification
            {
                UserId = translatorId,
                Type = "new_instant_assignment",
                Message = $"تم تعيينك لطلب ترجمة فورية: {request.Subject}",
                RelatedId = request.RequestId,
                RelatedType = "instant_request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "AssignInstantTranslator",
                    EntityType = "InstantRequest",
                    EntityId = requestId,
                    Description = $"تم تعيين المترجم {translator.Name} لطلب الترجمة الفورية",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تعيين المترجم بنجاح";
            return RedirectToAction(nameof(InstantRequestDetails), new { id = requestId });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelInstantRequest(int requestId, string reason)
        {
            var request = await _context.InstantTranslatorRequests.FindAsync(requestId);

            if (request == null)
            {
                return NotFound();
            }

          
            request.Status = "Canceled";
            request.Notes = (request.Notes ?? "") + "\nسبب الإلغاء: " + reason;
            request.UpdatedAt = DateTime.UtcNow;

            _context.Update(request);

            
            _context.Notifications.Add(new Notification
            {
                UserId = request.ClientId,
                Type = "instant_request_canceled",
                Message = "تم إلغاء طلب الترجمة الفورية: " + reason,
                RelatedId = request.RequestId,
                RelatedType = "instant_request",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "CancelInstantRequest",
                    EntityType = "InstantRequest",
                    EntityId = requestId,
                    Description = "تم إلغاء طلب الترجمة الفورية: " + reason,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إلغاء الطلب بنجاح";
            return RedirectToAction(nameof(InstantRequests));
        }

        // عرض التقارير والإحصائيات
        public async Task<IActionResult> Reports()
        {
            var payments = await _context.Payments.ToListAsync();

            var model = new ReportsViewModel
            {
                ProjectStatistics = new ProjectStatisticsViewModel
                {
                    Total = await _context.Projects.CountAsync(),
                    Open = await _context.Projects.CountAsync(p => p.Status == "Open"),
                    InProgress = await _context.Projects.CountAsync(p => p.Status == "Assigned" || p.Status == "In Progress"),
                    Completed = await _context.Projects.CountAsync(p => p.Status == "Completed" || p.Status == "Received"),
                    Canceled = await _context.Projects.CountAsync(p => p.Status == "Canceled")
                },
                FinancialStatistics = new FinancialStatisticsViewModel
                {
                    TotalRevenue = payments.Sum(p => p.Amount),
                    PlatformProfit = payments.Sum(p => p.PlatformFee ?? 0),
                    PendingPayments = await _context.Projects
                        .Where(p => p.Status == "Completed" && !_context.Payments.Any(pay => pay.ProjectId == p.ProjectId))
                        .SumAsync(p => p.Budget ?? 0),
                    CompletedPayments = payments.Where(p => p.Status == "Completed").Sum(p => p.Amount)
                },
                UserStatistics = new UserStatisticsViewModel
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalClients = await _context.Users.Include(u => u.Roles).Where(u => u.Roles.Any(r => r.RoleName == "Client")).CountAsync(),
                    TotalTranslators = await _context.Users.Include(u => u.Roles).Where(u => u.Roles.Any(r => r.RoleName == "Translator")).CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive == true),
                    InactiveUsers = await _context.Users.CountAsync(u => u.IsActive == false)
                },
                CategoryStatistics = await _context.Projects
                    .Where(p => p.CategoryId != null)
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new CategoryStatisticsViewModel
                    {
                        CategoryId = g.Key.Value,
                        CategoryName = g.First().Category.CategoryName,
                        ProjectCount = g.Count(),
                        TotalRevenue = g.Sum(p => p.Budget ?? 0)
                    })
                    .OrderByDescending(c => c.ProjectCount)
                    .ToListAsync(),

                LanguageStatistics = await _context.Languages
                    .Select(l => new LanguageStatisticsViewModel
                    {
                        LanguageId = l.LanguageId,
                        LanguageName = l.LanguageName,
                        AsSourceCount = _context.Projects.Count(p => p.SourceLanguageId == l.LanguageId),
                        AsTargetCount = _context.Projects.Count(p => p.TargetLanguageId == l.LanguageId)
                    })
                    .OrderByDescending(l => l.AsSourceCount + l.AsTargetCount)
                    .ToListAsync()
            };

            return View(model);
        }


        // إدارة تصنيفات الترجمة
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.TranslationCategories.ToListAsync();
            return View(categories);
        }

        // إضافة تصنيف جديد
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string categoryName, string description)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["ErrorMessage"] = "اسم التصنيف مطلوب";
                return RedirectToAction(nameof(Categories));
            }

            var existingCategory = await _context.TranslationCategories
                .FirstOrDefaultAsync(c => c.CategoryName == categoryName);

            if (existingCategory != null)
            {
                TempData["ErrorMessage"] = "هذا التصنيف موجود بالفعل";
                return RedirectToAction(nameof(Categories));
            }

            var category = new TranslationCategory
            {
                CategoryName = categoryName,
                Description = description
            };

            _context.TranslationCategories.Add(category);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "AddCategory",
                    EntityType = "Category",
                    Description = "تمت إضافة تصنيف جديد: " + categoryName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت إضافة التصنيف بنجاح";
            return RedirectToAction(nameof(Categories));
        }

        // تعديل تصنيف
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int categoryId, string categoryName, string description)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["ErrorMessage"] = "اسم التصنيف مطلوب";
                return RedirectToAction(nameof(Categories));
            }

            var category = await _context.TranslationCategories.FindAsync(categoryId);

            if (category == null)
            {
                return NotFound();
            }

            var existingCategory = await _context.TranslationCategories
                .FirstOrDefaultAsync(c => c.CategoryName == categoryName && c.CategoryId != categoryId);

            if (existingCategory != null)
            {
                TempData["ErrorMessage"] = "هذا التصنيف موجود بالفعل";
                return RedirectToAction(nameof(Categories));
            }

            category.CategoryName = categoryName;
            category.Description = description;

            _context.Update(category);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "EditCategory",
                    EntityType = "Category",
                    EntityId = categoryId,
                    Description = "تم تعديل التصنيف: " + categoryName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تعديل التصنيف بنجاح";
            return RedirectToAction(nameof(Categories));
        }

        // حذف تصنيف
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int categoryId)
        {
            var category = await _context.TranslationCategories.FindAsync(categoryId);

            if (category == null)
            {
                return NotFound();
            }

            // التحقق من استخدام التصنيف في المشاريع
            var projectsCount = await _context.Projects.CountAsync(p => p.CategoryId == categoryId);

            if (projectsCount > 0)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا التصنيف لأنه مستخدم في مشاريع";
                return RedirectToAction(nameof(Categories));
            }

            _context.TranslationCategories.Remove(category);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "DeleteCategory",
                    EntityType = "Category",
                    EntityId = categoryId,
                    Description = "تم حذف التصنيف: " + category.CategoryName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف التصنيف بنجاح";
            return RedirectToAction(nameof(Categories));
        }

        // إدارة اللغات
        public async Task<IActionResult> Languages()
        {
            var languages = await _context.Languages.ToListAsync();
            return View(languages);
        }

        // إضافة لغة جديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLanguage(string languageName, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageName) || string.IsNullOrWhiteSpace(languageCode))
            {
                TempData["ErrorMessage"] = "اسم اللغة ورمزها مطلوبان";
                return RedirectToAction(nameof(Languages));
            }

            var existingLanguage = await _context.Languages
                .FirstOrDefaultAsync(l => l.LanguageName == languageName || l.LanguageCode == languageCode);

            if (existingLanguage != null)
            {
                TempData["ErrorMessage"] = "هذه اللغة موجودة بالفعل";
                return RedirectToAction(nameof(Languages));
            }

            var language = new Language
            {
                LanguageName = languageName,
                LanguageCode = languageCode
            };

            _context.Languages.Add(language);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "AddLanguage",
                    EntityType = "Language",
                    Description = "تمت إضافة لغة جديدة: " + languageName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت إضافة اللغة بنجاح";
            return RedirectToAction(nameof(Languages));
        }

        // تعديل لغة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLanguage(int languageId, string languageName, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageName) || string.IsNullOrWhiteSpace(languageCode))
            {
                TempData["ErrorMessage"] = "اسم اللغة ورمزها مطلوبان";
                return RedirectToAction(nameof(Languages));
            }

            var language = await _context.Languages.FindAsync(languageId);

            if (language == null)
            {
                return NotFound();
            }

            var existingLanguage = await _context.Languages
                .FirstOrDefaultAsync(l => (l.LanguageName == languageName || l.LanguageCode == languageCode)
                               && l.LanguageId != languageId);

            if (existingLanguage != null)
            {
                TempData["ErrorMessage"] = "هذه اللغة موجودة بالفعل";
                return RedirectToAction(nameof(Languages));
            }

            language.LanguageName = languageName;
            language.LanguageCode = languageCode;

            _context.Update(language);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "EditLanguage",
                    EntityType = "Language",
                    EntityId = languageId,
                    Description = "تم تعديل اللغة: " + languageName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تعديل اللغة بنجاح";
            return RedirectToAction(nameof(Languages));
        }

        // حذف لغة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLanguage(int languageId)
        {
            var language = await _context.Languages.FindAsync(languageId);

            if (language == null)
            {
                return NotFound();
            }

            // التحقق من استخدام اللغة في المشاريع
            var projectsCount = await _context.Projects.CountAsync(p =>
                p.SourceLanguageId == languageId || p.TargetLanguageId == languageId);

            if (projectsCount > 0)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذه اللغة لأنها مستخدمة في مشاريع";
                return RedirectToAction(nameof(Languages));
            }

            _context.Languages.Remove(language);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "DeleteLanguage",
                    EntityType = "Language",
                    EntityId = languageId,
                    Description = "تم حذف اللغة: " + language.LanguageName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف اللغة بنجاح";
            return RedirectToAction(nameof(Languages));
        }

        // إدارة المهارات
        public async Task<IActionResult> Skills()
        {
            var skills = await _context.Skills.ToListAsync();
            return View(skills);
        }

        // إضافة مهارة جديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSkill(string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                TempData["ErrorMessage"] = "اسم المهارة مطلوب";
                return RedirectToAction(nameof(Skills));
            }

            var existingSkill = await _context.Skills
                .FirstOrDefaultAsync(s => s.SkillName == skillName);

            if (existingSkill != null)
            {
                TempData["ErrorMessage"] = "هذه المهارة موجودة بالفعل";
                return RedirectToAction(nameof(Skills));
            }

            var skill = new Skill
            {
                SkillName = skillName
            };

            _context.Skills.Add(skill);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "AddSkill",
                    EntityType = "Skill",
                    Description = "تمت إضافة مهارة جديدة: " + skillName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تمت إضافة المهارة بنجاح";
            return RedirectToAction(nameof(Skills));
        }

        // تعديل مهارة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSkill(int skillId, string skillName)
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                TempData["ErrorMessage"] = "اسم المهارة مطلوب";
                return RedirectToAction(nameof(Skills));
            }

            var skill = await _context.Skills.FindAsync(skillId);

            if (skill == null)
            {
                return NotFound();
            }

            var existingSkill = await _context.Skills
                .FirstOrDefaultAsync(s => s.SkillName == skillName && s.SkillId != skillId);

            if (existingSkill != null)
            {
                TempData["ErrorMessage"] = "هذه المهارة موجودة بالفعل";
                return RedirectToAction(nameof(Skills));
            }

            skill.SkillName = skillName;

            _context.Update(skill);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "EditSkill",
                    EntityType = "Skill",
                    EntityId = skillId,
                    Description = "تم تعديل المهارة: " + skillName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم تعديل المهارة بنجاح";
            return RedirectToAction(nameof(Skills));
        }

        // حذف مهارة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSkill(int skillId)
        {
            var skill = await _context.Skills.FindAsync(skillId);

            if (skill == null)
            {
                return NotFound();
            }

            // التحقق من استخدام المهارة من قبل المترجمين
            var translatorCount = await _context.Users
                .Where(u => u.Skills.Any(s => s.SkillId == skillId))
                .CountAsync();

            if (translatorCount > 0)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذه المهارة لأنها مستخدمة من قبل المترجمين";
                return RedirectToAction(nameof(Skills));
            }

            _context.Skills.Remove(skill);

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "DeleteSkill",
                    EntityType = "Skill",
                    EntityId = skillId,
                    Description = "تم حذف المهارة: " + skill.SkillName,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم حذف المهارة بنجاح";
            return RedirectToAction(nameof(Skills));
        }

        // النشاط الحديث
        public async Task<IActionResult> Activity(string type = null, int? userId = null)
        {
            var query = _context.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            // تطبيق فلتر النوع
            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            // تطبيق فلتر المستخدم
            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            var activities = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(100) // عرض آخر 100 نشاط
                .ToListAsync();

            ViewBag.ActivityTypes = await _context.ActivityLogs
                .Select(a => a.ActivityType)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            ViewBag.Users = await _context.Users
                .OrderBy(u => u.Name)
                .ToListAsync();

            ViewBag.Type = type;
            ViewBag.UserId = userId;

            return View(activities);
        }

        // إعدادات النظام
        public IActionResult Settings()
        {
            // يمكن إضافة استرجاع إعدادات النظام هنا
            return View();
        }

        // تحديث إعدادات النظام
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(decimal platformFeePercentage, bool allowInstantTranslation)
        {
            // يمكن إضافة حفظ إعدادات النظام هنا

            // تسجيل النشاط
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && int.TryParse(userId, out int userIdInt))
            {
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "UpdateSettings",
                    EntityType = "System",
                    Description = "تم تحديث إعدادات النظام",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "تم تحديث الإعدادات بنجاح";
            return RedirectToAction(nameof(Settings));
        }
    }
}
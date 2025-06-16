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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Tarjim.Models;
using Tarjim.ViewModels;

namespace Tarjim.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly MyDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClientController(MyDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // لوحة تحكم العميل - الصفحة الرئيسية
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // جلب معلومات العميل
            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            if (client == null)
            {
                return NotFound();
            }

            // المشاريع المفتوحة (قيد الانتظار)
            var openProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.ProjectOffers)
                .Where(p => p.ClientId == userIdInt && p.Status == "Open")
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)  // عرض أحدث 5 مشاريع فقط في الصفحة الرئيسية
                .ToListAsync();

            // المشاريع قيد التنفيذ
            var activeProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .Where(p => p.ClientId == userIdInt &&
                        (p.Status == "Assigned" || p.Status == "In Progress"))
                .OrderByDescending(p => p.Deadline)
                .Take(5)  // عرض أحدث 5 مشاريع فقط في الصفحة الرئيسية
                .ToListAsync();

            // المشاريع المكتملة مؤخراً
            var completedProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .Where(p => p.ClientId == userIdInt && p.Status == "Completed")
                .OrderByDescending(p => p.UpdatedAt)
                .Take(5)  // عرض أحدث 5 مشاريع فقط في الصفحة الرئيسية
                .ToListAsync();

            // الإشعارات غير المقروءة
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userIdInt && n.IsRead == false)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // عدد العروض الجديدة
            var newOffersCount = await _context.ProjectOffers
                .Include(o => o.Project)
                .Where(o => o.Project.ClientId == userIdInt && o.OfferStatus == "Pending")
                .CountAsync();

            var viewModel = new ClientDashboardViewModel
            {
                Client = client,
                OpenProjects = openProjects,
                ActiveProjects = activeProjects,
                CompletedProjects = completedProjects,
                UnreadNotifications = unreadNotifications,
                NewOffersCount = newOffersCount
            };

            ViewBag.ActiveSection = "all";
            return View(viewModel);
        }

        // دالة لعرض المشاريع قيد التنفيذ
        public async Task<IActionResult> InProgress()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            if (client == null)
            {
                return NotFound();
            }

            var activeProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .Where(p => p.ClientId == userIdInt &&
                        (p.Status == "Assigned" || p.Status == "In Progress"))
                .OrderByDescending(p => p.Deadline)
                .ToListAsync();

            ViewBag.ActiveSection = "in-progress";
            return View(activeProjects);
        }

        // دالة لعرض المشاريع المكتملة
        public async Task<IActionResult> Completed()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            if (client == null)
            {
                return NotFound();
            }

            var completedProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .Where(p => p.ClientId == userIdInt && p.Status == "Completed")
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();

            ViewBag.ActiveSection = "completed";
            return View(completedProjects);
        }

        // دالة لعرض المشاريع التي لم تقبل بعد
        public async Task<IActionResult> NotAccepted()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            if (client == null)
            {
                return NotFound();
            }

            var openProjects = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.ProjectOffers)
                .Where(p => p.ClientId == userIdInt && p.Status == "Open")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.ActiveSection = "not-accepted";
            return View(openProjects);
        }

        // عرض جميع المشاريع
        public async Task<IActionResult> MyProjects(string status = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // استعلام المشاريع الأساسي
            var projectsQuery = _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .Where(p => p.ClientId == userIdInt);

            // تطبيق الفلتر حسب الحالة (إذا تم تحديدها)
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "open":
                        projectsQuery = projectsQuery.Where(p => p.Status == "Open");
                        break;
                    case "active":
                        projectsQuery = projectsQuery.Where(p => p.Status == "Assigned" || p.Status == "In Progress");
                        break;
                    case "completed":
                        projectsQuery = projectsQuery.Where(p => p.Status == "Completed");
                        break;
                    default:
                        // لا تطبق أي تصفية
                        break;
                }
            }

            // تنفيذ الاستعلام وترتيب النتائج
            var projects = await projectsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // تسجيل نشاط عرض المشاريع
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "ViewProjects",
                EntityType = "Project",
                Description = "تمت مشاهدة قائمة المشاريع",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return View(projects);
        }

        // عرض العروض المقدمة على مشروع معين
        public async Task<IActionResult> ProjectOffers(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // التحقق من ملكية المشروع
            var project = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ClientId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // جلب العروض المقدمة على المشروع
            var offers = await _context.ProjectOffers
                .Include(o => o.Translator)
                .Where(o => o.ProjectId == projectId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var viewModel = new ProjectOffersViewModel
            {
                Project = project,
                Offers = offers
            };

            ViewBag.ActiveSection = "";
            return View(viewModel);
        }

        // دالة لعرض الإشعارات
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

            ViewBag.ActiveSection = "notifications";
            return View(notifications);
        }

        // دالة لعرض الرسائل
        public async Task<IActionResult> Messages()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.ReceiverId == userIdInt || m.SenderId == userIdInt)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.SenderId == userIdInt ? m.ReceiverId : m.SenderId)
                .Select(g => new ConversationViewModel
                {
                    UserId = g.Key,
                    UserName = g.First().SenderId == userIdInt
                        ? g.First().Receiver.Name
                        : g.First().Sender.Name,
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).First().Content,
                    LastMessageDate = g.OrderByDescending(m => m.CreatedAt).First().CreatedAt ?? DateTime.Now,
                    UnreadCount = g.Count(m => m.ReceiverId == userIdInt && m.IsRead == false)
                })
                .OrderByDescending(c => c.LastMessageDate)
                .ToList();

            ViewBag.ActiveSection = "messages";
            return View(conversations);
        }

        // دالة لعرض محادثة محددة مع مستخدم
        public async Task<IActionResult> Conversation(int userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out int currentUserIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // جلب المستخدم الآخر
            var otherUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (otherUser == null)
            {
                return NotFound();
            }

            // جلب الرسائل بين المستخدمين
            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserIdInt && m.ReceiverId == userId) ||
                           (m.SenderId == userId && m.ReceiverId == currentUserIdInt))
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // تحديث حالة الرسائل إلى مقروءة
            foreach (var message in messages.Where(m => m.ReceiverId == currentUserIdInt && m.IsRead == false))
            {
                message.IsRead = true;
                _context.Update(message);
            }

            await _context.SaveChangesAsync();

            ViewData["OtherUserId"] = userId;
            ViewData["OtherUserName"] = otherUser.Name;

            ViewBag.ActiveSection = "messages";
            return View(messages);
        }

        // إرسال رسالة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int receiverId, string content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "محتوى الرسالة مطلوب.";
                return RedirectToAction("Conversation", new { userId = receiverId });
            }

            // التحقق من وجود المستلم
            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
            {
                return NotFound();
            }

            // إنشاء الرسالة
            var message = new Message
            {
                SenderId = userIdInt,
                ReceiverId = receiverId,
                Content = content,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction("Conversation", new { userId = receiverId });
        }

        // دالة لعرض صفحة طلب مترجم فوري
        public IActionResult RequestTranslator()
        {
            ViewBag.ActiveSection = "request-translator";
            return View();
        }

        // دالة لعرض الإعدادات
        public async Task<IActionResult> Settings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // جلب معلومات العميل
            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userIdInt);

            if (client == null)
            {
                return NotFound();
            }

            // جلب إعدادات المستخدم
            var userSettings = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.UserId == userIdInt);

            var viewModel = new ClientProfileViewModel
            {
                UserId = client.UserId,
                Name = client.Name,
                Email = client.Email,
                Location = client.Location ?? "",
                Bio = client.Bio ?? "",
                AvatarUrl = client.AvatarUrl,
                NotificationEmail = userSettings?.NotificationEmail ?? true,
                NotificationSystem = userSettings?.NotificationSystem ?? true,
                LanguagePreference = userSettings?.LanguagePreference ?? "ar",
                Theme = userSettings?.Theme ?? "light"
            };

            ViewBag.ActiveSection = "settings";
            return View(viewModel);
        }

        // تحديث الملف الشخصي
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ClientProfileViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (ModelState.IsValid)
            {
                // جلب معلومات العميل
                var client = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userIdInt);

                if (client == null)
                {
                    return NotFound();
                }

                // تحديث معلومات العميل
                client.Name = model.Name;
                client.Bio = model.Bio;
                client.Location = model.Location;
                client.UpdatedAt = DateTime.UtcNow;

                _context.Update(client);

                // تحديث الصورة الشخصية
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    // التأكد من وجود مجلد الصور الشخصية
                    var profilesDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/profiles");
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

                    client.AvatarUrl = "/uploads/profiles/" + fileName;
                }

                // تحديث إعدادات المستخدم
                var userSettings = await _context.UserSettings
                    .FirstOrDefaultAsync(s => s.UserId == userIdInt);

                if (userSettings == null)
                {
                    // إنشاء إعدادات جديدة إذا لم تكن موجودة
                    userSettings = new UserSetting
                    {
                        UserId = userIdInt,
                        NotificationEmail = model.NotificationEmail,
                        NotificationSystem = model.NotificationSystem,
                        LanguagePreference = model.LanguagePreference,
                        Theme = model.Theme
                    };
                    _context.UserSettings.Add(userSettings);
                }
                else
                {
                    // تحديث الإعدادات الحالية
                    userSettings.NotificationEmail = model.NotificationEmail;
                    userSettings.NotificationSystem = model.NotificationSystem;
                    userSettings.LanguagePreference = model.LanguagePreference;
                    userSettings.Theme = model.Theme;
                    _context.Update(userSettings);
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
                return RedirectToAction("Settings");
            }

            // إذا كان النموذج غير صالح، عرض الصفحة مرة أخرى مع الأخطاء
            ViewBag.ActiveSection = "settings";
            return View("Settings", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitInstantTranslatorRequest(int sourceLanguageId, int targetLanguageId, string serviceType, DateTime appointmentDate, int duration, string subject, string notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                TempData["ErrorMessage"] = "لم يتم التحقق من المستخدم.";
                return RedirectToAction("RequestTranslator");
            }

            // التحقق من أن اللغات المصدر والهدف مختلفة
            if (sourceLanguageId == targetLanguageId)
            {
                TempData["ErrorMessage"] = "لا يمكن أن تكون اللغة المصدر واللغة الهدف متطابقتين.";
                return RedirectToAction("RequestTranslator");
            }

            try
            {
                // إنشاء طلب مترجم فوري جديد
                var request = new InstantTranslatorRequest
                {
                    ClientId = userIdInt,
                    SourceLanguageId = sourceLanguageId,
                    TargetLanguageId = targetLanguageId,
                    ServiceType = serviceType,
                    AppointmentDate = appointmentDate,
                    Duration = duration,
                    Subject = subject,
                    Notes = notes,
                    Status = "Pending", // حالة الطلب الأولية
                    CreatedAt = DateTime.UtcNow
                };

                // إضافة الطلب إلى قاعدة البيانات
                _context.InstantTranslatorRequests.Add(request);

                // حفظ التغييرات لتوليد قيمة RequestId
                await _context.SaveChangesAsync();

                // الآن بعد الحفظ، يمكننا استخدام request.RequestId

                // إضافة نشاط تقديم طلب مترجم فوري
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "SubmitInstantTranslatorRequest",
                    EntityType = "InstantTranslatorRequest",
                    EntityId = request.RequestId, // الآن RequestId له قيمة
                    Description = $"تم تقديم طلب مترجم فوري: {subject}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });

                // إرسال إشعار للأدمن عن الطلب الجديد
                var adminUsers = await _context.Users
                    .Where(u => u.Roles.Any(r => r.RoleName == "Admin"))
                    .ToListAsync();

                foreach (var admin in adminUsers)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = admin.UserId,
                        Type = "new_instant_request",
                        Message = $"طلب مترجم فوري جديد: {subject}",
                        RelatedId = request.RequestId,
                        RelatedType = "instant_translator_request",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم تقديم طلب المترجم الفوري بنجاح. سيتم التواصل معك قريباً.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء تقديم الطلب: " + ex.Message;
                return RedirectToAction("RequestTranslator");
            }
        }
        // قبول عرض على مشروع
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(int offerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // جلب العرض والتحقق من الصلاحيات
            var offer = await _context.ProjectOffers
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.OfferId == offerId && o.Project.ClientId == userIdInt);

            if (offer == null)
            {
                return NotFound();
            }

            // التحقق من أن المشروع مفتوح وأن العرض معلق
            if (offer.Project.Status != "Open" || offer.OfferStatus != "Pending")
            {
                TempData["ErrorMessage"] = "لا يمكن قبول هذا العرض في الحالة الحالية.";
                return RedirectToAction("ProjectOffers", new { projectId = offer.ProjectId });
            }

            // تحديث حالة العرض
            offer.OfferStatus = "Accepted";
            _context.Update(offer);

            // تحديث حالة المشروع وتعيين المترجم
            offer.Project.Status = "Assigned";
            offer.Project.AssignedTranslatorId = offer.TranslatorId;
            offer.Project.UpdatedAt = DateTime.UtcNow;
            _context.Update(offer.Project);

            // رفض العروض الأخرى على المشروع
            var otherOffers = await _context.ProjectOffers
                .Where(o => o.ProjectId == offer.ProjectId && o.OfferId != offer.OfferId)
                .ToListAsync();

            foreach (var otherOffer in otherOffers)
            {
                otherOffer.OfferStatus = "Rejected";
                _context.Update(otherOffer);

                // إرسال إشعارات للمترجمين الآخرين برفض عروضهم
                _context.Notifications.Add(new Notification
                {
                    UserId = otherOffer.TranslatorId,
                    Type = "offer_rejected",
                    Message = "تم اختيار مترجم آخر للمشروع: " + offer.Project.Title,
                    RelatedId = offer.ProjectId,
                    RelatedType = "project",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // إضافة نشاط قبول عرض
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "AcceptOffer",
                EntityType = "Offer",
                EntityId = offer.OfferId,
                Description = "تم قبول عرض المترجم على المشروع: " + offer.Project.Title,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });

            // إرسال إشعار للمترجم بقبول عرضه
            _context.Notifications.Add(new Notification
            {
                UserId = offer.TranslatorId,
                Type = "offer_accepted",
                Message = "تم قبول عرضك على المشروع: " + offer.Project.Title,
                RelatedId = offer.ProjectId,
                RelatedType = "project",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم قبول العرض وتعيين المترجم بنجاح.";
            return RedirectToAction("Details", "Project", new { id = offer.ProjectId });
        }

        // إكمال استلام مشروع
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteReceiving(int projectId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // جلب المشروع والتحقق من الصلاحيات
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ClientId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            // التحقق من أن المشروع مكتمل
            if (project.Status != "Completed")
            {
                TempData["ErrorMessage"] = "لا يمكن إكمال استلام هذا المشروع في الحالة الحالية.";
                return RedirectToAction("Details", "Project", new { id = projectId });
            }

            // تحديث حالة المشروع
            project.Status = "Received";
            project.UpdatedAt = DateTime.UtcNow;
            _context.Update(project);

            if (project.AssignedTranslatorId.HasValue && rating >= 1 && rating <= 5)
            {
                _context.Reviews.Add(new Review
                {
                    ProjectId = projectId,
                    TranslatorId = project.AssignedTranslatorId.Value,
                    ClientId = userIdInt,
                    Rating = rating,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                });


                _context.Notifications.Add(new Notification
                {
                    UserId = project.AssignedTranslatorId.Value,
                    Type = "review_received",
                    Message = "تم تقييم عملك على المشروع: " + project.Title,
                    RelatedId = projectId,
                    RelatedType = "project",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "CompleteReceiving",
                EntityType = "Project",
                EntityId = projectId,
                Description = "تم إكمال استلام المشروع: " + project.Title,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إكمال استلام المشروع وتقييم المترجم بنجاح.";
            return RedirectToAction("Index");
        }

        // حذف مشروع
        [HttpPost]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "لم يتم التحقق من المستخدم." });
            }

            try
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ClientId == userIdInt);

                if (project == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على المشروع." });
                }

                // حذف المشروع
                _context.Projects.Remove(project);

                // سجل النشاط
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "DeleteProject",
                    EntityType = "Project",
                    EntityId = projectId,
                    Description = "تم حذف المشروع: " + project.Title,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString()
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف المشروع: " + ex.Message });
            }
        }

        // تعليم جميع الإشعارات كمقروءة
        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "لم يتم التحقق من المستخدم." });
            }

            try
            {
                // جلب الإشعارات غير المقروءة
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userIdInt && n.IsRead == false)
                    .ToListAsync();

                // تعليم الإشعارات كمقروءة
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    _context.Update(notification);
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء تحديث الإشعارات: " + ex.Message });
            }
        }

        // حذف حساب المستخدم
        [HttpPost]
        public async Task<IActionResult> DeleteAccount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "لم يتم التحقق من المستخدم." });
            }

            try
            {
                // التحقق من وجود المستخدم
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userIdInt);

                if (user == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على المستخدم." });
                }

                // حذف المستخدم
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // تسجيل الخروج
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف الحساب: " + ex.Message });
            }
        }

        // البحث في المشاريع
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrEmpty(query) || query.Length < 3)
            {
                ViewBag.Query = query;
                ViewBag.ActiveSection = "";
                return View("SearchResults", new List<Project>());
            }

            // استعلام البحث
            var searchResults = await _context.Projects
                .Include(p => p.SourceLanguage)
                .Include(p => p.TargetLanguage)
                .Include(p => p.Category)
                .Include(p => p.AssignedTranslator)
                .Where(p => p.ClientId == userIdInt &&
                        (p.Title.Contains(query) ||
                        p.Description.Contains(query) ||
                        (p.Category != null && p.Category.CategoryName.Contains(query)) ||
                        (p.SourceLanguage != null && p.SourceLanguage.LanguageName.Contains(query)) ||
                        (p.TargetLanguage != null && p.TargetLanguage.LanguageName.Contains(query))))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // تسجيل نشاط البحث
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "Search",
                Description = $"تم البحث عن: {query}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            ViewBag.Query = query;
            ViewBag.ActiveSection = "";
            return View("SearchResults", searchResults);
        }

        // الحصول على عدد الإشعارات غير المقروءة
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotificationsCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userIdInt && n.IsRead == false);

            return Json(count);
        }

        // الحصول على عدد الرسائل غير المقروءة
        [HttpGet]
        public async Task<IActionResult> GetUnreadMessagesCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Unauthorized();
            }

            var count = await _context.Messages
                .CountAsync(m => m.ReceiverId == userIdInt && m.IsRead == false);

            return Json(count);
        }

        // قبول عرض على مشروع باستخدام AJAX
        [HttpPost]
        public async Task<IActionResult> AcceptOfferAjax(int offerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "لم يتم التحقق من المستخدم." });
            }

            try
            {
                // جلب العرض والتحقق من الصلاحيات
                var offer = await _context.ProjectOffers
                    .Include(o => o.Project)
                    .FirstOrDefaultAsync(o => o.OfferId == offerId && o.Project.ClientId == userIdInt);

                if (offer == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على العرض." });
                }

                // التحقق من أن المشروع مفتوح وأن العرض معلق
                if (offer.Project.Status != "Open" || offer.OfferStatus != "Pending")
                {
                    return Json(new { success = false, message = "لا يمكن قبول هذا العرض في الحالة الحالية." });
                }

                // تحديث حالة العرض
                offer.OfferStatus = "Accepted";
                _context.Update(offer);

                // تحديث حالة المشروع وتعيين المترجم
                offer.Project.Status = "Assigned";
                offer.Project.AssignedTranslatorId = offer.TranslatorId;
                offer.Project.UpdatedAt = DateTime.UtcNow;
                _context.Update(offer.Project);

                // رفض العروض الأخرى على المشروع
                var otherOffers = await _context.ProjectOffers
                    .Where(o => o.ProjectId == offer.ProjectId && o.OfferId != offer.OfferId)
                    .ToListAsync();

                foreach (var otherOffer in otherOffers)
                {
                    otherOffer.OfferStatus = "Rejected";
                    _context.Update(otherOffer);

                    // إرسال إشعارات للمترجمين الآخرين برفض عروضهم
                    _context.Notifications.Add(new Notification
                    {
                        UserId = otherOffer.TranslatorId,
                        Type = "offer_rejected",
                        Message = "تم اختيار مترجم آخر للمشروع: " + offer.Project.Title,
                        RelatedId = offer.ProjectId,
                        RelatedType = "project",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // إضافة نشاط قبول عرض
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "AcceptOffer",
                    EntityType = "Offer",
                    EntityId = offer.OfferId,
                    Description = "تم قبول عرض المترجم على المشروع: " + offer.Project.Title,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });

                // إرسال إشعار للمترجم بقبول عرضه
                _context.Notifications.Add(new Notification
                {
                    UserId = offer.TranslatorId,
                    Type = "offer_accepted",
                    Message = "تم قبول عرضك على المشروع: " + offer.Project.Title,
                    RelatedId = offer.ProjectId,
                    RelatedType = "project",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم قبول العرض وتعيين المترجم بنجاح." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء قبول العرض: " + ex.Message });
            }
        }

        // إكمال استلام المشروع عبر AJAX
        [HttpPost]
        public async Task<IActionResult> CompleteReceivingAjax(int projectId, int rating, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "لم يتم التحقق من المستخدم." });
            }

            try
            {
                // جلب المشروع والتحقق من الصلاحيات
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ClientId == userIdInt);

                if (project == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على المشروع." });
                }

                // التحقق من أن المشروع مكتمل
                if (project.Status != "Completed")
                {
                    return Json(new { success = false, message = "لا يمكن إكمال استلام هذا المشروع في الحالة الحالية." });
                }

                // تحديث حالة المشروع
                project.Status = "Received";
                project.UpdatedAt = DateTime.UtcNow;
                _context.Update(project);

                // إضافة تقييم للمترجم
                if (project.AssignedTranslatorId.HasValue && rating >= 1 && rating <= 5)
                {
                    _context.Reviews.Add(new Review
                    {
                        ProjectId = projectId,
                        TranslatorId = project.AssignedTranslatorId.Value,
                        ClientId = userIdInt,
                        Rating = rating,
                        Comment = comment,
                        CreatedAt = DateTime.UtcNow
                    });

                    // إرسال إشعار للمترجم بالتقييم
                    _context.Notifications.Add(new Notification
                    {
                        UserId = project.AssignedTranslatorId.Value,
                        Type = "review_received",
                        Message = "تم تقييم عملك على المشروع: " + project.Title,
                        RelatedId = projectId,
                        RelatedType = "project",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // إضافة نشاط إكمال استلام المشروع
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "CompleteReceiving",
                    EntityType = "Project",
                    EntityId = projectId,
                    Description = "تم إكمال استلام المشروع: " + project.Title,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تم إكمال استلام المشروع وتقييم المترجم بنجاح." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء إكمال استلام المشروع: " + ex.Message });
            }
        }

        // تحديث الملف الشخصي عبر AJAX
        [HttpPost]
        public async Task<IActionResult> UpdateProfileAjax(ClientProfileViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return Json(new { success = false, message = "لم يتم التحقق من المستخدم." });
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // جلب معلومات العميل
                    var client = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == userIdInt);

                    if (client == null)
                    {
                        return Json(new { success = false, message = "لم يتم العثور على المستخدم." });
                    }

                    // تحديث معلومات العميل
                    client.Name = model.Name;
                    client.Bio = model.Bio;
                    client.Location = model.Location;
                    client.UpdatedAt = DateTime.UtcNow;

                    _context.Update(client);

                    // تحديث الصورة الشخصية
                    if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                    {
                        // التأكد من وجود مجلد الصور الشخصية
                        var profilesDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/profiles");
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

                        client.AvatarUrl = "/uploads/profiles/" + fileName;
                    }

                    // تحديث إعدادات المستخدم
                    var userSettings = await _context.UserSettings
                        .FirstOrDefaultAsync(s => s.UserId == userIdInt);

                    if (userSettings == null)
                    {
                        // إنشاء إعدادات جديدة إذا لم تكن موجودة
                        userSettings = new UserSetting
                        {
                            UserId = userIdInt,
                            NotificationEmail = model.NotificationEmail,
                            NotificationSystem = model.NotificationSystem,
                            LanguagePreference = model.LanguagePreference,
                            Theme = model.Theme
                        };
                        _context.UserSettings.Add(userSettings);
                    }
                    else
                    {
                        // تحديث الإعدادات الحالية
                        userSettings.NotificationEmail = model.NotificationEmail;
                        userSettings.NotificationSystem = model.NotificationSystem;
                        userSettings.LanguagePreference = model.LanguagePreference;
                        userSettings.Theme = model.Theme;
                        _context.Update(userSettings);
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

                    return Json(new { success = true, message = "تم تحديث الملف الشخصي بنجاح." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "حدث خطأ أثناء تحديث الملف الشخصي: " + ex.Message });
                }
            }

            // إذا كان النموذج غير صالح
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "بيانات غير صالحة", errors = errors });
        }
    }
}
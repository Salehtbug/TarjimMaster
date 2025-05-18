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
using Microsoft.Data.SqlClient;

namespace Tarjim.Controllers
{
    public class OfferController : Controller
    {
        private readonly MyDbContext _context;

        public OfferController(MyDbContext context)
        {
            _context = context;
        }

        // عرض العروض المقدمة من المترجم
        [Authorize(Roles = "Translator")]
        public async Task<IActionResult> MyOffers()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var offers = await _context.ProjectOffers
                .Include(o => o.Project)
                .ThenInclude(p => p.Client)
                .Include(o => o.Project.SourceLanguage)
                .Include(o => o.Project.TargetLanguage)
                .Include(o => o.Project.Category)
                .Where(o => o.TranslatorId == userIdInt)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(offers);
        }

        // تقديم عرض على مشروع
        [HttpGet] // تغيير من HttpPost إلى HttpGet لأن هذه الدالة تعرض النموذج فقط
        [Authorize(Roles = "Translator")]
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // التحقق من وجود المشروع
                var project = await _context.Projects
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .Include(p => p.Client) // إضافة علاقة Client أيضاً
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId);

                if (project == null)
                {
                    TempData["ErrorMessage"] = "لم يتم العثور على المشروع المطلوب.";
                    return RedirectToAction("Index", "Translator");
                }

                // التحقق من أن المشروع مفتوح
                if (project.Status != "Open")
                {
                    TempData["ErrorMessage"] = "لا يمكن تقديم عرض على هذا المشروع حالياً لأنه ليس في حالة مفتوحة.";
                    return RedirectToAction("Details", "Project", new { id = projectId });
                }

                // التحقق من أن المترجم لم يقدم عرضاً من قبل
                var existingOffer = await _context.ProjectOffers
                    .FirstOrDefaultAsync(o => o.ProjectId == projectId && o.TranslatorId == userIdInt);

                if (existingOffer != null)
                {
                    TempData["ErrorMessage"] = "لقد قمت بتقديم عرض على هذا المشروع من قبل.";
                    return RedirectToAction("Details", "Project", new { id = projectId });
                }

                // تسجيل نشاط عرض صفحة تقديم العرض
                _context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userIdInt,
                    ActivityType = "ViewOfferForm",
                    EntityType = "Project",
                    EntityId = projectId,
                    Description = "عرض نموذج تقديم عرض على المشروع: " + project.Title,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                // إنشاء النموذج مع قيم افتراضية معقولة
                var viewModel = new CreateOfferViewModel
                {
                    ProjectId = projectId,
                    Project = project,
                    ProposedFee = project.Budget ?? 0,
                    DeliveryDate = project.Deadline ?? DateTime.Now.AddDays(7)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحميل صفحة تقديم العرض. يرجى المحاولة مرة أخرى.";

                // يمكن إضافة تسجيل الخطأ في نظام تسجيل الأخطاء هنا
                // _logger.LogError(ex, "حدث خطأ أثناء عرض نموذج تقديم العرض للمشروع: " + projectId);

                return RedirectToAction("Index", "Translator");
            }
        }

        // معالجة تقديم العرض
        [HttpPost]
        [Authorize(Roles = "Translator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOfferViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // التحقق من وجود المشروع
                    var project = await _context.Projects
                        .FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId);

                    if (project == null)
                    {
                        TempData["ErrorMessage"] = "لم يتم العثور على المشروع المطلوب.";
                        return RedirectToAction("Index", "Translator");
                    }

                    // التحقق من أن المشروع مفتوح
                    if (project.Status != "Open")
                    {
                        TempData["ErrorMessage"] = "لا يمكن تقديم عرض على هذا المشروع حالياً لأنه ليس في حالة مفتوحة.";
                        return RedirectToAction("Details", "Project", new { id = model.ProjectId });
                    }

                    // التحقق من أن المترجم لم يقدم عرضاً من قبل
                    var existingOffer = await _context.ProjectOffers
                        .FirstOrDefaultAsync(o => o.ProjectId == model.ProjectId && o.TranslatorId == userIdInt);

                    if (existingOffer != null)
                    {
                        TempData["ErrorMessage"] = "لقد قمت بتقديم عرض على هذا المشروع من قبل.";
                        return RedirectToAction("Details", "Project", new { id = model.ProjectId });
                    }

                    // التحقق من معلومات الاتصال في الرسالة (إذا كانت مطلوبة)
                    // يمكن تطبيق ذلك حسب متطلبات النظام

                    // إنشاء عرض جديد
                    var offer = new ProjectOffer
                    {
                        ProjectId = model.ProjectId,
                        TranslatorId = userIdInt,
                        ProposedFee = model.ProposedFee,
                        Message = model.Message,
                        DeliveryDate = model.DeliveryDate,
                        OfferStatus = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ProjectOffers.Add(offer);

                    // إضافة نشاط تقديم عرض
                    _context.ActivityLogs.Add(new ActivityLog
                    {
                        UserId = userIdInt,
                        ActivityType = "CreateOffer",
                        EntityType = "Offer",
                        EntityId = 0, // سيتم تحديثه بعد الحفظ
                        Description = "تم تقديم عرض على المشروع: " + project.Title,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();

                    // تحديث معرف العرض في سجل النشاط
                    var activityLog = await _context.ActivityLogs
                        .OrderByDescending(a => a.LogId)
                        .FirstOrDefaultAsync(a => a.UserId == userIdInt && a.ActivityType == "CreateOffer");

                    if (activityLog != null)
                    {
                        activityLog.EntityId = offer.OfferId;
                        await _context.SaveChangesAsync();
                    }

                    // إرسال إشعار للعميل بوجود عرض جديد
                    _context.Notifications.Add(new Notification
                    {
                        UserId = project.ClientId,
                        Type = "new_offer",
                        Message = "تم تقديم عرض جديد على مشروعك: " + project.Title,
                        RelatedId = model.ProjectId,
                        RelatedType = "project",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "تم تقديم عرضك بنجاح.";
                    return RedirectToAction("Details", "Project", new { id = model.ProjectId });
                }

                // إذا وصلنا إلى هنا، فهناك خطأ في النموذج
                // إعادة تحميل بيانات المشروع لعرضها مع النموذج
                model.Project = await _context.Projects
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .Include(p => p.Client)
                    .FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId);

                return View(model);
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ
                TempData["ErrorMessage"] = "حدث خطأ أثناء تقديم العرض. يرجى المحاولة مرة أخرى.";

                // يمكن إضافة تسجيل الخطأ في نظام تسجيل الأخطاء هنا
                // _logger.LogError(ex, "حدث خطأ أثناء تقديم عرض للمشروع: " + model.ProjectId);

                // إعادة تحميل بيانات المشروع
                model.Project = await _context.Projects
                    .Include(p => p.SourceLanguage)
                    .Include(p => p.TargetLanguage)
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProjectId == model.ProjectId);

                return View(model);
            }
        }

        // عرض تفاصيل العرض
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var offer = await _context.ProjectOffers
                .Include(o => o.Project)
                .Include(o => o.Project.Client)
                .Include(o => o.Translator)
                .Include(o => o.Project.SourceLanguage)
                .Include(o => o.Project.TargetLanguage)
                .Include(o => o.Project.Category)
                .FirstOrDefaultAsync(o => o.OfferId == id);

            if (offer == null)
            {
                return NotFound();
            }

            // التحقق من الصلاحيات
            bool canAccess = false;
            if (User.IsInRole("Admin"))
            {
                canAccess = true;
            }
            else if (User.IsInRole("Client") && offer.Project.ClientId == userIdInt)
            {
                canAccess = true;
            }
            else if (User.IsInRole("Translator") && offer.TranslatorId == userIdInt)
            {
                canAccess = true;
            }

            if (!canAccess)
            {
                return Forbid();
            }

            return View(offer);
        }

        // قبول عرض (للعملاء)
        [HttpPost]
        [Authorize(Roles = "Client")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var offer = await _context.ProjectOffers
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.OfferId == id);

            if (offer == null)
            {
                return NotFound();
            }

            // التحقق من أن المستخدم هو صاحب المشروع
            if (offer.Project.ClientId != userIdInt)
            {
                return Forbid();
            }

            // التحقق من أن المشروع مفتوح
            if (offer.Project.Status != "Open")
            {
                TempData["ErrorMessage"] = "لا يمكن قبول العرض بعد تعيين مترجم للمشروع.";
                return RedirectToAction("Details", "Project", new { id = offer.ProjectId });
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
            }

            // إضافة نشاط قبول عرض
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "AcceptOffer",
                EntityType = "Offer",
                EntityId = offer.OfferId,
                Description = "تم قبول عرض المترجم على المشروع رقم " + offer.ProjectId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
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

            // إرسال إشعارات للمترجمين الآخرين برفض عروضهم
            foreach (var otherOffer in otherOffers)
            {
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

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم قبول العرض وتعيين المترجم بنجاح.";
            return RedirectToAction("Details", "Project", new { id = offer.ProjectId });
        }

        // إلغاء عرض (للمترجمين)
        [HttpPost]
        [Authorize(Roles = "Translator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            var offer = await _context.ProjectOffers
                .Include(o => o.Project)
                .FirstOrDefaultAsync(o => o.OfferId == id);

            if (offer == null)
            {
                return NotFound();
            }

            // التحقق من أن المستخدم هو صاحب العرض
            if (offer.TranslatorId != userIdInt)
            {
                return Forbid();
            }

            // التحقق من أن العرض قيد الانتظار
            if (offer.OfferStatus != "Pending")
            {
                TempData["ErrorMessage"] = "لا يمكن إلغاء العرض في الحالة الحالية.";
                return RedirectToAction("Details", new { id = offer.OfferId });
            }

            // تحديث حالة العرض
            offer.OfferStatus = "Cancelled";
            _context.Update(offer);

            // إضافة نشاط إلغاء عرض
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userIdInt,
                ActivityType = "CancelOffer",
                EntityType = "Offer",
                EntityId = offer.OfferId,
                Description = "تم إلغاء العرض على المشروع رقم " + offer.ProjectId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow
            });

            // إرسال إشعار للعميل بإلغاء العرض
            _context.Notifications.Add(new Notification
            {
                UserId = offer.Project.ClientId,
                Type = "offer_cancelled",
                Message = "تم إلغاء عرض مترجم على المشروع: " + offer.Project.Title,
                RelatedId = offer.ProjectId,
                RelatedType = "project",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "تم إلغاء عرضك بنجاح.";
            return RedirectToAction("MyOffers");
        }

        // عرض العروض المقدمة على مشروع (للعملاء)
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> ProjectOffers(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int userIdInt))
            {
                return RedirectToAction("Login", "Auth");
            }

            // التحقق من وجود المشروع وأن المستخدم هو صاحب المشروع
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.ClientId == userIdInt);

            if (project == null)
            {
                return NotFound();
            }

            var offers = await _context.ProjectOffers
                .Include(o => o.Translator)
                .Where(o => o.ProjectId == projectId && o.OfferStatus == "Pending")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var viewModel = new ProjectOffersViewModel
            {
                Project = project,
                Offers = offers
            };

            return View(viewModel);
        }
    }
}
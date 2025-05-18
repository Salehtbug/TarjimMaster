using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tarjim.ViewModels
{
    public class ClientProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "نبذة عني")]
        public string Bio { get; set; } = string.Empty;

        [Display(Name = "الموقع")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "الصورة الشخصية")]
        public IFormFile? ProfileImage { get; set; }

        public string? AvatarUrl { get; set; }

        [Display(Name = "استلام الإشعارات عبر البريد الإلكتروني")]
        public bool NotificationEmail { get; set; } = true;

        [Display(Name = "استلام الإشعارات داخل النظام")]
        public bool NotificationSystem { get; set; } = true;

        [Display(Name = "اللغة المفضلة")]
        public string LanguagePreference { get; set; } = "ar";

        [Display(Name = "المظهر")]
        public string Theme { get; set; } = "light";
    }
}
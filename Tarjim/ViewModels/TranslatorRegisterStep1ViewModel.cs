using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tarjim.ViewModels
{
    public class TranslatorRegisterStep1ViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [StringLength(100, ErrorMessage = "كلمة المرور يجب أن تكون أكثر من {2} أحرف", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور")]
        [Compare("Password", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقين")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم الثنائي")]
        public string Name { get; set; }

        [Display(Name = "الصورة الشخصية")]
        public IFormFile Avatar { get; set; }

        [Display(Name = "وصف الخدمة")]
        [StringLength(1000, ErrorMessage = "الوصف لا يمكن أن يتجاوز 1000 حرف")]
        public string Bio { get; set; }
    }
}
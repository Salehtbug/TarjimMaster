using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tarjim.ViewModels
{
    public class RegisterStep2ViewModel
    {
        [Display(Name = "الصورة الشخصية")]
        public IFormFile? Avatar { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم الكامل")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "نوع الحساب مطلوب")]
        [Display(Name = "نوع الحساب")]
        public string? AccountType { get; set; }

        [Required(ErrorMessage = "البلد مطلوب")]
        [Display(Name = "البلد")]
        public string? Country { get; set; }

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [Display(Name = "المحافظة")]
        public string? City { get; set; }
    }
}

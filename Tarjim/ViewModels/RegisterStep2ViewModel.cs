using System.ComponentModel.DataAnnotations;

namespace Tarjim.ViewModels
{
    public class RegisterStep2ViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [Display(Name = "الاسم")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "نوع الحساب")]
        public string AccountType { get; set; } = "Personal";

        [Display(Name = "البلد")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "المدينة")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "الصورة الشخصية")]
        public IFormFile? Avatar { get; set; }

        // لا نعرض هذه الحقول في النموذج، ولكنها تمرر عبر TempData
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

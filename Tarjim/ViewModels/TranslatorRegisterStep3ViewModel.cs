using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tarjim.ViewModels
{
    public class TranslatorRegisterStep3ViewModel
    {
        [Display(Name = "نماذج سابقة من العمل")]
        public IFormFile WorkSample { get; set; }

        [Display(Name = "عضوية جمعية مترجمين أردنيين سارية المفعول")]
        public IFormFile MembershipDocument { get; set; }

        [Display(Name = "الجامعة")]
        public string University { get; set; }

        [Display(Name = "المحصل العلمي")]
        public string Major { get; set; }

        [Display(Name = "مجال الدراسة")]
        public string FieldOfStudy { get; set; }
    }
}
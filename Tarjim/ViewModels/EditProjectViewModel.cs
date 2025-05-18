using System.ComponentModel.DataAnnotations;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class EditProjectViewModel
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "عنوان المشروع مطلوب")]
        [Display(Name = "عنوان المشروع")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "وصف المشروع")]
        public string? Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "اللغة المصدر مطلوبة")]
        [Display(Name = "اللغة المصدر")]
        public int SourceLanguageId { get; set; }

        [Required(ErrorMessage = "اللغة الهدف مطلوبة")]
        [Display(Name = "اللغة الهدف")]
        public int TargetLanguageId { get; set; }

        [Required(ErrorMessage = "فئة الترجمة مطلوبة")]
        [Display(Name = "فئة الترجمة")]
        public int? CategoryId { get; set; }

        [Display(Name = "عدد الصفحات")]
        public int? PageCount { get; set; }

        [Display(Name = "الميزانية المتوقعة")]
        public decimal? Budget { get; set; }

        [Display(Name = "الموعد النهائي")]
        [DataType(DataType.Date)]
        public DateTime? Deadline { get; set; }

        [Display(Name = "ملفات جديدة")]
        public List<IFormFile>? NewFiles { get; set; }

        [Display(Name = "المتطلبات الخاصة")]
        public List<RequirementViewModel>? Requirements { get; set; }

        // للعرض في النموذج
        public List<Language>? Languages { get; set; }
        public List<TranslationCategory>? Categories { get; set; }
    }
}

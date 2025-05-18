using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tarjim.ViewModels
{
    public class TranslatorRegisterStep5ViewModel
    {
        [Display(Name = "أنواع الخدمة")]
        public List<int> SelectedServiceTypes { get; set; } = new List<int>();

        [Display(Name = "مجالات الاختصاص")]
        public List<int> SelectedSpecializations { get; set; } = new List<int>();

        [Display(Name = "اللغات المصدر")]
        public List<int> SourceLanguages { get; set; } = new List<int>();

        [Display(Name = "اللغات الهدف")]
        public List<int> TargetLanguages { get; set; } = new List<int>();

        [Display(Name = "أوقات التفرغ للعمل")]
        public List<int> AvailableDays { get; set; } = new List<int>();
    }
}
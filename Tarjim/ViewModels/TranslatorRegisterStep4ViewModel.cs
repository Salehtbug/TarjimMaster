using System.ComponentModel.DataAnnotations;

namespace Tarjim.ViewModels
{
    public class TranslatorRegisterStep4ViewModel
    {
        [Display(Name = "اسم الشهادة")]
        public string CertificateName { get; set; }

        [Display(Name = "الجهة المانحة")]
        public string Institution { get; set; }

        [Display(Name = "الشهر")]
        public int? IssueMonth { get; set; }

        [Display(Name = "السنة")]
        public int? IssueYear { get; set; }
    }
}
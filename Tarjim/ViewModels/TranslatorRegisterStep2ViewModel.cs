using System.ComponentModel.DataAnnotations;

namespace Tarjim.ViewModels
{
    public class TranslatorRegisterStep2ViewModel
    {
        [Required(ErrorMessage = "البلد مطلوب")]
        [Display(Name = "بلد الإقامة")]
        public string Country { get; set; }

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [Display(Name = "المحافظة")]
        public string City { get; set; }
    }
}
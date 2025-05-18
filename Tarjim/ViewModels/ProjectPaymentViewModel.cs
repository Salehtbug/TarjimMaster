using System.ComponentModel.DataAnnotations;

namespace Tarjim.ViewModels
{
    public class ProjectPaymentViewModel
    {
        [Required(ErrorMessage = "طريقة الدفع مطلوبة")]
        [Display(Name = "طريقة الدفع")]
        public string PaymentMethod { get; set; } = string.Empty;

        [Display(Name = "رقم البطاقة")]
        public string? CardNumber { get; set; }

        [Display(Name = "تاريخ انتهاء الصلاحية")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "رمز التحقق CVV")]
        public string? CVV { get; set; }
    }
}
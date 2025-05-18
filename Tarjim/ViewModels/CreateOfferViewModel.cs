using System;
using System.ComponentModel.DataAnnotations;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class CreateOfferViewModel
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "الرسوم المقترحة مطلوبة")]
        [Range(1, 1000000, ErrorMessage = "يجب أن تكون الرسوم المقترحة بين 1 و 1,000,000")]
        [Display(Name = "الرسوم المقترحة")]
        public decimal ProposedFee { get; set; }

        [Required(ErrorMessage = "تاريخ التسليم المتوقع مطلوب")]
        [Display(Name = "تاريخ التسليم المتوقع")]
        [DataType(DataType.Date)]
        public DateTime DeliveryDate { get; set; }

        [Display(Name = "رسالة للعميل")]
        [StringLength(1000, ErrorMessage = "يجب ألا تتجاوز الرسالة 1000 حرف")]
        public string? Message { get; set; }

        // نموذج المشروع للعرض في الصفحة
        public Project? Project { get; set; }
    }
}

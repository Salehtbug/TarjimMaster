using System.ComponentModel.DataAnnotations;

public class TranslatorRegisterViewModel
{
    [Required(ErrorMessage = "الاسم مطلوب")]
    [Display(Name = "الاسم")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, ErrorMessage = "يجب أن تكون كلمة المرور على الأقل {2} حرفًا.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    [Compare("Password", ErrorMessage = "كلمة المرور وتأكيد كلمة المرور غير متطابقتين.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "الموقع")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "نبذة مختصرة")]
    public string Bio { get; set; } = string.Empty;

    [Display(Name = "المؤهلات التعليمية")]
    public string Education { get; set; } = string.Empty;

    [Display(Name = "الخبرات السابقة")]
    public string Experience { get; set; } = string.Empty;
}
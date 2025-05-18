namespace Tarjim.ViewModels
{
    public class RequirementViewModel
    {
        public int Id { get; set; }
        public string? Type { get; set; } 
        public string? Label { get; set; } 
        public string? Value { get; set; } 
        public bool? IsRequired { get; set; }
    }
}

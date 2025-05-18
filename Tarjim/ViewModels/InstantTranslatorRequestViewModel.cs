namespace Tarjim.ViewModels
{
    public class InstantTranslatorRequestViewModel
    {
        public int RequestId { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int SourceLanguageId { get; set; }
        public string SourceLanguageName { get; set; }
        public int TargetLanguageId { get; set; }
        public string TargetLanguageName { get; set; }
        public string ServiceType { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int Duration { get; set; }
        public string Subject { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
        public int? AssignedTranslatorId { get; set; }
        public string AssignedTranslatorName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
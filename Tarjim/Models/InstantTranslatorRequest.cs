using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class InstantTranslatorRequest
{
    public int RequestId { get; set; }

    public int ClientId { get; set; }

    public int SourceLanguageId { get; set; }

    public int TargetLanguageId { get; set; }

    public string ServiceType { get; set; } = null!;

    public DateTime AppointmentDate { get; set; }

    public int Duration { get; set; }

    public string Subject { get; set; } = null!;

    public string? Notes { get; set; }

    public string Status { get; set; } = null!;

    public int? AssignedTranslatorId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedTranslator { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual Language SourceLanguage { get; set; } = null!;

    public virtual Language TargetLanguage { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public int ClientId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int? SourceLanguageId { get; set; }

    public int? TargetLanguageId { get; set; }

    public int? CategoryId { get; set; }

    public int? PageCount { get; set; }

    public decimal? Budget { get; set; }

    public DateTime? Deadline { get; set; }

    public string Status { get; set; } = null!;

    public int? AssignedTranslatorId { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedTranslator { get; set; }

    public virtual TranslationCategory? Category { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ProjectFile> ProjectFiles { get; set; } = new List<ProjectFile>();

    public virtual ICollection<ProjectOffer> ProjectOffers { get; set; } = new List<ProjectOffer>();

    public virtual ICollection<ProjectRequirement> ProjectRequirements { get; set; } = new List<ProjectRequirement>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Language? SourceLanguage { get; set; }

    public virtual ICollection<SystemMessage> SystemMessages { get; set; } = new List<SystemMessage>();

    public virtual Language? TargetLanguage { get; set; }
}

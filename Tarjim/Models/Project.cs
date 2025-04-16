using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public int ClientId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? SourceLanguage { get; set; }

    public string? TargetLanguage { get; set; }

    public string? Category { get; set; }

    public int? PageCount { get; set; }

    public decimal? Budget { get; set; }

    public DateOnly? Deadline { get; set; }

    public string Status { get; set; } = null!;

    public int? AssignedTranslatorId { get; set; }

    public string? FileUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? AssignedTranslator { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual ICollection<ProjectOffer> ProjectOffers { get; set; } = new List<ProjectOffer>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}

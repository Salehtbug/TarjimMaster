using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int ProjectId { get; set; }

    public int TranslatorId { get; set; }

    public int ClientId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User Translator { get; set; } = null!;
}

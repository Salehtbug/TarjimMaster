using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class TranslatorCv
{
    public int CvId { get; set; }

    public int TranslatorId { get; set; }

    public string Type { get; set; } = null!;

    public string Value { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Note { get; set; }

    public virtual User Translator { get; set; } = null!;
}

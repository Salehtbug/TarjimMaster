using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class TranslationCategory
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}

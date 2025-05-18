using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class ProjectRequirement
{
    public int RequirementId { get; set; }

    public int ProjectId { get; set; }

    public string? RequirementType { get; set; }

    public string? RequirementLabel { get; set; }

    public string? RequirementValue { get; set; }

    public bool? IsRequired { get; set; }

    public virtual Project Project { get; set; } = null!;
}

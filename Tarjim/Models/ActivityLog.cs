using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class ActivityLog
{
    public int LogId { get; set; }

    public int? UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public string? EntityType { get; set; }

    public int? EntityId { get; set; }

    public string? Description { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
}

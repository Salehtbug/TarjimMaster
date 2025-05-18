using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? LinkUrl { get; set; }

    public int? RelatedId { get; set; }

    public string? RelatedType { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}

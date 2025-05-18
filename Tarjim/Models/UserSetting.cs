using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class UserSetting
{
    public int UserId { get; set; }

    public bool? NotificationEmail { get; set; }

    public bool? NotificationSystem { get; set; }

    public string? LanguagePreference { get; set; }

    public string? Theme { get; set; }

    public virtual User User { get; set; } = null!;
}

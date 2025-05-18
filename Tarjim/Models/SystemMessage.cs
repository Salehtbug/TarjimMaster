using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class SystemMessage
{
    public int MessageId { get; set; }

    public int ProjectId { get; set; }

    public int SenderId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}

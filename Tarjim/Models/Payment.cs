using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int ProjectId { get; set; }

    public int ClientId { get; set; }

    public int TranslatorId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? PaymentMethod { get; set; }

    public string? TransactionId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? PlatformFee { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User Client { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;

    public virtual User Translator { get; set; } = null!;
}

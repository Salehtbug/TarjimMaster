using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class ProjectOffer
{
    public int OfferId { get; set; }

    public int ProjectId { get; set; }

    public int TranslatorId { get; set; }

    public decimal ProposedFee { get; set; }

    public string? Message { get; set; }

    public DateOnly? DeliveryDate { get; set; }

    public string OfferStatus { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User Translator { get; set; } = null!;
}

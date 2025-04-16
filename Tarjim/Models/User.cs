using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string AccountType { get; set; } = null!;

    public string? AvatarUrl { get; set; }

    public string? Bio { get; set; }

    public string? Location { get; set; }

    public decimal? Rating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Project> ProjectAssignedTranslators { get; set; } = new List<Project>();

    public virtual ICollection<Project> ProjectClients { get; set; } = new List<Project>();

    public virtual ICollection<ProjectOffer> ProjectOffers { get; set; } = new List<ProjectOffer>();

    public virtual ICollection<Review> ReviewClients { get; set; } = new List<Review>();

    public virtual ICollection<Review> ReviewTranslators { get; set; } = new List<Review>();

    public virtual ICollection<TranslatorCv> TranslatorCvs { get; set; } = new List<TranslatorCv>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<Skill> Skills { get; set; } = new List<Skill>();
}

using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Skill
{
    public int SkillId { get; set; }

    public string SkillName { get; set; } = null!;

    public virtual ICollection<User> Translators { get; set; } = new List<User>();
}

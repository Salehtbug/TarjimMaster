using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class Language
{
    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = null!;

    public string LanguageCode { get; set; } = null!;

    public virtual ICollection<InstantTranslatorRequest> InstantTranslatorRequestSourceLanguages { get; set; } = new List<InstantTranslatorRequest>();

    public virtual ICollection<InstantTranslatorRequest> InstantTranslatorRequestTargetLanguages { get; set; } = new List<InstantTranslatorRequest>();

    public virtual ICollection<Project> ProjectSourceLanguages { get; set; } = new List<Project>();

    public virtual ICollection<Project> ProjectTargetLanguages { get; set; } = new List<Project>();
}

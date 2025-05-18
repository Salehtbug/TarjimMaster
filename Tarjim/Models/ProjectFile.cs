using System;
using System.Collections.Generic;

namespace Tarjim.Models;

public partial class ProjectFile
{
    public int FileId { get; set; }

    public int ProjectId { get; set; }

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public int? FileSize { get; set; }

    public string? FileType { get; set; }

    public bool? IsOriginal { get; set; }

    public int UploadedBy { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User UploadedByNavigation { get; set; } = null!;
}

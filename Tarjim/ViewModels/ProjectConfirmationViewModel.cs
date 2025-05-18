using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tarjim.Models;

namespace Tarjim.ViewModels
{
    public class ProjectConfirmationViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public int SourceLanguageId { get; set; }
        public int TargetLanguageId { get; set; }
        public int? CategoryId { get; set; }
        public int? PageCount { get; set; }
        public decimal? Budget { get; set; }
        public DateTime? Deadline { get; set; }
        public string PaymentMethod { get; set; }

        // أسماء للعرض
        public string SourceLanguageName { get; set; }
        public string TargetLanguageName { get; set; }
        public string CategoryName { get; set; }

        // قائمة الملفات المرفقة
        public List<string> UploadedFilePaths { get; set; } = new List<string>();
        public List<string> UploadedFileNames { get; set; } = new List<string>();

        // المتطلبات الخاصة
        public List<RequirementViewModel> Requirements { get; set; } = new List<RequirementViewModel>();
    }
}
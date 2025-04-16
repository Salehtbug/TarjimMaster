using System;

namespace Tarjim.ViewModels
{
    public class ProjectCardViewModel
    {
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; }

        // Status constants
        public static class ProjectStatus
        {
            public const string All = "الكل";
            public const string Completed = "مكتمل";
            public const string InProgress = "قيد التنفيذ";
            public const string NotAccepted = "لم يقبل بعد";
        }

        // Helper properties for UI
        public string StatusColorClass
        {
            get
            {
                return Status switch
                {
                    ProjectStatus.Completed => "completed",
                    ProjectStatus.InProgress => "in-progress",
                    ProjectStatus.NotAccepted => "not-accepted",
                    _ => string.Empty
                };
            }
        }

        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    ProjectStatus.Completed => "fas fa-check-circle",
                    ProjectStatus.InProgress => "fas fa-hourglass-half",
                    ProjectStatus.NotAccepted => "fas fa-clock",
                    _ => "fas fa-question-circle"
                };
            }
        }

        public string FormattedCreatedDate => CreatedAt.ToString("dd/MM/yyyy");
        public string FormattedDeadlineDate => Deadline.ToString("dd/MM/yyyy");
    }
}
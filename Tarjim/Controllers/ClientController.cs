using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tarjim.Models;
using Tarjim.ViewModels;

namespace Tarjim.Controllers
{
    public class ClientController : Controller
    {
        private readonly MyDbContext _context;

        public ClientController()
        {
            _context = new MyDbContext();
        }

        public IActionResult Dashboard()
        {
            // Try to get the current client ID from session
            int? clientId = HttpContext.Session.GetInt32("UserId");

            // If user is not logged in and we want authentication, uncomment this
            // if (clientId == null)
            // {
            //     return RedirectToAction("Login", "Account");
            // }

            // Get projects for the client (or all projects for now)
            IQueryable<Project> query = _context.Projects
                .OrderByDescending(p => p.CreatedAt);

            // Filter by client ID if we're using authentication
            // if (clientId.HasValue)
            // {
            //     query = query.Where(p => p.ClientId == clientId.Value);
            // }

            // Map to view model
            var projectsViewModel = query
                .Select(p => new ProjectCardViewModel
                {
                    ProjectId = p.ProjectId,
                    Title = p.Title ?? "ترجمة شهادة خبرة", // Default title if null
                    CreatedAt = p.CreatedAt ?? DateTime.Now,
                    Deadline = p.Deadline.HasValue
                        ? p.Deadline.Value.ToDateTime(TimeOnly.MinValue)
                        : DateTime.Now.AddDays(7), // Default deadline if null
                    Status = p.Status
                })
                .ToList();

            return View(projectsViewModel);
        }

        [HttpGet]
        public IActionResult GetJobsPartial(string status, string searchQuery = "")
        {
            // Start with all projects
            IQueryable<Project> query = _context.Projects
                .OrderByDescending(p => p.CreatedAt);

            // Get client ID if we want to filter by client
            // int? clientId = HttpContext.Session.GetInt32("UserId");
            // if (clientId.HasValue)
            // {
            //     query = query.Where(p => p.ClientId == clientId.Value);
            // }

            // Apply status filter if provided and not "All"
            if (!string.IsNullOrWhiteSpace(status) && status != "الكل")
            {
                query = query.Where(p => p.Status.Trim() == status.Trim());
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.ToLower();
                query = query.Where(p =>
                    (p.Title != null && p.Title.ToLower().Contains(searchQuery)) ||
                    (p.Description != null && p.Description.ToLower().Contains(searchQuery)));
            }

            // Map to view model
            var projectsViewModel = query
                .Select(p => new ProjectCardViewModel
                {
                    ProjectId = p.ProjectId,
                    Title = p.Title ?? "ترجمة شهادة خبرة", // Default title if null
                    CreatedAt = p.CreatedAt ?? DateTime.Now,
                    Deadline = p.Deadline.HasValue
                        ? p.Deadline.Value.ToDateTime(TimeOnly.MinValue)
                        : DateTime.Now.AddDays(7), // Default deadline if null
                    Status = p.Status
                })
                .ToList();

            return PartialView("_DashboardJobsPartial", projectsViewModel);
        }

        [HttpPost]
        public IActionResult DeleteProject(int projectId)
        {
            try
            {
                // Get the project
                var project = _context.Projects.Find(projectId);

                if (project == null)
                {
                    return Json(new { success = false, message = "المشروع غير موجود" });
                }

                // Optional: Check if this project belongs to the current client
                // int? clientId = HttpContext.Session.GetInt32("UserId");
                // if (clientId != project.ClientId)
                // {
                //     return Json(new { success = false, message = "لا يمكنك حذف هذا المشروع" });
                // }

                // Delete the project
                _context.Projects.Remove(project);
                _context.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ أثناء حذف المشروع: " + ex.Message });
            }
        }

        // Method to get sample data until database is populated
        private List<ProjectCardViewModel> GetSampleProjects()
        {
            return new List<ProjectCardViewModel>
            {
                new ProjectCardViewModel
                {
                    ProjectId = 1,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "مكتمل"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 2,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "مكتمل"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 3,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "مكتمل"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 4,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "مكتمل"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 5,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "قيد التنفيذ"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 6,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "قيد التنفيذ"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 7,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "قيد التنفيذ"
                },
                new ProjectCardViewModel
                {
                    ProjectId = 8,
                    Title = "ترجمة شهادة خبرة",
                    CreatedAt = DateTime.Parse("14/12/2023"),
                    Deadline = DateTime.Parse("22/12/2023"),
                    Status = "لم يقبل بعد"
                }
            };
        }
    }
}
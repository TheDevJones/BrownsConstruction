using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BCSApp.Data;
using BCSApp.Models;
using Newtonsoft.Json;

namespace BCSApp.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DocumentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Document
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var documents = await _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Project)
                .Include(d => d.Task)
                .Include(d => d.MaintenanceRequest)
                .Where(d => user.Role == "Admin" || 
                          user.Role == "ProjectManager" ||
                          d.Project != null && (d.Project.ClientId == user.Id || d.Project.ProjectManagerId == user.Id) ||
                          d.Task != null && (d.Task.AssignedToId == user.Id || d.Task.CreatedById == user.Id) ||
                          d.MaintenanceRequest != null && (d.MaintenanceRequest.ClientId == user.Id || d.MaintenanceRequest.AssignedToId == user.Id))
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return View(documents);
        }

        // GET: Document/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Project)
                .Include(d => d.Task)
                .Include(d => d.MaintenanceRequest)
                .Include(d => d.AccessLogs)
                    .ThenInclude(a => a.AccessedBy)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessDocument(document, user)) return Forbid();

            // Log document access
            await LogDocumentAccess(document.Id, user.Id, "View");

            return View(document);
        }

        // GET: Document/Upload
        [Authorize(Roles = "Admin,ProjectManager,Contractor")]
        public async Task<IActionResult> Upload()
        {
            var user = await _userManager.GetUserAsync(User);
            var projects = await _context.Projects
                .Where(p => user.Role == "Admin" || p.ProjectManagerId == user.Id || p.ClientId == user.Id)
                .ToListAsync();
            var tasks = await _context.Tasks
                .Where(t => user.Role == "Admin" || t.AssignedToId == user.Id || t.CreatedById == user.Id)
                .ToListAsync();
            var maintenanceRequests = await _context.MaintenanceRequests
                .Where(m => user.Role == "Admin" || m.ClientId == user.Id || m.AssignedToId == user.Id)
                .ToListAsync();

            ViewBag.Projects = projects;
            ViewBag.Tasks = tasks;
            ViewBag.MaintenanceRequests = maintenanceRequests;

            return View();
        }

        // POST: Document/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager,Contractor")]
        public async Task<IActionResult> Upload(IFormFile file, string name, string description, string documentType, int? projectId, int? taskId, int? maintenanceRequestId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Upload));
            }

            var user = await _userManager.GetUserAsync(User);
            var allowedExtensions = new[] { ".pdf", ".docx", ".png", ".jpg", ".jpeg", ".dwg", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "File type not allowed. Allowed types: PDF, DOCX, PNG, JPG, JPEG, DWG, TXT";
                return RedirectToAction(nameof(Upload));
            }

            try
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documents");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Name = string.IsNullOrEmpty(name) ? file.FileName : name,
                    Description = description,
                    FileName = fileName,
                    FilePath = $"/uploads/documents/{fileName}",
                    FileType = fileExtension,
                    FileSize = file.Length,
                    DocumentType = documentType,
                    ProjectId = projectId,
                    TaskId = taskId,
                    MaintenanceRequestId = maintenanceRequestId,
                    UploadedById = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Add(document);
                await _context.SaveChangesAsync();

                // Log the action
                await LogAuditAction("Document", document.Id, "Upload", user.Id, null, JsonConvert.SerializeObject(document));

                TempData["Success"] = "File uploaded successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error uploading file: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
        }

        // GET: Document/Download/5
        public async Task<IActionResult> Download(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (!CanAccessDocument(document, user)) return Forbid();

            // Log document access
            await LogDocumentAccess(document.Id, user.Id, "Download");

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
            {
                TempData["Error"] = "File not found.";
                return RedirectToAction(nameof(Index));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", document.Name);
        }

        // GET: Document/Delete/5
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.UploadedBy)
                .Include(d => d.Project)
                .Include(d => d.Task)
                .Include(d => d.MaintenanceRequest)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (document == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user.Role != "Admin" && document.UploadedById != user.Id) return Forbid();

            return View(document);
        }

        // POST: Document/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProjectManager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document != null)
            {
                var user = await _userManager.GetUserAsync(User);
                
                // Log the action before deletion
                await LogAuditAction("Document", document.Id, "Delete", user.Id, 
                    JsonConvert.SerializeObject(document), null);

                // Delete physical file
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DocumentExists(int id)
        {
            return _context.Documents.Any(e => e.Id == id);
        }

        private bool CanAccessDocument(Document document, ApplicationUser user)
        {
            return user.Role == "Admin" || 
                   user.Role == "ProjectManager" ||
                   document.UploadedById == user.Id ||
                   (document.Project != null && (document.Project.ClientId == user.Id || document.Project.ProjectManagerId == user.Id)) ||
                   (document.Task != null && (document.Task.AssignedToId == user.Id || document.Task.CreatedById == user.Id)) ||
                   (document.MaintenanceRequest != null && (document.MaintenanceRequest.ClientId == user.Id || document.MaintenanceRequest.AssignedToId == user.Id));
        }

        private async System.Threading.Tasks.Task LogDocumentAccess(int documentId, string userId, string accessType)
        {
            var accessLog = new DocumentAccess
            {
                DocumentId = documentId,
                AccessedById = userId,
                AccessType = accessType,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                AccessedAt = DateTime.Now
            };

            _context.DocumentAccesses.Add(accessLog);
            await _context.SaveChangesAsync();
        }

        private async System.Threading.Tasks.Task LogAuditAction(string entityType, int entityId, string action, string userId, string? oldValues, string? newValues)
        {
            var auditLog = new AuditLog
            {
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                OldValues = oldValues,
                NewValues = newValues,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}

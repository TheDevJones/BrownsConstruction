using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BCSApp.Models;

namespace BCSApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectPhase> ProjectPhases { get; set; }
        public DbSet<ProjectContractor> ProjectContractors { get; set; }
        public DbSet<BCSApp.Models.Task> Tasks { get; set; }
        public DbSet<TaskUpdate> TaskUpdates { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<MaintenanceUpdate> MaintenanceUpdates { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentAccess> DocumentAccesses { get; set; }
        public DbSet<Quotation> Quotations { get; set; }
        public DbSet<QuotationItem> QuotationItems { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageAttachment> MessageAttachments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AIAnalysis> AIAnalyses { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Project>()
                .HasOne(p => p.ProjectManager)
                .WithMany(u => u.ManagedProjects)
                .HasForeignKey(p => p.ProjectManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasOne(p => p.Client)
                .WithMany(u => u.ClientProjects)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BCSApp.Models.Task>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<BCSApp.Models.Task>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Client)
                .WithMany(u => u.MaintenanceRequests)
                .HasForeignKey(m => m.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasOne(m => m.AssignedTo)
                .WithMany()
                .HasForeignKey(m => m.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Recipient)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Quotation relationships
            builder.Entity<Quotation>()
                .HasOne(q => q.Client)
                .WithMany()
                .HasForeignKey(q => q.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Quotation>()
                .HasOne(q => q.CreatedBy)
                .WithMany()
                .HasForeignKey(q => q.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Quotation>()
                .HasOne(q => q.Project)
                .WithMany()
                .HasForeignKey(q => q.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Quotation>()
                .HasOne(q => q.MaintenanceRequest)
                .WithMany()
                .HasForeignKey(q => q.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Invoice relationships
            builder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Invoice>()
                .HasOne(i => i.CreatedBy)
                .WithMany()
                .HasForeignKey(i => i.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Invoice>()
                .HasOne(i => i.Project)
                .WithMany()
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Invoice>()
                .HasOne(i => i.Quotation)
                .WithMany()
                .HasForeignKey(i => i.QuotationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Payment relationships
            builder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Document relationships
            builder.Entity<Document>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Document>()
                .HasOne(d => d.Project)
                .WithMany(p => p.Documents)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Document>()
                .HasOne(d => d.MaintenanceRequest)
                .WithMany(m => m.Attachments)
                .HasForeignKey(d => d.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure DocumentAccess relationships
            builder.Entity<DocumentAccess>()
                .HasOne(da => da.Document)
                .WithMany()
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DocumentAccess>()
                .HasOne(da => da.AccessedBy)
                .WithMany()
                .HasForeignKey(da => da.AccessedById)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Notification relationships
            builder.Entity<Notification>()
                .HasOne(n => n.Recipient)
                .WithMany()
                .HasForeignKey(n => n.RecipientId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.Project)
                .WithMany()
                .HasForeignKey(n => n.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.MaintenanceRequest)
                .WithMany()
                .HasForeignKey(n => n.MaintenanceRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Notification>()
                .HasOne(n => n.Task)
                .WithMany()
                .HasForeignKey(n => n.TaskId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure indexes
            builder.Entity<Project>()
                .HasIndex(p => p.Status);

            builder.Entity<BCSApp.Models.Task>()
                .HasIndex(t => t.Status);

            builder.Entity<BCSApp.Models.Task>()
                .HasIndex(t => t.DueDate);

            builder.Entity<MaintenanceRequest>()
                .HasIndex(m => m.Status);

            builder.Entity<MaintenanceRequest>()
                .HasIndex(m => m.Priority);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.EntityType);

            builder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);

            builder.Entity<Notification>()
                .HasIndex(n => n.Status);

            builder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt);
        }
    }
}

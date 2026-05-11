using DataAccessLayer.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SubjectMembership> SubjectMemberships { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<QuestionUsage> QuestionUsages { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationMember> OrganizationMembers { get; set; }
        public DbSet<OrganizationSubscription> OrganizationSubscriptions { get; set; }
        public DbSet<BillingInvoice> BillingInvoices { get; set; }
        public DbSet<CheckoutSession> CheckoutSessions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SubjectUserMemory> SubjectUserMemories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SubjectMembership>()
                .HasIndex(m => new { m.SubjectId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<SubscriptionPlan>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<QuestionUsage>()
                .HasIndex(u => new { u.UserId, u.UsageDate })
                .IsUnique();

            modelBuilder.Entity<QuestionUsage>()
                .HasIndex(u => new { u.OrganizationId, u.UsageDate });

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.Slug)
                .IsUnique();

            modelBuilder.Entity<OrganizationMember>()
                .HasIndex(m => new { m.OrganizationId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<OrganizationSubscription>()
                .HasIndex(s => new { s.OrganizationId, s.IsActive });

            modelBuilder.Entity<CheckoutSession>()
                .HasIndex(s => s.ReferenceCode)
                .IsUnique();

            modelBuilder.Entity<BillingInvoice>()
                .Property(i => i.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CheckoutSession>()
                .Property(s => s.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ChatSession>()
                .HasIndex(s => new { s.SubjectId, s.UserId });

            modelBuilder.Entity<SubjectUserMemory>()
                .HasIndex(m => new { m.SubjectId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.UploadedByUserId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => l.CreatedAt);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => new { l.OrganizationId, l.CreatedAt });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(l => new { l.SubjectId, l.CreatedAt });

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.OrganizationId);

            modelBuilder.Entity<SubjectMembership>()
                .HasOne(m => m.Subject)
                .WithMany()
                .HasForeignKey(m => m.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubjectMembership>()
                .HasOne(m => m.User)
                .WithMany(u => u.SubjectMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatSession>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SubjectUserMemory>()
                .HasOne(m => m.Subject)
                .WithMany()
                .HasForeignKey(m => m.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubjectUserMemory>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Organization)
                .WithMany(o => o.Subjects)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrganizationMember>()
                .HasOne(m => m.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationMember>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganizationSubscription>()
                .HasOne(s => s.Organization)
                .WithMany(o => o.Subscriptions)
                .HasForeignKey(s => s.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionUsage>()
                .HasOne(u => u.Organization)
                .WithMany()
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

// TODO(1): Placeholder
// TODO(2): Placeholder
// TODO(3): Placeholder
// TODO(4): Placeholder
// TODO(5): Placeholder
// TODO(6): Placeholder
// TODO(7): Placeholder
// TODO(8): Placeholder
// TODO(9): Placeholder
// TODO(10): Placeholder
// TODO(11): Placeholder
// TODO(12): Placeholder
// TODO(13): Placeholder
// TODO(14): Placeholder
// TODO(15): Placeholder
// TODO(16): Placeholder
// TODO(17): Placeholder
// TODO(18): Placeholder
// TODO(19): Placeholder
// TODO(20): Placeholder
// TODO(21): Placeholder
// TODO(22): Placeholder
// TODO(23): Placeholder
// TODO(24): Placeholder
// TODO(25): Placeholder
// TODO(26): Placeholder
// TODO(27): Placeholder
// TODO(28): Placeholder
// TODO(29): Placeholder
// TODO(30): Placeholder
// TODO(31): Placeholder
// TODO(32): Placeholder
// TODO(33): Placeholder
// TODO(34): Placeholder
// TODO(35): Placeholder
// TODO(36): Placeholder
// TODO(37): Placeholder
// TODO(38): Placeholder
// TODO(39): Placeholder
// TODO(40): Placeholder
// TODO(41): Placeholder
// TODO(42): Placeholder
// TODO(43): Placeholder
// TODO(44): Placeholder
// TODO(45): Placeholder
// TODO(46): Placeholder
// TODO(47): Placeholder
// TODO(48): Placeholder
// TODO(49): Placeholder
// TODO(50): Placeholder
// TODO(51): Placeholder
// TODO(52): Placeholder
// TODO(53): Placeholder
// TODO(54): Placeholder
// TODO(55): Placeholder
// TODO(56): Placeholder
// TODO(57): Placeholder
// TODO(58): Placeholder
// TODO(59): Placeholder
// TODO(60): Placeholder
// TODO(61): Placeholder
// TODO(62): Placeholder
// TODO(63): Placeholder
// TODO(64): Placeholder
// TODO(65): Placeholder
// TODO(66): Placeholder
// TODO(67): Placeholder
// TODO(68): Placeholder
// TODO(69): Placeholder
// TODO(70): Placeholder
// TODO(71): Placeholder
// TODO(72): Placeholder
// TODO(73): Placeholder
// TODO(74): Placeholder
// TODO(75): Placeholder
// TODO(76): Placeholder
// TODO(77): Placeholder
// TODO(78): Placeholder
// TODO(79): Placeholder
// TODO(80): Placeholder

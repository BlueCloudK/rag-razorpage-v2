using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;

namespace DataAccessLayer.Repositories;

/// <summary>
/// DAL boundary for EF Core persistence. Business services may compose queries through
/// this abstraction, but only this layer owns ApplicationDbContext and SaveChanges.
/// </summary>
public interface IDataRepository
{
    IRepositorySet<Subject> Subjects { get; }
    IRepositorySet<Document> Documents { get; }
    IRepositorySet<ChatSession> ChatSessions { get; }
    IRepositorySet<ChatMessage> ChatMessages { get; }
    IRepositorySet<SubjectMembership> SubjectMemberships { get; }
    IRepositorySet<SubscriptionPlan> SubscriptionPlans { get; }
    IRepositorySet<UserSubscription> UserSubscriptions { get; }
    IRepositorySet<QuestionUsage> QuestionUsages { get; }
    IRepositorySet<Organization> Organizations { get; }
    IRepositorySet<OrganizationMember> OrganizationMembers { get; }
    IRepositorySet<OrganizationSubscription> OrganizationSubscriptions { get; }
    IRepositorySet<BillingInvoice> BillingInvoices { get; }
    IRepositorySet<CheckoutSession> CheckoutSessions { get; }
    IRepositorySet<AuditLog> AuditLogs { get; }
    IRepositorySet<SubjectUserMemory> SubjectUserMemories { get; }
    IRepositorySet<TokenUsage> TokenUsages { get; }
    IRepositorySet<ApplicationUser> Users { get; }
    IRepositorySet<IdentityRole> Roles { get; }
    IRepositorySet<IdentityUserRole<string>> UserRoles { get; }
    IQueryable<TEntity> Query<TEntity>() where TEntity : class;
    Task<TEntity?> FindAsync<TEntity>(params object[] keyValues) where TEntity : class;
    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;
    void RemoveRange<TEntity>(System.Collections.Generic.IEnumerable<TEntity> entities) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task MigrateAsync(CancellationToken cancellationToken = default);
    Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default);
}

public interface IRepositorySet<TEntity> : IQueryable<TEntity>, IAsyncEnumerable<TEntity> where TEntity : class
{
    Task<TEntity?> FindAsync(params object[] keyValues);
    void Add(TEntity entity);
    void Remove(TEntity entity);
    void RemoveRange(System.Collections.Generic.IEnumerable<TEntity> entities);
}

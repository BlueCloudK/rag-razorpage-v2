using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories;

public sealed class EfDataRepository : IDataRepository
{
    private readonly ApplicationDbContext _context;

    public EfDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepositorySet<Subject> Subjects => new EfRepositorySet<Subject>(_context.Subjects);
    public IRepositorySet<Document> Documents => new EfRepositorySet<Document>(_context.Documents);
    public IRepositorySet<ChatSession> ChatSessions => new EfRepositorySet<ChatSession>(_context.ChatSessions);
    public IRepositorySet<ChatMessage> ChatMessages => new EfRepositorySet<ChatMessage>(_context.ChatMessages);
    public IRepositorySet<SubjectMembership> SubjectMemberships => new EfRepositorySet<SubjectMembership>(_context.SubjectMemberships);
    public IRepositorySet<SubscriptionPlan> SubscriptionPlans => new EfRepositorySet<SubscriptionPlan>(_context.SubscriptionPlans);
    public IRepositorySet<UserSubscription> UserSubscriptions => new EfRepositorySet<UserSubscription>(_context.UserSubscriptions);
    public IRepositorySet<QuestionUsage> QuestionUsages => new EfRepositorySet<QuestionUsage>(_context.QuestionUsages);
    public IRepositorySet<Organization> Organizations => new EfRepositorySet<Organization>(_context.Organizations);
    public IRepositorySet<OrganizationMember> OrganizationMembers => new EfRepositorySet<OrganizationMember>(_context.OrganizationMembers);
    public IRepositorySet<OrganizationSubscription> OrganizationSubscriptions => new EfRepositorySet<OrganizationSubscription>(_context.OrganizationSubscriptions);
    public IRepositorySet<BillingInvoice> BillingInvoices => new EfRepositorySet<BillingInvoice>(_context.BillingInvoices);
    public IRepositorySet<CheckoutSession> CheckoutSessions => new EfRepositorySet<CheckoutSession>(_context.CheckoutSessions);
    public IRepositorySet<AuditLog> AuditLogs => new EfRepositorySet<AuditLog>(_context.AuditLogs);
    public IRepositorySet<SubjectUserMemory> SubjectUserMemories => new EfRepositorySet<SubjectUserMemory>(_context.SubjectUserMemories);
    public IRepositorySet<TokenUsage> TokenUsages => new EfRepositorySet<TokenUsage>(_context.TokenUsages);
    public IRepositorySet<ApplicationUser> Users => new EfRepositorySet<ApplicationUser>(_context.Users);
    public IRepositorySet<IdentityRole> Roles => new EfRepositorySet<IdentityRole>(_context.Roles);
    public IRepositorySet<IdentityUserRole<string>> UserRoles => new EfRepositorySet<IdentityUserRole<string>>(_context.UserRoles);

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => _context.Set<TEntity>();

    public async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues) where TEntity : class
        => await _context.Set<TEntity>().FindAsync(keyValues);

    public void Add<TEntity>(TEntity entity) where TEntity : class => _context.Set<TEntity>().Add(entity);

    public void Remove<TEntity>(TEntity entity) where TEntity : class => _context.Set<TEntity>().Remove(entity);

    public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class => _context.Set<TEntity>().RemoveRange(entities);

    public void Update<TEntity>(TEntity entity) where TEntity : class => _context.Set<TEntity>().Update(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => _context.SaveChangesAsync(cancellationToken);

    public Task MigrateAsync(CancellationToken cancellationToken = default) => _context.Database.MigrateAsync(cancellationToken);

    public Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default)
        => _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
}

internal sealed class EfRepositorySet<TEntity> : IRepositorySet<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _set;

    public EfRepositorySet(DbSet<TEntity> set) => _set = set;

    public Type ElementType => ((IQueryable<TEntity>)_set).ElementType;
    public System.Linq.Expressions.Expression Expression => ((IQueryable<TEntity>)_set).Expression;
    public IQueryProvider Provider => ((IQueryable<TEntity>)_set).Provider;
    public IEnumerator<TEntity> GetEnumerator() => ((IEnumerable<TEntity>)_set).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    public IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => ((IAsyncEnumerable<TEntity>)_set).GetAsyncEnumerator(cancellationToken);
    public async Task<TEntity?> FindAsync(params object[] keyValues) => await _set.FindAsync(keyValues);
    public void Add(TEntity entity) => _set.Add(entity);
    public void Remove(TEntity entity) => _set.Remove(entity);
    public void RemoveRange(IEnumerable<TEntity> entities) => _set.RemoveRange(entities);
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SET09102_2024_5.Data;
using SET09102_2024_5.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SensorMonitoringContext _context;

    public Repository(SensorMonitoringContext context) => _context = context;

    public Task<T> GetByIdAsync(int id) => _context.Set<T>().FindAsync(id).AsTask();
    public Task<IEnumerable<T>> GetAllAsync() => _context.Set<T>().ToListAsync().ContinueWith(t => (IEnumerable<T>)t.Result);
    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> pred) => _context.Set<T>().Where(pred).ToListAsync().ContinueWith(t => (IEnumerable<T>)t.Result);
    public Task AddAsync(T e) => _context.Set<T>().AddAsync(e).AsTask();
    public Task AddRangeAsync(IEnumerable<T> es) => _context.Set<T>().AddRangeAsync(es);
    public void Update(T e) => _context.Set<T>().Update(e);
    public void Remove(T e) => _context.Set<T>().Remove(e);
    public void RemoveRange(IEnumerable<T> es) => _context.Set<T>().RemoveRange(es);
    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();
}
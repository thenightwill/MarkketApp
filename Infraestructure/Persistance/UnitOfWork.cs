using Application.Common;
using Application.Interfaces.Persistance;
using Infraestructure.Persistance.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistance;

public class UnitOfWork : IUnitOfWork
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context) => _context = context;

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The data was modified by another transaction.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation } sql)
        {
            throw MapUniqueViolation(sql);
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            _context.ChangeTracker.Clear();
            throw;
        }
    }

    private static AppException MapUniqueViolation(SqlException exception)
    {
        var message = exception.Message;

        if (message.Contains(ProductConfiguration.ActiveIdentityIndexName, StringComparison.Ordinal))
            return AppException.Conflict(AppErrorCodes.DuplicateProduct, "An active product with the same identity already exists.");

        if (message.Contains(WarehouseStockConfiguration.BatchIndexName, StringComparison.Ordinal))
            return AppException.Conflict(AppErrorCodes.DuplicateBatch, "The batch number already exists for the product.");

        if (message.Contains(UserConfiguration.EmailIndexName, StringComparison.Ordinal))
            return AppException.Conflict(AppErrorCodes.EmailAlreadyExists, "The email is already registered.");

        return AppException.Conflict(AppErrorCodes.ValidationError, "A record with the same unique values already exists.");
    }
}

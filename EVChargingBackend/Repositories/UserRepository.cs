/*
 * Author: EV Charging System
 * Date: 2025-10-04
 * Purpose: User repository for data access operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;


/// Repository for User entity data access operations

public class UserRepository : IRepository<User>
{
    private readonly IMongoCollection<User> _collection;

    
    /// Initializes a new instance of the UserRepository
    
    /// <param name="context">MongoDB database context</param>
    public UserRepository(MongoDbContext context)
    {
        _collection = context.Users;
    }

    
    /// Gets a user by ID
    
    /// <param name="id">User ID</param>
    /// <returns>User if found, null otherwise</returns>
    public async Task<User?> GetByIdAsync(string id)
    {
        return await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    
    /// Gets all users
    
    /// <returns>Collection of users</returns>
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    
    /// Gets users matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching users</returns>
    public async Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    
    /// Gets the first user matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching user if found, null otherwise</returns>
    public async Task<User?> FindOneAsync(Expression<Func<User, bool>> filter)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    
    /// Counts users matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching users</returns>
    public async Task<long> CountAsync(Expression<Func<User, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter);
    }

    
    /// Creates a new user
    
    /// <param name="entity">User to create</param>
    /// <returns>Created user</returns>
    public async Task<User> CreateAsync(User entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    
    /// Updates an existing user
    
    /// <param name="id">User ID</param>
    /// <param name="entity">Updated user</param>
    /// <returns>Updated user if found, null otherwise</returns>
    public async Task<User?> UpdateAsync(string id, User entity)
    {
        var result = await _collection.ReplaceOneAsync(u => u.Id == id, entity);
        return result.IsAcknowledged && result.ModifiedCount > 0 ? entity : null;
    }

    
    /// Deletes a user by ID
    
    /// <param name="id">User ID</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _collection.DeleteOneAsync(u => u.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    
    /// Checks if a user exists with the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(Expression<Func<User, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    
    /// Gets a user by username
    
    /// <param name="username">Username</param>
    /// <returns>User if found, null otherwise</returns>
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _collection.Find(u => u.Username == username).FirstOrDefaultAsync();
    }

    
    /// Gets paginated users
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="filter">Optional filter</param>
    /// <returns>Paginated users</returns>
    public async Task<(IEnumerable<User> Users, long TotalCount)> GetPaginatedAsync(int page, int pageSize, string search, string role, Expression<Func<User, bool>>? filter = null)
    {
        var query = filter != null ? _collection.Find(filter) : _collection.Find(_ => true);
        
        var totalCount = await query.CountDocumentsAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (users, totalCount);
    }

    internal async Task<(object users, long totalCount)> GetPaginatedAsync(int page, int pageSize, string search, string role, bool? isActive)
    {
        throw new NotImplementedException();
    }
}
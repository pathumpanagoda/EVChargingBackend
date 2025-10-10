/*
 * Author: EV Charging System
 * Date: 2025-10-04
 * Purpose: EV Owner repository for data access operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;


/// Repository for EVOwner entity data access operations

public class EVOwnerRepository : IRepository<EVOwner>
{
    private readonly IMongoCollection<EVOwner> _collection;

    
    /// Initializes a new instance of the EVOwnerRepository
    
    /// <param name="context">MongoDB database context</param>
    public EVOwnerRepository(MongoDbContext context)
    {
        _collection = context.EVOwners;
    }

    
    /// Gets an EV owner by NIC
    
    /// <param name="nic">NIC</param>
    /// <returns>EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> GetByIdAsync(string nic)
    {
        return await _collection.Find(e => e.NIC == nic).FirstOrDefaultAsync();
    }

    
    /// Gets all EV owners
    
    /// <returns>Collection of EV owners</returns>
    public async Task<IEnumerable<EVOwner>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    
    /// Gets EV owners matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching EV owners</returns>
    public async Task<IEnumerable<EVOwner>> FindAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    
    /// Gets the first EV owner matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> FindOneAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    
    /// Counts EV owners matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching EV owners</returns>
    public async Task<long> CountAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter);
    }

    
    /// Creates a new EV owner
    
    /// <param name="entity">EV owner to create</param>
    /// <returns>Created EV owner</returns>
    public async Task<EVOwner> CreateAsync(EVOwner entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    
    /// Updates an existing EV owner
    
    /// <param name="nic">NIC</param>
    /// <param name="entity">Updated EV owner</param>
    /// <returns>Updated EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> UpdateAsync(string nic, EVOwner entity)
    {
        var result = await _collection.ReplaceOneAsync(e => e.NIC == nic, entity);
        return result.IsAcknowledged && result.ModifiedCount > 0 ? entity : null;
    }

    
    /// Deletes an EV owner by NIC
    
    /// <param name="nic">NIC</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> DeleteAsync(string nic)
    {
        var result = await _collection.DeleteOneAsync(e => e.NIC == nic);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    
    /// Checks if an EV owner exists with the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    
    /// Gets an EV owner by email
    
    /// <param name="email">Email address</param>
    /// <returns>EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> GetByEmailAsync(string email)
    {
        return await _collection.Find(e => e.Email == email).FirstOrDefaultAsync();
    }

    
    /// Gets paginated EV owners
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Optional search term</param>
    /// <returns>Paginated EV owners</returns>
    public async Task<(IEnumerable<EVOwner> EVOwners, long TotalCount)> GetPaginatedAsync(int page, int pageSize, string? search = null)
    {
        var filter = Builders<EVOwner>.Filter.Empty;
        
        if (!string.IsNullOrEmpty(search))
        {
            var searchFilter = Builders<EVOwner>.Filter.Or(
                Builders<EVOwner>.Filter.Regex(e => e.NIC, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<EVOwner>.Filter.Regex(e => e.Name, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                Builders<EVOwner>.Filter.Regex(e => e.Email, new MongoDB.Bson.BsonRegularExpression(search, "i"))
            );
            filter = searchFilter;
        }

        var totalCount = await _collection.CountDocumentsAsync(filter);
        var evOwners = await _collection
            .Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (evOwners, totalCount);
    }
}
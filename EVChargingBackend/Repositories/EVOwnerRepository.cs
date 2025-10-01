/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: EV Owner repository for data access operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;

/// <summary>
/// Repository for EVOwner entity data access operations
/// </summary>
public class EVOwnerRepository : IRepository<EVOwner>
{
    private readonly IMongoCollection<EVOwner> _collection;

    /// <summary>
    /// Initializes a new instance of the EVOwnerRepository
    /// </summary>
    /// <param name="context">MongoDB database context</param>
    public EVOwnerRepository(MongoDbContext context)
    {
        _collection = context.EVOwners;
    }

    /// <summary>
    /// Gets an EV owner by NIC
    /// </summary>
    /// <param name="nic">NIC</param>
    /// <returns>EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> GetByIdAsync(string nic)
    {
        return await _collection.Find(e => e.NIC == nic).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets all EV owners
    /// </summary>
    /// <returns>Collection of EV owners</returns>
    public async Task<IEnumerable<EVOwner>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    /// <summary>
    /// Gets EV owners matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching EV owners</returns>
    public async Task<IEnumerable<EVOwner>> FindAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    /// <summary>
    /// Gets the first EV owner matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> FindOneAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Counts EV owners matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching EV owners</returns>
    public async Task<long> CountAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Creates a new EV owner
    /// </summary>
    /// <param name="entity">EV owner to create</param>
    /// <returns>Created EV owner</returns>
    public async Task<EVOwner> CreateAsync(EVOwner entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    /// <summary>
    /// Updates an existing EV owner
    /// </summary>
    /// <param name="nic">NIC</param>
    /// <param name="entity">Updated EV owner</param>
    /// <returns>Updated EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> UpdateAsync(string nic, EVOwner entity)
    {
        var result = await _collection.ReplaceOneAsync(e => e.NIC == nic, entity);
        return result.IsAcknowledged && result.ModifiedCount > 0 ? entity : null;
    }

    /// <summary>
    /// Deletes an EV owner by NIC
    /// </summary>
    /// <param name="nic">NIC</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> DeleteAsync(string nic)
    {
        var result = await _collection.DeleteOneAsync(e => e.NIC == nic);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    /// <summary>
    /// Checks if an EV owner exists with the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(Expression<Func<EVOwner, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    /// <summary>
    /// Gets an EV owner by email
    /// </summary>
    /// <param name="email">Email address</param>
    /// <returns>EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> GetByEmailAsync(string email)
    {
        return await _collection.Find(e => e.Email == email).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets paginated EV owners
    /// </summary>
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
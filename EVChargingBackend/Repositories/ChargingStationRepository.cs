/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Charging Station repository for data access operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;


/// Repository for ChargingStation entity data access operations

public class ChargingStationRepository : IRepository<ChargingStation>
{
    private readonly IMongoCollection<ChargingStation> _collection;

    
    /// Initializes a new instance of the ChargingStationRepository
    
    /// <param name="context">MongoDB database context</param>
    public ChargingStationRepository(MongoDbContext context)
    {
        _collection = context.ChargingStations;
    }

    
    /// Gets a charging station by ID
    
    /// <param name="id">Station ID</param>
    /// <returns>Charging station if found, null otherwise</returns>
    public async Task<ChargingStation?> GetByIdAsync(string id)
    {
        return await _collection.Find(s => s.Id == id).FirstOrDefaultAsync();
    }

    
    /// Gets all charging stations
    
    /// <returns>Collection of charging stations</returns>
    public async Task<IEnumerable<ChargingStation>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    
    /// Gets charging stations matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching charging stations</returns>
    public async Task<IEnumerable<ChargingStation>> FindAsync(Expression<Func<ChargingStation, bool>> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    
    /// Gets the first charging station matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching charging station if found, null otherwise</returns>
    public async Task<ChargingStation?> FindOneAsync(Expression<Func<ChargingStation, bool>> filter)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    
    /// Counts charging stations matching the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching charging stations</returns>
    public async Task<long> CountAsync(Expression<Func<ChargingStation, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter);
    }

    
    /// Creates a new charging station
    
    /// <param name="entity">Charging station to create</param>
    /// <returns>Created charging station</returns>
    public async Task<ChargingStation> CreateAsync(ChargingStation entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    
    /// Updates an existing charging station
    
    /// <param name="id">Station ID</param>
    /// <param name="entity">Updated charging station</param>
    /// <returns>Updated charging station if found, null otherwise</returns>
    public async Task<ChargingStation?> UpdateAsync(string id, ChargingStation entity)
    {
        var result = await _collection.ReplaceOneAsync(s => s.Id == id, entity);
        return result.IsAcknowledged && result.ModifiedCount > 0 ? entity : null;
    }

    
    /// Deletes a charging station by ID
    
    /// <param name="id">Station ID</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _collection.DeleteOneAsync(s => s.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    
    /// Checks if a charging station exists with the specified filter
    
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(Expression<Func<ChargingStation, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    
    /// Gets active charging stations
    
    /// <returns>Collection of active charging stations</returns>
    public async Task<IEnumerable<ChargingStation>> GetActiveStationsAsync()
    {
        return await _collection.Find(s => s.IsActive).ToListAsync();
    }

    
    /// Gets charging stations by type
    
    /// <param name="type">Station type (AC/DC)</param>
    /// <returns>Collection of charging stations of the specified type</returns>
    public async Task<IEnumerable<ChargingStation>> GetByTypeAsync(string type)
    {
        return await _collection.Find(s => s.Type == type && s.IsActive).ToListAsync();
    }

    
    /// Gets nearby charging stations within a specified distance
    
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxDistanceKm">Maximum distance in kilometers</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Collection of nearby charging stations</returns>
    public async Task<IEnumerable<ChargingStation>> GetNearbyStationsAsync(double latitude, double longitude, double maxDistanceKm = 10.0, int limit = 20)
    {
        var point = GeoJson.Point(GeoJson.Geographic(longitude, latitude));
        var filter = Builders<ChargingStation>.Filter.And(
            Builders<ChargingStation>.Filter.Eq(s => s.IsActive, true),
            Builders<ChargingStation>.Filter.NearSphere(s => s.Location, point, maxDistanceKm * 1000) // Convert km to meters
        );

        return await _collection
            .Find(filter)
            .Limit(limit)
            .ToListAsync();
    }

    
    /// Gets paginated charging stations
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="type">Optional station type filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <returns>Paginated charging stations</returns>
    public async Task<(IEnumerable<ChargingStation> Stations, long TotalCount)> GetPaginatedAsync(int page, int pageSize, string? type = null, bool? isActive = null)
    {
        var filter = Builders<ChargingStation>.Filter.Empty;

        if (!string.IsNullOrEmpty(type))
        {
            filter = Builders<ChargingStation>.Filter.And(filter, Builders<ChargingStation>.Filter.Eq(s => s.Type, type));
        }

        if (isActive.HasValue)
        {
            filter = Builders<ChargingStation>.Filter.And(filter, Builders<ChargingStation>.Filter.Eq(s => s.IsActive, isActive.Value));
        }

        var totalCount = await _collection.CountDocumentsAsync(filter);
        var stations = await _collection
            .Find(filter)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (stations, totalCount);
    }
}

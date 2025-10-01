/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Booking repository for data access operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace EVChargingBackend.Repositories;

/// <summary>
/// Repository for Booking entity data access operations
/// </summary>
public class BookingRepository : IRepository<Booking>
{
    private readonly IMongoCollection<Booking> _collection;

    /// <summary>
    /// Initializes a new instance of the BookingRepository
    /// </summary>
    /// <param name="context">MongoDB database context</param>
    public BookingRepository(MongoDbContext context)
    {
        _collection = context.Bookings;
    }

    /// <summary>
    /// Gets a booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking if found, null otherwise</returns>
    public async Task<Booking?> GetByIdAsync(string id)
    {
        return await _collection.Find(b => b.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets all bookings
    /// </summary>
    /// <returns>Collection of bookings</returns>
    public async Task<IEnumerable<Booking>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    /// <summary>
    /// Gets bookings matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>Collection of matching bookings</returns>
    public async Task<IEnumerable<Booking>> FindAsync(Expression<Func<Booking, bool>> filter)
    {
        return await _collection.Find(filter).ToListAsync();
    }

    /// <summary>
    /// Gets the first booking matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>First matching booking if found, null otherwise</returns>
    public async Task<Booking?> FindOneAsync(Expression<Func<Booking, bool>> filter)
    {
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Counts bookings matching the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>Count of matching bookings</returns>
    public async Task<long> CountAsync(Expression<Func<Booking, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter);
    }

    /// <summary>
    /// Creates a new booking
    /// </summary>
    /// <param name="entity">Booking to create</param>
    /// <returns>Created booking</returns>
    public async Task<Booking> CreateAsync(Booking entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    /// <summary>
    /// Updates an existing booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="entity">Updated booking</param>
    /// <returns>Updated booking if found, null otherwise</returns>
    public async Task<Booking?> UpdateAsync(string id, Booking entity)
    {
        var result = await _collection.ReplaceOneAsync(b => b.Id == id, entity);
        return result.IsAcknowledged && result.ModifiedCount > 0 ? entity : null;
    }

    /// <summary>
    /// Deletes a booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> DeleteAsync(string id)
    {
        var result = await _collection.DeleteOneAsync(b => b.Id == id);
        return result.IsAcknowledged && result.DeletedCount > 0;
    }

    /// <summary>
    /// Checks if a booking exists with the specified filter
    /// </summary>
    /// <param name="filter">Filter expression</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> ExistsAsync(Expression<Func<Booking, bool>> filter)
    {
        return await _collection.CountDocumentsAsync(filter) > 0;
    }

    /// <summary>
    /// Gets bookings for a specific EV owner
    /// </summary>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <param name="includeHistory">Include historical bookings</param>
    /// <returns>Collection of bookings for the EV owner</returns>
    public async Task<IEnumerable<Booking>> GetByEVOwnerAsync(string evOwnerNIC, bool includeHistory = true)
    {
        var filter = Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC);
        
        if (!includeHistory)
        {
            filter = Builders<Booking>.Filter.And(
                filter,
                Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, DateTime.UtcNow)
            );
        }

        return await _collection
            .Find(filter)
            .SortByDescending(b => b.ReservationDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets bookings for a specific charging station
    /// </summary>
    /// <param name="stationId">Charging station ID</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>Collection of bookings for the station</returns>
    public async Task<IEnumerable<Booking>> GetByStationAsync(string stationId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var filter = Builders<Booking>.Filter.Eq(b => b.StationId, stationId);

        if (fromDate.HasValue)
        {
            filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, fromDate.Value));
        }

        if (toDate.HasValue)
        {
            filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Lte(b => b.ReservationDateTime, toDate.Value));
        }

        return await _collection
            .Find(filter)
            .SortBy(b => b.ReservationDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets bookings by status
    /// </summary>
    /// <param name="status">Booking status</param>
    /// <param name="fromDate">Start date filter</param>
    /// <returns>Collection of bookings with the specified status</returns>
    public async Task<IEnumerable<Booking>> GetByStatusAsync(string status, DateTime? fromDate = null)
    {
        var filter = Builders<Booking>.Filter.Eq(b => b.Status, status);

        if (fromDate.HasValue)
        {
            filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, fromDate.Value));
        }

        return await _collection
            .Find(filter)
            .SortBy(b => b.ReservationDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets paginated bookings
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="evOwnerNIC">Optional EV owner NIC filter</param>
    /// <param name="stationId">Optional station ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Paginated bookings</returns>
    public async Task<(IEnumerable<Booking> Bookings, long TotalCount)> GetPaginatedAsync(int page, int pageSize, string? evOwnerNIC = null, string? stationId = null, string? status = null)
    {
        var filter = Builders<Booking>.Filter.Empty;

        if (!string.IsNullOrEmpty(evOwnerNIC))
        {
            filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC));
        }

        if (!string.IsNullOrEmpty(stationId))
        {
            filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Eq(b => b.StationId, stationId));
        }

        if (!string.IsNullOrEmpty(status))
        {
            filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Eq(b => b.Status, status));
        }

        var totalCount = await _collection.CountDocumentsAsync(filter);
        var bookings = await _collection
            .Find(filter)
            .SortByDescending(b => b.ReservationDateTime)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (bookings, totalCount);
    }
}

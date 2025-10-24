using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;

namespace EVChargingBackend.Repositories;

/// <summary>
/// Repository for managing station schedule overrides
/// </summary>
public class StationScheduleOverrideRepository
{
    private readonly IMongoCollection<StationScheduleOverride> _collection;

    public StationScheduleOverrideRepository(MongoDbContext context)
    {
        _collection = context.StationScheduleOverrides;
    }

    /// <summary>
    /// Get all overrides for a station within a date range
    /// </summary>
    public async Task<List<StationScheduleOverride>> GetOverridesForStationAsync(
        string stationId, 
        DateTime startDate, 
        DateTime endDate)
    {
        var filter = Builders<StationScheduleOverride>.Filter.And(
            Builders<StationScheduleOverride>.Filter.Eq(o => o.StationId, stationId),
            Builders<StationScheduleOverride>.Filter.Gte(o => o.Date, startDate.Date),
            Builders<StationScheduleOverride>.Filter.Lte(o => o.Date, endDate.Date)
        );

        return await _collection.Find(filter).ToListAsync();
    }

    /// <summary>
    /// Get override for a specific station and date
    /// </summary>
    public async Task<StationScheduleOverride?> GetOverrideForDateAsync(string stationId, DateTime date)
    {
        var filter = Builders<StationScheduleOverride>.Filter.And(
            Builders<StationScheduleOverride>.Filter.Eq(o => o.StationId, stationId),
            Builders<StationScheduleOverride>.Filter.Eq(o => o.Date, date.Date)
        );

        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Create or update an override
    /// </summary>
    public async Task<StationScheduleOverride> UpsertOverrideAsync(StationScheduleOverride overrideData)
    {
        var filter = Builders<StationScheduleOverride>.Filter.And(
            Builders<StationScheduleOverride>.Filter.Eq(o => o.StationId, overrideData.StationId),
            Builders<StationScheduleOverride>.Filter.Eq(o => o.Date, overrideData.Date.Date)
        );

        var options = new ReplaceOptions { IsUpsert = true };
        await _collection.ReplaceOneAsync(filter, overrideData, options);
        return overrideData;
    }

    /// <summary>
    /// Delete an override for a specific date
    /// </summary>
    public async Task<bool> DeleteOverrideAsync(string stationId, DateTime date)
    {
        var filter = Builders<StationScheduleOverride>.Filter.And(
            Builders<StationScheduleOverride>.Filter.Eq(o => o.StationId, stationId),
            Builders<StationScheduleOverride>.Filter.Eq(o => o.Date, date.Date)
        );

        var result = await _collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// Check if there are any overrides that would conflict with existing approved bookings
    /// </summary>
    public async Task<List<StationScheduleOverride>> GetConflictingOverridesAsync(
        string stationId, 
        DateTime startDate, 
        DateTime endDate)
    {
        var filter = Builders<StationScheduleOverride>.Filter.And(
            Builders<StationScheduleOverride>.Filter.Eq(o => o.StationId, stationId),
            Builders<StationScheduleOverride>.Filter.Gte(o => o.Date, startDate.Date),
            Builders<StationScheduleOverride>.Filter.Lte(o => o.Date, endDate.Date),
            Builders<StationScheduleOverride>.Filter.Or(
                Builders<StationScheduleOverride>.Filter.Eq(o => o.Closed, true),
                Builders<StationScheduleOverride>.Filter.Exists(o => o.OpenTime),
                Builders<StationScheduleOverride>.Filter.Exists(o => o.CloseTime),
                Builders<StationScheduleOverride>.Filter.Size(o => o.MaintenanceWindows, 0)
            )
        );

        return await _collection.Find(filter).ToListAsync();
    }
}

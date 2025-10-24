/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: MongoDB database context for the EV Charging System
 */

using EVChargingBackend.Models;
using MongoDB.Driver;

namespace EVChargingBackend.Data;

/// <summary>
/// MongoDB database context for managing database connections and collections
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the MongoDbContext
    /// </summary>
    /// <param name="connectionString">MongoDB connection string</param>
    /// <param name="databaseName">Database name</param>
    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    /// <summary>
    /// Gets the Users collection
    /// </summary>
    public IMongoCollection<User> Users => _database.GetCollection<User>("users");

    /// <summary>
    /// Gets the EVOwners collection
    /// </summary>
    public IMongoCollection<EVOwner> EVOwners => _database.GetCollection<EVOwner>("evowners");

    /// <summary>
    /// Gets the ChargingStations collection
    /// </summary>
    public IMongoCollection<ChargingStation> ChargingStations => _database.GetCollection<ChargingStation>("chargingstations");

    /// <summary>
    /// Gets the Bookings collection
    /// </summary>
    public IMongoCollection<Booking> Bookings => _database.GetCollection<Booking>("bookings");

    /// <summary>
    /// Gets the StationScheduleOverrides collection
    /// </summary>
    public IMongoCollection<StationScheduleOverride> StationScheduleOverrides => _database.GetCollection<StationScheduleOverride>("stationscheduleoverrides");

    /// <summary>
    /// Creates all required indexes for the collections
    /// </summary>
    public async Task CreateIndexesAsync()
    {
        try
        {
            // Ensure collections exist by creating them if they don't
            await EnsureCollectionExistsAsync("users");
            await EnsureCollectionExistsAsync("evowners");
            await EnsureCollectionExistsAsync("chargingstations");
            await EnsureCollectionExistsAsync("bookings");
            await EnsureCollectionExistsAsync("stationscheduleoverrides");

            // Users collection indexes
            await CreateIndexIfNotExistsAsync(
                Users,
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true, Name = "username_unique" }
            );

            // EVOwners collection indexes - NIC is already the _id field, so no need for separate unique index
            // The _id field is automatically unique in MongoDB

            // ChargingStations collection indexes
            await CreateIndexIfNotExistsAsync(
                ChargingStations,
                Builders<ChargingStation>.IndexKeys.Ascending(s => s.IsActive),
                new CreateIndexOptions { Name = "is_active_index" }
            );

            // Geospatial index for location-based queries
            await CreateIndexIfNotExistsAsync(
                ChargingStations,
                Builders<ChargingStation>.IndexKeys.Geo2DSphere(s => s.Location),
                new CreateIndexOptions { Name = "location_2dsphere" }
            );

            // Bookings collection indexes
            await CreateIndexIfNotExistsAsync(
                Bookings,
                Builders<Booking>.IndexKeys.Ascending(b => b.StationId).Ascending(b => b.ReservationDateTime),
                new CreateIndexOptions { Name = "station_reservation_index" }
            );

            await CreateIndexIfNotExistsAsync(
                Bookings,
                Builders<Booking>.IndexKeys.Ascending(b => b.EVOwnerNIC).Ascending(b => b.ReservationDateTime),
                new CreateIndexOptions { Name = "owner_reservation_index" }
            );

            await CreateIndexIfNotExistsAsync(
                Bookings,
                Builders<Booking>.IndexKeys.Ascending(b => b.Status).Ascending(b => b.ReservationDateTime),
                new CreateIndexOptions { Name = "status_reservation_index" }
            );

            // Compound index for capacity management: station + hour + status
            await CreateIndexIfNotExistsAsync(
                Bookings,
                Builders<Booking>.IndexKeys.Ascending(b => b.StationId).Ascending(b => b.StartHourKey).Ascending(b => b.Status),
                new CreateIndexOptions { Name = "station_hour_status_index" }
            );

            // StationScheduleOverrides collection indexes
            await CreateIndexIfNotExistsAsync(
                StationScheduleOverrides,
                Builders<StationScheduleOverride>.IndexKeys.Ascending(o => o.StationId).Ascending(o => o.Date),
                new CreateIndexOptions { Name = "station_date_index" }
            );
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to prevent application startup failure
            Console.WriteLine($"Warning: Failed to create some indexes: {ex.Message}");
        }
    }

    /// <summary>
    /// Ensures a collection exists by creating it if it doesn't
    /// </summary>
    /// <param name="collectionName">Name of the collection</param>
    private async Task EnsureCollectionExistsAsync(string collectionName)
    {
        try
        {
            var collections = await _database.ListCollectionNamesAsync();
            var collectionNames = await collections.ToListAsync();
            
            if (!collectionNames.Contains(collectionName))
            {
                await _database.CreateCollectionAsync(collectionName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to ensure collection '{collectionName}' exists: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates an index if it doesn't already exist
    /// </summary>
    /// <typeparam name="T">Document type</typeparam>
    /// <param name="collection">MongoDB collection</param>
    /// <param name="keys">Index keys</param>
    /// <param name="options">Index options</param>
    private async Task CreateIndexIfNotExistsAsync<T>(IMongoCollection<T> collection, IndexKeysDefinition<T> keys, CreateIndexOptions options)
    {
        try
        {
            var existingIndexes = await collection.Indexes.ListAsync();
            var indexList = await existingIndexes.ToListAsync();
            
            // Check if an index with the same name already exists
            var indexExists = indexList.Any(index => 
                index.TryGetElement("name", out var nameElement) && 
                nameElement.Value.AsString == options.Name);

            if (!indexExists)
            {
                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<T>(keys, options)
                );
                Console.WriteLine($"Successfully created index: {options.Name}");
            }
            else
            {
                Console.WriteLine($"Index already exists: {options.Name}");
            }
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexKeySpecsConflict" || ex.CodeName == "IndexOptionsConflict")
        {
            Console.WriteLine($"Index already exists or similar key pattern found: {options.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to create index '{options.Name}': {ex.Message}");
        }
    }
}
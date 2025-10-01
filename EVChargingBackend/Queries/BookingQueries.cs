/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Booking queries for complex business logic operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Models;
using MongoDB.Driver;

namespace EVChargingBackend.Queries;

/// <summary>
/// Repository for complex booking queries and business logic operations
/// </summary>
public class BookingQueries
{
    private readonly IMongoCollection<Booking> _collection;

    /// <summary>
    /// Initializes a new instance of the BookingQueries
    /// </summary>
    /// <param name="context">MongoDB database context</param>
    public BookingQueries(MongoDbContext context)
    {
        _collection = context.Bookings;
    }

    /// <summary>
    /// Checks if a charging station has any active future bookings
    /// </summary>
    /// <param name="stationId">Charging station ID</param>
    /// <param name="fromUtc">Start date for checking (usually current UTC time)</param>
    /// <returns>True if there are active future bookings, false otherwise</returns>
    public async Task<bool> HasActiveFutureBookingsForStationAsync(string stationId, DateTime fromUtc)
    {
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, fromUtc),
            Builders<Booking>.Filter.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved })
        );

        var count = await _collection.CountDocumentsAsync(filter);
        return count > 0;
    }

    /// <summary>
    /// Counts overlapping approved bookings for a station within a time range
    /// </summary>
    /// <param name="stationId">Charging station ID</param>
    /// <param name="startUtc">Start time of the booking slot</param>
    /// <param name="endUtc">End time of the booking slot</param>
    /// <returns>Number of overlapping approved bookings</returns>
    public async Task<int> CountOverlappingApprovedAsync(string stationId, DateTime startUtc, DateTime endUtc)
    {
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved),
            Builders<Booking>.Filter.Or(
                // Booking starts within the time range
                Builders<Booking>.Filter.And(
                    Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, startUtc),
                    Builders<Booking>.Filter.Lt(b => b.ReservationDateTime, endUtc)
                ),
                // Booking ends within the time range
                Builders<Booking>.Filter.And(
                    Builders<Booking>.Filter.Gt(b => b.ReservationDateTime, startUtc),
                    Builders<Booking>.Filter.Lte(b => b.ReservationDateTime, endUtc)
                ),
                // Booking encompasses the entire time range
                Builders<Booking>.Filter.And(
                    Builders<Booking>.Filter.Lte(b => b.ReservationDateTime, startUtc),
                    Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, endUtc)
                )
            )
        );

        var count = await _collection.CountDocumentsAsync(filter);
        return (int)count;
    }

    /// <summary>
    /// Gets dashboard statistics for an EV owner
    /// </summary>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <returns>Dashboard statistics</returns>
    public async Task<DashboardStats> GetDashboardStatsAsync(string evOwnerNIC)
    {
        var now = DateTime.UtcNow;

        // Count pending reservations
        var pendingFilter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Pending),
            Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, now)
        );
        var pendingCount = await _collection.CountDocumentsAsync(pendingFilter);

        // Count approved future reservations
        var approvedFilter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved),
            Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, now)
        );
        var approvedCount = await _collection.CountDocumentsAsync(approvedFilter);

        // Count total bookings
        var totalFilter = Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC);
        var totalCount = await _collection.CountDocumentsAsync(totalFilter);

        return new DashboardStats
        {
            PendingReservations = (int)pendingCount,
            ApprovedFutureReservations = (int)approvedCount,
            TotalBookings = (int)totalCount
        };
    }

    /// <summary>
    /// Gets bookings that can be cancelled (more than 12 hours before reservation)
    /// </summary>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <returns>Collection of cancellable bookings</returns>
    public async Task<IEnumerable<Booking>> GetCancellableBookingsAsync(string evOwnerNIC)
    {
        var twelveHoursFromNow = DateTime.UtcNow.AddHours(12);
        
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC),
            Builders<Booking>.Filter.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved }),
            Builders<Booking>.Filter.Gt(b => b.ReservationDateTime, twelveHoursFromNow)
        );

        return await _collection
            .Find(filter)
            .SortBy(b => b.ReservationDateTime)
            .ToListAsync();
    }

    /// <summary>
    /// Gets bookings that can be updated (more than 12 hours before reservation)
    /// </summary>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <returns>Collection of updatable bookings</returns>
    public async Task<IEnumerable<Booking>> GetUpdatableBookingsAsync(string evOwnerNIC)
    {
        var twelveHoursFromNow = DateTime.UtcNow.AddHours(12);
        
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.EVOwnerNIC, evOwnerNIC),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Pending),
            Builders<Booking>.Filter.Gt(b => b.ReservationDateTime, twelveHoursFromNow)
        );

        return await _collection
            .Find(filter)
            .SortBy(b => b.ReservationDateTime)
            .ToListAsync();
    }
}

/// <summary>
/// Dashboard statistics for EV owners
/// </summary>
public class DashboardStats
{
    /// <summary>
    /// Number of pending reservations
    /// </summary>
    public int PendingReservations { get; set; }

    /// <summary>
    /// Number of approved future reservations
    /// </summary>
    public int ApprovedFutureReservations { get; set; }

    /// <summary>
    /// Total number of bookings
    /// </summary>
    public int TotalBookings { get; set; }
}

/*
 * Author: EV Charging System
 * Date: 2025-10-04
 * Purpose: Booking queries for complex business logic operations
 */

using EVChargingBackend.Data;
using EVChargingBackend.Helpers;
using EVChargingBackend.Models;
using MongoDB.Driver;

namespace EVChargingBackend.Queries;


/// Repository for complex booking queries and business logic operations

public class BookingQueries
{
    private readonly IMongoCollection<Booking> _collection;

    
    /// Initializes a new instance of the BookingQueries
    
    /// <param name="context">MongoDB database context</param>
    public BookingQueries(MongoDbContext context)
    {
        _collection = context.Bookings;
    }

    
    /// Checks if a charging station has any active future bookings
    
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

    
    /// Checks if a charging station has any approved future bookings (for deactivation guard)
    
    /// <param name="stationId">Charging station ID</param>
    /// <param name="fromUtc">Start date for checking (usually current UTC time)</param>
    /// <returns>True if there are approved future bookings, false otherwise</returns>
    public async Task<bool> HasApprovedFutureBookingsForStationAsync(string stationId, DateTime fromUtc)
    {
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, fromUtc),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved)
        );

        var count = await _collection.CountDocumentsAsync(filter);
        return count > 0;
    }

    
    /// Counts overlapping approved bookings for a station within a time range
    
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

    
    /// Gets dashboard statistics for an EV owner
    
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

    
    /// Gets bookings that can be cancelled (more than 12 hours before reservation)
    
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

    
    /// Gets bookings that can be updated (more than 12 hours before reservation)
    
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

    
    /// Counts approved bookings for a specific station and hour key
    
    /// <param name="stationId">Charging station ID</param>
    /// <param name="hourKey">Hour key in yyyyMMddHH format</param>
    /// <returns>Number of approved bookings for that station and hour</returns>
    public async Task<int> CountApprovedForStationAndHourAsync(string stationId, string hourKey)
    {
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Eq(b => b.StartHourKey, hourKey),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved)
        );

        var count = await _collection.CountDocumentsAsync(filter);
        return (int)count;
    }

    
    /// Gets utilization data for a station for the next 7 days
    
    /// <param name="stationId">Charging station ID</param>
    /// <param name="totalSlots">Total slots available at the station</param>
    /// <returns>Utilization data grouped by hour</returns>
    public async Task<Dictionary<string, (int Approved, int Pending)>> GetStationUtilizationAsync(string stationId, int totalSlots)
    {
        var now = DateTime.UtcNow;
        var sevenDaysFromNow = now.AddDays(7);

        // Get all bookings for this station in the next 7 days
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, now),
            Builders<Booking>.Filter.Lt(b => b.ReservationDateTime, sevenDaysFromNow),
            Builders<Booking>.Filter.In(b => b.Status, new[] { BookingStatus.Pending, BookingStatus.Approved })
        );

        var bookings = await _collection
            .Find(filter)
            .Project(b => new { b.StartHourKey, b.Status, b.ReservationDateTime })
            .ToListAsync();

        // Group by hour key and count by status
        var utilization = new Dictionary<string, (int Approved, int Pending)>();
        
        foreach (var booking in bookings)
        {
            var hourKey = booking.StartHourKey ?? TimeNormalizationHelper.GenerateHourKey(booking.ReservationDateTime);
            
            if (!utilization.ContainsKey(hourKey))
            {
                utilization[hourKey] = (0, 0);
            }

            var current = utilization[hourKey];
            if (booking.Status == BookingStatus.Approved)
            {
                utilization[hourKey] = (current.Approved + 1, current.Pending);
            }
            else if (booking.Status == BookingStatus.Pending)
            {
                utilization[hourKey] = (current.Approved, current.Pending + 1);
            }
        }

        return utilization;
    }

    /// <summary>
    /// Counts pending bookings for a specific station and hour key
    /// </summary>
    /// <param name="stationId">Charging station ID</param>
    /// <param name="hourKey">Hour key in yyyyMMddHH format</param>
    /// <returns>Number of pending bookings for that station and hour</returns>
    public async Task<int> CountPendingForStationAndHourAsync(string stationId, string hourKey)
    {
        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Eq(b => b.StartHourKey, hourKey),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Pending)
        );

        var count = await _collection.CountDocumentsAsync(filter);
        return (int)count;
    }

    /// <summary>
    /// Gets approved bookings for a specific station and date
    /// </summary>
    /// <param name="stationId">Charging station ID</param>
    /// <param name="date">Date to check</param>
    /// <returns>List of approved bookings for that date</returns>
    public async Task<List<Booking>> GetApprovedBookingsForDateAsync(string stationId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var filter = Builders<Booking>.Filter.And(
            Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
            Builders<Booking>.Filter.Gte(b => b.ReservationDateTime, startOfDay),
            Builders<Booking>.Filter.Lt(b => b.ReservationDateTime, endOfDay),
            Builders<Booking>.Filter.Eq(b => b.Status, BookingStatus.Approved)
        );

        return await _collection.Find(filter).ToListAsync();
    }
}


/// Dashboard statistics for EV owners

public class DashboardStats
{
    
    /// Number of pending reservations
    
    public int PendingReservations { get; set; }

    
    /// Number of approved future reservations
    
    public int ApprovedFutureReservations { get; set; }

    
    /// Total number of bookings
    
    public int TotalBookings { get; set; }
}

using EVChargingBackend.DTOs;
using EVChargingBackend.Helpers;
using EVChargingBackend.Models;
using EVChargingBackend.Queries;
using EVChargingBackend.Repositories;

namespace EVChargingBackend.Services;

/// <summary>
/// Service for calculating station availability with overrides and maintenance windows
/// </summary>
public class StationAvailabilityService
{
    private readonly ChargingStationRepository _stationRepository;
    private readonly StationScheduleOverrideRepository _overrideRepository;
    private readonly BookingQueries _bookingQueries;

    public StationAvailabilityService(
        ChargingStationRepository stationRepository,
        StationScheduleOverrideRepository overrideRepository,
        BookingQueries bookingQueries)
    {
        _stationRepository = stationRepository;
        _overrideRepository = overrideRepository;
        _bookingQueries = bookingQueries;
    }

    /// <summary>
    /// Get availability data for a station within a date range
    /// </summary>
    public async Task<StationAvailabilityResponse?> GetStationAvailabilityAsync(
        string stationId, 
        DateTime startDate, 
        int days)
    {
        var station = await _stationRepository.GetByIdAsync(stationId);
        if (station == null)
        {
            return null;
        }

        var endDate = startDate.AddDays(days);
        var overrides = await _overrideRepository.GetOverridesForStationAsync(stationId, startDate, endDate);
        var overrideDict = overrides.ToDictionary(o => o.Date.Date, o => o);

        var dateAvailability = new List<DateAvailability>();

        for (var currentDate = startDate.Date; currentDate < endDate.Date; currentDate = currentDate.AddDays(1))
        {
            var dateOverride = overrideDict.GetValueOrDefault(currentDate);
            var dateAvail = await CalculateDateAvailabilityAsync(station, currentDate, dateOverride);
            dateAvailability.Add(dateAvail);
        }

        return new StationAvailabilityResponse
        {
            StationId = station.Id,
            StationName = station.Name,
            TotalSlots = station.TotalSlots,
            OpenTime = station.OpenTime,
            CloseTime = station.CloseTime,
            IsActive = station.IsActive,
            DateAvailability = dateAvailability
        };
    }

    /// <summary>
    /// Calculate availability for a specific date
    /// </summary>
    private async Task<DateAvailability> CalculateDateAvailabilityAsync(
        ChargingStation station, 
        DateTime date, 
        StationScheduleOverride? dateOverride)
    {
        var dateAvail = new DateAvailability
        {
            Date = date.ToString("yyyy-MM-dd"),
            IsClosed = dateOverride?.Closed ?? false,
            SpecialOpenTime = dateOverride?.OpenTime,
            SpecialCloseTime = dateOverride?.CloseTime
        };

        // If station is inactive or date is closed, all hours are closed
        if (!station.IsActive || dateAvail.IsClosed)
        {
            return dateAvail;
        }

        // Determine working hours for this date
        var openTime = dateOverride?.OpenTime ?? station.OpenTime;
        var closeTime = dateOverride?.CloseTime ?? station.CloseTime;

        // Generate hourly slots
        var currentHour = date.Add(openTime);
        var endHour = date.Add(closeTime);

        while (currentHour < endHour)
        {
            var hourKey = TimeNormalizationHelper.GenerateHourKey(currentHour);
            var hourStr = currentHour.ToString("HH:00");

            // Get booking counts for this hour
            var approvedCount = await _bookingQueries.CountApprovedForStationAndHourAsync(station.Id, hourKey);
            var pendingCount = await _bookingQueries.CountPendingForStationAndHourAsync(station.Id, hourKey);

            // Check if this hour is in maintenance
            var maintenanceReason = GetMaintenanceReason(currentHour, dateOverride?.MaintenanceWindows);

            var hourAvail = new HourAvailability
            {
                Hour = hourStr,
                Capacity = station.TotalSlots,
                ApprovedCount = approvedCount,
                PendingCount = pendingCount,
                Status = maintenanceReason != null ? "maintenance" : "open",
                Reason = maintenanceReason
            };

            dateAvail.HourAvailability.Add(hourAvail);
            currentHour = currentHour.AddHours(1);
        }

        return dateAvail;
    }

    /// <summary>
    /// Check if an hour falls within a maintenance window
    /// </summary>
    private string? GetMaintenanceReason(DateTime hour, List<MaintenanceWindow>? maintenanceWindows)
    {
        if (maintenanceWindows == null || maintenanceWindows.Count == 0)
        {
            return null;
        }

        var hourTime = hour.TimeOfDay;

        foreach (var window in maintenanceWindows)
        {
            if (hourTime >= window.StartTime && hourTime < window.EndTime)
            {
                return window.Reason;
            }
        }

        return null;
    }

    /// <summary>
    /// Validate that schedule overrides don't conflict with existing approved bookings
    /// </summary>
    public async Task<List<string>> ValidateOverrideConflictsAsync(
        string stationId, 
        StationScheduleOverride overrideData)
    {
        var conflicts = new List<string>();

        // If closing the day, check for approved bookings
        if (overrideData.Closed)
        {
            var approvedBookings = await _bookingQueries.GetApprovedBookingsForDateAsync(
                stationId, overrideData.Date);
            
            if (approvedBookings.Any())
            {
                conflicts.Add($"Cannot close day {overrideData.Date:yyyy-MM-dd} - {approvedBookings.Count} approved bookings exist");
            }
        }

        // If changing hours, check for approved bookings outside new hours
        if (overrideData.OpenTime.HasValue || overrideData.CloseTime.HasValue)
        {
            var newOpenTime = overrideData.OpenTime ?? TimeSpan.Zero;
            var newCloseTime = overrideData.CloseTime ?? TimeSpan.FromHours(24);

            var approvedBookings = await _bookingQueries.GetApprovedBookingsForDateAsync(
                stationId, overrideData.Date);

            foreach (var booking in approvedBookings)
            {
                var bookingTime = booking.ReservationDateTime.TimeOfDay;
                if (bookingTime < newOpenTime || bookingTime >= newCloseTime)
                {
                    conflicts.Add($"Booking at {bookingTime:hh\\:mm} conflicts with new hours {newOpenTime:hh\\:mm}-{newCloseTime:hh\\:mm}");
                }
            }
        }

        return conflicts;
    }
}

using System;
using System.Collections.Generic;

namespace EVChargingBackend.DTOs;

/// <summary>
/// Response for station availability data
/// </summary>
public class StationAvailabilityResponse
{
    /// <summary>
    /// Station ID
    /// </summary>
    public string StationId { get; set; } = string.Empty;

    /// <summary>
    /// Station name
    /// </summary>
    public string StationName { get; set; } = string.Empty;

    /// <summary>
    /// Total slots available at the station
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Base working hours for the station
    /// </summary>
    public TimeSpan OpenTime { get; set; }

    /// <summary>
    /// Base closing time for the station
    /// </summary>
    public TimeSpan CloseTime { get; set; }

    /// <summary>
    /// Whether the station is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Availability data grouped by date
    /// </summary>
    public List<DateAvailability> DateAvailability { get; set; } = new();
}

/// <summary>
/// Availability data for a specific date
/// </summary>
public class DateAvailability
{
    /// <summary>
    /// Date (YYYY-MM-DD format)
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Whether this date is closed (override)
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>
    /// Special opening time for this date (if different from base)
    /// </summary>
    public TimeSpan? SpecialOpenTime { get; set; }

    /// <summary>
    /// Special closing time for this date (if different from base)
    /// </summary>
    public TimeSpan? SpecialCloseTime { get; set; }

    /// <summary>
    /// Hourly availability data
    /// </summary>
    public List<HourAvailability> HourAvailability { get; set; } = new();
}

/// <summary>
/// Availability data for a specific hour
/// </summary>
public class HourAvailability
{
    /// <summary>
    /// Hour in HH:00 format (e.g., "08:00")
    /// </summary>
    public string Hour { get; set; } = string.Empty;

    /// <summary>
    /// Total capacity for this hour
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Number of approved bookings (consuming capacity)
    /// </summary>
    public int ApprovedCount { get; set; }

    /// <summary>
    /// Number of pending bookings (not consuming capacity)
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Available slots (Capacity - ApprovedCount)
    /// </summary>
    public int Available => Capacity - ApprovedCount;

    /// <summary>
    /// Status of this hour: open, closed, maintenance
    /// </summary>
    public string Status { get; set; } = "open";

    /// <summary>
    /// Reason for status (if not open)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Whether this hour is fully booked
    /// </summary>
    public bool IsFull => ApprovedCount >= Capacity;

    /// <summary>
    /// Whether this hour is nearly full (>= 80% capacity)
    /// </summary>
    public bool IsNearlyFull => Capacity > 0 && (ApprovedCount * 100 / Capacity) >= 80;
}

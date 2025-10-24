/*
 * Author: EV Charging System
 * Date: 2025-01-27
 * Purpose: DTO for station utilization data
 */

namespace EVChargingBackend.DTOs;

/// <summary>
/// Response for station utilization data
/// </summary>
public class StationUtilizationResponse
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
    /// Total slots available
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Working hours
    /// </summary>
    public TimeSpan OpenTime { get; set; }

    /// <summary>
    /// Working hours
    /// </summary>
    public TimeSpan CloseTime { get; set; }

    /// <summary>
    /// Hourly utilization data for the next 7 days
    /// </summary>
    public List<HourlyUtilization> HourlyData { get; set; } = new();
}

/// <summary>
/// Hourly utilization data
/// </summary>
public class HourlyUtilization
{
    /// <summary>
    /// Date and hour (normalized to top of hour)
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Hour key in yyyyMMddHH format
    /// </summary>
    public string HourKey { get; set; } = string.Empty;

    /// <summary>
    /// Number of approved bookings for this hour
    /// </summary>
    public int ApprovedCount { get; set; }

    /// <summary>
    /// Number of pending bookings for this hour
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Total capacity for this hour
    /// </summary>
    public int TotalCapacity { get; set; }

    /// <summary>
    /// Available slots (TotalCapacity - ApprovedCount)
    /// </summary>
    public int AvailableSlots => TotalCapacity - ApprovedCount;

    /// <summary>
    /// Utilization percentage (ApprovedCount / TotalCapacity * 100)
    /// </summary>
    public double UtilizationPercentage => TotalCapacity > 0 ? (double)ApprovedCount / TotalCapacity * 100 : 0;
}

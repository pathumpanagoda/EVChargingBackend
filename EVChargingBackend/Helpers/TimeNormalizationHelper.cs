/*
 * Author: EV Charging System
 * Date: 2025-01-27
 * Purpose: Helper for time normalization and slot management
 */

using System;

namespace EVChargingBackend.Helpers;

/// <summary>
/// Helper class for time normalization and slot management
/// </summary>
public static class TimeNormalizationHelper
{
    /// <summary>
    /// Normalizes a datetime to the top of the hour in UTC
    /// </summary>
    /// <param name="dateTime">The datetime to normalize</param>
    /// <returns>Normalized datetime at the top of the hour</returns>
    public static DateTime NormalizeToHour(DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// Generates a canonical hour key in yyyyMMddHH format
    /// </summary>
    /// <param name="dateTime">The datetime to generate key for</param>
    /// <returns>Hour key in yyyyMMddHH format</returns>
    public static string GenerateHourKey(DateTime dateTime)
    {
        var normalized = NormalizeToHour(dateTime);
        return normalized.ToString("yyyyMMddHH");
    }

    /// <summary>
    /// Checks if a datetime is within working hours
    /// </summary>
    /// <param name="dateTime">The datetime to check</param>
    /// <param name="openTime">Station opening time</param>
    /// <param name="closeTime">Station closing time</param>
    /// <returns>True if within working hours</returns>
    public static bool IsWithinWorkingHours(DateTime dateTime, TimeSpan openTime, TimeSpan closeTime)
    {
        var timeOfDay = dateTime.TimeOfDay;
        return timeOfDay >= openTime && timeOfDay < closeTime;
    }

    /// <summary>
    /// Checks if a datetime is within the 7-day booking window
    /// </summary>
    /// <param name="dateTime">The datetime to check</param>
    /// <returns>True if within 7 days from now</returns>
    public static bool IsWithin7DayWindow(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var sevenDaysFromNow = now.AddDays(7);
        return dateTime >= now && dateTime < sevenDaysFromNow;
    }

    /// <summary>
    /// Checks if a datetime is at least 12 hours in the future
    /// </summary>
    /// <param name="dateTime">The datetime to check</param>
    /// <returns>True if at least 12 hours in the future</returns>
    public static bool IsAtLeast12HoursAhead(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var twelveHoursFromNow = now.AddHours(12);
        return dateTime >= twelveHoursFromNow;
    }

    /// <summary>
    /// Gets the end time for a 1-hour slot
    /// </summary>
    /// <param name="startTime">The start time</param>
    /// <returns>End time (start + 1 hour)</returns>
    public static DateTime GetEndTime(DateTime startTime)
    {
        return startTime.AddHours(1);
    }

    /// <summary>
    /// Calculates the end time for a booking, normalizing to hour boundaries
    /// </summary>
    /// <param name="startTime">The start time</param>
    /// <param name="endTime">The requested end time (optional)</param>
    /// <returns>Normalized end time</returns>
    public static DateTime CalculateEndTime(DateTime startTime, DateTime? endTime)
    {
        if (endTime.HasValue)
        {
            // Normalize end time to the top of the hour
            var normalizedEnd = NormalizeToHour(endTime.Value);
            // If end time is same as start time, add 1 hour
            if (normalizedEnd <= startTime)
            {
                return startTime.AddHours(1);
            }
            return normalizedEnd;
        }
        else
        {
            // Default to 1-hour slot
            return startTime.AddHours(1);
        }
    }

    /// <summary>
    /// Generates a list of hour keys for a multi-hour booking
    /// </summary>
    /// <param name="startTime">The start time</param>
    /// <param name="endTime">The end time</param>
    /// <returns>List of hour keys that this booking occupies</returns>
    public static List<string> GenerateOccupiedHourKeys(DateTime startTime, DateTime endTime)
    {
        var hourKeys = new List<string>();
        var currentHour = NormalizeToHour(startTime);
        var normalizedEnd = NormalizeToHour(endTime);

        while (currentHour < normalizedEnd)
        {
            hourKeys.Add(GenerateHourKey(currentHour));
            currentHour = currentHour.AddHours(1);
        }

        return hourKeys;
    }

    /// <summary>
    /// Validates that a time range is within working hours
    /// </summary>
    /// <param name="startTime">The start time</param>
    /// <param name="endTime">The end time</param>
    /// <param name="openTime">Station opening time</param>
    /// <param name="closeTime">Station closing time</param>
    /// <returns>True if the entire range is within working hours</returns>
    public static bool IsTimeRangeWithinWorkingHours(DateTime startTime, DateTime endTime, TimeSpan openTime, TimeSpan closeTime)
    {
        Console.WriteLine($"IsTimeRangeWithinWorkingHours: Start={startTime}, End={endTime}, Open={openTime}, Close={closeTime}");
        
        var currentHour = NormalizeToHour(startTime);
        var normalizedEnd = NormalizeToHour(endTime);

        Console.WriteLine($"Normalized: CurrentHour={currentHour}, NormalizedEnd={normalizedEnd}");

        while (currentHour < normalizedEnd)
        {
            var timeOfDay = currentHour.TimeOfDay;
            var isWithinHours = timeOfDay >= openTime && timeOfDay < closeTime;
            Console.WriteLine($"Checking hour {currentHour}: TimeOfDay={timeOfDay}, IsWithinHours={isWithinHours}");
            
            if (!isWithinHours)
            {
                Console.WriteLine($"Hour {currentHour} is outside working hours");
                return false;
            }
            currentHour = currentHour.AddHours(1);
        }

        Console.WriteLine("All hours are within working hours");
        return true;
    }
}

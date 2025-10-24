/*
 * Author: EV Charging System
 * Date: 2025-01-27
 * Purpose: Tests for capacity management and slot normalization
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Helpers;
using EVChargingBackend.Models;
using EVChargingBackend.Queries;
using EVChargingBackend.Repositories;
using EVChargingBackend.Services;
using EVChargingBackend.Validators;
using FluentAssertions;
using Moq;

namespace EVChargingBackend.Tests;

/// <summary>
/// Tests for capacity management and slot normalization
/// </summary>
public class CapacityTests
{
    [Fact]
    public void TimeNormalizationHelper_NormalizeToHour_ShouldRoundToTopOfHour()
    {
        // Arrange
        var input = new DateTime(2025, 1, 27, 14, 30, 45, DateTimeKind.Utc);
        var expected = new DateTime(2025, 1, 27, 14, 0, 0, DateTimeKind.Utc);

        // Act
        var result = TimeNormalizationHelper.NormalizeToHour(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void TimeNormalizationHelper_GenerateHourKey_ShouldCreateCorrectFormat()
    {
        // Arrange
        var input = new DateTime(2025, 1, 27, 14, 0, 0, DateTimeKind.Utc);
        var expected = "2025012714";

        // Act
        var result = TimeNormalizationHelper.GenerateHourKey(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void TimeNormalizationHelper_IsWithin7DayWindow_ShouldReturnTrueForValidDate()
    {
        // Arrange
        var validDate = DateTime.UtcNow.AddDays(3);

        // Act
        var result = TimeNormalizationHelper.IsWithin7DayWindow(validDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TimeNormalizationHelper_IsWithin7DayWindow_ShouldReturnFalseForInvalidDate()
    {
        // Arrange
        var invalidDate = DateTime.UtcNow.AddDays(8);

        // Act
        var result = TimeNormalizationHelper.IsWithin7DayWindow(invalidDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimeNormalizationHelper_IsAtLeast12HoursAhead_ShouldReturnTrueForValidDate()
    {
        // Arrange
        var validDate = DateTime.UtcNow.AddHours(15);

        // Act
        var result = TimeNormalizationHelper.IsAtLeast12HoursAhead(validDate);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TimeNormalizationHelper_IsAtLeast12HoursAhead_ShouldReturnFalseForInvalidDate()
    {
        // Arrange
        var invalidDate = DateTime.UtcNow.AddHours(6);

        // Act
        var result = TimeNormalizationHelper.IsAtLeast12HoursAhead(invalidDate);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimeNormalizationHelper_IsWithinWorkingHours_ShouldReturnTrueForValidTime()
    {
        // Arrange
        var validTime = new DateTime(2025, 1, 27, 10, 0, 0, DateTimeKind.Utc);
        var openTime = new TimeSpan(8, 0, 0);
        var closeTime = new TimeSpan(20, 0, 0);

        // Act
        var result = TimeNormalizationHelper.IsWithinWorkingHours(validTime, openTime, closeTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void TimeNormalizationHelper_IsWithinWorkingHours_ShouldReturnFalseForInvalidTime()
    {
        // Arrange
        var invalidTime = new DateTime(2025, 1, 27, 22, 0, 0, DateTimeKind.Utc);
        var openTime = new TimeSpan(8, 0, 0);
        var closeTime = new TimeSpan(20, 0, 0);

        // Act
        var result = TimeNormalizationHelper.IsWithinWorkingHours(invalidTime, openTime, closeTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TimeNormalizationHelper_GetEndTime_ShouldAddOneHour()
    {
        // Arrange
        var startTime = new DateTime(2025, 1, 27, 14, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2025, 1, 27, 15, 0, 0, DateTimeKind.Utc);

        // Act
        var result = TimeNormalizationHelper.GetEndTime(startTime);

        // Assert
        result.Should().Be(expected);
    }
}

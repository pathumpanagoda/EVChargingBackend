/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: FluentValidation validators for Charging Station DTOs
 */

using EVChargingBackend.DTOs;
using FluentValidation;

namespace EVChargingBackend.Validators;

/// <summary>
/// Validator for Charging Station creation/update requests
/// </summary>
public class ChargingStationRequestValidator : AbstractValidator<ChargingStationRequest>
{
    /// <summary>
    /// Initializes a new instance of the ChargingStationRequestValidator
    /// </summary>
    public ChargingStationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Station name is required")
            .Length(2, 100).WithMessage("Station name must be between 2 and 100 characters");

        RuleFor(x => x.Location)
            .NotNull().WithMessage("Location is required");

        RuleFor(x => x.Location.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90 degrees");

        RuleFor(x => x.Location.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180 degrees");

        RuleFor(x => x.Location.Address)
            .NotEmpty().WithMessage("Address is required")
            .Length(10, 200).WithMessage("Address must be between 10 and 200 characters");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Station type is required")
            .Must(type => type == "AC" || type == "DC").WithMessage("Station type must be either AC or DC");

        RuleFor(x => x.TotalSlots)
            .GreaterThan(0).WithMessage("Total slots must be greater than 0")
            .LessThanOrEqualTo(50).WithMessage("Total slots cannot exceed 50");

        RuleFor(x => x.OperatorId)
            .NotEmpty().WithMessage("Operator ID is required");
    }
}

/// <summary>
/// Validator for Charging Station schedule update requests
/// </summary>
public class StationScheduleRequestValidator : AbstractValidator<StationScheduleRequest>
{
    /// <summary>
    /// Initializes a new instance of the StationScheduleRequestValidator
    /// </summary>
    public StationScheduleRequestValidator()
    {
        RuleFor(x => x.Schedule)
            .NotNull().WithMessage("Schedule is required")
            .Must(schedule => schedule.Count > 0).WithMessage("At least one schedule entry is required");

        RuleForEach(x => x.Schedule)
            .SetValidator(new DailyScheduleValidator());
    }
}

/// <summary>
/// Validator for daily schedule entries
/// </summary>
public class DailyScheduleValidator : AbstractValidator<DailyScheduleRequest>
{
    /// <summary>
    /// Initializes a new instance of the DailyScheduleValidator
    /// </summary>
    public DailyScheduleValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required")
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Date cannot be in the past");

        RuleFor(x => x.Open)
            .NotEmpty().WithMessage("Opening time is required");

        RuleFor(x => x.Close)
            .NotEmpty().WithMessage("Closing time is required")
            .GreaterThan(x => x.Open).WithMessage("Closing time must be after opening time");

        RuleFor(x => x.SlotsAvailable)
            .GreaterThanOrEqualTo(0).WithMessage("Slots available cannot be negative")
            .LessThanOrEqualTo(50).WithMessage("Slots available cannot exceed 50");
    }
}

/// <summary>
/// Charging Station request DTO
/// </summary>
public record ChargingStationRequest
{
    /// <summary>
    /// Name of the charging station
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Location of the charging station
    /// </summary>
    public LocationRequest Location { get; init; } = new();

    /// <summary>
    /// Type of charging station (AC/DC)
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Total number of charging slots
    /// </summary>
    public int TotalSlots { get; init; }

    /// <summary>
    /// ID of the operator managing this station
    /// </summary>
    public string OperatorId { get; init; } = string.Empty;
}

/// <summary>
/// Location request DTO
/// </summary>
public record LocationRequest
{
    /// <summary>
    /// Latitude coordinate
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// Human-readable address
    /// </summary>
    public string Address { get; init; } = string.Empty;
}

/// <summary>
/// Station schedule request DTO
/// </summary>
public record StationScheduleRequest
{
    /// <summary>
    /// Daily schedule entries
    /// </summary>
    public List<DailyScheduleRequest> Schedule { get; init; } = new();
}

/// <summary>
/// Daily schedule request DTO
/// </summary>
public record DailyScheduleRequest
{
    /// <summary>
    /// Date for this schedule entry
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Opening time
    /// </summary>
    public TimeSpan Open { get; init; }

    /// <summary>
    /// Closing time
    /// </summary>
    public TimeSpan Close { get; init; }

    /// <summary>
    /// Number of slots available
    /// </summary>
    public int SlotsAvailable { get; init; }
}

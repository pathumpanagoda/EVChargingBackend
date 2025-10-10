/*
 * Author: EV Charging System
 * Date: 2025-10-04
 * Purpose: FluentValidation validators for Charging Station DTOs
 */

using EVChargingBackend.DTOs;
using FluentValidation;

namespace EVChargingBackend.Validators;


/// Validator for Charging Station creation/update requests

public class ChargingStationRequestValidator : AbstractValidator<ChargingStationRequest>
{
    
    /// Initializes a new instance of the ChargingStationRequestValidator
    
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


/// Validator for Charging Station schedule update requests

public class StationScheduleRequestValidator : AbstractValidator<StationScheduleRequest>
{
    
    /// Initializes a new instance of the StationScheduleRequestValidator
    
    public StationScheduleRequestValidator()
    {
        RuleFor(x => x.Schedule)
            .NotNull().WithMessage("Schedule is required")
            .Must(schedule => schedule.Count > 0).WithMessage("At least one schedule entry is required");

        RuleForEach(x => x.Schedule)
            .SetValidator(new DailyScheduleValidator());
    }
}


/// Validator for daily schedule entries

public class DailyScheduleValidator : AbstractValidator<DailyScheduleRequest>
{
    
    /// Initializes a new instance of the DailyScheduleValidator
    
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


/// Charging Station request DTO

public record ChargingStationRequest
{
    
    /// Name of the charging station
    
    public string Name { get; init; } = string.Empty;

    
    /// Location of the charging station
    
    public LocationRequest Location { get; init; } = new();

    
    /// Type of charging station (AC/DC)
    
    public string Type { get; init; } = string.Empty;

    
    /// Total number of charging slots
    
    public int TotalSlots { get; init; }

    
    /// ID of the operator managing this station
    
    public string OperatorId { get; init; } = string.Empty;
}


/// Location request DTO

public record LocationRequest
{
    
    /// Latitude coordinate
    
    public double Latitude { get; init; }

    
    /// Longitude coordinate
    
    public double Longitude { get; init; }

    
    /// Human-readable address
    
    public string Address { get; init; } = string.Empty;
}


/// Station schedule request DTO

public record StationScheduleRequest
{
    
    /// Daily schedule entries
    
    public List<DailyScheduleRequest> Schedule { get; init; } = new();
}


/// Daily schedule request DTO

public record DailyScheduleRequest
{
    
    /// Date for this schedule entry
    
    public DateTime Date { get; init; }

    
    /// Opening time
    
    public TimeSpan Open { get; init; }

    
    /// Closing time
    
    public TimeSpan Close { get; init; }

    
    /// Number of slots available
    
    public int SlotsAvailable { get; init; }
}

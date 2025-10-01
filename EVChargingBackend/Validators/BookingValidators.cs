/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: FluentValidation validators for Booking DTOs
 */

using EVChargingBackend.DTOs;
using FluentValidation;

namespace EVChargingBackend.Validators;

/// <summary>
/// Validator for BookingRequest DTO
/// </summary>
public class BookingRequestValidator : AbstractValidator<BookingRequest>
{
    /// <summary>
    /// Initializes a new instance of the BookingRequestValidator
    /// </summary>
    public BookingRequestValidator()
    {
        RuleFor(x => x.StationId)
            .NotEmpty().WithMessage("Station ID is required");

        RuleFor(x => x.ReservationDateTime)
            .NotEmpty().WithMessage("Reservation date and time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Reservation time must be in the future")
            .Must(BeWithinSevenDays).WithMessage("Reservation must be within 7 days from booking date");
    }

    /// <summary>
    /// Validates that the reservation date is within 7 days
    /// </summary>
    /// <param name="reservationDateTime">Reservation date and time</param>
    /// <returns>True if within 7 days, false otherwise</returns>
    private static bool BeWithinSevenDays(DateTime reservationDateTime)
    {
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        return reservationDateTime <= sevenDaysFromNow;
    }
}

/// <summary>
/// Validator for BookingUpdateRequest DTO
/// </summary>
public class BookingUpdateRequestValidator : AbstractValidator<BookingUpdateRequest>
{
    /// <summary>
    /// Initializes a new instance of the BookingUpdateRequestValidator
    /// </summary>
    public BookingUpdateRequestValidator()
    {
        RuleFor(x => x.ReservationDateTime)
            .NotEmpty().WithMessage("Reservation date and time is required")
            .GreaterThan(DateTime.UtcNow).WithMessage("Reservation time must be in the future")
            .Must(BeWithinSevenDays).WithMessage("Reservation must be within 7 days from booking date");
    }

    /// <summary>
    /// Validates that the reservation date is within 7 days
    /// </summary>
    /// <param name="reservationDateTime">Reservation date and time</param>
    /// <returns>True if within 7 days, false otherwise</returns>
    private static bool BeWithinSevenDays(DateTime reservationDateTime)
    {
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        return reservationDateTime <= sevenDaysFromNow;
    }
}

/// <summary>
/// Validator for BookingCompleteRequest DTO
/// </summary>
public class BookingCompleteRequestValidator : AbstractValidator<BookingCompleteRequest>
{
    /// <summary>
    /// Initializes a new instance of the BookingCompleteRequestValidator
    /// </summary>
    public BookingCompleteRequestValidator()
    {
        RuleFor(x => x.QRPayload)
            .NotEmpty().WithMessage("QR payload is required")
            .Must(BeValidQRPayload).WithMessage("Invalid QR payload format");
    }

    /// <summary>
    /// Validates that the QR payload has the correct format
    /// </summary>
    /// <param name="qrPayload">QR payload string</param>
    /// <returns>True if valid format, false otherwise</returns>
    private static bool BeValidQRPayload(string qrPayload)
    {
        if (string.IsNullOrEmpty(qrPayload))
            return false;

        var parts = qrPayload.Split(':');
        return parts.Length == 5 && parts[0] == "BOOKING";
    }
}

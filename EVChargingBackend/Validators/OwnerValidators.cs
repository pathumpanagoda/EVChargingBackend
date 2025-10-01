/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: FluentValidation validators for EV Owner DTOs
 */

using EVChargingBackend.DTOs;
using FluentValidation;

namespace EVChargingBackend.Validators;

/// <summary>
/// Validator for EV Owner creation/update requests
/// </summary>
public class EVOwnerRequestValidator : AbstractValidator<EVOwnerRequest>
{
    /// <summary>
    /// Initializes a new instance of the EVOwnerRequestValidator
    /// </summary>
    public EVOwnerRequestValidator()
    {
        RuleFor(x => x.NIC)
            .NotEmpty().WithMessage("NIC is required")
            .Length(10, 12).WithMessage("NIC must be between 10 and 12 characters")
            .Matches(@"^[0-9]{9}[VX]|[0-9]{12}$").WithMessage("NIC must be in valid Sri Lankan format");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .Length(2, 100).WithMessage("Name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\s]+$").WithMessage("Name can only contain letters and spaces");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email must not exceed 100 characters");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone is required")
            .Length(10, 15).WithMessage("Phone must be between 10 and 15 characters")
            .Matches(@"^[0-9+\-\s()]+$").WithMessage("Phone can only contain numbers, spaces, and common phone symbols");

        When(x => !string.IsNullOrEmpty(x.Password), () =>
        {
            RuleFor(x => x.Password)
                .MinimumLength(6).WithMessage("Password must be at least 6 characters")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit");
        });
    }
}

/// <summary>
/// EV Owner request DTO for creation and updates
/// </summary>
public record EVOwnerRequest
{
    /// <summary>
    /// National Identity Card number
    /// </summary>
    public string NIC { get; init; } = string.Empty;

    /// <summary>
    /// Full name of the EV owner
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>
    /// Password (optional for updates)
    /// </summary>
    public string? Password { get; init; }
}

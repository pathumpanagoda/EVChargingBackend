/*
 * Author: EV Charging System
 * Date: 2025-10-04
 * Purpose: FluentValidation validators for authentication DTOs
 */

using EVChargingBackend.DTOs;
using FluentValidation;

namespace EVChargingBackend.Validators;


/// Validator for LoginRequest DTO

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    
    /// Initializes a new instance of the LoginRequestValidator
    
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username or NIC is required")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");
    }
}


/// Validator for RegisterRequest DTO

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    
    /// Initializes a new instance of the RegisterRequestValidator
    
    public RegisterRequestValidator()
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

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Password must contain at least one lowercase letter, one uppercase letter, and one digit");
    }
}

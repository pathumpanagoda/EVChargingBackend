/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Registration request DTO for EV owner self-registration
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;


/// Request DTO for EV owner self-registration

public record RegisterRequest
{
    
    /// National Identity Card number
    
    [Required(ErrorMessage = "NIC is required")]
    [StringLength(12, MinimumLength = 10, ErrorMessage = "NIC must be between 10 and 12 characters")]
    public string NIC { get; init; } = string.Empty;

    
    /// Full name of the EV owner
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    
    /// Email address
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    public string Email { get; init; } = string.Empty;

    
    /// Phone number
    
    [Required(ErrorMessage = "Phone is required")]
    [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone must be between 10 and 15 characters")]
    public string Phone { get; init; } = string.Empty;

    
    /// Password for authentication
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; init; } = string.Empty;
}
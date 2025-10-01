/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Registration request DTO for EV owner self-registration
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;

/// <summary>
/// Request DTO for EV owner self-registration
/// </summary>
public record RegisterRequest
{
    /// <summary>
    /// National Identity Card number
    /// </summary>
    [Required(ErrorMessage = "NIC is required")]
    [StringLength(12, MinimumLength = 10, ErrorMessage = "NIC must be between 10 and 12 characters")]
    public string NIC { get; init; } = string.Empty;

    /// <summary>
    /// Full name of the EV owner
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email must not exceed 100 characters")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    [Required(ErrorMessage = "Phone is required")]
    [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone must be between 10 and 15 characters")]
    public string Phone { get; init; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; init; } = string.Empty;
}
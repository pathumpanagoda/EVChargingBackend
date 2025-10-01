/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Login request DTO for authentication
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;

/// <summary>
/// Request DTO for user login authentication
/// </summary>
public record LoginRequest
{
    /// <summary>
    /// Username or NIC for login
    /// </summary>
    [Required(ErrorMessage = "Username or NIC is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password for authentication
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; init; } = string.Empty;
}
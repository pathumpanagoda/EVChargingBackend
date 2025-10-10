/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: Login request DTO for authentication
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;


/// Request DTO for user login authentication

public record LoginRequest
{
    
    /// Username or NIC for login
    
    [Required(ErrorMessage = "Username or NIC is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; init; } = string.Empty;

    
    /// Password for authentication
    
    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; init; } = string.Empty;
}
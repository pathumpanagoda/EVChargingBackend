/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Pagination DTOs for API requests
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;

/// <summary>
/// Pagination parameters for API requests
/// </summary>
public record PaginationRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; init; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Search term for filtering
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Sort field
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    public string? SortDirection { get; init; } = "asc";
}

/// <summary>
/// Pagination parameters for nearby stations request
/// </summary>
public record NearbyStationsRequest : PaginationRequest
{
    /// <summary>
    /// Latitude coordinate
    /// </summary>
    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate
    /// </summary>
    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; init; }

    /// <summary>
    /// Maximum distance in kilometers
    /// </summary>
    [Range(0.1, 100, ErrorMessage = "Distance must be between 0.1 and 100 km")]
    public double MaxDistance { get; init; } = 10.0;
}
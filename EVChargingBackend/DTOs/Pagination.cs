/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: Pagination DTOs for API requests
 */

using System.ComponentModel.DataAnnotations;

namespace EVChargingBackend.DTOs;


/// Pagination parameters for API requests

public record PaginationRequest
{
    
    /// Page number (1-based)
    
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; init; } = 1;

    
    /// Number of items per page
    
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; init; } = 10;

    
    /// Search term for filtering
    
    public string? Search { get; init; }

    
    /// Sort field
    
    public string? SortBy { get; init; }

    
    /// Sort direction (asc/desc)
    
    public string? SortDirection { get; init; } = "asc";
}


/// Pagination parameters for nearby stations request

public record NearbyStationsRequest : PaginationRequest
{
    
    /// Latitude coordinate
    
    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; init; }

    
    /// Longitude coordinate
    
    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; init; }

    
    /// Maximum distance in kilometers
    
    [Range(0.1, 100, ErrorMessage = "Distance must be between 0.1 and 100 km")]
    public double MaxDistance { get; init; } = 10.0;
}
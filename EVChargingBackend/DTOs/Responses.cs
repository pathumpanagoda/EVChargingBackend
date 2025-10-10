/*
 * Author: EV Charging System
 * Date: 2025-09-23
 * Purpose: Response DTOs for API responses
 */

namespace EVChargingBackend.DTOs;


/// Standard API response wrapper

/// <typeparam name="T">Type of data being returned</typeparam>
public record ApiResponse<T>
{
    
    /// Indicates if the operation was successful
    
    public bool Success { get; init; } = true;

    
    /// Response message
    
    public string Message { get; init; } = string.Empty;

    
    /// Response data
    
    public T? Data { get; init; }

    
    /// List of validation errors
    
    public List<string> Errors { get; init; } = new();
}


/// Authentication response containing JWT token and user information

public record AuthResponse
{
    
    /// JWT access token
    
    public string Token { get; init; } = string.Empty;

    
    /// Token expiration time
    
    public DateTime ExpiresAt { get; init; }

    
    /// User role
    
    public string Role { get; init; } = string.Empty;

    
    /// User ID or NIC
    
    public string UserId { get; init; } = string.Empty;
    
    /// EV Owner NIC (for EV owners only)
    
    public string? Nic { get; init; }
}




/// Paginated response wrapper

/// <typeparam name="T">Type of items in the collection</typeparam>
public record PaginatedResponse<T>
{
    
    /// Collection of items
    
    public List<T> Items { get; init; } = new();

    
    /// Current page number
    
    public int Page { get; init; }

    
    /// Number of items per page
    
    public int PageSize { get; init; }

    
    /// Total number of items
    
    public long TotalCount { get; init; }

    
    /// Total number of pages
    
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    
    /// Indicates if there is a next page
    
    public bool HasNextPage => Page < TotalPages;

    
    /// Indicates if there is a previous page
    
    public bool HasPreviousPage => Page > 1;
}


/// Dashboard statistics response

public record DashboardResponse
{
    
    /// Number of pending reservations
    
    public int PendingReservations { get; init; }

    
    /// Number of approved future reservations
    
    public int ApprovedFutureReservations { get; init; }

    
    /// Total number of bookings
    
    public int TotalBookings { get; init; }
}
/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Response DTOs for API responses
 */

namespace EVChargingBackend.DTOs;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public record ApiResponse<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Response message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> Errors { get; init; } = new();
}

/// <summary>
/// Authentication response containing JWT token and user information
/// </summary>
public record AuthResponse
{
    /// <summary>
    /// JWT access token
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// User ID or NIC
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// EV Owner NIC (for EV owners only)
    /// </summary>
    public string? Nic { get; init; }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public record PaginatedResponse<T>
{
    /// <summary>
    /// Collection of items
    /// </summary>
    public List<T> Items { get; init; } = new();

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public long TotalCount { get; init; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Dashboard statistics response
/// </summary>
public record DashboardResponse
{
    /// <summary>
    /// Number of pending reservations
    /// </summary>
    public int PendingReservations { get; init; }

    /// <summary>
    /// Number of approved future reservations
    /// </summary>
    public int ApprovedFutureReservations { get; init; }

    /// <summary>
    /// Total number of bookings
    /// </summary>
    public int TotalBookings { get; init; }
}
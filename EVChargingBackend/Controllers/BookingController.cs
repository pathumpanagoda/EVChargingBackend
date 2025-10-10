/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Booking management controller for charging station reservations
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace EVChargingBackend.Controllers;

/// <summary>
/// Controller for booking management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Booking management endpoints for charging station reservations")]
public class BookingController : ControllerBase
{
    private readonly BookingService _bookingService;

    /// <summary>
    /// Initializes a new instance of the BookingController
    /// </summary>
    /// <param name="bookingService">Booking service</param>
    public BookingController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Creates a new booking
    /// </summary>
    /// <param name="request">Booking creation request</param>
    /// <returns>Created booking</returns>
    [HttpPost]
    [Authorize]
    [SwaggerOperation(
        Summary = "Create Booking",
        Description = "Creates a new charging station booking"
    )]
    [SwaggerResponse(201, "Booking created successfully", typeof(ApiResponse<Booking>))]
    [SwaggerResponse(400, "Validation error or business rule violation", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(409, "No slots available", typeof(ApiResponse<object>))]
    [SwaggerResponse(422, "Business rule violation (7-day rule, slot availability)", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<Booking>>> CreateBooking([FromBody] BookingRequest request)
    {
        try
        {
            var evOwnerNIC = GetEVOwnerNIC();
            var booking = await _bookingService.CreateBookingAsync(request, evOwnerNIC);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, new ApiResponse<Booking>
            {
                Success = true,
                Message = "Booking created successfully",
                Data = booking
            });
        }
        catch (Exception ex)
        {
            // Check if it's a slot availability issue
            if (ex.Message == "No slots available")
            {
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets a booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking if found and authorized</returns>
    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get Booking",
        Description = "Gets a booking by ID (owner can access own bookings, Backoffice/Operator can access any)"
    )]
    [SwaggerResponse(200, "Booking found", typeof(ApiResponse<Booking>))]
    [SwaggerResponse(404, "Booking not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<Booking>>> GetBooking(string id)
    {
        var booking = await _bookingService.GetBookingAsync(id);
        if (booking == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Booking not found"
            });
        }

        // Check authorization
        if (!IsAuthorizedForBooking(booking))
        {
            return Forbid();
        }

        return Ok(new ApiResponse<Booking>
        {
            Success = true,
            Message = "Booking found",
            Data = booking
        });
    }

    /// <summary>
    /// Gets bookings for an EV owner
    /// </summary>
    /// <param name="nic">EV owner NIC</param>
    /// <param name="includeHistory">Include historical bookings</param>
    /// <returns>Collection of bookings for the EV owner</returns>
    [HttpGet("owner/{nic}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get Owner Bookings",
        Description = "Gets bookings for a specific EV owner (owner can access own bookings, Backoffice can access any)"
    )]
    [SwaggerResponse(200, "Bookings retrieved successfully", typeof(ApiResponse<IEnumerable<Booking>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<Booking>>>> GetOwnerBookings(string nic, [FromQuery] bool includeHistory = true)
    {
        // Check authorization
        if (!IsAuthorizedForEVOwner(nic))
        {
            return Forbid();
        }

        var bookings = await _bookingService.GetBookingsByOwnerAsync(nic, includeHistory);
        return Ok(new ApiResponse<IEnumerable<Booking>>
        {
            Success = true,
            Message = "Bookings retrieved successfully",
            Data = bookings
        });
    }

    /// <summary>
    /// Gets dashboard statistics for an EV owner
    /// </summary>
    /// <param name="nic">EV owner NIC</param>
    /// <returns>Dashboard statistics</returns>
    [HttpGet("dashboard/{nic}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get Dashboard Stats",
        Description = "Gets dashboard statistics for an EV owner (owner can access own stats, Backoffice can access any)"
    )]
    [SwaggerResponse(200, "Dashboard stats retrieved successfully", typeof(ApiResponse<DashboardResponse>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> GetDashboardStats(string nic)
    {
        // Check authorization
        if (!IsAuthorizedForEVOwner(nic))
        {
            return Forbid();
        }

        var stats = await _bookingService.GetDashboardStatsAsync(nic);
        return Ok(new ApiResponse<DashboardResponse>
        {
            Success = true,
            Message = "Dashboard stats retrieved successfully",
            Data = stats
        });
    }

    /// <summary>
    /// Updates a booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="request">Booking update request</param>
    /// <returns>Updated booking if found and authorized</returns>
    [HttpPut("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update Booking",
        Description = "Updates an existing booking (owner can update own bookings, 12-hour rule applies)"
    )]
    [SwaggerResponse(200, "Booking updated successfully", typeof(ApiResponse<Booking>))]
    [SwaggerResponse(400, "Validation error", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Booking not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(409, "No slots available", typeof(ApiResponse<object>))]
    [SwaggerResponse(422, "Business rule violation (12-hour rule, 7-day rule, slot availability)", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<Booking>>> UpdateBooking(string id, [FromBody] BookingUpdateRequest request)
    {
        try
        {
            var evOwnerNIC = GetEVOwnerNIC();
            var booking = await _bookingService.UpdateBookingAsync(id, request, evOwnerNIC);
            if (booking == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Booking not found"
                });
            }

            return Ok(new ApiResponse<Booking>
            {
                Success = true,
                Message = "Booking updated successfully",
                Data = booking
            });
        }
        catch (Exception ex)
        {
            // Check if it's a slot availability issue
            if (ex.Message == "No slots available")
            {
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Cancels a booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Cancel Booking",
        Description = "Cancels an existing booking (owner can cancel own bookings, 12-hour rule applies)"
    )]
    [SwaggerResponse(200, "Booking cancelled successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Booking not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(422, "Business rule violation (12-hour rule)", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> CancelBooking(string id)
    {
        try
        {
            var evOwnerNIC = GetEVOwnerNIC();
            var result = await _bookingService.CancelBookingAsync(id, evOwnerNIC);
            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Booking not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Booking cancelled successfully"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Approves a booking (Backoffice/Operator only)
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking if found</returns>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    [SwaggerOperation(
        Summary = "Approve Booking",
        Description = "Approves a pending booking and generates QR code (Backoffice or StationOperator access required)"
    )]
    [SwaggerResponse(200, "Booking approved successfully", typeof(ApiResponse<Booking>))]
    [SwaggerResponse(404, "Booking not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Booking cannot be approved", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    [SwaggerResponse(409, "No slots available", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<Booking>>> ApproveBooking(string id)
    {
        try
        {
            var booking = await _bookingService.ApproveBookingAsync(id);
            if (booking == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Booking not found"
                });
            }

            return Ok(new ApiResponse<Booking>
            {
                Success = true,
                Message = "Booking approved successfully",
                Data = booking
            });
        }
        catch (Exception ex)
        {
            // Check if it's a slot availability issue
            if (ex.Message == "No slots available")
            {
                return Conflict(new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Completes a booking via QR scan
    /// </summary>
    /// <param name="request">Booking complete request with QR payload</param>
    /// <returns>Updated booking if found and valid</returns>
    [HttpPost("complete")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    [SwaggerOperation(
        Summary = "Complete Booking",
        Description = "Completes a booking via QR code scan (Backoffice or StationOperator access required)"
    )]
    [SwaggerResponse(200, "Booking completed successfully", typeof(ApiResponse<Booking>))]
    [SwaggerResponse(400, "Invalid QR payload or booking cannot be completed", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Booking not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<Booking>>> CompleteBooking([FromBody] BookingCompleteRequest request)
    {
        try
        {
            var booking = await _bookingService.CompleteBookingAsync(request);
            if (booking == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Booking not found"
                });
            }

            return Ok(new ApiResponse<Booking>
            {
                Success = true,
                Message = "Booking completed successfully",
                Data = booking
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets paginated bookings with optional filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="evOwnerNIC">Optional EV owner NIC filter</param>
    /// <param name="stationId">Optional station ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Paginated bookings</returns>
    [HttpGet]
    [Authorize(Roles = "Backoffice,StationOperator")]
    [SwaggerOperation(
        Summary = "List Bookings",
        Description = "Gets paginated list of bookings with optional filtering (Backoffice or StationOperator access required)"
    )]
    [SwaggerResponse(200, "Bookings retrieved successfully", typeof(ApiResponse<PaginatedResponse<Booking>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<Booking>>>> GetBookings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? evOwnerNIC = null,
        [FromQuery] string? stationId = null,
        [FromQuery] string? status = null)
    {
        var bookings = await _bookingService.GetBookingsAsync(page, pageSize, evOwnerNIC, stationId, status);
        return Ok(new ApiResponse<PaginatedResponse<Booking>>
        {
            Success = true,
            Message = "Bookings retrieved successfully",
            Data = bookings
        });
    }

    /// <summary>
    /// Gets the EV owner NIC from the current user's claims
    /// </summary>
    /// <returns>EV owner NIC</returns>
    private string GetEVOwnerNIC()
    {
        var nic = User.FindFirst("nic")?.Value;
        if (string.IsNullOrEmpty(nic))
        {
            throw new UnauthorizedAccessException("EV owner NIC not found in token");
        }
        return nic;
    }

    /// <summary>
    /// Checks if the current user is authorized to access booking data
    /// </summary>
    /// <param name="booking">Booking entity</param>
    /// <returns>True if authorized, false otherwise</returns>
    private bool IsAuthorizedForBooking(Booking booking)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userNIC = User.FindFirst("nic")?.Value;

        // Backoffice and StationOperator can access any booking
        if (userRole == "Backoffice" || userRole == "StationOperator")
        {
            return true;
        }

        // EV owner can only access their own bookings
        if (userRole == "EVOwner" && userNIC == booking.EVOwnerNIC)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if the current user is authorized to access EV owner data
    /// </summary>
    /// <param name="nic">EV owner NIC</param>
    /// <returns>True if authorized, false otherwise</returns>
    private bool IsAuthorizedForEVOwner(string nic)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userNIC = User.FindFirst("nic")?.Value;

        // Backoffice can access any EV owner
        if (userRole == "Backoffice")
        {
            return true;
        }

        // EV owner can only access their own data
        if (userRole == "EVOwner" && userNIC == nic)
        {
            return true;
        }

        return false;
    }
}

/*
 * Author: EV Charging System
 * Date: 2025-10-02
 * Purpose: Booking management controller for charging station reservations
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Services;
using EVChargingBackend.Helpers;
using EVChargingBackend.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace EVChargingBackend.Controllers;


/// Controller for booking management operations

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Booking management endpoints for charging station reservations")]
public class BookingController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly ChargingStationService _stationService;
    private readonly BookingQueries _bookingQueries;

    
    /// Initializes a new instance of the BookingController
    
    /// <param name="bookingService">Booking service</param>
    /// <param name="stationService">Charging station service</param>
    /// <param name="bookingQueries">Booking queries</param>
    public BookingController(BookingService bookingService, ChargingStationService stationService, BookingQueries bookingQueries)
    {
        _bookingService = bookingService;
        _stationService = stationService;
        _bookingQueries = bookingQueries;
    }

    
    /// Creates a new booking
    
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
            Console.WriteLine($"CreateBooking called with StationId: {request.StationId}, ReservationDateTime: {request.ReservationDateTime}, EndDateTime: {request.EndDateTime}");
            
            var evOwnerNIC = GetEVOwnerNIC();
            Console.WriteLine($"EV Owner NIC: {evOwnerNIC}");
            
            var booking = await _bookingService.CreateBookingAsync(request, evOwnerNIC);
            Console.WriteLine($"Booking created successfully with ID: {booking.Id}");
            
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, new ApiResponse<Booking>
            {
                Success = true,
                Message = "Booking created successfully",
                Data = booking
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Unauthorized access: {ex.Message}");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Argument error: {ex.Message}");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Invalid operation: {ex.Message}");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
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

    
    /// Gets a booking by ID
    
    /// <param name="id">Booking ID</param>
    /// <returns>Booking if found and authorized</returns>
    [HttpGet("{id}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get Booking",
        Description = "Gets a booking by ID (owner can access own bookings, Backoffice can access any, StationOperator can access their stations' bookings)"
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
        if (!await IsAuthorizedForBookingAsync(booking))
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

    
    /// Gets bookings for an EV owner
    
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

    
    /// Gets dashboard statistics for an EV owner
    
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

    
    /// Updates a booking
    
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

    
    /// Cancels a booking
    
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

    
    /// Approves a booking (Backoffice/Operator only)
    
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking if found</returns>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "StationOperator")]
    [SwaggerOperation(
        Summary = "Approve Booking",
        Description = "Approves a pending booking and generates QR code (StationOperator can approve their stations' bookings only)"
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
            // Get the booking first to check authorization
            var existingBooking = await _bookingService.GetBookingAsync(id);
            if (existingBooking == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Booking not found"
                });
            }

            // Check authorization for booking approval (only StationOperator can approve)
            if (!await IsAuthorizedForBookingApprovalAsync(existingBooking))
            {
                return Forbid();
            }

            var booking = await _bookingService.ApproveBookingAsync(id);

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

    
    /// Completes a booking via QR scan
    
    /// <param name="request">Booking complete request with QR payload</param>
    /// <returns>Updated booking if found and valid</returns>
    [HttpPost("complete")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    [SwaggerOperation(
        Summary = "Complete Booking",
        Description = "Completes a booking via QR code scan (Backoffice can complete any, StationOperator can complete their stations' bookings)"
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

            // Check authorization for station operators
            if (!await IsAuthorizedForBookingAsync(booking))
            {
                return Forbid();
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

    
    /// Gets paginated bookings with optional filtering
    
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
        Description = "Gets paginated list of bookings with optional filtering (Backoffice can see all, StationOperator sees only their stations' bookings)"
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
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        PaginatedResponse<Booking> bookings;

        // Station operators can only see bookings for their own stations
        if (userRole == "StationOperator")
        {
            var operatorId = GetUserId();
            bookings = await _bookingService.GetBookingsForOperatorAsync(page, pageSize, operatorId, evOwnerNIC, status);
        }
        else
        {
            // Backoffice can see all bookings
            bookings = await _bookingService.GetBookingsAsync(page, pageSize, evOwnerNIC, stationId, status);
        }

        return Ok(new ApiResponse<PaginatedResponse<Booking>>
        {
            Success = true,
            Message = "Bookings retrieved successfully",
            Data = bookings
        });
    }

    
    /// Test endpoint to debug authentication issues
    
    /// <returns>Current user information</returns>
    [HttpGet("debug-auth")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Debug Authentication",
        Description = "Debug endpoint to check current user authentication status and claims"
    )]
    [SwaggerResponse(200, "Authentication info retrieved", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    public ActionResult<ApiResponse<object>> DebugAuth()
    {
        var claims = User.Claims.Select(c => new { Type = c.Type, Value = c.Value }).ToList();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var nic = User.FindFirst("nic")?.Value;

        // Try to get EV owner NIC using the same method as booking creation
        string? evOwnerNIC = null;
        try
        {
            evOwnerNIC = GetEVOwnerNIC();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting EV owner NIC: {ex.Message}");
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "Authentication debug info",
            Data = new
            {
                Claims = claims,
                Role = userRole,
                UserId = userId,
                NIC = nic,
                EVOwnerNIC = evOwnerNIC,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                AuthenticationType = User.Identity?.AuthenticationType,
                Name = User.Identity?.Name
            }
        });
    }

    
    /// Test endpoint to create a simple booking for debugging
    
    /// <returns>Test booking result</returns>
    [HttpPost("test-booking")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Test Booking Creation",
        Description = "Test endpoint to create a simple booking for debugging purposes"
    )]
    [SwaggerResponse(200, "Test booking created", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> TestBooking()
    {
        try
        {
            Console.WriteLine("=== Test Booking Debug ===");
            
            // Get EV owner NIC
            var evOwnerNIC = GetEVOwnerNIC();
            Console.WriteLine($"EV Owner NIC: {evOwnerNIC}");
            
            // Create a simple test booking request
            var testRequest = new BookingRequest
            {
                StationId = "test-station-id", // This will likely fail, but we can see the error
                ReservationDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2)
            };
            
            Console.WriteLine($"Test request: StationId={testRequest.StationId}, Start={testRequest.ReservationDateTime}, End={testRequest.EndDateTime}");
            
            // Try to create booking
            var booking = await _bookingService.CreateBookingAsync(testRequest, evOwnerNIC);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Test booking created successfully",
                Data = new { BookingId = booking.Id, EVOwnerNIC = evOwnerNIC }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test booking error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message,
                Data = new { ErrorType = ex.GetType().Name, StackTrace = ex.StackTrace }
            });
        }
    }

    
    /// Gets available time slots for a station on a specific date
    
    /// <param name="stationId">Station ID</param>
    /// <param name="date">Date to check (YYYY-MM-DD format)</param>
    /// <returns>Available time slots</returns>
    [HttpGet("availability/{stationId}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get Available Time Slots",
        Description = "Gets available 1-hour time slots for a station on a specific date"
    )]
    [SwaggerResponse(200, "Available slots retrieved", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> GetAvailableSlots(string stationId, [FromQuery] string date)
    {
        try
        {
            Console.WriteLine($"Getting availability for station {stationId} on {date}");
            
            // Parse the date
            if (!DateTime.TryParse(date, out var targetDate))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid date format. Use YYYY-MM-DD"
                });
            }

            // Get station details
            var station = await _stationService.GetStationAsync(stationId);
            if (station == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Station not found"
                });
            }

            // Generate time slots (6 AM to 10 PM)
            var timeSlots = new List<object>();
            var now = DateTime.UtcNow;
            var targetDateTime = targetDate.Date;

            for (int hour = 6; hour <= 22; hour++)
            {
                var slotTime = targetDateTime.AddHours(hour);
                var slotEndTime = slotTime.AddHours(1);
                
                // Check if slot is in the future and respects 12-hour rule
                if (slotTime > now && slotTime > now.AddHours(12))
                {
                    // Check if slot is within station working hours
                    var stationOpenTime = targetDateTime.Add(station.OpenTime);
                    var stationCloseTime = targetDateTime.Add(station.CloseTime);
                    
                    if (slotTime >= stationOpenTime && slotEndTime <= stationCloseTime)
                    {
                        // Check availability (simplified - you can add actual booking count check here)
                        var hourKey = TimeNormalizationHelper.GenerateHourKey(slotTime);
                        var approvedCount = await _bookingQueries.CountApprovedForStationAndHourAsync(stationId, hourKey);
                        var pendingCount = await _bookingQueries.CountPendingForStationAndHourAsync(stationId, hourKey);
                        
                        var isAvailable = (approvedCount + pendingCount) < station.TotalSlots;
                        
                        timeSlots.Add(new
                        {
                            time = slotTime.ToString("HH:mm"),
                            available = isAvailable,
                            approvedBookings = approvedCount,
                            pendingBookings = pendingCount,
                            totalSlots = station.TotalSlots
                        });
                    }
                }
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Available slots retrieved",
                Data = new
                {
                    StationId = stationId,
                    Date = date,
                    TimeSlots = timeSlots
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting availability: {ex.Message}");
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    
    /// Gets the EV owner NIC from the current user's claims
    
    /// <returns>EV owner NIC</returns>
    private string GetEVOwnerNIC()
    {
        // Debug: Log all claims for troubleshooting
        Console.WriteLine("=== JWT Claims Debug ===");
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"Claim Type: {claim.Type}, Value: {claim.Value}");
        }
        Console.WriteLine("=== End JWT Claims Debug ===");
        
        // Try to get NIC from the "nic" claim first
        var nic = User.FindFirst("nic")?.Value;
        Console.WriteLine($"NIC from 'nic' claim: {nic}");
        
        // If not found, try to get it from the NameIdentifier claim (for EV owners, this is the NIC)
        if (string.IsNullOrEmpty(nic))
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            Console.WriteLine($"User role: {userRole}");
            if (userRole == "EVOwner")
            {
                nic = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                Console.WriteLine($"NIC from NameIdentifier claim: {nic}");
            }
        }
        
        if (string.IsNullOrEmpty(nic))
        {
            throw new UnauthorizedAccessException("EV owner NIC not found in token");
        }
        return nic;
    }

    
    /// Gets the user ID from the current user's claims
    
    /// <returns>User ID</returns>
    private string GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    
    /// Checks if the current user is authorized to access booking data
    
    /// <param name="booking">Booking entity</param>
    /// <returns>True if authorized, false otherwise</returns>
    private async Task<bool> IsAuthorizedForBookingAsync(Booking booking)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userNIC = User.FindFirst("nic")?.Value;

        // Backoffice can access any booking
        if (userRole == "Backoffice")
        {
            return true;
        }

        // StationOperator can only access bookings for their own stations
        if (userRole == "StationOperator")
        {
            var userId = GetUserId();
            var station = await _stationService.GetStationAsync(booking.StationId);
            return station != null && station.OperatorId == userId;
        }

        // EV owner can only access their own bookings
        if (userRole == "EVOwner" && userNIC == booking.EVOwnerNIC)
        {
            return true;
        }

        return false;
    }

    
    /// Checks if the current user is authorized to approve bookings (StationOperator only)
    
    /// <param name="booking">Booking entity</param>
    /// <returns>True if authorized, false otherwise</returns>
    private async Task<bool> IsAuthorizedForBookingApprovalAsync(Booking booking)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // Only StationOperator can approve bookings
        if (userRole == "StationOperator")
        {
            var userId = GetUserId();
            var station = await _stationService.GetStationAsync(booking.StationId);
            return station != null && station.OperatorId == userId;
        }

        return false;
    }

    
    /// Checks if the current user is authorized to access EV owner data
    
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

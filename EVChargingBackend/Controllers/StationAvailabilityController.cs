using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Repositories;
using EVChargingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EVChargingBackend.Controllers;

/// <summary>
/// Controller for station availability and schedule management
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Backoffice,StationOperator")]
public class StationAvailabilityController : ControllerBase
{
    private readonly StationAvailabilityService _availabilityService;
    private readonly StationScheduleOverrideRepository _overrideRepository;
    private readonly ChargingStationRepository _stationRepository;

    public StationAvailabilityController(
        StationAvailabilityService availabilityService,
        StationScheduleOverrideRepository overrideRepository,
        ChargingStationRepository stationRepository)
    {
        _availabilityService = availabilityService;
        _overrideRepository = overrideRepository;
        _stationRepository = stationRepository;
    }

    /// <summary>
    /// Get availability data for a station within a date range
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="start">Start date (YYYY-MM-DD)</param>
    /// <param name="days">Number of days (default: 7, max: 30)</param>
    /// <returns>Station availability matrix</returns>
    [HttpGet("{id}/availability")]
    [SwaggerOperation(
        Summary = "Get Station Availability",
        Description = "Gets hourly availability data for a charging station within a date range (Backoffice and StationOperator access required)"
    )]
    [SwaggerResponse(200, "Availability data retrieved successfully", typeof(ApiResponse<StationAvailabilityResponse>))]
    [SwaggerResponse(404, "Charging station not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<StationAvailabilityResponse>>> GetStationAvailability(
        string id, 
        [FromQuery] string start, 
        [FromQuery] int days = 7)
    {
        try
        {
            // Validate parameters
            if (!DateTime.TryParse(start, out var startDate))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid start date format. Use YYYY-MM-DD"
                });
            }

            if (days < 1 || days > 30)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Days must be between 1 and 30"
                });
            }

            var availability = await _availabilityService.GetStationAvailabilityAsync(id, startDate, days);
            if (availability == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Charging station not found"
                });
            }

            return Ok(new ApiResponse<StationAvailabilityResponse>
            {
                Success = true,
                Message = "Availability data retrieved successfully",
                Data = availability
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
    /// Create or update schedule overrides for a station
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Schedule override data</param>
    /// <returns>Success response</returns>
    [HttpPost("{id}/schedule/overrides")]
    [SwaggerOperation(
        Summary = "Create Schedule Override",
        Description = "Creates or updates schedule overrides for a charging station (Backoffice and StationOperator access required)"
    )]
    [SwaggerResponse(200, "Schedule override created successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid request or conflicts with existing bookings", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Charging station not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> CreateScheduleOverride(
        string id, 
        [FromBody] CreateScheduleOverrideRequest request)
    {
        try
        {
            // Validate station exists
            var station = await _stationRepository.GetByIdAsync(id);
            if (station == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Charging station not found"
                });
            }

            // Validate request
            if (!DateTime.TryParse(request.Date, out var date))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid date format. Use YYYY-MM-DD"
                });
            }

            // Check for conflicts with existing approved bookings
            var overrideData = new StationScheduleOverride
            {
                StationId = id,
                Date = date,
                Closed = request.Closed,
                OpenTime = request.OpenTime,
                CloseTime = request.CloseTime,
                MaintenanceWindows = request.MaintenanceWindows ?? new List<MaintenanceWindow>(),
                CreatedBy = User.Identity?.Name ?? "Unknown"
            };

            var conflicts = await _availabilityService.ValidateOverrideConflictsAsync(id, overrideData);
            if (conflicts.Any())
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Cannot apply override due to conflicts with existing approved bookings",
                    Data = conflicts
                });
            }

            // Create or update the override
            await _overrideRepository.UpsertOverrideAsync(overrideData);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Schedule override created successfully"
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
    /// Get schedule overrides for a station within a date range
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="start">Start date (YYYY-MM-DD)</param>
    /// <param name="days">Number of days (default: 7, max: 30)</param>
    /// <returns>Schedule overrides</returns>
    [HttpGet("{id}/schedule/overrides")]
    [SwaggerOperation(
        Summary = "Get Schedule Overrides",
        Description = "Gets schedule overrides for a charging station within a date range (Backoffice and StationOperator access required)"
    )]
    [SwaggerResponse(200, "Schedule overrides retrieved successfully", typeof(ApiResponse<List<StationScheduleOverride>>))]
    [SwaggerResponse(400, "Invalid request parameters", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<List<StationScheduleOverride>>>> GetScheduleOverrides(
        string id, 
        [FromQuery] string start, 
        [FromQuery] int days = 7)
    {
        try
        {
            // Validate parameters
            if (!DateTime.TryParse(start, out var startDate))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Invalid start date format. Use YYYY-MM-DD"
                });
            }

            if (days < 1 || days > 30)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Days must be between 1 and 30"
                });
            }

            var endDate = startDate.AddDays(days);
            var overrides = await _overrideRepository.GetOverridesForStationAsync(id, startDate, endDate);

            return Ok(new ApiResponse<List<StationScheduleOverride>>
            {
                Success = true,
                Message = "Schedule overrides retrieved successfully",
                Data = overrides
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
}

/// <summary>
/// Request model for creating schedule overrides
/// </summary>
public class CreateScheduleOverrideRequest
{
    /// <summary>
    /// Date for the override (YYYY-MM-DD format)
    /// </summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>
    /// Whether the station is closed for this entire day
    /// </summary>
    public bool Closed { get; set; } = false;

    /// <summary>
    /// Special opening time for this date (optional)
    /// </summary>
    public TimeSpan? OpenTime { get; set; }

    /// <summary>
    /// Special closing time for this date (optional)
    /// </summary>
    public TimeSpan? CloseTime { get; set; }

    /// <summary>
    /// Maintenance windows for this date (optional)
    /// </summary>
    public List<MaintenanceWindow>? MaintenanceWindows { get; set; }
}

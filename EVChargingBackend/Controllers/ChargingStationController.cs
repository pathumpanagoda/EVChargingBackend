/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Charging Station management controller
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Services;
using EVChargingBackend.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EVChargingBackend.Controllers;

/// <summary>
/// Controller for charging station management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Charging Station management endpoints")]
public class ChargingStationController : ControllerBase
{
    private readonly ChargingStationService _stationService;

    /// <summary>
    /// Initializes a new instance of the ChargingStationController
    /// </summary>
    /// <param name="stationService">Charging station service</param>
    public ChargingStationController(ChargingStationService stationService)
    {
        _stationService = stationService;
    }

    /// <summary>
    /// Creates a new charging station (Backoffice only)
    /// </summary>
    /// <param name="request">Station creation request</param>
    /// <returns>Created charging station</returns>
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    [SwaggerOperation(
        Summary = "Create Charging Station",
        Description = "Creates a new charging station (Backoffice access required)"
    )]
    [SwaggerResponse(201, "Charging station created successfully", typeof(ApiResponse<ChargingStation>))]
    [SwaggerResponse(400, "Validation error", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ChargingStation>>> CreateStation([FromBody] ChargingStationRequest request)
    {
        try
        {
            var station = await _stationService.CreateStationAsync(request);
            return CreatedAtAction(nameof(GetStation), new { id = station.Id }, new ApiResponse<ChargingStation>
            {
                Success = true,
                Message = "Charging station created successfully",
                Data = station
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
    /// Gets a charging station by ID
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <returns>Charging station if found</returns>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get Charging Station",
        Description = "Gets a charging station by ID"
    )]
    [SwaggerResponse(200, "Charging station found", typeof(ApiResponse<ChargingStation>))]
    [SwaggerResponse(404, "Charging station not found", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ChargingStation>>> GetStation(string id)
    {
        var station = await _stationService.GetStationAsync(id);
        if (station == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "Charging station not found"
            });
        }

        return Ok(new ApiResponse<ChargingStation>
        {
            Success = true,
            Message = "Charging station found",
            Data = station
        });
    }

    /// <summary>
    /// Gets active charging stations with optional filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="type">Station type filter (AC/DC)</param>
    /// <returns>Paginated charging stations</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "List Charging Stations",
        Description = "Gets paginated list of active charging stations with optional filtering"
    )]
    [SwaggerResponse(200, "Charging stations retrieved successfully", typeof(ApiResponse<PaginatedResponse<ChargingStation>>))]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ChargingStation>>>> GetStations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? type = null)
    {
        var stations = await _stationService.GetStationsAsync(page, pageSize, type);
        return Ok(new ApiResponse<PaginatedResponse<ChargingStation>>
        {
            Success = true,
            Message = "Charging stations retrieved successfully",
            Data = stations
        });
    }

    /// <summary>
    /// Gets nearby charging stations within a specified distance
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxDistanceKm">Maximum distance in kilometers</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Collection of nearby charging stations</returns>
    [HttpGet("nearby")]
    [SwaggerOperation(
        Summary = "Get Nearby Charging Stations",
        Description = "Gets charging stations within a specified distance from given coordinates"
    )]
    [SwaggerResponse(200, "Nearby charging stations retrieved successfully", typeof(ApiResponse<IEnumerable<ChargingStation>>))]
    [SwaggerResponse(400, "Invalid coordinates", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<IEnumerable<ChargingStation>>>> GetNearbyStations(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double maxDistanceKm = 10.0,
        [FromQuery] int limit = 20)
    {
        try
        {
            var stations = await _stationService.GetNearbyStationsAsync(latitude, longitude, maxDistanceKm, limit);
            return Ok(new ApiResponse<IEnumerable<ChargingStation>>
            {
                Success = true,
                Message = "Nearby charging stations retrieved successfully",
                Data = stations
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
    /// Updates a charging station
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Station update request</param>
    /// <returns>Updated charging station if found</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    [SwaggerOperation(
        Summary = "Update Charging Station",
        Description = "Updates an existing charging station (Backoffice or StationOperator access required)"
    )]
    [SwaggerResponse(200, "Charging station updated successfully", typeof(ApiResponse<ChargingStation>))]
    [SwaggerResponse(400, "Validation error", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Charging station not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ChargingStation>>> UpdateStation(string id, [FromBody] ChargingStationRequest request)
    {
        try
        {
            var station = await _stationService.UpdateStationAsync(id, request);
            if (station == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Charging station not found"
                });
            }

            return Ok(new ApiResponse<ChargingStation>
            {
                Success = true,
                Message = "Charging station updated successfully",
                Data = station
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
    /// Updates the daily schedule for a charging station
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <param name="request">Schedule update request</param>
    /// <returns>Updated charging station if found</returns>
    [HttpPut("{id}/schedule")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    [SwaggerOperation(
        Summary = "Update Station Schedule",
        Description = "Updates the daily schedule for a charging station (Backoffice or StationOperator access required)"
    )]
    [SwaggerResponse(200, "Station schedule updated successfully", typeof(ApiResponse<ChargingStation>))]
    [SwaggerResponse(400, "Validation error", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Charging station not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<ChargingStation>>> UpdateSchedule(string id, [FromBody] StationScheduleRequest request)
    {
        try
        {
            var station = await _stationService.UpdateScheduleAsync(id, request);
            if (station == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Charging station not found"
                });
            }

            return Ok(new ApiResponse<ChargingStation>
            {
                Success = true,
                Message = "Station schedule updated successfully",
                Data = station
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
    /// Gets station availability for a specific date
    /// </summary>
    /// <param name="stationId">Station ID</param>
    /// <param name="date">Optional date to check availability (defaults to current time)</param>
    /// <returns>Station availability information</returns>
    [HttpGet("availability")]
    [SwaggerOperation(
        Summary = "Get Station Availability",
        Description = "Gets availability information for a charging station at a specific date"
    )]
    [SwaggerResponse(200, "Availability information retrieved successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(400, "Invalid station ID", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> GetAvailability([FromQuery] string stationId, [FromQuery] DateTime? date)
    {
        if (string.IsNullOrWhiteSpace(stationId))
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "stationId is required"
            });
        }

        var dt = date ?? DateTime.UtcNow;

        try
        {
            // TODO: Replace stubs with real queries
            // Fetch station, schedule, and existing bookings at/around 'dt'
            var hasCapacity = true; // stub
            var nextSlots = new[]
            {
                new { start = dt.AddMinutes(15), end = dt.AddMinutes(45) },
                new { start = dt.AddMinutes(60), end = dt.AddMinutes(90) }
            };

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Availability information retrieved successfully",
                Data = new
                {
                    stationId,
                    date = dt,
                    isAvailable = hasCapacity,
                    nextSlots
                }
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
    /// Deactivates a charging station
    /// </summary>
    /// <param name="id">Station ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Backoffice")]
    [SwaggerOperation(
        Summary = "Deactivate Charging Station",
        Description = "Deactivates a charging station (Backoffice access required). Cannot deactivate if active future bookings exist."
    )]
    [SwaggerResponse(200, "Charging station deactivated successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "Charging station not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(409, "Cannot deactivate station with active future bookings", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateStation(string id)
    {
        try
        {
            var result = await _stationService.DeactivateStationAsync(id);
            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Charging station not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Charging station deactivated successfully"
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}

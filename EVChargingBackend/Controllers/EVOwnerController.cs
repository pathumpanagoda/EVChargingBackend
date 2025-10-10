/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: EV Owner management controller
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Services;
using EVChargingBackend.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace EVChargingBackend.Controllers;


/// Controller for EV owner management operations

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("EV Owner management endpoints")]
public class EVOwnerController : ControllerBase
{
    private readonly EVOwnerService _evOwnerService;

    
    /// Initializes a new instance of the EVOwnerController
    
    /// <param name="evOwnerService">EV owner service</param>
    public EVOwnerController(EVOwnerService evOwnerService)
    {
        _evOwnerService = evOwnerService;
    }

    
    /// Creates a new EV owner (Backoffice only)
    
    /// <param name="request">EV owner creation request</param>
    /// <returns>Created EV owner</returns>
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    [SwaggerOperation(
        Summary = "Create EV Owner",
        Description = "Creates a new EV owner (Backoffice access required)"
    )]
    [SwaggerResponse(201, "EV owner created successfully", typeof(ApiResponse<EVOwner>))]
    [SwaggerResponse(400, "Validation error or NIC/email already exists", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<EVOwner>>> CreateEVOwner([FromBody] EVOwnerRequest request)
    {
        try
        {
            var evOwner = await _evOwnerService.CreateEVOwnerAsync(request);
            return CreatedAtAction(nameof(GetEVOwner), new { nic = evOwner.NIC }, new ApiResponse<EVOwner>
            {
                Success = true,
                Message = "EV owner created successfully",
                Data = evOwner
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

    
    /// Gets an EV owner by NIC
    
    /// <param name="nic">NIC</param>
    /// <returns>EV owner if found</returns>
    [HttpGet("{nic}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get EV Owner",
        Description = "Gets an EV owner by NIC (owner can access own data, Backoffice can access any)"
    )]
    [SwaggerResponse(200, "EV owner found", typeof(ApiResponse<EVOwner>))]
    [SwaggerResponse(404, "EV owner not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<EVOwner>>> GetEVOwner(string nic)
    {
        // Check authorization
        if (!IsAuthorizedForEVOwner(nic))
        {
            return Forbid();
        }

        var evOwner = await _evOwnerService.GetEVOwnerAsync(nic);
        if (evOwner == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "EV owner not found"
            });
        }

        return Ok(new ApiResponse<EVOwner>
        {
            Success = true,
            Message = "EV owner found",
            Data = evOwner
        });
    }

    
    /// Gets paginated EV owners with optional search
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term</param>
    /// <returns>Paginated EV owners</returns>
    [HttpGet]
    [Authorize(Roles = "Backoffice")]
    [SwaggerOperation(
        Summary = "List EV Owners",
        Description = "Gets paginated list of EV owners with optional search (Backoffice access required)"
    )]
    [SwaggerResponse(200, "EV owners retrieved successfully", typeof(ApiResponse<PaginatedResponse<EVOwner>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EVOwner>>>> GetEVOwners(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var evOwners = await _evOwnerService.GetEVOwnersAsync(page, pageSize, search);
        return Ok(new ApiResponse<PaginatedResponse<EVOwner>>
        {
            Success = true,
            Message = "EV owners retrieved successfully",
            Data = evOwners
        });
    }

    
    /// Updates an EV owner
    
    /// <param name="nic">NIC</param>
    /// <param name="request">EV owner update request</param>
    /// <returns>Updated EV owner if found</returns>
    [HttpPut("{nic}")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Update EV Owner",
        Description = "Updates an existing EV owner (owner can update own data, Backoffice can update any)"
    )]
    [SwaggerResponse(200, "EV owner updated successfully", typeof(ApiResponse<EVOwner>))]
    [SwaggerResponse(400, "Validation error or NIC/email already exists", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "EV owner not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<EVOwner>>> UpdateEVOwner(string nic, [FromBody] EVOwnerRequest request)
    {
        // Check authorization
        if (!IsAuthorizedForEVOwner(nic))
        {
            return Forbid();
        }

        try
        {
            var evOwner = await _evOwnerService.UpdateEVOwnerAsync(nic, request);
            if (evOwner == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "EV owner not found"
                });
            }

            return Ok(new ApiResponse<EVOwner>
            {
                Success = true,
                Message = "EV owner updated successfully",
                Data = evOwner
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

    
    /// Deactivates an EV owner
    
    /// <param name="nic">NIC</param>
    /// <returns>Success status</returns>
    [HttpPost("{nic}/deactivate")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Deactivate EV Owner",
        Description = "Deactivates an EV owner (owner can deactivate own account, Backoffice can deactivate any)"
    )]
    [SwaggerResponse(200, "EV owner deactivated successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "EV owner not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - insufficient permissions", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateEVOwner(string nic)
    {
        // Check authorization
        if (!IsAuthorizedForEVOwner(nic))
        {
            return Forbid();
        }

        var result = await _evOwnerService.DeactivateEVOwnerAsync(nic);
        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "EV owner not found"
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "EV owner deactivated successfully"
        });
    }

    
    /// Reactivates an EV owner (Backoffice only)
    
    /// <param name="nic">NIC</param>
    /// <returns>Success status</returns>
    [HttpPost("{nic}/reactivate")]
    [Authorize(Roles = "Backoffice")]
    [SwaggerOperation(
        Summary = "Reactivate EV Owner",
        Description = "Reactivates an EV owner (Backoffice access required)"
    )]
    [SwaggerResponse(200, "EV owner reactivated successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "EV owner not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> ReactivateEVOwner(string nic)
    {
        var result = await _evOwnerService.ReactivateEVOwnerAsync(nic);
        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "EV owner not found"
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "EV owner reactivated successfully"
        });
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

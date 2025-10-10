/*
 * Author: EV Charging System
 * Date: 2025-10-02
 * Purpose: User management controller for system users (Backoffice only)
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace EVChargingBackend.Controllers;


/// Controller for user management operations (Backoffice only)

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Backoffice")]
[SwaggerTag("User management endpoints for system users (Backoffice access required)")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;

    
    /// Initializes a new instance of the UserController
    
    /// <param name="userService">User service</param>
    public UserController(UserService userService)
    {
        _userService = userService;
    }

    
    /// Creates a new system user
    
    /// <param name="request">User creation request</param>
    /// <returns>Created user</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create User",
        Description = "Creates a new system user (Backoffice or StationOperator)"
    )]
    [SwaggerResponse(201, "User created successfully", typeof(ApiResponse<User>))]
    [SwaggerResponse(400, "Validation error or username already exists", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<User>>> CreateUser([FromBody] UserRequest request)
    {
        try
        {
            var user = await _userService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<User>
            {
                Success = true,
                Message = "User created successfully",
                Data = user
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

    
    /// Gets a user by ID
    
    /// <param name="id">User ID</param>
    /// <returns>User if found</returns>
    [HttpGet("{id}")]
    [SwaggerOperation(
        Summary = "Get User",
        Description = "Gets a system user by ID"
    )]
    [SwaggerResponse(200, "User found", typeof(ApiResponse<User>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<User>>> GetUser(string id)
    {
        var user = await _userService.GetUserAsync(id);
        if (user == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            });
        }

        return Ok(new ApiResponse<User>
        {
            Success = true,
            Message = "User found",
            Data = user
        });
    }

    
    /// Gets paginated users with optional filtering
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term</param>
    /// <param name="role">Role filter</param>
    /// <param name="isActive">Active status filter</param>
    /// <returns>Paginated users</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "List Users",
        Description = "Gets paginated list of system users with optional filtering"
    )]
    [SwaggerResponse(200, "Users retrieved successfully", typeof(ApiResponse<PaginatedResponse<User>>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<User>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null)
    {
        var users = await _userService.GetUsersAsync(page, pageSize, search, role, isActive);
        return Ok(new ApiResponse<PaginatedResponse<User>>
        {
            Success = true,
            Message = "Users retrieved successfully",
            Data = users
        });
    }

    
    /// Updates a user
    
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Updated user if found</returns>
    [HttpPut("{id}")]
    [SwaggerOperation(
        Summary = "Update User",
        Description = "Updates an existing system user"
    )]
    [SwaggerResponse(200, "User updated successfully", typeof(ApiResponse<User>))]
    [SwaggerResponse(400, "Validation error or username already exists", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<User>>> UpdateUser(string id, [FromBody] UserUpdateRequest request)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, request);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User updated successfully",
                Data = user
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

    
    /// Deletes a user (soft delete)
    
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [SwaggerOperation(
        Summary = "Delete User",
        Description = "Deletes a system user (soft delete by setting inactive)"
    )]
    [SwaggerResponse(200, "User deleted successfully", typeof(ApiResponse<object>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Unauthorized", typeof(ApiResponse<object>))]
    [SwaggerResponse(403, "Forbidden - Backoffice access required", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User deleted successfully"
        });
    }
}

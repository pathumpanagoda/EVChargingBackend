/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Authentication controller for login, registration, and token refresh
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace EVChargingBackend.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Authentication endpoints for user login, registration, and token management")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    /// <summary>
    /// Initializes a new instance of the AuthController
    /// </summary>
    /// <param name="authService">Authentication service</param>
    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "User Login",
        Description = "Authenticates a user (system user or EV owner) and returns a JWT token"
    )]
    [SwaggerResponse(200, "Login successful", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(400, "Validation error", typeof(ApiResponse<object>))]
    [SwaggerResponse(401, "Invalid credentials", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var authResponse = await _authService.LoginAsync(request);
            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Login successful",
                Data = authResponse
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
    /// Registers a new EV owner
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>Authentication response with JWT token</returns>
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "EV Owner Registration",
        Description = "Registers a new EV owner and returns a JWT token"
    )]
    [SwaggerResponse(200, "Registration successful", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(400, "Validation error or user already exists", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var authResponse = await _authService.RegisterAsync(request);
            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Registration successful",
                Data = authResponse
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
    /// Refreshes a JWT token
    /// </summary>
    /// <param name="request">Token refresh request</param>
    /// <returns>New authentication response with refreshed token</returns>
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Refresh Token",
        Description = "Refreshes an existing JWT token and returns a new one"
    )]
    [SwaggerResponse(200, "Token refreshed successfully", typeof(ApiResponse<AuthResponse>))]
    [SwaggerResponse(401, "Invalid token", typeof(ApiResponse<object>))]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] TokenRefreshRequest request)
    {
        try
        {
            var authResponse = await _authService.RefreshTokenAsync(request.Token);
            return Ok(new ApiResponse<AuthResponse>
            {
                Success = true,
                Message = "Token refreshed successfully",
                Data = authResponse
            });
        }
        catch (Exception ex)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }
}

/// <summary>
/// Token refresh request DTO
/// </summary>
public record TokenRefreshRequest
{
    /// <summary>
    /// Current JWT token
    /// </summary>
    public string Token { get; init; } = string.Empty;
}

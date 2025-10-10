/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Authentication service for user login and registration
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Helpers;
using EVChargingBackend.Models;
using EVChargingBackend.Repositories;
using EVChargingBackend.Validators;
using FluentValidation;

namespace EVChargingBackend.Services;


/// Service for handling authentication operations

public class AuthService
{
    private readonly UserRepository _userRepository;
    private readonly EVOwnerRepository _evOwnerRepository;
    private readonly JwtHelper _jwtHelper;
    private readonly LoginRequestValidator _loginValidator;
    private readonly RegisterRequestValidator _registerValidator;

    
    /// Initializes a new instance of the AuthService
    
    /// <param name="userRepository">User repository</param>
    /// <param name="evOwnerRepository">EV owner repository</param>
    /// <param name="jwtHelper">JWT helper</param>
    /// <param name="loginValidator">Login request validator</param>
    /// <param name="registerValidator">Register request validator</param>
    public AuthService(
        UserRepository userRepository,
        EVOwnerRepository evOwnerRepository,
        JwtHelper jwtHelper,
        LoginRequestValidator loginValidator,
        RegisterRequestValidator registerValidator)
    {
        _userRepository = userRepository;
        _evOwnerRepository = evOwnerRepository;
        _jwtHelper = jwtHelper;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    
    /// Authenticates a user and returns a JWT token
    
    /// <param name="request">Login request</param>
    /// <returns>Authentication response with token</returns>
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Validate request
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Try to find user by username first
        var user = await _userRepository.GetByUsernameAsync(request.Username);
        if (user != null)
        {
            return await AuthenticateUserAsync(user, request.Password);
        }

        // Try to find EV owner by NIC
        var evOwner = await _evOwnerRepository.GetByIdAsync(request.Username);
        if (evOwner != null)
        {
            return await AuthenticateEVOwnerAsync(evOwner, request.Password);
        }

        throw new UnauthorizedAccessException("Invalid username or password");
    }

    
    /// Registers a new EV owner
    
    /// <param name="request">Registration request</param>
    /// <returns>Authentication response with token</returns>
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate request
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Check if EV owner already exists
        if (await _evOwnerRepository.ExistsAsync(e => e.NIC == request.NIC))
        {
            throw new ArgumentException("EV owner with this NIC already exists");
        }

        if (await _evOwnerRepository.ExistsAsync(e => e.Email == request.Email))
        {
            throw new ArgumentException("EV owner with this email already exists");
        }

        // Create new EV owner
        var evOwner = new EVOwner
        {
            NIC = request.NIC,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _evOwnerRepository.CreateAsync(evOwner);

        // Generate token
        var (token, expiresAt) = _jwtHelper.GenerateTokenForEVOwner(evOwner.NIC, evOwner.Name);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Role = "EVOwner",
            UserId = evOwner.NIC,
            Nic = evOwner.NIC
        };
    }

    
    /// Refreshes a JWT token
    
    /// <param name="token">Current token</param>
    /// <returns>New authentication response</returns>
    public async Task<AuthResponse> RefreshTokenAsync(string token)
    {
        var principal = _jwtHelper.ValidateToken(token);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
        {
            throw new UnauthorizedAccessException("Invalid token claims");
        }

        // Verify user still exists and is active
        if (role == "EVOwner")
        {
            var evOwner = await _evOwnerRepository.GetByIdAsync(userId);
            if (evOwner == null || !evOwner.IsActive)
            {
                throw new UnauthorizedAccessException("User account is inactive or deleted");
            }

            var (newToken, expiresAt) = _jwtHelper.GenerateTokenForEVOwner(evOwner.NIC, evOwner.Name);
            return new AuthResponse
            {
                Token = newToken,
                ExpiresAt = expiresAt,
                Role = role,
                UserId = userId
            };
        }
        else
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User account is inactive or deleted");
            }

            var (newToken, expiresAt) = _jwtHelper.GenerateTokenForUser(user.Id, user.Username, user.Role);
            return new AuthResponse
            {
                Token = newToken,
                ExpiresAt = expiresAt,
                Role = role,
                UserId = userId
            };
        }
    }

    
    /// Authenticates a system user
    
    /// <param name="user">User entity</param>
    /// <param name="password">Plain text password</param>
    /// <returns>Authentication response</returns>
    private async Task<AuthResponse> AuthenticateUserAsync(User user, string password)
    {
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        var (token, expiresAt) = _jwtHelper.GenerateTokenForUser(user.Id, user.Username, user.Role);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Role = user.Role,
            UserId = user.Id
        };
    }

    
    /// Authenticates an EV owner
    
    /// <param name="evOwner">EV owner entity</param>
    /// <param name="password">Plain text password</param>
    /// <returns>Authentication response</returns>
    private async Task<AuthResponse> AuthenticateEVOwnerAsync(EVOwner evOwner, string password)
    {
        if (!evOwner.IsActive)
        {
            throw new UnauthorizedAccessException("EV owner account is inactive");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, evOwner.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username or password");
        }

        var (token, expiresAt) = _jwtHelper.GenerateTokenForEVOwner(evOwner.NIC, evOwner.Name);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Role = "EVOwner",
            UserId = evOwner.NIC,
            Nic = evOwner.NIC
        };
    }
}

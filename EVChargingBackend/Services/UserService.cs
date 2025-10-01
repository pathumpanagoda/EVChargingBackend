/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: User management service for system users
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Helpers;
using EVChargingBackend.Models;
using EVChargingBackend.Repositories;
using EVChargingBackend.Validators;
using FluentValidation;
using System.Linq.Expressions;

namespace EVChargingBackend.Services;

/// <summary>
/// Service for managing system users
/// </summary>
public class UserService
{
    private readonly UserRepository _userRepository;
    private readonly UserRequestValidator _userValidator;

    /// <summary>
    /// Initializes a new instance of the UserService
    /// </summary>
    /// <param name="userRepository">User repository</param>
    /// <param name="userValidator">User request validator</param>
    public UserService(UserRepository userRepository, UserRequestValidator userValidator)
    {
        _userRepository = userRepository;
        _userValidator = userValidator;
    }

    /// <summary>
    /// Creates a new system user
    /// </summary>
    /// <param name="request">User creation request</param>
    /// <returns>Created user</returns>
    public async Task<User> CreateUserAsync(UserRequest request)
    {
        // Validate request
        var validationResult = await _userValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Check if username already exists
        if (await _userRepository.ExistsAsync(u => u.Username == request.Username))
        {
            throw new ArgumentException("Username already exists");
        }

        // Create new user
        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return await _userRepository.CreateAsync(user);
    }

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User if found, null otherwise</returns>
    public async Task<User?> GetUserAsync(string id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets paginated users with optional filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term</param>
    /// <param name="role">Role filter</param>
    /// <param name="isActive">Active status filter</param>
    /// <returns>Paginated users</returns>
    public async Task<PaginatedResponse<User>> GetUsersAsync(int page, int pageSize, string? search = null, string? role = null, bool? isActive = null)
    {
        // Build filter expression based on parameters
        Expression<Func<User, bool>>? filter = null;
        
        if (!string.IsNullOrEmpty(search) || !string.IsNullOrEmpty(role) || isActive.HasValue)
        {
            filter = u => 
                (string.IsNullOrEmpty(search) || u.Username.Contains(search)) &&
                (string.IsNullOrEmpty(role) || u.Role == role) &&
                (!isActive.HasValue || u.IsActive == isActive.Value);
        }

        var (users, totalCount) = await _userRepository.GetPaginatedAsync(page, pageSize, search ?? string.Empty, role ?? string.Empty, filter);

        return new PaginatedResponse<User>
        {
            Items = users.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Updates a user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">User update request</param>
    /// <returns>Updated user if found, null otherwise</returns>
    public async Task<User?> UpdateUserAsync(string id, UserUpdateRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return null;
        }

        // Update fields
        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            if (await _userRepository.ExistsAsync(u => u.Username == request.Username))
            {
                throw new ArgumentException("Username already exists");
            }
            user.Username = request.Username;
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            user.Role = request.Role;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        return await _userRepository.UpdateAsync(id, user);
    }

    /// <summary>
    /// Deletes a user (soft delete by setting inactive)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>True if deleted, false if not found</returns>
    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        // Soft delete by setting inactive
        user.IsActive = false;
        await _userRepository.UpdateAsync(id, user);
        return true;
    }

    /// <summary>
    /// Creates a default admin user if none exists
    /// </summary>
    /// <returns>True if admin was created, false if already exists</returns>
    public async Task<bool> CreateDefaultAdminAsync()
    {
        if (await _userRepository.ExistsAsync(u => u.Role == "Backoffice"))
        {
            return false;
        }

        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Backoffice",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(admin);
        return true;
    }
}

/// <summary>
/// User request DTO for creation
/// </summary>
public record UserRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; init; } = string.Empty;
}

/// <summary>
/// User update request DTO
/// </summary>
public record UserUpdateRequest
{
    /// <summary>
    /// Username
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Password
    /// </summary>
    public string? Password { get; init; }

    /// <summary>
    /// User role
    /// </summary>
    public string? Role { get; init; }

    /// <summary>
    /// Active status
    /// </summary>
    public bool? IsActive { get; init; }
}

/// <summary>
/// Validator for UserRequest DTO
/// </summary>
public class UserRequestValidator : AbstractValidator<UserRequest>
{
    /// <summary>
    /// Initializes a new instance of the UserRequestValidator
    /// </summary>
    public UserRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(role => role == "Backoffice" || role == "StationOperator")
            .WithMessage("Role must be either Backoffice or StationOperator");
    }
}

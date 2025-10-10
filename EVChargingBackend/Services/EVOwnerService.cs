/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: EV Owner management service
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Repositories;
using EVChargingBackend.Validators;
using FluentValidation;

namespace EVChargingBackend.Services;


/// Service for managing EV owners

public class EVOwnerService
{
    private readonly EVOwnerRepository _evOwnerRepository;
    private readonly EVOwnerRequestValidator _evOwnerValidator;

    
    /// Initializes a new instance of the EVOwnerService
    
    /// <param name="evOwnerRepository">EV owner repository</param>
    /// <param name="evOwnerValidator">EV owner request validator</param>
    public EVOwnerService(EVOwnerRepository evOwnerRepository, EVOwnerRequestValidator evOwnerValidator)
    {
        _evOwnerRepository = evOwnerRepository;
        _evOwnerValidator = evOwnerValidator;
    }

    
    /// Creates a new EV owner (by Backoffice)
    
    /// <param name="request">EV owner creation request</param>
    /// <returns>Created EV owner</returns>
    public async Task<EVOwner> CreateEVOwnerAsync(EVOwnerRequest request)
    {
        // Validate request
        var validationResult = await _evOwnerValidator.ValidateAsync(request);
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password ?? "DefaultPassword123!"),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _evOwnerRepository.CreateAsync(evOwner);
    }

    
    /// Gets an EV owner by NIC
    
    /// <param name="nic">NIC</param>
    /// <returns>EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> GetEVOwnerAsync(string nic)
    {
        return await _evOwnerRepository.GetByIdAsync(nic);
    }

    
    /// Gets paginated EV owners with optional search
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="search">Search term</param>
    /// <returns>Paginated EV owners</returns>
    public async Task<PaginatedResponse<EVOwner>> GetEVOwnersAsync(int page, int pageSize, string? search = null)
    {
        var (evOwners, totalCount) = await _evOwnerRepository.GetPaginatedAsync(page, pageSize, search);

        return new PaginatedResponse<EVOwner>
        {
            Items = evOwners.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    
    /// Updates an EV owner
    
    /// <param name="nic">NIC</param>
    /// <param name="request">EV owner update request</param>
    /// <returns>Updated EV owner if found, null otherwise</returns>
    public async Task<EVOwner?> UpdateEVOwnerAsync(string nic, EVOwnerRequest request)
    {
        var evOwner = await _evOwnerRepository.GetByIdAsync(nic);
        if (evOwner == null)
        {
            return null;
        }

        // Check for conflicts if updating NIC or email
        if (request.NIC != nic && await _evOwnerRepository.ExistsAsync(e => e.NIC == request.NIC))
        {
            throw new ArgumentException("EV owner with this NIC already exists");
        }

        if (request.Email != evOwner.Email && await _evOwnerRepository.ExistsAsync(e => e.Email == request.Email))
        {
            throw new ArgumentException("EV owner with this email already exists");
        }

        // Update fields
        evOwner.NIC = request.NIC;
        evOwner.Name = request.Name;
        evOwner.Email = request.Email;
        evOwner.Phone = request.Phone;
        evOwner.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Password))
        {
            evOwner.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        return await _evOwnerRepository.UpdateAsync(nic, evOwner);
    }

    
    /// Deactivates an EV owner
    
    /// <param name="nic">NIC</param>
    /// <returns>True if deactivated, false if not found</returns>
    public async Task<bool> DeactivateEVOwnerAsync(string nic)
    {
        var evOwner = await _evOwnerRepository.GetByIdAsync(nic);
        if (evOwner == null)
        {
            return false;
        }

        evOwner.IsActive = false;
        evOwner.UpdatedAt = DateTime.UtcNow;
        await _evOwnerRepository.UpdateAsync(nic, evOwner);
        return true;
    }

    
    /// Reactivates an EV owner (Backoffice only)
    
    /// <param name="nic">NIC</param>
    /// <returns>True if reactivated, false if not found</returns>
    public async Task<bool> ReactivateEVOwnerAsync(string nic)
    {
        var evOwner = await _evOwnerRepository.GetByIdAsync(nic);
        if (evOwner == null)
        {
            return false;
        }

        evOwner.IsActive = true;
        evOwner.UpdatedAt = DateTime.UtcNow;
        await _evOwnerRepository.UpdateAsync(nic, evOwner);
        return true;
    }
}

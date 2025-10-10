/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Charging Station management service
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Queries;
using EVChargingBackend.Repositories;
using EVChargingBackend.Validators;
using FluentValidation;

namespace EVChargingBackend.Services;


/// Service for managing charging stations

public class ChargingStationService
{
    private readonly ChargingStationRepository _stationRepository;
    private readonly BookingQueries _bookingQueries;
    private readonly ChargingStationRequestValidator _stationValidator;
    private readonly StationScheduleRequestValidator _scheduleValidator;

    
    /// Initializes a new instance of the ChargingStationService
    
    /// <param name="stationRepository">Charging station repository</param>
    /// <param name="bookingQueries">Booking queries</param>
    /// <param name="stationValidator">Station request validator</param>
    /// <param name="scheduleValidator">Schedule request validator</param>
    public ChargingStationService(
        ChargingStationRepository stationRepository,
        BookingQueries bookingQueries,
        ChargingStationRequestValidator stationValidator,
        StationScheduleRequestValidator scheduleValidator)
    {
        _stationRepository = stationRepository;
        _bookingQueries = bookingQueries;
        _stationValidator = stationValidator;
        _scheduleValidator = scheduleValidator;
    }

    
    /// Creates a new charging station
    
    /// <param name="request">Station creation request</param>
    /// <returns>Created charging station</returns>
    public async Task<ChargingStation> CreateStationAsync(ChargingStationRequest request)
    {
        // Validate request
        var validationResult = await _stationValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Create new charging station
        var station = new ChargingStation
        {
            Name = request.Name,
            Location = new Location
            {
                Latitude = request.Location.Latitude,
                Longitude = request.Location.Longitude,
                Address = request.Location.Address
            },
            Type = request.Type,
            TotalSlots = request.TotalSlots,
            AvailableSlots = request.TotalSlots,
            Schedule = new List<DailySchedule>(),
            IsActive = true,
            OperatorId = request.OperatorId,
            CreatedAt = DateTime.UtcNow
        };

        return await _stationRepository.CreateAsync(station);
    }

    
    /// Gets a charging station by ID
    
    /// <param name="id">Station ID</param>
    /// <returns>Charging station if found, null otherwise</returns>
    public async Task<ChargingStation?> GetStationAsync(string id)
    {
        return await _stationRepository.GetByIdAsync(id);
    }

    
    /// Gets active charging stations with optional filtering
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="type">Station type filter</param>
    /// <returns>Paginated charging stations</returns>
    public async Task<PaginatedResponse<ChargingStation>> GetStationsAsync(int page, int pageSize, string? type = null)
    {
        // Show all stations (both active and inactive) - pass null for isActive to include all
        var (stations, totalCount) = await _stationRepository.GetPaginatedAsync(page, pageSize, type, null);

        return new PaginatedResponse<ChargingStation>
        {
            Items = stations.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    
    /// Gets nearby charging stations within a specified distance
    
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="maxDistanceKm">Maximum distance in kilometers</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Collection of nearby charging stations</returns>
    public async Task<IEnumerable<ChargingStation>> GetNearbyStationsAsync(double latitude, double longitude, double maxDistanceKm = 10.0, int limit = 20)
    {
        return await _stationRepository.GetNearbyStationsAsync(latitude, longitude, maxDistanceKm, limit);
    }

    
    /// Updates a charging station
    
    /// <param name="id">Station ID</param>
    /// <param name="request">Station update request</param>
    /// <returns>Updated charging station if found, null otherwise</returns>
    public async Task<ChargingStation?> UpdateStationAsync(string id, ChargingStationRequest request)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
        {
            return null;
        }

        // Validate request
        var validationResult = await _stationValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Update fields
        station.Name = request.Name;
        station.Location = new Location
        {
            Latitude = request.Location.Latitude,
            Longitude = request.Location.Longitude,
            Address = request.Location.Address
        };
        station.Type = request.Type;
        station.TotalSlots = request.TotalSlots;
        station.OperatorId = request.OperatorId;

        // Recalculate available slots
        station.AvailableSlots = station.TotalSlots;

        return await _stationRepository.UpdateAsync(id, station);
    }

    
    /// Updates the daily schedule for a charging station
    
    /// <param name="id">Station ID</param>
    /// <param name="request">Schedule update request</param>
    /// <returns>Updated charging station if found, null otherwise</returns>
    public async Task<ChargingStation?> UpdateScheduleAsync(string id, StationScheduleRequest request)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
        {
            return null;
        }

        // Validate request
        var validationResult = await _scheduleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Update schedule
        station.Schedule = request.Schedule.Select(s => new DailySchedule
        {
            Date = s.Date,
            Open = s.Open,
            Close = s.Close,
            SlotsAvailable = s.SlotsAvailable
        }).ToList();

        return await _stationRepository.UpdateAsync(id, station);
    }

    
    /// Deactivates a charging station
    
    /// <param name="id">Station ID</param>
    /// <returns>True if deactivated, false if not found or has active bookings</returns>
    public async Task<bool> DeactivateStationAsync(string id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
        {
            return false;
        }

        // Check if station has active future bookings
        if (await _bookingQueries.HasActiveFutureBookingsForStationAsync(id, DateTime.UtcNow))
        {
            throw new InvalidOperationException("Cannot deactivate station with active future bookings");
        }

        station.IsActive = false;
        await _stationRepository.UpdateAsync(id, station);
        return true;
    }

    
    /// Activates a charging station
    
    /// <param name="id">Station ID</param>
    /// <returns>True if activated, false if not found</returns>
    public async Task<bool> ActivateStationAsync(string id)
    {
        var station = await _stationRepository.GetByIdAsync(id);
        if (station == null)
        {
            return false;
        }

        station.IsActive = true;
        await _stationRepository.UpdateAsync(id, station);
        return true;
    }

    
    /// Gets charging stations by type
    
    /// <param name="type">Station type (AC/DC)</param>
    /// <returns>Collection of charging stations of the specified type</returns>
    public async Task<IEnumerable<ChargingStation>> GetStationsByTypeAsync(string type)
    {
        return await _stationRepository.GetByTypeAsync(type);
    }
}

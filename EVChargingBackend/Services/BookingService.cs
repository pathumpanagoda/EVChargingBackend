/*
 * Author: EV Charging System
 * Date: 2025-10-04
 * Purpose: Booking management service with business rules enforcement
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Helpers;
using EVChargingBackend.Models;
using EVChargingBackend.Queries;
using EVChargingBackend.Repositories;
using EVChargingBackend.Validators;
using FluentValidation;

namespace EVChargingBackend.Services;


/// Service for managing bookings with business rule enforcement

public class BookingService
{
    private readonly BookingRepository _bookingRepository;
    private readonly ChargingStationRepository _stationRepository;
    private readonly EVOwnerRepository _evOwnerRepository;
    private readonly BookingQueries _bookingQueries;
    private readonly QRCodeGenerator _qrGenerator;
    private readonly BookingRequestValidator _bookingValidator;
    private readonly BookingUpdateRequestValidator _updateValidator;
    private readonly BookingCompleteRequestValidator _completeValidator;

    
    /// Initializes a new instance of the BookingService
    
    /// <param name="bookingRepository">Booking repository</param>
    /// <param name="stationRepository">Charging station repository</param>
    /// <param name="evOwnerRepository">EV owner repository</param>
    /// <param name="bookingQueries">Booking queries</param>
    /// <param name="qrGenerator">QR code generator</param>
    /// <param name="bookingValidator">Booking request validator</param>
    /// <param name="updateValidator">Booking update validator</param>
    /// <param name="completeValidator">Booking complete validator</param>
    public BookingService(
        BookingRepository bookingRepository,
        ChargingStationRepository stationRepository,
        EVOwnerRepository evOwnerRepository,
        BookingQueries bookingQueries,
        QRCodeGenerator qrGenerator,
        BookingRequestValidator bookingValidator,
        BookingUpdateRequestValidator updateValidator,
        BookingCompleteRequestValidator completeValidator)
    {
        _bookingRepository = bookingRepository;
        _stationRepository = stationRepository;
        _evOwnerRepository = evOwnerRepository;
        _bookingQueries = bookingQueries;
        _qrGenerator = qrGenerator;
        _bookingValidator = bookingValidator;
        _updateValidator = updateValidator;
        _completeValidator = completeValidator;
    }

    
    /// Creates a new booking
    
    /// <param name="request">Booking creation request</param>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <returns>Created booking</returns>
    public async Task<Booking> CreateBookingAsync(BookingRequest request, string evOwnerNIC)
    {
        Console.WriteLine($"CreateBookingAsync called with StationId: {request.StationId}, ReservationDateTime: {request.ReservationDateTime}, EndDateTime: {request.EndDateTime}");
        
        // Validate request
        var validationResult = await _bookingValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            Console.WriteLine($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
            throw new ValidationException(validationResult.Errors);
        }

        // Verify EV owner exists and is active
        var evOwner = await _evOwnerRepository.GetByIdAsync(evOwnerNIC);
        if (evOwner == null || !evOwner.IsActive)
        {
            Console.WriteLine($"EV owner not found or inactive: {evOwnerNIC}");
            throw new ArgumentException("EV owner not found or inactive");
        }

        // Verify charging station exists and is active
        var station = await _stationRepository.GetByIdAsync(request.StationId);
        if (station == null || !station.IsActive)
        {
            Console.WriteLine($"Charging station not found or inactive: {request.StationId}");
            throw new ArgumentException("Charging station not found or inactive");
        }

        Console.WriteLine($"Station found: {station.Name}, OpenTime: {station.OpenTime}, CloseTime: {station.CloseTime}");

        // Normalize start time to hour boundary
        var normalizedStartTime = TimeNormalizationHelper.NormalizeToHour(request.ReservationDateTime);
        
        // Calculate end time (normalize to hour boundaries)
        var normalizedEndTime = TimeNormalizationHelper.CalculateEndTime(normalizedStartTime, request.EndDateTime);

        Console.WriteLine($"Normalized start time: {normalizedStartTime}, Normalized end time: {normalizedEndTime}");

        // Validate 7-day window
        if (!TimeNormalizationHelper.IsWithin7DayWindow(normalizedStartTime))
        {
            Console.WriteLine($"7-day window validation failed for: {normalizedStartTime}");
            throw new InvalidOperationException("Reservation must be within 7 days from booking date");
        }

        // Validate working hours for the entire time range
        if (!TimeNormalizationHelper.IsTimeRangeWithinWorkingHours(normalizedStartTime, normalizedEndTime, station.OpenTime, station.CloseTime))
        {
            Console.WriteLine($"Working hours validation failed. Start: {normalizedStartTime}, End: {normalizedEndTime}, Open: {station.OpenTime}, Close: {station.CloseTime}");
            throw new InvalidOperationException("Outside working hours");
        }

        // Generate hour keys for all occupied slots
        var occupiedHourKeys = TimeNormalizationHelper.GenerateOccupiedHourKeys(normalizedStartTime, normalizedEndTime);
        var startHourKey = occupiedHourKeys.FirstOrDefault();

        // Note: Pending bookings don't consume capacity, so no capacity check here

        // Create new booking with normalized times
        var booking = new Booking
        {
            EVOwnerNIC = evOwnerNIC,
            StationId = request.StationId,
            BookingDate = DateTime.UtcNow,
            ReservationDateTime = normalizedStartTime, // Use normalized start time
            EndDateTime = normalizedEndTime, // Store normalized end time
            StartHourKey = startHourKey, // Store primary hour key for backward compatibility
            OccupiedHourKeys = occupiedHourKeys, // Store all occupied hour keys
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _bookingRepository.CreateAsync(booking);
    }

    
    /// Gets a booking by ID
    
    /// <param name="id">Booking ID</param>
    /// <returns>Booking if found, null otherwise</returns>
    public async Task<Booking?> GetBookingAsync(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking != null)
        {
            await PopulateStationNamesAsync(new[] { booking });
        }
        return booking;
    }

    
    /// Gets bookings for an EV owner
    
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <param name="includeHistory">Include historical bookings</param>
    /// <returns>Collection of bookings for the EV owner</returns>
    public async Task<IEnumerable<Booking>> GetBookingsByOwnerAsync(string evOwnerNIC, bool includeHistory = true)
    {
        var bookings = await _bookingRepository.GetByEVOwnerAsync(evOwnerNIC, includeHistory);
        await PopulateStationNamesAsync(bookings);
        return bookings;
    }

    
    /// Gets dashboard statistics for an EV owner
    
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <returns>Dashboard statistics</returns>
    public async Task<DashboardResponse> GetDashboardStatsAsync(string evOwnerNIC)
    {
        var stats = await _bookingQueries.GetDashboardStatsAsync(evOwnerNIC);
        return new DashboardResponse
        {
            PendingReservations = stats.PendingReservations,
            ApprovedFutureReservations = stats.ApprovedFutureReservations,
            TotalBookings = stats.TotalBookings
        };
    }

    
    /// Updates a booking
    
    /// <param name="id">Booking ID</param>
    /// <param name="request">Booking update request</param>
    /// <param name="evOwnerNIC">EV owner NIC (for authorization)</param>
    /// <returns>Updated booking if found and authorized, null otherwise</returns>
    public async Task<Booking?> UpdateBookingAsync(string id, BookingUpdateRequest request, string evOwnerNIC)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            return null;
        }

        // Verify ownership
        if (booking.EVOwnerNIC != evOwnerNIC)
        {
            throw new UnauthorizedAccessException("You can only update your own bookings");
        }

        // Validate request
        var validationResult = await _updateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Enforce 12-hour rule
        if (!TimeNormalizationHelper.IsAtLeast12HoursAhead(booking.ReservationDateTime))
        {
            throw new InvalidOperationException("Changes allowed only up to 12 hours before start");
        }

        // Normalize new start time to hour boundary
        var normalizedStartTime = TimeNormalizationHelper.NormalizeToHour(request.ReservationDateTime);
        
        // Calculate new end time (normalize to hour boundaries)
        var normalizedEndTime = TimeNormalizationHelper.CalculateEndTime(normalizedStartTime, request.EndDateTime);

        // Validate 7-day window
        if (!TimeNormalizationHelper.IsWithin7DayWindow(normalizedStartTime))
        {
            throw new InvalidOperationException("Reservation must be within 7 days from booking date");
        }

        // Get station for validation
        var station = await _stationRepository.GetByIdAsync(booking.StationId);
        if (station == null)
        {
            throw new ArgumentException("Charging station not found");
        }

        // Validate working hours for the entire time range
        if (!TimeNormalizationHelper.IsTimeRangeWithinWorkingHours(normalizedStartTime, normalizedEndTime, station.OpenTime, station.CloseTime))
        {
            throw new InvalidOperationException("Outside working hours");
        }

        // Generate new hour keys for all occupied slots
        var newOccupiedHourKeys = TimeNormalizationHelper.GenerateOccupiedHourKeys(normalizedStartTime, normalizedEndTime);
        var newStartHourKey = newOccupiedHourKeys.FirstOrDefault();

        // If changing to different slots, check capacity (only for approved bookings)
        if (booking.Status == BookingStatus.Approved)
        {
            var currentOccupiedKeys = booking.OccupiedHourKeys.Any() 
                ? booking.OccupiedHourKeys 
                : new List<string> { booking.StartHourKey ?? TimeNormalizationHelper.GenerateHourKey(booking.ReservationDateTime) };

            // Check if any new slots are different from current ones
            var hasSlotChanges = !newOccupiedHourKeys.SequenceEqual(currentOccupiedKeys);

            if (hasSlotChanges)
            {
                // Check capacity for all new slots
                foreach (var hourKey in newOccupiedHourKeys)
                {
                    var approvedCount = await _bookingQueries.CountApprovedForStationAndHourAsync(
                        booking.StationId, 
                        hourKey
                    );

                    if (approvedCount >= station.TotalSlots)
                    {
                        throw new InvalidOperationException($"No slots available for hour {hourKey}");
                    }
                }
            }
        }

        // Update booking with normalized times
        booking.ReservationDateTime = normalizedStartTime;
        booking.EndDateTime = normalizedEndTime;
        booking.StartHourKey = newStartHourKey;
        booking.OccupiedHourKeys = newOccupiedHourKeys;
        booking.UpdatedAt = DateTime.UtcNow;

        var updatedBooking = await _bookingRepository.UpdateAsync(id, booking);
        if (updatedBooking != null)
        {
            await PopulateStationNamesAsync(new[] { updatedBooking });
        }
        return updatedBooking;
    }

    
    /// Cancels a booking
    
    /// <param name="id">Booking ID</param>
    /// <param name="evOwnerNIC">EV owner NIC (for authorization)</param>
    /// <returns>True if cancelled, false if not found or not authorized</returns>
    public async Task<bool> CancelBookingAsync(string id, string evOwnerNIC)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            return false;
        }

        // Verify ownership
        if (booking.EVOwnerNIC != evOwnerNIC)
        {
            throw new UnauthorizedAccessException("You can only cancel your own bookings");
        }

        // Enforce 12-hour rule
        if (!TimeNormalizationHelper.IsAtLeast12HoursAhead(booking.ReservationDateTime))
        {
            throw new InvalidOperationException("Changes allowed only up to 12 hours before start");
        }

        // Cancel booking
        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(id, booking);
        return true;
    }

    
    /// Approves a booking (Backoffice/Operator only)
    
    /// <param name="id">Booking ID</param>
    /// <returns>Updated booking if found, null otherwise</returns>
    public async Task<Booking?> ApproveBookingAsync(string id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            return null;
        }

        if (booking.Status != BookingStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be approved");
        }

        // Get station for capacity check
        var station = await _stationRepository.GetByIdAsync(booking.StationId);
        if (station == null)
        {
            throw new ArgumentException("Charging station not found");
        }

        // Check capacity for all occupied slots
        var occupiedHourKeys = booking.OccupiedHourKeys.Any() 
            ? booking.OccupiedHourKeys 
            : new List<string> { booking.StartHourKey ?? TimeNormalizationHelper.GenerateHourKey(booking.ReservationDateTime) };

        foreach (var hourKey in occupiedHourKeys)
        {
            var approvedCount = await _bookingQueries.CountApprovedForStationAndHourAsync(
                booking.StationId, 
                hourKey
            );

            if (approvedCount >= station.TotalSlots)
            {
                throw new InvalidOperationException($"No slots available for hour {hourKey}");
            }
        }

        // Generate QR payload
        var qrPayload = _qrGenerator.GetPayload(booking);
        booking.QRPayload = qrPayload;
        booking.Status = BookingStatus.Approved;
        booking.ApprovedAt = DateTime.UtcNow;
        booking.UpdatedAt = DateTime.UtcNow;

        var updatedBooking = await _bookingRepository.UpdateAsync(id, booking);
        if (updatedBooking != null)
        {
            await PopulateStationNamesAsync(new[] { updatedBooking });
        }
        return updatedBooking;
    }

    
    /// Completes a booking via QR scan
    
    /// <param name="request">Booking complete request with QR payload</param>
    /// <returns>Updated booking if found and valid, null otherwise</returns>
    public async Task<Booking?> CompleteBookingAsync(BookingCompleteRequest request)
    {
        // Validate request
        var validationResult = await _completeValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Extract booking info from QR payload
        var bookingInfo = _qrGenerator.ExtractBookingInfo(request.QRPayload);
        if (bookingInfo == null)
        {
            throw new ArgumentException("Invalid QR payload");
        }

        // Get booking
        var booking = await _bookingRepository.GetByIdAsync(bookingInfo.BookingId);
        if (booking == null)
        {
            return null;
        }

        if (booking.Status != BookingStatus.Approved)
        {
            throw new InvalidOperationException("Only approved bookings can be completed");
        }

        // Complete booking
        booking.Status = BookingStatus.Completed;
        booking.UpdatedAt = DateTime.UtcNow;

        var completedBooking = await _bookingRepository.UpdateAsync(bookingInfo.BookingId, booking);
        if (completedBooking != null)
        {
            await PopulateStationNamesAsync(new[] { completedBooking });
        }
        return completedBooking;
    }

    
    /// Gets paginated bookings with optional filtering
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="evOwnerNIC">Optional EV owner NIC filter</param>
    /// <param name="stationId">Optional station ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Paginated bookings</returns>
    public async Task<PaginatedResponse<Booking>> GetBookingsAsync(int page, int pageSize, string? evOwnerNIC = null, string? stationId = null, string? status = null)
    {
        var (bookings, totalCount) = await _bookingRepository.GetPaginatedAsync(page, pageSize, evOwnerNIC, stationId, status);
        await PopulateStationNamesAsync(bookings);

        return new PaginatedResponse<Booking>
        {
            Items = bookings.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    
    /// Gets paginated bookings for a station operator
    
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="operatorId">Operator user ID</param>
    /// <param name="evOwnerNIC">Optional EV owner NIC filter</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Paginated bookings for operator's stations</returns>
    public async Task<PaginatedResponse<Booking>> GetBookingsForOperatorAsync(int page, int pageSize, string operatorId, string? evOwnerNIC = null, string? status = null)
    {
        // Get all stations owned by this operator
        var stations = await _stationRepository.FindAsync(s => s.OperatorId == operatorId);
        var stationIds = stations.Select(s => s.Id).ToList();

        // If operator has no stations, return empty result
        if (!stationIds.Any())
        {
            return new PaginatedResponse<Booking>
            {
                Items = new List<Booking>(),
                Page = page,
                PageSize = pageSize,
                TotalCount = 0
            };
        }

        var (bookings, totalCount) = await _bookingRepository.GetPaginatedByStationsAsync(page, pageSize, stationIds, evOwnerNIC, status);
        await PopulateStationNamesAsync(bookings);

        return new PaginatedResponse<Booking>
        {
            Items = bookings.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    
    /// Gets the available slots for a specific date from the station's schedule
    
    /// <param name="station">Charging station</param>
    /// <param name="date">Date to check</param>
    /// <returns>Number of available slots for the date</returns>
    private int GetAvailableSlotsForDate(ChargingStation station, DateTime date)
    {
        // Look for a specific schedule entry for the date
        var scheduleEntry = station.Schedule.FirstOrDefault(s => s.Date.Date == date.Date);
        
        if (scheduleEntry != null)
        {
            return scheduleEntry.SlotsAvailable;
        }
        
        // If no specific schedule found, use the station's total slots as fallback
        return station.TotalSlots;
    }

    
    /// Populates station names for a collection of bookings
    
    /// <param name="bookings">Collection of bookings to populate</param>
    private async Task PopulateStationNamesAsync(IEnumerable<Booking> bookings)
    {
        if (bookings == null || !bookings.Any())
        {
            return;
        }

        // Get unique station IDs
        var stationIds = bookings.Select(b => b.StationId).Distinct().ToList();
        
        // Fetch all stations in one query
        var stations = await _stationRepository.FindAsync(s => stationIds.Contains(s.Id));
        var stationDict = stations.ToDictionary(s => s.Id, s => s.Name);

        // Populate station names
        foreach (var booking in bookings)
        {
            if (stationDict.TryGetValue(booking.StationId, out var stationName))
            {
                booking.StationName = stationName;
            }
        }
    }
}

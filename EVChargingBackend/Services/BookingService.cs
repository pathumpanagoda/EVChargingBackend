/*
 * Author: EV Charging System
 * Date: 2024-12-19
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

/// <summary>
/// Service for managing bookings with business rule enforcement
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the BookingService
    /// </summary>
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

    /// <summary>
    /// Creates a new booking
    /// </summary>
    /// <param name="request">Booking creation request</param>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <returns>Created booking</returns>
    public async Task<Booking> CreateBookingAsync(BookingRequest request, string evOwnerNIC)
    {
        // Validate request
        var validationResult = await _bookingValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // Verify EV owner exists and is active
        var evOwner = await _evOwnerRepository.GetByIdAsync(evOwnerNIC);
        if (evOwner == null || !evOwner.IsActive)
        {
            throw new ArgumentException("EV owner not found or inactive");
        }

        // Verify charging station exists and is active
        var station = await _stationRepository.GetByIdAsync(request.StationId);
        if (station == null || !station.IsActive)
        {
            throw new ArgumentException("Charging station not found or inactive");
        }

        // Enforce 7-day rule
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        if (request.ReservationDateTime > sevenDaysFromNow)
        {
            throw new InvalidOperationException("Reservation must be within 7 days from booking date");
        }

        // Check slot availability
        var overlappingCount = await _bookingQueries.CountOverlappingApprovedAsync(
            request.StationId, 
            request.ReservationDateTime, 
            request.ReservationDateTime.AddHours(1) // Assuming 1-hour booking slots
        );

        // Get available slots for the requested date
        var availableSlots = GetAvailableSlotsForDate(station, request.ReservationDateTime.Date);
        
        if (overlappingCount >= availableSlots)
        {
            throw new InvalidOperationException("No slots available");
        }

        // Create new booking
        var booking = new Booking
        {
            EVOwnerNIC = evOwnerNIC,
            StationId = request.StationId,
            BookingDate = DateTime.UtcNow,
            ReservationDateTime = request.ReservationDateTime,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return await _bookingRepository.CreateAsync(booking);
    }

    /// <summary>
    /// Gets a booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking if found, null otherwise</returns>
    public async Task<Booking?> GetBookingAsync(string id)
    {
        return await _bookingRepository.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets bookings for an EV owner
    /// </summary>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <param name="includeHistory">Include historical bookings</param>
    /// <returns>Collection of bookings for the EV owner</returns>
    public async Task<IEnumerable<Booking>> GetBookingsByOwnerAsync(string evOwnerNIC, bool includeHistory = true)
    {
        return await _bookingRepository.GetByEVOwnerAsync(evOwnerNIC, includeHistory);
    }

    /// <summary>
    /// Gets dashboard statistics for an EV owner
    /// </summary>
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

    /// <summary>
    /// Updates a booking
    /// </summary>
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
        var twelveHoursFromNow = DateTime.UtcNow.AddHours(12);
        if (booking.ReservationDateTime <= twelveHoursFromNow)
        {
            throw new InvalidOperationException("Booking cannot be modified within 12 hours of reservation time");
        }

        // Enforce 7-day rule
        var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
        if (request.ReservationDateTime > sevenDaysFromNow)
        {
            throw new InvalidOperationException("Reservation must be within 7 days from booking date");
        }

        // Check slot availability for new time
        var overlappingCount = await _bookingQueries.CountOverlappingApprovedAsync(
            booking.StationId,
            request.ReservationDateTime,
            request.ReservationDateTime.AddHours(1)
        );

        var station = await _stationRepository.GetByIdAsync(booking.StationId);
        if (station != null)
        {
            var availableSlots = GetAvailableSlotsForDate(station, request.ReservationDateTime.Date);
            if (overlappingCount >= availableSlots)
            {
                throw new InvalidOperationException("No slots available");
            }
        }

        // Update booking
        booking.ReservationDateTime = request.ReservationDateTime;
        booking.UpdatedAt = DateTime.UtcNow;

        return await _bookingRepository.UpdateAsync(id, booking);
    }

    /// <summary>
    /// Cancels a booking
    /// </summary>
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
        var twelveHoursFromNow = DateTime.UtcNow.AddHours(12);
        if (booking.ReservationDateTime <= twelveHoursFromNow)
        {
            throw new InvalidOperationException("Booking cannot be cancelled within 12 hours of reservation time");
        }

        // Cancel booking
        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        await _bookingRepository.UpdateAsync(id, booking);
        return true;
    }

    /// <summary>
    /// Approves a booking (Backoffice/Operator only)
    /// </summary>
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

        // Check slot availability before approval
        var overlappingCount = await _bookingQueries.CountOverlappingApprovedAsync(
            booking.StationId,
            booking.ReservationDateTime,
            booking.ReservationDateTime.AddHours(1)
        );

        var station = await _stationRepository.GetByIdAsync(booking.StationId);
        if (station != null)
        {
            var availableSlots = GetAvailableSlotsForDate(station, booking.ReservationDateTime.Date);
            if (overlappingCount >= availableSlots)
            {
                throw new InvalidOperationException("No slots available");
            }
        }

        // Generate QR payload
        var qrPayload = _qrGenerator.GetPayload(booking);
        booking.QRPayload = qrPayload;
        booking.Status = BookingStatus.Approved;
        booking.UpdatedAt = DateTime.UtcNow;

        return await _bookingRepository.UpdateAsync(id, booking);
    }

    /// <summary>
    /// Completes a booking via QR scan
    /// </summary>
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

        return await _bookingRepository.UpdateAsync(bookingInfo.BookingId, booking);
    }

    /// <summary>
    /// Gets paginated bookings with optional filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="evOwnerNIC">Optional EV owner NIC filter</param>
    /// <param name="stationId">Optional station ID filter</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>Paginated bookings</returns>
    public async Task<PaginatedResponse<Booking>> GetBookingsAsync(int page, int pageSize, string? evOwnerNIC = null, string? stationId = null, string? status = null)
    {
        var (bookings, totalCount) = await _bookingRepository.GetPaginatedAsync(page, pageSize, evOwnerNIC, stationId, status);

        return new PaginatedResponse<Booking>
        {
            Items = bookings.ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Gets the available slots for a specific date from the station's schedule
    /// </summary>
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
}

/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Unit tests for BookingService business rules
 */

using EVChargingBackend.DTOs;
using EVChargingBackend.Models;
using EVChargingBackend.Queries;
using EVChargingBackend.Repositories;
using EVChargingBackend.Services;
using EVChargingBackend.Validators;
using FluentAssertions;
using Moq;

namespace EVChargingBackend.Tests;

/// <summary>
/// Unit tests for BookingService
/// </summary>
public class BookingServiceTests
{
    private readonly Mock<BookingRepository> _mockBookingRepository;
    private readonly Mock<ChargingStationRepository> _mockStationRepository;
    private readonly Mock<EVOwnerRepository> _mockEVOwnerRepository;
    private readonly Mock<BookingQueries> _mockBookingQueries;
    private readonly Mock<QRCodeGenerator> _mockQRGenerator;
    private readonly BookingService _bookingService;

    public BookingServiceTests()
    {
        _mockBookingRepository = new Mock<BookingRepository>(Mock.Of<EVChargingBackend.Data.MongoDbContext>());
        _mockStationRepository = new Mock<ChargingStationRepository>(Mock.Of<EVChargingBackend.Data.MongoDbContext>());
        _mockEVOwnerRepository = new Mock<EVOwnerRepository>(Mock.Of<EVChargingBackend.Data.MongoDbContext>());
        _mockBookingQueries = new Mock<BookingQueries>(Mock.Of<EVChargingBackend.Data.MongoDbContext>());
        _mockQRGenerator = new Mock<QRCodeGenerator>(false);

        _bookingService = new BookingService(
            _mockBookingRepository.Object,
            _mockStationRepository.Object,
            _mockEVOwnerRepository.Object,
            _mockBookingQueries.Object,
            _mockQRGenerator.Object,
            new BookingRequestValidator(),
            new BookingUpdateRequestValidator(),
            new BookingCompleteRequestValidator()
        );
    }

    [Fact]
    public async Task CreateBookingAsync_WhenReservationDateExceeds7Days_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new BookingRequest
        {
            StationId = "station123",
            ReservationDateTime = DateTime.UtcNow.AddDays(8) // 8 days in the future
        };

        var evOwnerNIC = "123456789V";

        var evOwner = new EVOwner
        {
            NIC = evOwnerNIC,
            Name = "Test Owner",
            Email = "test@example.com",
            Phone = "1234567890",
            PasswordHash = "hashed",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var station = new ChargingStation
        {
            Id = "station123",
            Name = "Test Station",
            Location = new Location { Latitude = 6.9271, Longitude = 79.8612, Address = "Test Address" },
            Type = "AC",
            TotalSlots = 5,
            AvailableSlots = 5,
            IsActive = true,
            OperatorId = "operator123",
            CreatedAt = DateTime.UtcNow
        };

        _mockEVOwnerRepository.Setup(x => x.GetByIdAsync(evOwnerNIC))
            .ReturnsAsync(evOwner);

        _mockStationRepository.Setup(x => x.GetByIdAsync("station123"))
            .ReturnsAsync(station);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CreateBookingAsync(request, evOwnerNIC));

        exception.Message.Should().Contain("Reservation must be within 7 days from booking date");
    }

    [Fact]
    public async Task CancelBookingAsync_WhenWithin12Hours_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var bookingId = "booking123";
        var evOwnerNIC = "123456789V";

        var booking = new Booking
        {
            Id = bookingId,
            EVOwnerNIC = evOwnerNIC,
            StationId = "station123",
            BookingDate = DateTime.UtcNow.AddDays(-1),
            ReservationDateTime = DateTime.UtcNow.AddHours(6), // 6 hours from now (within 12-hour rule)
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockBookingRepository.Setup(x => x.GetByIdAsync(bookingId))
            .ReturnsAsync(booking);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _bookingService.CancelBookingAsync(bookingId, evOwnerNIC));

        exception.Message.Should().Contain("Booking cannot be cancelled within 12 hours of reservation time");
    }

    [Fact]
    public async Task CreateBookingAsync_WhenValidRequest_ShouldCreateBooking()
    {
        // Arrange
        var request = new BookingRequest
        {
            StationId = "station123",
            ReservationDateTime = DateTime.UtcNow.AddDays(1) // 1 day in the future
        };

        var evOwnerNIC = "123456789V";

        var evOwner = new EVOwner
        {
            NIC = evOwnerNIC,
            Name = "Test Owner",
            Email = "test@example.com",
            Phone = "1234567890",
            PasswordHash = "hashed",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var station = new ChargingStation
        {
            Id = "station123",
            Name = "Test Station",
            Location = new Location { Latitude = 6.9271, Longitude = 79.8612, Address = "Test Address" },
            Type = "AC",
            TotalSlots = 5,
            AvailableSlots = 5,
            IsActive = true,
            OperatorId = "operator123",
            CreatedAt = DateTime.UtcNow
        };

        var expectedBooking = new Booking
        {
            Id = "booking123",
            EVOwnerNIC = evOwnerNIC,
            StationId = "station123",
            BookingDate = DateTime.UtcNow,
            ReservationDateTime = request.ReservationDateTime,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockEVOwnerRepository.Setup(x => x.GetByIdAsync(evOwnerNIC))
            .ReturnsAsync(evOwner);

        _mockStationRepository.Setup(x => x.GetByIdAsync("station123"))
            .ReturnsAsync(station);

        _mockBookingQueries.Setup(x => x.CountOverlappingApprovedAsync("station123", It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(0); // No overlapping bookings

        _mockBookingRepository.Setup(x => x.CreateAsync(It.IsAny<Booking>()))
            .ReturnsAsync(expectedBooking);

        // Act
        var result = await _bookingService.CreateBookingAsync(request, evOwnerNIC);

        // Assert
        result.Should().NotBeNull();
        result.EVOwnerNIC.Should().Be(evOwnerNIC);
        result.StationId.Should().Be("station123");
        result.Status.Should().Be(BookingStatus.Pending);
        result.ReservationDateTime.Should().Be(request.ReservationDateTime);

        _mockBookingRepository.Verify(x => x.CreateAsync(It.IsAny<Booking>()), Times.Once);
    }
}

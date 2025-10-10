/*
 * Author: EV Charging System
 * Date: 2025-09-28
 * Purpose: QR Code generation helper for booking verification
 */

using EVChargingBackend.Models;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace EVChargingBackend.Helpers;


/// Helper class for QR code generation and validation

public class QRCodeGenerator
{
    private readonly bool _generatePngImages;

    
    /// Initializes a new instance of the QRCodeGenerator
    
    /// <param name="generatePngImages">Whether to generate PNG images or just payload strings</param>
    public QRCodeGenerator(bool generatePngImages = false)
    {
        _generatePngImages = generatePngImages;
    }

    
    /// Generates a QR payload string for a booking
    
    /// <param name="booking">Booking to generate QR for</param>
    /// <returns>QR payload string</returns>
    public string GetPayload(Booking booking)
    {
        var payload = $"BOOKING:{booking.Id}:{booking.EVOwnerNIC}:{booking.StationId}";
        var hash = ComputeSHA256Hash(payload);
        return $"{payload}:{hash}";
    }

    
    /// Generates a QR payload string for a booking ID
    
    /// <param name="bookingId">Booking ID</param>
    /// <param name="evOwnerNIC">EV owner NIC</param>
    /// <param name="stationId">Station ID</param>
    /// <returns>QR payload string</returns>
    public string GetPayload(string bookingId, string evOwnerNIC, string stationId)
    {
        var payload = $"BOOKING:{bookingId}:{evOwnerNIC}:{stationId}";
        var hash = ComputeSHA256Hash(payload);
        return $"{payload}:{hash}";
    }

    
    /// Generates a QR code PNG image as base64 string
    
    /// <param name="payload">QR payload string</param>
    /// <returns>Base64 encoded PNG image</returns>
    public string GetPngBase64(string payload)
    {
        if (!_generatePngImages)
        {
            throw new InvalidOperationException("PNG generation is disabled. Use GetPayload() for payload strings only.");
        }

        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
        
        var imageBytes = qrCode.GetGraphic(20);
        
        return Convert.ToBase64String(imageBytes);
    }

    
    /// Validates a QR payload string
    
    /// <param name="payload">QR payload string to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidatePayload(string payload)
    {
        try
        {
            var parts = payload.Split(':');
            if (parts.Length != 5 || parts[0] != "BOOKING")
            {
                return false;
            }

            var bookingId = parts[1];
            var evOwnerNIC = parts[2];
            var stationId = parts[3];
            var providedHash = parts[4];

            var expectedPayload = $"BOOKING:{bookingId}:{evOwnerNIC}:{stationId}";
            var expectedHash = ComputeSHA256Hash(expectedPayload);

            return string.Equals(providedHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    
    /// Extracts booking information from a QR payload
    
    /// <param name="payload">QR payload string</param>
    /// <returns>Booking information if valid, null otherwise</returns>
    public QRBookingInfo? ExtractBookingInfo(string payload)
    {
        if (!ValidatePayload(payload))
        {
            return null;
        }

        try
        {
            var parts = payload.Split(':');
            return new QRBookingInfo
            {
                BookingId = parts[1],
                EVOwnerNIC = parts[2],
                StationId = parts[3],
                Hash = parts[4]
            };
        }
        catch
        {
            return null;
        }
    }

    
    /// Computes SHA256 hash of a string
    
    /// <param name="input">Input string</param>
    /// <returns>SHA256 hash as hexadecimal string</returns>
    private static string ComputeSHA256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}


/// Booking information extracted from QR payload

public class QRBookingInfo
{
    
    /// Booking ID
    
    public string BookingId { get; set; } = string.Empty;

    
    /// EV owner NIC
    
    public string EVOwnerNIC { get; set; } = string.Empty;

    
    /// Station ID
    
    public string StationId { get; set; } = string.Empty;

    
    /// Hash for verification
    
    public string Hash { get; set; } = string.Empty;
}

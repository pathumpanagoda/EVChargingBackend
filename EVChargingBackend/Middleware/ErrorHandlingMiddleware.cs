/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Global error handling middleware for consistent API responses
 */

using EVChargingBackend.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace EVChargingBackend.Middleware;


/// Middleware for handling exceptions and returning consistent error responses

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    
    /// Initializes a new instance of the ErrorHandlingMiddleware
    
    /// <param name="next">Next middleware in the pipeline</param>
    /// <param name="logger">Logger instance</param>
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    
    /// Invokes the middleware
    
    /// <param name="context">HTTP context</param>
    /// <returns>Task representing the middleware execution</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    
    /// Handles exceptions and returns appropriate HTTP responses
    
    /// <param name="context">HTTP context</param>
    /// <param name="exception">Exception to handle</param>
    /// <returns>Task representing the exception handling</returns>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        ApiResponse<object> response = exception switch
        {
            ValidationException validationEx => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "Validation failed",
                Errors = validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            },
            UnauthorizedAccessException => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "Unauthorized access",
                Errors = new List<string>()
            },
            ArgumentException argEx => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = argEx.Message,
                Errors = new List<string>()
            },
            KeyNotFoundException => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "Resource not found",
                Errors = new List<string>()
            },
            InvalidOperationException opEx when opEx.Message.Contains("12 hours") => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "Booking cannot be modified within 12 hours of reservation time",
                Errors = new List<string>()
            },
            InvalidOperationException opEx when opEx.Message.Contains("7 days") => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "Reservation must be within 7 days from booking date",
                Errors = new List<string>()
            },
            InvalidOperationException opEx when opEx.Message.Contains("active bookings") => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "Cannot deactivate station with active future bookings",
                Errors = new List<string>()
            },
            InvalidOperationException opEx when opEx.Message.Contains("slot") => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "No available slots for the requested time",
                Errors = new List<string>()
            },
            InvalidOperationException opEx => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = opEx.Message,
                Errors = new List<string>()
            },
            _ => new ApiResponse<object>
            {
                Success = false,
                Data = null,
                Message = "An internal server error occurred",
                Errors = new List<string>()
            }
        };

        context.Response.StatusCode = exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            InvalidOperationException opEx when opEx.Message.Contains("12 hours") => (int)HttpStatusCode.UnprocessableEntity,
            InvalidOperationException opEx when opEx.Message.Contains("7 days") => (int)HttpStatusCode.UnprocessableEntity,
            InvalidOperationException opEx when opEx.Message.Contains("active bookings") => (int)HttpStatusCode.Conflict,
            InvalidOperationException opEx when opEx.Message.Contains("slot") => (int)HttpStatusCode.Conflict,
            InvalidOperationException => (int)HttpStatusCode.UnprocessableEntity,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}


/// Extension methods for registering the error handling middleware

public static class ErrorHandlingMiddlewareExtensions
{
    
    /// Adds the error handling middleware to the application pipeline
    
    /// <param name="builder">Application builder</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}

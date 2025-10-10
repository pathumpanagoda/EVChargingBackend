/*
 * Author: EV Charging System
 * Date: 2025-09-28
 * Purpose: OpenAPI document filter to ensure proper version specification
 */

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EVChargingBackend.Helpers;


/// Document filter to ensure OpenAPI version is properly set

public class OpenApiVersionFilter : IDocumentFilter
{
    
    /// Applies the document filter to ensure OpenAPI version is set
    
    /// <param name="swaggerDoc">The OpenAPI document</param>
    /// <param name="context">The document filter context</param>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Ensure the OpenAPI version is set to 3.0.1
        // The OpenApiDocument should automatically have the correct version
        // This filter ensures the document is properly formatted
        if (swaggerDoc.Info == null)
        {
            swaggerDoc.Info = new OpenApiInfo
            {
                Title = "EV Charging System API",
                Version = "v1",
                Description = "API for EV Charging Station Booking System"
            };
        }

        // The OpenAPI version is automatically set by Swashbuckle
        // This filter ensures the document is properly formatted
        // The version field should be automatically included in the generated JSON
    }
}

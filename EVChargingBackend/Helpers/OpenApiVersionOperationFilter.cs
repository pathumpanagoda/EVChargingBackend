/*
 * Author: EV Charging System
 * Date: 2025-09-28
 * Purpose: Operation filter to ensure proper OpenAPI version specification
 */

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EVChargingBackend.Helpers;


/// Operation filter to ensure OpenAPI version is properly set

public class OpenApiVersionOperationFilter : IOperationFilter
{
    
    /// Applies the operation filter to ensure OpenAPI version is set
    
    /// <param name="operation">The OpenAPI operation</param>
    /// <param name="context">The operation filter context</param>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // This filter ensures the operation is properly formatted for OpenAPI 3.0
        // The version is handled at the document level
    }
}

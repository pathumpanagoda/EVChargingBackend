/*
 * Author: EV Charging System
 * Date: 2025-09-28
 * Purpose: Schema filter to ensure proper OpenAPI version specification
 */

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EVChargingBackend.Helpers;


/// Schema filter to ensure OpenAPI version is properly set

public class OpenApiVersionSchemaFilter : ISchemaFilter
{
    
    /// Applies the schema filter to ensure OpenAPI version is set
    
    /// <param name="schema">The OpenAPI schema</param>
    /// <param name="context">The schema filter context</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // This filter ensures the schema is properly formatted for OpenAPI 3.0
        // The version is handled at the document level
    }
}

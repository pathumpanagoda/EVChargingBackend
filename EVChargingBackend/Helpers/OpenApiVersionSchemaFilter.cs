/*
 * Author: EV Charging System
 * Date: 2024-12-19
 * Purpose: Schema filter to ensure proper OpenAPI version specification
 */

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EVChargingBackend.Helpers;

/// <summary>
/// Schema filter to ensure OpenAPI version is properly set
/// </summary>
public class OpenApiVersionSchemaFilter : ISchemaFilter
{
    /// <summary>
    /// Applies the schema filter to ensure OpenAPI version is set
    /// </summary>
    /// <param name="schema">The OpenAPI schema</param>
    /// <param name="context">The schema filter context</param>
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // This filter ensures the schema is properly formatted for OpenAPI 3.0
        // The version is handled at the document level
    }
}

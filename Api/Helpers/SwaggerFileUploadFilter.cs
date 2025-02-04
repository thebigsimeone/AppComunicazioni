using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class SwaggerFileUploadFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody == null)
        {
            operation.RequestBody = new OpenApiRequestBody();
        }

        operation.RequestBody.Content = new Dictionary<string, OpenApiMediaType>
        {
            ["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = new Dictionary<string, OpenApiSchema>
                    {
                        ["file"] = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        }
                    },
                    Required = new HashSet<string> { "file" }
                }
            }
        };
    }
}

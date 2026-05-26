using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Sabro.API.Configuration;

/// <summary>
/// Forces enum schemas to be emitted as JSON strings with their member names,
/// matching the runtime behavior of <see cref="System.Text.Json.Serialization.JsonStringEnumConverter"/>
/// configured globally on the MVC pipeline. Without this transformer
/// Microsoft.AspNetCore.OpenApi defaults to the enum's underlying numeric type,
/// which breaks clients that bind to the string wire format
/// (e.g. <c>"Noun"</c> not <c>0</c>).
/// </summary>
internal sealed class StringEnumSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;
        var enumType = type.IsEnum ? type : Nullable.GetUnderlyingType(type);

        if (enumType is { IsEnum: true })
        {
            schema.Type = JsonSchemaType.String;
            schema.Format = null;
            schema.Enum = Enum.GetNames(enumType)
                .Select(name => (JsonNode)JsonValue.Create(name)!)
                .ToList();
        }

        return Task.CompletedTask;
    }
}

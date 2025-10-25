
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace NuGet.ProjectModel;

internal static class JsonSourceGenExtensions
{
    public static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options)
    {
        return (JsonTypeInfo<T>)options.TypeInfoResolver.GetTypeInfo(typeof(T), options);
    }

    public static JsonSerializerOptions WithSourceGenContext(this JsonSerializerOptions options, JsonSerializerContext context)
    {
        var newOptions = new JsonSerializerOptions(options)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(context, options.TypeInfoResolver)
        };
        return newOptions;
    }
}
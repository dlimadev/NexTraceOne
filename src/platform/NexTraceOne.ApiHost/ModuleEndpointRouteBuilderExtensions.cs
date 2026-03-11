using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Extensões para descoberta e mapeamento automático de endpoints dos módulos.
/// </summary>
public static class ModuleEndpointRouteBuilderExtensions
{
    /// <summary>Descobre assemblies de API do NexTraceOne e executa os módulos de endpoint encontrados.</summary>
    public static IEndpointRouteBuilder MapAllModuleEndpoints(this IEndpointRouteBuilder app)
    {
        var apiAssemblies = LoadApiAssemblies();

        foreach (var assembly in apiAssemblies.OrderBy(assembly => assembly.FullName, StringComparer.Ordinal))
        {
            var endpointModules = GetLoadableTypes(assembly)
                .Where(type => type.IsClass && type.Name.EndsWith("EndpointModule", StringComparison.Ordinal))
                .Select(type => new
                {
                    Type = type,
                    Method = type.GetMethod("MapEndpoints", BindingFlags.Public | BindingFlags.Static)
                })
                .Where(item => item.Method is not null && IsSupportedSignature(item.Method))
                .OrderBy(item => item.Type.FullName, StringComparer.Ordinal);

            foreach (var endpointModule in endpointModules)
            {
                endpointModule.Method!.Invoke(null, [app]);
            }
        }

        return app;
    }

    private static IReadOnlyList<Assembly> LoadApiAssemblies()
        => DependencyContext.Default?
            .RuntimeLibraries
            .Where(library => library.Name.StartsWith("NexTraceOne.", StringComparison.Ordinal)
                && library.Name.EndsWith(".API", StringComparison.Ordinal))
            .Select(library => Assembly.Load(new AssemblyName(library.Name)))
            .DistinctBy(assembly => assembly.FullName)
            .ToArray()
           ?? [];

    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
    }

    private static bool IsSupportedSignature(MethodInfo methodInfo)
    {
        var parameters = methodInfo.GetParameters();
        return parameters.Length == 1 && typeof(IEndpointRouteBuilder).IsAssignableFrom(parameters[0].ParameterType);
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Naner.DependencyInjection;

/// <summary>
/// Provides a centralized service host for Naner applications.
/// Manages the service provider lifecycle and provides easy access to services.
/// </summary>
public class NanerServiceHost : IDisposable, IAsyncDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    /// <summary>
    /// Gets the underlying service provider.
    /// </summary>
    public IServiceProvider Services => _serviceProvider;

    private NanerServiceHost(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates a new service host with all Naner services configured.
    /// </summary>
    /// <param name="nanerRoot">The Naner root directory path</param>
    /// <param name="configureServices">Optional action to add additional services</param>
    /// <returns>Configured service host</returns>
    public static NanerServiceHost Create(string nanerRoot, Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();

        // Add all Naner services
        services.AddNaner(nanerRoot);

        // Allow custom service registration
        configureServices?.Invoke(services);

        var provider = services.BuildServiceProvider();
        return new NanerServiceHost(provider);
    }

    /// <summary>
    /// Creates a new service host with minimal services for lightweight operations.
    /// </summary>
    /// <param name="nanerRoot">The Naner root directory path</param>
    /// <returns>Configured service host with minimal services</returns>
    public static NanerServiceHost CreateMinimal(string nanerRoot)
    {
        var services = new ServiceCollection();

        services.AddNanerCore(nanerRoot);
        services.AddNanerInfrastructure();

        var provider = services.BuildServiceProvider();
        return new NanerServiceHost(provider);
    }

    /// <summary>
    /// Gets a required service of the specified type.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance</returns>
    /// <exception cref="InvalidOperationException">Thrown if service is not registered</exception>
    public T GetRequiredService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Gets a service of the specified type, or null if not registered.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance or null</returns>
    public T? GetService<T>() where T : class
    {
        return _serviceProvider.GetService<T>();
    }

    /// <summary>
    /// Gets all services of the specified type.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>All registered services of the type</returns>
    public IEnumerable<T> GetServices<T>() where T : notnull
    {
        return _serviceProvider.GetServices<T>();
    }

    /// <summary>
    /// Creates a new scope for scoped services.
    /// </summary>
    /// <returns>A new service scope</returns>
    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _serviceProvider.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await _serviceProvider.DisposeAsync();
        _disposed = true;
    }
}

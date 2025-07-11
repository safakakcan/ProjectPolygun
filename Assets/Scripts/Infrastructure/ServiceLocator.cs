using ProjectPolygun.Core.Interfaces;

namespace ProjectPolygun.Infrastructure
{
    /// <summary>
    /// Static service locator for easy access to core services
    /// Provides a simple way to access services without direct dependency injection
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        /// Get the event bus instance
        /// </summary>
        public static IEventBus EventBus => GameBootstrapper.GetEventBus();
        
        /// <summary>
        /// Get the service container instance
        /// </summary>
        public static IServiceContainer Container => GameBootstrapper.GetServiceContainer();
        
        /// <summary>
        /// Resolve a service from the container
        /// </summary>
        /// <typeparam name="T">Service type to resolve</typeparam>
        /// <returns>Service instance</returns>
        public static T Resolve<T>()
        {
            return Container.Resolve<T>();
        }
        
        /// <summary>
        /// Try to resolve a service from the container
        /// </summary>
        /// <typeparam name="T">Service type to resolve</typeparam>
        /// <param name="service">Resolved service or default</param>
        /// <returns>True if service was resolved</returns>
        public static bool TryResolve<T>(out T service)
        {
            return Container.TryResolve(out service);
        }
        
        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">Service type to check</typeparam>
        /// <returns>True if service is registered</returns>
        public static bool IsRegistered<T>()
        {
            return Container.IsRegistered<T>();
        }
    }
} 
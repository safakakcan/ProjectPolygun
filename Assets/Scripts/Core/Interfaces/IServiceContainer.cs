using System;

namespace ProjectPolygun.Core.Interfaces
{
    /// <summary>
    /// Simple dependency injection container for loose coupling between systems
    /// </summary>
    public interface IServiceContainer
    {
        /// <summary>
        /// Register a service implementation
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <typeparam name="TImplementation">Implementation type</typeparam>
        void Register<TInterface, TImplementation>() 
            where TImplementation : class, TInterface;
        
        /// <summary>
        /// Register a service instance
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <param name="instance">Service instance</param>
        void RegisterInstance<TInterface>(TInterface instance);
        
        /// <summary>
        /// Resolve a service by type
        /// </summary>
        /// <typeparam name="T">Service type to resolve</typeparam>
        /// <returns>Service instance</returns>
        T Resolve<T>();
        
        /// <summary>
        /// Try to resolve a service by type
        /// </summary>
        /// <typeparam name="T">Service type to resolve</typeparam>
        /// <param name="service">Resolved service or default</param>
        /// <returns>True if service was resolved</returns>
        bool TryResolve<T>(out T service);
        
        /// <summary>
        /// Check if a service is registered
        /// </summary>
        /// <typeparam name="T">Service type to check</typeparam>
        /// <returns>True if service is registered</returns>
        bool IsRegistered<T>();
    }
} 
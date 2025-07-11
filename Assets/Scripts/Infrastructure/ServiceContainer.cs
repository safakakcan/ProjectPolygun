using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectPolygun.Core.Interfaces;

namespace ProjectPolygun.Infrastructure
{
    /// <summary>
    /// Simple dependency injection container implementation
    /// </summary>
    public class ServiceContainer : MonoBehaviour, IServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Type> _registrations = new Dictionary<Type, Type>();

        public void Register<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            var interfaceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);
            
            _registrations[interfaceType] = implementationType;
        }

        public void RegisterInstance<TInterface>(TInterface instance)
        {
            if (instance == null)
            {
                Debug.LogWarning($"Attempted to register null instance for {typeof(TInterface).Name}");
                return;
            }

            var interfaceType = typeof(TInterface);
            _services[interfaceType] = instance;
        }

        public T Resolve<T>()
        {
            if (TryResolve<T>(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered");
        }

        public bool TryResolve<T>(out T service)
        {
            var serviceType = typeof(T);
            
            // Check if instance is already created
            if (_services.ContainsKey(serviceType))
            {
                service = (T)_services[serviceType];
                return true;
            }

            // Check if type is registered for creation
            if (_registrations.ContainsKey(serviceType))
            {
                var implementationType = _registrations[serviceType];
                try
                {
                    var instance = Activator.CreateInstance(implementationType);
                    _services[serviceType] = instance;
                    service = (T)instance;
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to create instance of {implementationType.Name}: {ex.Message}");
                    service = default(T);
                    return false;
                }
            }

            service = default(T);
            return false;
        }

        public bool IsRegistered<T>()
        {
            var serviceType = typeof(T);
            return _services.ContainsKey(serviceType) || _registrations.ContainsKey(serviceType);
        }

        private void OnDestroy()
        {
            _services.Clear();
            _registrations.Clear();
        }
    }
} 
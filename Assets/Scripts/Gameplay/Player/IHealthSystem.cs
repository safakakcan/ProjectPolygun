using System;
using UnityEngine;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    /// Interface for health and damage management
    /// </summary>
    public interface IHealthSystem
    {
        /// <summary>
        /// Current health value
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// Maximum health value
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// Health as percentage (0-1)
        /// </summary>
        float HealthPercentage { get; }
        
        /// <summary>
        /// Whether the entity is currently alive
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Event fired when health changes
        /// </summary>
        event Action<float, float> OnHealthChanged; // currentHealth, maxHealth
        
        /// <summary>
        /// Event fired when entity dies
        /// </summary>
        event Action OnDeath;
        
        /// <summary>
        /// Apply damage to the entity
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="damageSource">Source of the damage (optional)</param>
        /// <returns>Actual damage taken</returns>
        float TakeDamage(float damage, GameObject damageSource = null);
        
        /// <summary>
        /// Heal the entity
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        /// <returns>Actual amount healed</returns>
        float Heal(float healAmount);
        
        /// <summary>
        /// Set health to maximum
        /// </summary>
        void FullHeal();
        
        /// <summary>
        /// Set health to a specific value
        /// </summary>
        /// <param name="health">Health value to set</param>
        void SetHealth(float health);
        
        /// <summary>
        /// Initialize the health system
        /// </summary>
        /// <param name="maxHealth">Maximum health value</param>
        void Initialize(float maxHealth);
        
        /// <summary>
        /// Reset health system to initial state
        /// </summary>
        void Reset();
    }
} 
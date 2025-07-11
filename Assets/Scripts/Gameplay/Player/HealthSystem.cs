using System;
using UnityEngine;
using ProjectPolygun.Core.Events;
using ProjectPolygun.Infrastructure;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    ///     Health system implementation for managing entity health and damage
    /// </summary>
    public class HealthSystem : MonoBehaviour, IHealthSystem
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool invulnerable = false;
        
        private float _currentHealth;
        private bool _isInitialized;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        public bool IsAlive => CurrentHealth > 0;

        public event Action<float, float> OnHealthChanged;
        public event Action OnDeath;

        private void Awake()
        {
            if (!_isInitialized)
            {
                Initialize(maxHealth);
            }
        }

        public void Initialize(float maxHealthValue)
        {
            maxHealth = maxHealthValue;
            _currentHealth = maxHealth;
            _isInitialized = true;
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public float TakeDamage(float damage, GameObject damageSource = null)
        {
            if (!IsAlive || invulnerable || damage <= 0)
                return 0f;

            var actualDamage = Mathf.Min(damage, _currentHealth);
            var previousHealth = _currentHealth;
            
            _currentHealth = Mathf.Max(0, _currentHealth - actualDamage);
            
            // Fire health changed event
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            
            // Publish global damage event
            var damageEvent = new PlayerDamagedEvent(
                GetPlayerID(), 
                actualDamage, 
                _currentHealth, 
                damageSource
            );
            ServiceLocator.EventBus.Publish(damageEvent);
            
            // Check for death
            if (previousHealth > 0 && _currentHealth <= 0)
            {
                HandleDeath(damageSource);
            }
            
            return actualDamage;
        }

        public float Heal(float healAmount)
        {
            if (!IsAlive || healAmount <= 0)
                return 0f;

            var actualHeal = Mathf.Min(healAmount, maxHealth - _currentHealth);
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + actualHeal);
            
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            
            return actualHeal;
        }

        public void FullHeal()
        {
            if (_currentHealth < maxHealth)
            {
                _currentHealth = maxHealth;
                OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            }
        }

        public void SetHealth(float health)
        {
            var clampedHealth = Mathf.Clamp(health, 0, maxHealth);
            var wasAlive = IsAlive;
            
            _currentHealth = clampedHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            
            // Check for death transition
            if (wasAlive && !IsAlive)
            {
                HandleDeath(null);
            }
        }

        public void Reset()
        {
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void HandleDeath(GameObject damageSource)
        {
            OnDeath?.Invoke();
            
            // Publish global death event
            var deathEvent = new PlayerDeathEvent(
                GetPlayerID(),
                GetKillerID(damageSource),
                GetWeaponUsed(damageSource),
                transform.position
            );
            ServiceLocator.EventBus.Publish(deathEvent);
        }

        private uint GetPlayerID()
        {
            // Try to get player ID from PlayerController or NetworkIdentity
            var playerController = GetComponent<IPlayerController>();
            if (playerController != null)
                return playerController.PlayerId;
                
            // Fallback to instance ID
            return (uint)GetInstanceID();
        }

        private uint GetKillerID(GameObject damageSource)
        {
            if (damageSource == null) return 0;
            
            var killerController = damageSource.GetComponent<IPlayerController>();
            if (killerController != null)
                return killerController.PlayerId;
                
            // Check parent for player controller
            var parentController = damageSource.GetComponentInParent<IPlayerController>();
            if (parentController != null)
                return parentController.PlayerId;
                
            return 0;
        }

        private string GetWeaponUsed(GameObject damageSource)
        {
            if (damageSource == null) return "Unknown";
            
            // Try to get weapon name from damage source
            var weaponName = damageSource.name;
            
            // Clean up weapon name (remove (Clone) suffix, etc.)
            if (weaponName.Contains("(Clone)"))
                weaponName = weaponName.Replace("(Clone)", "").Trim();
                
            return weaponName;
        }

        /// <summary>
        ///     Set invulnerability state (useful for respawn protection)
        /// </summary>
        public void SetInvulnerable(bool invulnerableState)
        {
            invulnerable = invulnerableState;
        }

        /// <summary>
        ///     Check if currently invulnerable
        /// </summary>
        public bool IsInvulnerable => invulnerable;
    }
} 
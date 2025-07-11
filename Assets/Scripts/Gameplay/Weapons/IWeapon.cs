using UnityEngine;

namespace ProjectPolygun.Gameplay.Weapons
{
    /// <summary>
    ///     Base interface for all weapon types
    /// </summary>
    public interface IWeapon
    {
        /// <summary>
        ///     Weapon name identifier
        /// </summary>
        string WeaponName { get; }

        /// <summary>
        ///     Current ammunition count
        /// </summary>
        int CurrentAmmo { get; }

        /// <summary>
        ///     Maximum ammunition capacity
        /// </summary>
        int MaxAmmo { get; }

        /// <summary>
        ///     Whether the weapon can currently fire
        /// </summary>
        bool CanFire { get; }

        /// <summary>
        ///     Whether the weapon is currently reloading
        /// </summary>
        bool IsReloading { get; }

        /// <summary>
        ///     Fire rate in shots per minute
        /// </summary>
        float FireRate { get; }

        /// <summary>
        ///     Weapon damage per shot
        /// </summary>
        float Damage { get; }

        /// <summary>
        ///     Weapon range in units
        /// </summary>
        float Range { get; }

        /// <summary>
        ///     Attempt to fire the weapon
        /// </summary>
        /// <param name="origin">Fire origin position</param>
        /// <param name="direction">Fire direction</param>
        /// <returns>True if weapon fired successfully</returns>
        bool TryFire(Vector3 origin, Vector3 direction);

        /// <summary>
        ///     Start reloading the weapon
        /// </summary>
        void StartReload();

        /// <summary>
        ///     Complete the reload process
        /// </summary>
        void CompleteReload();

        /// <summary>
        ///     Update weapon state
        /// </summary>
        /// <param name="deltaTime">Frame time</param>
        void Update(float deltaTime);

        /// <summary>
        ///     Initialize the weapon
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Cleanup weapon resources
        /// </summary>
        void Cleanup();
    }
}
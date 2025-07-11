using UnityEngine;

namespace ProjectPolygun.Gameplay.Player
{
    /// <summary>
    ///     Core player controller interface for FPS mechanics
    /// </summary>
    public interface IPlayerController
    {
        /// <summary>
        ///     Player's unique network ID
        /// </summary>
        uint PlayerId { get; }

        /// <summary>
        ///     Current player position
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        ///     Current player rotation
        /// </summary>
        Quaternion Rotation { get; }

        /// <summary>
        ///     Whether this player is controlled locally
        /// </summary>
        bool IsLocalPlayer { get; }

        /// <summary>
        ///     Whether the player is currently alive
        /// </summary>
        bool IsAlive { get; }

        /// <summary>
        ///     Initialize the player controller
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Handle player input (local player only)
        /// </summary>
        /// <param name="deltaTime">Frame time</param>
        void HandleInput(float deltaTime);

        /// <summary>
        ///     Update player state
        /// </summary>
        /// <param name="deltaTime">Frame time</param>
        void UpdatePlayer(float deltaTime);

        /// <summary>
        ///     Respawn the player at specified position
        /// </summary>
        /// <param name="spawnPosition">Position to spawn at</param>
        /// <param name="spawnRotation">Rotation to spawn with</param>
        void Respawn(Vector3 spawnPosition, Quaternion spawnRotation);
    }
}
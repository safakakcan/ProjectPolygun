using UnityEngine;

namespace StinkySteak.SimulationTimer
{
    public struct SimulationTimer
    {
        public static SimulationTimer None => default;

        public float TargetTime { get; private set; }

        public static SimulationTimer CreateFromSeconds(float duration)
        {
            return new SimulationTimer
            {
                TargetTime = duration + Time.time
            };
        }

        public bool IsRunning => TargetTime > 0;

        public bool IsExpired()
        {
            return Time.time >= TargetTime && IsRunning;
        }

        public bool IsExpiredOrNotRunning()
        {
            return Time.time >= TargetTime;
        }

        public float RemainingSeconds
            => Mathf.Max(TargetTime - Time.time, 0);
    }
}
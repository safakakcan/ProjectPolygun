using UnityEngine;

namespace StinkySteak.SimulationTimer
{
    public struct PauseableSimulationTimer
    {
        public static PauseableSimulationTimer None => default;

        private float _targetTime;

        private float _pauseAtTime;

        public float TargetTime => GetTargetTime();
        public bool IsPaused { get; private set; }

        private float GetTargetTime()
        {
            if (!IsPaused) return _targetTime;

            return _targetTime + Time.time - _pauseAtTime;
        }

        public static PauseableSimulationTimer CreateFromSeconds(float duration)
        {
            return new PauseableSimulationTimer
            {
                _targetTime = duration + Time.time
            };
        }

        public void Pause()
        {
            if (IsPaused) return;

            IsPaused = true;
            _pauseAtTime = Time.time;
        }

        public void Resume()
        {
            if (!IsPaused) return;

            _targetTime = GetTargetTime();
            IsPaused = false;
            _pauseAtTime = 0;
        }

        public bool IsRunning => _targetTime > 0;

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
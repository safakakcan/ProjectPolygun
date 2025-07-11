using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StinkySteak.NetcodeBenchmark
{
    [Serializable]
    public struct SinRandomMoveWrapper : IMoveWrapper
    {
        [SerializeField] private float _minSpeed;
        [SerializeField] private float _maxSpeed;
        [SerializeField] private float _amplitude;

        private Vector3 _targetPosition;
        private Vector3 _initialPosition;

        private float _speed;

        public static SinRandomMoveWrapper CreateDefault()
        {
            var wrapper = new SinRandomMoveWrapper();
            wrapper._minSpeed = 1f;
            wrapper._maxSpeed = 1f;
            wrapper._amplitude = 1f;

            return wrapper;
        }

        public void NetworkStart(Transform transform)
        {
            _speed = Random.Range(_minSpeed, _maxSpeed);
            _targetPosition = RandomVector3.Get(1f);
        }

        public void NetworkUpdate(Transform transform)
        {
            var sin = Mathf.Sin(Time.time * _speed) * _amplitude;

            transform.position = _initialPosition + _targetPosition * sin;
        }
    }
}
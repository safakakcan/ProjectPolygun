using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StinkySteak.NetcodeBenchmark
{
    [Serializable]
    public struct SinMoveYWrapper : IMoveWrapper
    {
        [SerializeField] private float _minSpeed;
        [SerializeField] private float _maxSpeed;
        [SerializeField] private float _minAmplitude;
        [SerializeField] private float _maxAmplitude;
        [SerializeField] private float _positionMaxRandom;

        private Vector3 _initialPosition;

        private float _speed;
        private float _amplitude;

        public static SinMoveYWrapper CreateDefault()
        {
            var wrapper = new SinMoveYWrapper();
            wrapper._minSpeed = 0.5f;
            wrapper._maxSpeed = 1f;
            wrapper._minAmplitude = 0.5f;
            wrapper._maxAmplitude = 1f;
            wrapper._positionMaxRandom = 5f;

            return wrapper;
        }

        public void NetworkStart(Transform transform)
        {
            _speed = Random.Range(_minSpeed, _maxSpeed);
            _amplitude = Random.Range(_minAmplitude, _maxAmplitude);
            _initialPosition = RandomVector3.Get(_positionMaxRandom);
        }

        public void NetworkUpdate(Transform transform)
        {
            var sin = Mathf.Sin(Time.time * _speed) * _amplitude;

            transform.position = _initialPosition + Vector3.up * sin;
        }
    }
}
using UnityEngine;

namespace Mirror.Examples.CouchCoop
{
    public class PlatformMovement : NetworkBehaviour
    {
        private Vector3 lastPlatformPosition;

        // A separate script to handle platform behaviour, see its partner script, MovingPlatform.cs
        private bool onPlatform;
        private Transform platformTransform;

        private void FixedUpdate()
        {
            if (onPlatform)
            {
                var deltaPosition = platformTransform.position - lastPlatformPosition;
                transform.position += deltaPosition;
                lastPlatformPosition = platformTransform.position;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "Finish")
            {
                platformTransform = collision.gameObject.GetComponent<Transform>();
                lastPlatformPosition = platformTransform.position;
                onPlatform = true;
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            // ideally set a Platform tag, but we'l just use a Unity Pre-set.
            if (collision.gameObject.tag == "Finish")
            {
                onPlatform = false;
                platformTransform = null;
            }
        }

        public override void OnStartAuthority()
        {
            enabled = true;
        }
    }
}
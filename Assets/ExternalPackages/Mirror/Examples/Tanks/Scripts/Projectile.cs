using UnityEngine;

namespace Mirror.Examples.Tanks
{
    public class Projectile : NetworkBehaviour
    {
        public float destroyAfter = 2f;
        public Rigidbody rigidBody;
        public float force = 1000f;

        // set velocity for server and client. this way we don't have to sync the
        // position, because both the server and the client simulate it.
        private void Start()
        {
            rigidBody.AddForce(transform.forward * force);
        }

        // ServerCallback because we don't want a warning
        // if OnTriggerEnter is called on the client
        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Hit: " + other.name);
            if (other.transform.parent.TryGetComponent(out Tank tank))
            {
                --tank.health;
                if (tank.health == 0)
                    NetworkServer.RemovePlayerForConnection(tank.netIdentity.connectionToClient, RemovePlayerOptions.Destroy);

                DestroySelf();
            }
        }

        public override void OnStartServer()
        {
            Invoke(nameof(DestroySelf), destroyAfter);
        }

        // destroy for everyone on the server
        [Server]
        private void DestroySelf()
        {
            NetworkServer.Destroy(gameObject);
        }
    }
}
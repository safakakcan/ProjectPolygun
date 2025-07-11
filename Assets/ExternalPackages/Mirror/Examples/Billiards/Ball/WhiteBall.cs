using UnityEngine;

namespace Mirror.Examples.Billiards
{
    public class WhiteBall : NetworkBehaviour
    {
        public LineRenderer dragIndicator;
        public Rigidbody rigidBody;
        public float forceMultiplier = 2;
        public float maxForce = 40;

        // remember start position to reset to after entering a pocket
        private Vector3 startPosition;

        private void Awake()
        {
            startPosition = transform.position;
        }

        [ClientCallback]
        private void OnMouseDown()
        {
            // enable drag indicator
            dragIndicator.SetPosition(0, transform.position);
            dragIndicator.SetPosition(1, transform.position);
            dragIndicator.gameObject.SetActive(true);
        }

        [ClientCallback]
        private void OnMouseDrag()
        {
            // cast mouse position to world
            if (!MouseToWorld(out var current)) return;

            // drag indicator
            dragIndicator.SetPosition(0, transform.position);
            dragIndicator.SetPosition(1, current);
        }

        [ClientCallback]
        private void OnMouseUp()
        {
            // cast mouse position to world
            if (!MouseToWorld(out var current)) return;

            // calculate delta from ball to mouse
            // ball may have moved since we started dragging,
            // so always use current ball position here.
            var from = transform.position;

            // debug drawing: only works if Gizmos are enabled!
            Debug.DrawLine(from, current, Color.white, 2);

            // calculate pending force delta
            var delta = from - current;
            var force = delta * forceMultiplier;

            // there should be a maximum allowed force
            force = Vector3.ClampMagnitude(force, maxForce);

            // apply force to rigidbody.
            // it will take a round trip to show the effect.
            // the goal for prediction will be to show it immediately.
            CmdApplyForce(force);

            // disable drag indicator
            dragIndicator.gameObject.SetActive(false);
        }

        // reset position when entering a pocket.
        // there's only one trigger in the scene (the pocket).
        [ServerCallback]
        private void OnTriggerEnter(Collider other)
        {
            rigidBody.position = startPosition;
            rigidBody.Sleep(); // reset forces
            GetComponent<NetworkRigidbodyUnreliable>().RpcTeleport(startPosition);
        }

        // cast mouse position on screen to world position
        private bool MouseToWorld(out Vector3 position)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.up, transform.position);
            if (plane.Raycast(ray, out var distance))
            {
                position = ray.GetPoint(distance);
                return true;
            }

            position = default;
            return false;
        }

        // all players can apply force to the white ball.
        // (this is not cheat safe)
        [Command(requiresAuthority = false)]
        private void CmdApplyForce(Vector3 force)
        {
            // AddForce has different force modes, see this excellent diagram:
            // https://www.reddit.com/r/Unity3D/comments/psukm1/know_the_difference_between_forcemodes_a_little/
            // when applying a one-time force to the ball, we need 'Impulse'.
            rigidBody.AddForce(force, ForceMode.Impulse);
        }
    }
}
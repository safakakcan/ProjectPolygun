using UnityEngine;

namespace Mirror.Examples.CCU
{
    public class Player : NetworkBehaviour
    {
        public Vector3 cameraOffset = new(0, 40, -40);

        // automated movement.
        // player may switch to manual movement any time
        [Header("Automated Movement")] public bool autoMove = true;

        public float autoSpeed = 2;
        public float movementProbability = 0.5f;
        public float movementDistance = 20;

        [Header("Manual Movement")] public float manualSpeed = 10;

        private Vector3 destination;
        private bool moving;
        private Vector3 start;

        // cache .transform for benchmark demo.
        // Component.get_transform shows in profiler otherwise.
        private Transform tf;

        private void Update()
        {
            if (!isLocalPlayer) return;

            // player may interrupt auto movement to switch to manual
            if (Interrupted()) autoMove = false;

            // move
            if (autoMove) AutoMove();
            else ManualMove();
        }

        public override void OnStartLocalPlayer()
        {
            tf = transform;
            start = tf.position;

            // make camera follow
            Camera.main.transform.SetParent(transform, false);
            Camera.main.transform.localPosition = cameraOffset;
        }

        public override void OnStopLocalPlayer()
        {
            // free the camera so we don't destroy it too
            Camera.main.transform.SetParent(null, true);
        }

        private void AutoMove()
        {
            if (moving)
            {
                if (Vector3.Distance(tf.position, destination) <= 0.01f)
                    moving = false;
                else
                    tf.position = Vector3.MoveTowards(tf.position, destination, autoSpeed * Time.deltaTime);
            }
            else
            {
                var r = Random.value;
                if (r < movementProbability * Time.deltaTime)
                {
                    // calculate a random position in a circle
                    var circleX = Mathf.Cos(Random.value * Mathf.PI);
                    var circleZ = Mathf.Sin(Random.value * Mathf.PI);
                    var circlePos = new Vector2(circleX, circleZ);
                    var dir = new Vector3(circlePos.x, 0, circlePos.y);

                    // set destination on random pos in a circle around start.
                    // (don't want to wander off)
                    destination = start + dir * movementDistance;
                    moving = true;
                }
            }
        }

        private void ManualMove()
        {
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");

            var direction = new Vector3(h, 0, v);
            transform.position += direction.normalized * (Time.deltaTime * manualSpeed);
        }

        private static bool Interrupted()
        {
            return Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0;
        }
    }
}
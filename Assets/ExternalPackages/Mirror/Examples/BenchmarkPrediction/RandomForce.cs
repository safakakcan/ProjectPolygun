using UnityEngine;

namespace Mirror.Examples.PredictionBenchmark
{
    public class RandomForce : NetworkBehaviour
    {
        public float force = 10;
        public float interval = 3;
        private PredictedRigidbody prediction;
        private Rigidbody rb => prediction.predictedRigidbody;

        private void Awake()
        {
            prediction = GetComponent<PredictedRigidbody>();
        }

        // every(!) connected client adds force to all objects(!)
        // the more clients, the more crazier it gets.
        // this is intentional for benchmarks.
        public override void OnStartClient()
        {
            // start at a random time, but repeat at a fixed time
            var randomStart = Random.Range(0, interval);
            InvokeRepeating(nameof(ApplyForce), randomStart, interval);
        }


        [ClientCallback]
        private void ApplyForce()
        {
            // calculate force in random direction but always upwards
            var direction2D = Random.insideUnitCircle;
            var direction3D = new Vector3(direction2D.x, 1.0f, direction2D.y);
            var impulse = direction3D * force;

            // grab the current Rigidbody from PredictedRigidbody.
            // sometimes this is on a ghost object, so always grab it live:


            // predicted locally and sync to server for others to see.
            // PredictedRigidbody will take care of corrections automatically.
            rb.AddForce(impulse, ForceMode.Impulse);
            CmdApplyForce(impulse);
        }

        [Command(requiresAuthority = false)] // everyone can call this
        private void CmdApplyForce(Vector3 impulse)
        {
            rb.AddForce(impulse, ForceMode.Impulse);
        }
    }
}
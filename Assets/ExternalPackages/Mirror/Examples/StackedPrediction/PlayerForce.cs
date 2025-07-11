// players can apply force to any stacked cube.
// this has to be on the player instead of on the cube via OnMouseDown,
// because OnMouseDown would get blocked by the predicted ghost objects.

using UnityEngine;

namespace Mirror.Examples.PredictionBenchmark
{
    public class PlayerForce : NetworkBehaviour
    {
        public float force = 50;

        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetMouseButtonDown(0))
            {
                // raycast into camera direction
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit))
                    // we may have hit the ghost object.
                    // find the original.
                    if (PredictedRigidbody.IsPredicted(hit.collider, out var predicted))
                    {
                        // apply force in a random direction, this looks best
                        Debug.Log($"Applying force to: {hit.collider.name}");
                        var impulse = Random.insideUnitSphere * force;
                        predicted.predictedRigidbody.AddForce(impulse, ForceMode.Impulse);
                        CmdApplyForce(predicted.netIdentity, impulse);
                    }
            }
        }

        // every play can apply force to this object (no authority required)
        [Command]
        private void CmdApplyForce(NetworkIdentity cube, Vector3 impulse)
        {
            // apply force in that direction
            Debug.LogWarning($"CmdApplyForce: {force} to {cube.name}");
            var rb = cube.GetComponent<Rigidbody>();
            rb.AddForce(impulse, ForceMode.Impulse);
        }
    }
}
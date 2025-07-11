using UnityEngine;

namespace Mirror.Examples.Common.Controllers.Player
{
    [AddComponentMenu("Network/Player Controller RB (Hybrid)")]
    [RequireComponent(typeof(NetworkTransformHybrid))]
    public class PlayerControllerRBHybrid : PlayerControllerRBBase
    {
        public override void Reset()
        {
            base.Reset();
            GetComponent<NetworkTransformHybrid>().useFixedUpdate = true;
        }

        protected override void OnValidate()
        {
            if (Application.isPlaying) return;
            base.OnValidate();

            Reset();
        }
    }
}
// straight forward Vector3.Distance based interest management.

using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    [AddComponentMenu("Network/ Interest Management/ Distance/Distance Interest Management")]
    public class DistanceInterestManagement : InterestManagement
    {
        [Tooltip("The maximum range that objects will be visible at. Add DistanceInterestManagementCustomRange onto NetworkIdentities for custom ranges.")]
        public int visRange = 500;

        [Tooltip("Rebuild all every 'rebuildInterval' seconds.")]
        public float rebuildInterval = 1;

        // cache custom ranges to avoid runtime TryGetComponent lookups
        private readonly Dictionary<NetworkIdentity, DistanceInterestManagementCustomRange> CustomRanges = new();

        private double lastRebuildTime;

        [ServerCallback]
        private void LateUpdate()
        {
            // rebuild all spawned NetworkIdentity's observers every interval
            if (NetworkTime.localTime >= lastRebuildTime + rebuildInterval)
            {
                RebuildAll();
                lastRebuildTime = NetworkTime.localTime;
            }
        }

        // helper function to get vis range for a given object, or default.
        [ServerCallback]
        private int GetVisRange(NetworkIdentity identity)
        {
            return CustomRanges.TryGetValue(identity, out var custom) ? custom.visRange : visRange;
        }

        [ServerCallback]
        public override void ResetState()
        {
            lastRebuildTime = 0D;
            CustomRanges.Clear();
        }

        public override void OnSpawned(NetworkIdentity identity)
        {
            if (identity.TryGetComponent(out DistanceInterestManagementCustomRange custom))
                CustomRanges[identity] = custom;
        }

        public override void OnDestroyed(NetworkIdentity identity)
        {
            CustomRanges.Remove(identity);
        }

        public override bool OnCheckObserver(NetworkIdentity identity, NetworkConnectionToClient newObserver)
        {
            var range = GetVisRange(identity);
            return Vector3.Distance(identity.transform.position, newObserver.identity.transform.position) < range;
        }

        public override void OnRebuildObservers(NetworkIdentity identity, HashSet<NetworkConnectionToClient> newObservers)
        {
            // cache range and .transform because both call GetComponent.
            var range = GetVisRange(identity);
            var position = identity.transform.position;

            // brute force distance check
            // -> only player connections can be observers, so it's enough if we
            //    go through all connections instead of all spawned identities.
            // -> compared to UNET's sphere cast checking, this one is orders of
            //    magnitude faster. if we have 10k monsters and run a sphere
            //    cast 10k times, we will see a noticeable lag even with physics
            //    layers. but checking to every connection is fast.
            foreach (var conn in NetworkServer.connections.Values)
                // authenticated and joined world with a player?
                if (conn != null && conn.isAuthenticated && conn.identity != null)
                    // check distance
                    if (Vector3.Distance(conn.identity.transform.position, position) < range)
                        newObservers.Add(conn);
        }
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Environment
{
    public static class NitroPickupRegistry
    {
        static readonly List<NitroPickup> Active = new();

        public static void Register(NitroPickup pickup)
        {
            if (pickup != null && !Active.Contains(pickup))
                Active.Add(pickup);
        }

        public static void Unregister(NitroPickup pickup)
        {
            if (pickup != null)
                Active.Remove(pickup);
        }

        public static bool TryGetNearestAvailable(Vector3 position, float maxRange, out Vector3 pickupPosition)
        {
            pickupPosition = Vector3.zero;
            var maxRangeSq = maxRange * maxRange;
            var bestDistSq = maxRangeSq;
            var found = false;

            for (var i = Active.Count - 1; i >= 0; i--)
            {
                var pickup = Active[i];
                if (pickup == null)
                {
                    Active.RemoveAt(i);
                    continue;
                }

                if (!pickup.IsAvailable)
                    continue;

                var distSq = (pickup.WorldPosition - position).sqrMagnitude;
                if (distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                pickupPosition = pickup.WorldPosition;
                found = true;
            }

            return found;
        }
    }
}

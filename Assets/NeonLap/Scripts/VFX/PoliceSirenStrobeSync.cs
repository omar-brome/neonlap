using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.VFX
{
    /// <summary>
    /// Aggregates active roof siren flash colors for helicopter searchlight sync during chases.
    /// </summary>
    public static class PoliceSirenStrobeSync
    {
        static readonly List<PoliceRoofSirenVFX> ActiveSirens = new();

        public static void Register(PoliceRoofSirenVFX siren)
        {
            if (siren == null || ActiveSirens.Contains(siren))
                return;

            ActiveSirens.Add(siren);
        }

        public static void Unregister(PoliceRoofSirenVFX siren)
        {
            if (siren == null)
                return;

            ActiveSirens.Remove(siren);
        }

        public static bool TryGetDominantStrobeColor(out Color color)
        {
            color = Color.white;
            if (ActiveSirens.Count == 0)
                return false;

            var sum = Vector3.zero;
            var count = 0;
            for (var i = ActiveSirens.Count - 1; i >= 0; i--)
            {
                var siren = ActiveSirens[i];
                if (siren == null)
                {
                    ActiveSirens.RemoveAt(i);
                    continue;
                }

                var strobe = siren.CurrentStrobeColor;
                sum += new Vector3(strobe.r, strobe.g, strobe.b);
                count++;
            }

            if (count == 0)
                return false;

            sum /= count;
            color = new Color(sum.x, sum.y, sum.z, 1f);
            return true;
        }
    }
}

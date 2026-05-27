using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Track
{
    public class ShortcutEntryGate : MonoBehaviour
    {
        TrackShortcutDefinition definition;

        public void Configure(TrackShortcutDefinition shortcutDefinition)
        {
            definition = shortcutDefinition;
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null || !racer.IsPlayer || racer.IsFinished || racer.IsEliminated)
                return;

            RaceShortcutTracker.Instance?.BeginShortcut(definition);
        }
    }

    public class ShortcutMergeGate : MonoBehaviour
    {
        TrackShortcutDefinition definition;

        public void Configure(TrackShortcutDefinition shortcutDefinition)
        {
            definition = shortcutDefinition;
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null || !racer.IsPlayer || racer.IsFinished || racer.IsEliminated)
                return;

            if (definition == null)
                return;

            var tracker = RaceShortcutTracker.Instance;
            var scoreSystem = racer.GetComponent<RaceScoreSystem>();
            scoreSystem?.RegisterShortcutMerge(definition, tracker);
        }
    }
}

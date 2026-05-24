using System;
using UnityEngine;

namespace NeonLap.Race
{
    public class TrackCheckpoint : MonoBehaviour
    {
        [SerializeField] int index;
        [SerializeField] bool isFinishLine;

        public int Index => index;
        public bool IsFinishLine => isFinishLine;

        public event Action<TrackCheckpoint, RacerProgress> OnPassed;

        public void Configure(int checkpointIndex, bool finishLine)
        {
            index = checkpointIndex;
            isFinishLine = finishLine;
        }

        void OnTriggerEnter(Collider other)
        {
            var racer = other.GetComponentInParent<RacerProgress>();
            if (racer == null)
                return;

            OnPassed?.Invoke(this, racer);
        }
    }
}

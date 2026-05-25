using UnityEngine;

namespace NeonLap.Core
{
    public class MainMenuCarRacer : MonoBehaviour
    {
        Vector3[] path;
        float progress;
        float lapSpeed = 0.07f;
        float hoverHeight = 1.35f;
        float rotationSmoothing = 10f;

        public void Configure(Vector3[] loopPath, float startProgress, float speed, float height)
        {
            path = loopPath;
            progress = startProgress;
            lapSpeed = speed;
            hoverHeight = height;
        }

        void Update()
        {
            if (path == null || path.Length < 2)
                return;

            progress = (progress + lapSpeed * Time.deltaTime) % 1f;
            var position = SamplePath(progress);
            var lookAhead = SamplePath((progress + 0.015f) % 1f);
            transform.position = position + Vector3.up * hoverHeight;

            var direction = lookAhead - position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            var targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
        }

        Vector3 SamplePath(float t)
        {
            var count = path.Length;
            var scaled = t * count;
            var index = Mathf.FloorToInt(scaled) % count;
            var nextIndex = (index + 1) % count;
            var localT = scaled - Mathf.Floor(scaled);
            return Vector3.Lerp(path[index], path[nextIndex], localT);
        }
    }
}

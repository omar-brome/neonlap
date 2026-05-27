using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonLap.Race
{
    [Serializable]
    public struct ReplayFrameSnapshot
    {
        public float Time;
        public float Px;
        public float Py;
        public float Pz;
        public float Qx;
        public float Qy;
        public float Qz;
        public float Qw;

        public Vector3 Position => new(Px, Py, Pz);

        public Quaternion Rotation => new(Qx, Qy, Qz, Qw);

        public static ReplayFrameSnapshot FromTransform(float time, Vector3 position, Quaternion rotation)
        {
            return new ReplayFrameSnapshot
            {
                Time = time,
                Px = position.x,
                Py = position.y,
                Pz = position.z,
                Qx = rotation.x,
                Qy = rotation.y,
                Qz = rotation.z,
                Qw = rotation.w
            };
        }
    }

    [Serializable]
    public class GhostRecordingData
    {
        public float Duration;
        /// <summary>Race-mode ghosts: sample when RaceTime >= this (final-lap PB clips).</summary>
        public float AnchorRaceTime;
        public float[] Times;
        public float[] Px;
        public float[] Py;
        public float[] Pz;
        public float[] Qx;
        public float[] Qy;
        public float[] Qz;
        public float[] Qw;

        public int FrameCount => Times != null ? Times.Length : 0;

        public bool IsValid => FrameCount >= 2 && Duration > 0.2f;

        public static GhostRecordingData FromFrames(IReadOnlyList<ReplayFrameSnapshot> frames, int maxFrames = 720)
        {
            if (frames == null || frames.Count < 2)
                return null;

            var sampled = Downsample(frames, maxFrames);
            var data = new GhostRecordingData
            {
                Duration = sampled[^1].Time,
                Times = new float[sampled.Count],
                Px = new float[sampled.Count],
                Py = new float[sampled.Count],
                Pz = new float[sampled.Count],
                Qx = new float[sampled.Count],
                Qy = new float[sampled.Count],
                Qz = new float[sampled.Count],
                Qw = new float[sampled.Count]
            };

            for (var i = 0; i < sampled.Count; i++)
            {
                var frame = sampled[i];
                data.Times[i] = frame.Time;
                data.Px[i] = frame.Px;
                data.Py[i] = frame.Py;
                data.Pz[i] = frame.Pz;
                data.Qx[i] = frame.Qx;
                data.Qy[i] = frame.Qy;
                data.Qz[i] = frame.Qz;
                data.Qw[i] = frame.Qw;
            }

            return data;
        }

        public List<ReplayFrameSnapshot> ToFrames()
        {
            var list = new List<ReplayFrameSnapshot>(FrameCount);
            if (!IsValid)
                return list;

            for (var i = 0; i < FrameCount; i++)
            {
                list.Add(new ReplayFrameSnapshot
                {
                    Time = Times[i],
                    Px = Px[i],
                    Py = Py[i],
                    Pz = Pz[i],
                    Qx = Qx[i],
                    Qy = Qy[i],
                    Qz = Qz[i],
                    Qw = Qw[i]
                });
            }

            return list;
        }

        static List<ReplayFrameSnapshot> Downsample(IReadOnlyList<ReplayFrameSnapshot> frames, int maxFrames)
        {
            if (frames.Count <= maxFrames)
            {
                var copy = new List<ReplayFrameSnapshot>(frames.Count);
                for (var i = 0; i < frames.Count; i++)
                    copy.Add(frames[i]);
                return copy;
            }

            var result = new List<ReplayFrameSnapshot>(maxFrames);
            var step = (frames.Count - 1) / (float)(maxFrames - 1);
            for (var i = 0; i < maxFrames; i++)
            {
                var index = Mathf.Clamp(Mathf.RoundToInt(i * step), 0, frames.Count - 1);
                result.Add(frames[index]);
            }

            return result;
        }
    }

    public static class GhostPlaybackSampler
    {
        public static void Sample(IReadOnlyList<ReplayFrameSnapshot> frames, float time, out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (frames == null || frames.Count == 0)
                return;

            if (frames.Count == 1)
            {
                position = frames[0].Position;
                rotation = frames[0].Rotation;
                return;
            }

            if (time <= frames[0].Time)
            {
                position = frames[0].Position;
                rotation = frames[0].Rotation;
                return;
            }

            var last = frames[^1];
            if (time >= last.Time)
            {
                position = last.Position;
                rotation = last.Rotation;
                return;
            }

            for (var i = 0; i < frames.Count - 1; i++)
            {
                var a = frames[i];
                var b = frames[i + 1];
                if (time > b.Time)
                    continue;

                var t = Mathf.InverseLerp(a.Time, b.Time, time);
                position = Vector3.Lerp(a.Position, b.Position, t);
                rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t);
                return;
            }

            position = last.Position;
            rotation = last.Rotation;
        }
    }
}

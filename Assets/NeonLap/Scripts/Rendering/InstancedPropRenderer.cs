using System;
using System.Collections.Generic;
using NeonLap.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonLap.Rendering
{
    public class InstancedPropRenderer : MonoBehaviour
    {
        const int MaxInstancesPerBatch = 1023;

        struct PropBatch
        {
            public Mesh Mesh;
            public Material Material;
            public readonly List<Matrix4x4> Matrices;

            public PropBatch(Mesh mesh, Material material)
            {
                Mesh = mesh;
                Material = material;
                Matrices = new List<Matrix4x4>(256);
            }
        }

        static InstancedPropRenderer instance;

        readonly Dictionary<int, PropBatch> batches = new();
        Mesh capsuleMesh;
        Mesh cylinderMesh;
        Mesh cubeMesh;
        MaterialPropertyBlock propertyBlock;
        readonly Matrix4x4[] matrixDrawBuffer = new Matrix4x4[MaxInstancesPerBatch];

        public static InstancedPropRenderer Instance => instance;

        public static InstancedPropRenderer Ensure(Transform parent)
        {
            if (instance != null)
                return instance;

            var go = new GameObject("InstancedProps");
            go.transform.SetParent(parent, false);
            return go.AddComponent<InstancedPropRenderer>();
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            propertyBlock = new MaterialPropertyBlock();
            CachePrimitiveMeshes();
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void LateUpdate()
        {
            if (!GameQualitySettings.UseGpuInstancing)
                return;

            foreach (var batch in batches.Values)
            {
                if (batch.Mesh == null || batch.Material == null || batch.Matrices.Count == 0)
                    continue;

                DrawBatch(batch);
            }

            ClearAll();
        }

        public void AddCapsule(Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            AddInstance(capsuleMesh, position, rotation, scale, material);
        }

        public void AddCylinder(Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            AddInstance(cylinderMesh, position, rotation, scale, material);
        }

        public void AddCube(Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            AddInstance(cubeMesh, position, rotation, scale, material);
        }

        public void AddInstance(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            if (!GameQualitySettings.UseGpuInstancing || mesh == null || material == null)
                return;

            var key = HashCode.Combine(mesh, material);
            if (!batches.TryGetValue(key, out var batch))
            {
                batch = new PropBatch(mesh, material);
                batches[key] = batch;
            }

            batch.Matrices.Add(Matrix4x4.TRS(position, rotation, scale));
        }

        void DrawBatch(PropBatch batch)
        {
            var matrices = batch.Matrices;
            var offset = 0;
            while (offset < matrices.Count)
            {
                var count = Mathf.Min(MaxInstancesPerBatch, matrices.Count - offset);
                for (var i = 0; i < count; i++)
                    matrixDrawBuffer[i] = matrices[offset + i];

                Graphics.DrawMeshInstanced(
                    batch.Mesh,
                    0,
                    batch.Material,
                    matrixDrawBuffer,
                    count,
                    propertyBlock,
                    ShadowCastingMode.Off,
                    false,
                    gameObject.layer);
                offset += count;
            }
        }

        public void ClearAll()
        {
            foreach (var batch in batches.Values)
                batch.Matrices.Clear();
        }

        void CachePrimitiveMeshes()
        {
            capsuleMesh = CreatePrimitiveMesh(PrimitiveType.Capsule);
            cylinderMesh = CreatePrimitiveMesh(PrimitiveType.Cylinder);
            cubeMesh = CreatePrimitiveMesh(PrimitiveType.Cube);
        }

        static Mesh CreatePrimitiveMesh(PrimitiveType type)
        {
            var temp = GameObject.CreatePrimitive(type);
            var mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);
            return mesh;
        }
    }
}

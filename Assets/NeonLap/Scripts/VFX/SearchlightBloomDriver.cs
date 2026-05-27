using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NeonLap.VFX
{
    public class SearchlightBloomDriver : MonoBehaviour
    {
        static SearchlightBloomDriver instance;

        Volume volume;
        Bloom bloom;
        float baseIntensity = 0.4f;
        float baseThreshold = 1f;
        float glare;

        public static SearchlightBloomDriver Instance => instance;

        public static SearchlightBloomDriver Ensure(UnityEngine.Camera camera)
        {
            if (camera == null)
                return instance;

            if (instance != null)
                return instance;

            var existing = camera.GetComponent<SearchlightBloomDriver>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            instance = camera.gameObject.AddComponent<SearchlightBloomDriver>();
            instance.Initialize();
            return instance;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
            Initialize();
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void Initialize()
        {
            volume = GetComponent<Volume>();
            if (volume == null)
                volume = gameObject.AddComponent<Volume>();

            volume.isGlobal = false;
            volume.priority = 25f;
            volume.weight = 1f;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();

            bloom = volume.profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.intensity.Override(0.35f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.72f);
            baseIntensity = bloom.intensity.value;
            baseThreshold = bloom.threshold.value;
        }

        public void SetGlare(float normalizedGlare)
        {
            glare = Mathf.Clamp01(normalizedGlare);
        }

        void LateUpdate()
        {
            if (bloom == null)
                return;

            var boostedIntensity = Mathf.Lerp(baseIntensity, baseIntensity + 1.35f, glare);
            var boostedThreshold = Mathf.Lerp(baseThreshold, 0.55f, glare);
            bloom.intensity.value = boostedIntensity;
            bloom.threshold.value = boostedThreshold;
            volume.weight = Mathf.Lerp(0.35f, 1f, glare);
        }
    }
}

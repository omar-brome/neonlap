using NeonLap.Input;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.VFX
{
    public class VehicleHornSfx : MonoBehaviour
    {
        [SerializeField] float hornCooldown = 0.55f;

        AudioSource hornSource;
        PlayerInputReader inputReader;
        float nextHornTime;

        public static void Setup(GameObject playerCar)
        {
            if (playerCar == null)
                return;

            var horn = playerCar.GetComponent<VehicleHornSfx>();
            if (horn == null)
                horn = playerCar.AddComponent<VehicleHornSfx>();

            horn.Bind(playerCar);
        }

        void Bind(GameObject playerCar)
        {
            inputReader = playerCar.GetComponent<PlayerInputReader>();
            if (hornSource == null)
            {
                hornSource = gameObject.AddComponent<AudioSource>();
                hornSource.playOnAwake = false;
                hornSource.spatialBlend = 0.65f;
                hornSource.volume = 0.55f;
            }
        }

        void Update()
        {
            if (!CareerCosmeticStore.HornUnlocked || inputReader == null)
                return;

            if (!inputReader.HornPressed || Time.time < nextHornTime)
                return;

            nextHornTime = Time.time + hornCooldown;
            PlayHorn();
        }

        void PlayHorn()
        {
            if (hornSource == null)
                return;

            hornSource.pitch = Random.Range(0.92f, 1.05f);
            hornSource.PlayOneShot(CreateProceduralClip());
        }

        static AudioClip CreateProceduralClip()
        {
            const int sampleRate = 22050;
            const float duration = 0.22f;
            var samples = Mathf.CeilToInt(sampleRate * duration);
            var data = new float[samples];

            for (var i = 0; i < samples; i++)
            {
                var t = i / (float)sampleRate;
                var envelope = Mathf.Exp(-t * 14f);
                var tone = Mathf.Sin(2f * Mathf.PI * 420f * t) * 0.55f;
                var overtone = Mathf.Sin(2f * Mathf.PI * 840f * t) * 0.25f;
                data[i] = (tone + overtone) * envelope;
            }

            var clip = AudioClip.Create("NeonHorn", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

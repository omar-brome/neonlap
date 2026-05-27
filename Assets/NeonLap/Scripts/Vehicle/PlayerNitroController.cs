using NeonLap.Environment;
using NeonLap.Input;
using NeonLap.Race;
using UnityEngine;

namespace NeonLap.Vehicle
{
    [RequireComponent(typeof(VehicleNitroBoost))]
    public class PlayerNitroController : MonoBehaviour
    {
        [SerializeField] float minTimeBetweenActivations = 0.35f;

        IVehicleInputProvider inputProvider;
        VehicleNitroBoost nitro;
        RacerProgress racer;
        float nextAllowedTime;

        void Awake()
        {
            nitro = GetComponent<VehicleNitroBoost>();
            inputProvider = GetComponent<IVehicleInputProvider>();
            racer = GetComponent<RacerProgress>();
        }

        void Update()
        {
            if (nitro == null || inputProvider == null || racer == null)
                return;

            if (racer.IsFinished || racer.IsEliminated)
                return;

            if (!inputProvider.NitroPressed || Time.time < nextAllowedTime)
                return;

            nextAllowedTime = Time.time + minTimeBetweenActivations;
            if (nitro.TryActivateFromInput())
                StadiumIncidentHub.Report("NITRO BOOST!");
        }
    }
}


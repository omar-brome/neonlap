using UnityEngine;

namespace NeonLap.Core
{
    public static class GameMinimapSettings
    {
        const string PrefKey = "NeonLap.Minimap.RotateWithCar";

        public static bool RotateWithCar { get; private set; }

        public static void Load()
        {
            RotateWithCar = PlayerPrefs.GetInt(PrefKey, 0) == 1;
        }

        public static void SetRotateWithCar(bool rotateWithCar)
        {
            RotateWithCar = rotateWithCar;
            PlayerPrefs.SetInt(PrefKey, rotateWithCar ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ToggleRotateWithCar()
        {
            SetRotateWithCar(!RotateWithCar);
        }
    }
}

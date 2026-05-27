using UnityEngine;

namespace NeonLap.Race
{
    public static class CareerXpStore
    {
        const string TotalXpKey = "NeonLap.Career.TotalXp";

        public static int TotalXp
        {
            get => PlayerPrefs.GetInt(TotalXpKey, 0);
            private set
            {
                PlayerPrefs.SetInt(TotalXpKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static int Add(int amount)
        {
            if (amount <= 0)
                return TotalXp;

            TotalXp += amount;
            return TotalXp;
        }
    }
}

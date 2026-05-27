using UnityEngine;

namespace NeonLap.Race
{
    public static class CareerCurrencyStore
    {
        const string BalanceKey = "NeonLap.Career.Credits";

        public static int Balance
        {
            get => PlayerPrefs.GetInt(BalanceKey, 0);
            private set
            {
                PlayerPrefs.SetInt(BalanceKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        public static int CreditsFromRaceScore(int score) => Mathf.Max(score / 25, 10);

        public static void Add(int amount)
        {
            if (amount <= 0)
                return;

            Balance += amount;
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0 || Balance < amount)
                return false;

            Balance -= amount;
            return true;
        }
    }
}

using UnityEngine;

namespace NeonLap.Race
{
    public static class RaceFinishRewards
    {
        public static int GetPlacementStars(int placement) =>
            placement switch
            {
                1 => 3,
                2 => 2,
                3 => 1,
                _ => 0,
            };

        public static int GetXpEarned(int score, int placement)
        {
            var baseXp = Mathf.Max(35, score / 15);
            var placementBonus = placement switch
            {
                1 => 150,
                2 => 90,
                3 => 45,
                _ => 20,
            };
            return baseXp + placementBonus;
        }

        public static string GetPlacementStarLine(int placementStars)
        {
            return placementStars switch
            {
                3 => "★ ★ ★",
                2 => "★ ★ ☆",
                1 => "★ ☆ ☆",
                _ => "☆ ☆ ☆",
            };
        }
    }
}

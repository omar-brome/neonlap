using System.Collections;
using System.Collections.Generic;
using NeonLap.Race;
using UnityEngine;
using UnityEngine.UI;

namespace NeonLap.UI
{
    /// <summary>
    /// Animated end-of-race stats: placement stars, XP/credits, best lap.
    /// </summary>
    public class RaceFinishScreenView : MonoBehaviour
    {
        [SerializeField] Text starOneText;
        [SerializeField] Text starTwoText;
        [SerializeField] Text starThreeText;
        [SerializeField] Text rewardsLineText;
        [SerializeField] Text placementLineText;

        static readonly Color StarLit = new(1f, 0.92f, 0.35f);
        static readonly Color StarDim = new(0.28f, 0.32f, 0.42f);
        static readonly Color RewardsColor = new(0.75f, 0.95f, 1f);

        RaceFinishSummary pendingSummary;
        Coroutine animateRoutine;

        public void Configure(Text starOne, Text starTwo, Text starThree, Text rewardsLine, Text placementLine)
        {
            starOneText = starOne;
            starTwoText = starTwo;
            starThreeText = starThree;
            rewardsLineText = rewardsLine;
            placementLineText = placementLine;
        }

        public void Prepare(RaceFinishSummary summary) => pendingSummary = summary;

        public void Present()
        {
            if (!pendingSummary.HasData)
            {
                HideStars();
                return;
            }

            if (animateRoutine != null)
                StopCoroutine(animateRoutine);

            animateRoutine = StartCoroutine(AnimateIn(pendingSummary));
        }

        public void Clear()
        {
            if (animateRoutine != null)
            {
                StopCoroutine(animateRoutine);
                animateRoutine = null;
            }

            pendingSummary = default;
            HideStars();
        }

        void HideStars()
        {
            SetStar(starOneText, false, 0f);
            SetStar(starTwoText, false, 0f);
            SetStar(starThreeText, false, 0f);

            if (rewardsLineText != null)
                rewardsLineText.text = string.Empty;

            if (placementLineText != null)
                placementLineText.text = string.Empty;
        }

        IEnumerator AnimateIn(RaceFinishSummary summary)
        {
            HideStars();

            if (placementLineText != null)
            {
                placementLineText.text = BuildPlacementLine(summary);
                placementLineText.color = RewardsColor;
            }

            if (rewardsLineText != null)
            {
                rewardsLineText.text = BuildRewardsLine(summary);
                rewardsLineText.color = new Color(1f, 0.92f, 0.35f);
            }

            yield return new WaitForSecondsRealtime(0.12f);

            var stars = summary.PlacementStars;
            if (stars >= 1)
            {
                SetStar(starOneText, true, 1f);
                yield return new WaitForSecondsRealtime(0.14f);
            }

            if (stars >= 2)
            {
                SetStar(starTwoText, true, 1f);
                yield return new WaitForSecondsRealtime(0.14f);
            }

            if (stars >= 3)
            {
                SetStar(starThreeText, true, 1f);
                yield return new WaitForSecondsRealtime(0.1f);
            }

            PulseStars(stars);
            animateRoutine = null;
        }

        void PulseStars(int litCount)
        {
            if (litCount >= 1)
                SetStar(starOneText, true, 1.12f);
            if (litCount >= 2)
                SetStar(starTwoText, true, 1.12f);
            if (litCount >= 3)
                SetStar(starThreeText, true, 1.12f);
        }

        static void SetStar(Text star, bool lit, float scale)
        {
            if (star == null)
                return;

            star.text = lit ? "★" : "☆";
            star.color = lit ? StarLit : StarDim;
            star.transform.localScale = Vector3.one * scale;
        }

        static string BuildPlacementLine(RaceFinishSummary summary)
        {
            var place = RaceUI.GetPlacementLabelPublic(summary.Placement);
            var starLabel = RaceFinishRewards.GetPlacementStarLine(summary.PlacementStars);
            return $"{place} Place  •  {starLabel}";
        }

        static string BuildRewardsLine(RaceFinishSummary summary)
        {
            var parts = new List<string>();

            if (summary.XpEarned > 0)
                parts.Add($"XP +{summary.XpEarned:N0}");

            if (summary.CreditsEarned > 0)
                parts.Add($"Credits +{summary.CreditsEarned:N0}");

            if (summary.BestLapTime > 0.05f)
                parts.Add($"Best Lap {RaceUI.FormatTimePublic(summary.BestLapTime)}");

            if (summary.RaceTime > 0.05f)
                parts.Add($"Race {RaceUI.FormatTimePublic(summary.RaceTime)}");

            if (summary.TotalXp > 0)
                parts.Add($"Total XP {summary.TotalXp:N0}");

            return parts.Count > 0 ? string.Join("  •  ", parts) : "Race complete";
        }
    }
}

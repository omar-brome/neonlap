using System.Collections.Generic;
using System.Text;

namespace NeonLap.Race
{
    public struct ScoreLine
    {
        public string Label;
        public int Amount;
        public int Count;

        public string Format()
        {
            if (Count > 1)
                return $"{FormatSigned(Amount)} {Label} x{Count}";

            return $"{FormatSigned(Amount)} {Label}";
        }

        static string FormatSigned(int amount)
        {
            return amount >= 0 ? $"+{amount:N0}" : $"{amount:N0}";
        }
    }

    public sealed class ScoreBreakdown
    {
        int progressPoints;
        int progressHits;
        int driftPoints;
        int driftTicks;
        int collisionPoints;
        int collisionHits;
        int overtakenPenalty;
        int overtakenHits;
        int lapLeadPoints;
        int lapLeadCount;
        int barrelRollPoints;
        int barrelRollHits;
        int nitroPickupPoints;
        int nitroPickupHits;
        int nitroBoostPoints;
        int nitroBoostTicks;
        int policeEscapePoints;
        int policeEscapeHits;
        int cleanLapPoints;
        int cleanLapCount;
        int barrelKnockPoints;
        int barrelKnockHits;
        int comboBonusPoints;
        int comboHits;
        int dailyBonusPoints;
        int shortcutPoints;
        bool shortcutBeatGhost;
        int resetPenaltyPoints;
        int resetPenaltyHits;
        int offTrackPenaltyPoints;
        int offTrackPenaltyHits;
        int finishBonus;

        public void Reset()
        {
            progressPoints = 0;
            progressHits = 0;
            driftPoints = 0;
            driftTicks = 0;
            collisionPoints = 0;
            collisionHits = 0;
            overtakenPenalty = 0;
            overtakenHits = 0;
            lapLeadPoints = 0;
            lapLeadCount = 0;
            barrelRollPoints = 0;
            barrelRollHits = 0;
            nitroPickupPoints = 0;
            nitroPickupHits = 0;
            nitroBoostPoints = 0;
            nitroBoostTicks = 0;
            policeEscapePoints = 0;
            policeEscapeHits = 0;
            cleanLapPoints = 0;
            cleanLapCount = 0;
            barrelKnockPoints = 0;
            barrelKnockHits = 0;
            comboBonusPoints = 0;
            comboHits = 0;
            dailyBonusPoints = 0;
            shortcutPoints = 0;
            shortcutBeatGhost = false;
            resetPenaltyPoints = 0;
            resetPenaltyHits = 0;
            offTrackPenaltyPoints = 0;
            offTrackPenaltyHits = 0;
            finishBonus = 0;
        }

        public void AddComboBonus(int bonus)
        {
            comboBonusPoints += bonus;
            comboHits++;
        }

        public void SetDailyBonus(int bonus)
        {
            dailyBonusPoints = bonus;
        }

        public void AddShortcut(int points, bool beatGhost)
        {
            shortcutPoints += points;
            shortcutBeatGhost = shortcutBeatGhost || beatGhost;
        }

        public void AddProgress(int points)
        {
            progressPoints += points;
            progressHits++;
        }

        public void AddDrift(int points)
        {
            driftPoints += points;
            driftTicks++;
        }

        public void AddCollision(int points)
        {
            collisionPoints += points;
            collisionHits++;
        }

        public void AddOvertakenPenalty(int penalty)
        {
            overtakenPenalty += penalty;
            overtakenHits++;
        }

        public void AddLapLead(int bonus)
        {
            lapLeadPoints += bonus;
            lapLeadCount++;
        }

        public void AddBarrelRoll(int points)
        {
            barrelRollPoints += points;
            barrelRollHits++;
        }

        public void AddNitroPickup(int points)
        {
            nitroPickupPoints += points;
            nitroPickupHits++;
        }

        public void AddNitroBoost(int points)
        {
            nitroBoostPoints += points;
            nitroBoostTicks++;
        }

        public void AddPoliceEscape(int points)
        {
            policeEscapePoints += points;
            policeEscapeHits++;
        }

        public void AddCleanLap(int bonus)
        {
            cleanLapPoints += bonus;
            cleanLapCount++;
        }

        public void AddBarrelKnock(int points)
        {
            barrelKnockPoints += points;
            barrelKnockHits++;
        }

        public void AddResetPenalty(int penalty)
        {
            resetPenaltyPoints += penalty;
            resetPenaltyHits++;
        }

        public void AddOffTrackPenalty(int penalty)
        {
            offTrackPenaltyPoints += penalty;
            offTrackPenaltyHits++;
        }

        public void SetFinishBonus(int bonus)
        {
            finishBonus = bonus;
        }

        public List<ScoreLine> BuildLines(int placement)
        {
            var lines = new List<ScoreLine>();

            if (progressPoints > 0)
                lines.Add(new ScoreLine { Label = "Progress", Amount = progressPoints, Count = progressHits });

            if (driftPoints > 0)
                lines.Add(new ScoreLine { Label = "Drift", Amount = driftPoints, Count = driftTicks });

            if (barrelRollPoints > 0)
                lines.Add(new ScoreLine { Label = "Barrel Roll", Amount = barrelRollPoints, Count = barrelRollHits });

            if (nitroPickupPoints > 0)
                lines.Add(new ScoreLine { Label = "Nitro Pickup", Amount = nitroPickupPoints, Count = nitroPickupHits });

            if (nitroBoostPoints > 0)
                lines.Add(new ScoreLine { Label = "Nitro Boost", Amount = nitroBoostPoints, Count = nitroBoostTicks });

            if (policeEscapePoints > 0)
                lines.Add(new ScoreLine { Label = "Police Escape", Amount = policeEscapePoints, Count = policeEscapeHits });

            if (cleanLapPoints > 0)
                lines.Add(new ScoreLine { Label = "Clean Lap", Amount = cleanLapPoints, Count = cleanLapCount });

            if (collisionPoints > 0)
                lines.Add(new ScoreLine { Label = "Rival Tap", Amount = collisionPoints, Count = collisionHits });

            if (barrelKnockPoints > 0)
                lines.Add(new ScoreLine { Label = "Barrel Knock", Amount = barrelKnockPoints, Count = barrelKnockHits });

            if (overtakenPenalty < 0)
                lines.Add(new ScoreLine { Label = "Overtaken", Amount = overtakenPenalty, Count = overtakenHits });

            if (lapLeadPoints > 0)
                lines.Add(new ScoreLine { Label = "Lap Lead", Amount = lapLeadPoints, Count = lapLeadCount });

            if (comboBonusPoints > 0)
                lines.Add(new ScoreLine { Label = "Combo 2×", Amount = comboBonusPoints, Count = comboHits });

            if (dailyBonusPoints > 0)
                lines.Add(new ScoreLine { Label = "Daily Challenge", Amount = dailyBonusPoints, Count = 1 });

            if (shortcutPoints > 0)
            {
                var label = shortcutBeatGhost ? "Shortcut (beat ghost)" : "Shortcut taken";
                lines.Add(new ScoreLine { Label = label, Amount = shortcutPoints, Count = 1 });
            }

            if (resetPenaltyPoints < 0)
                lines.Add(new ScoreLine { Label = "Reset", Amount = resetPenaltyPoints, Count = resetPenaltyHits });

            if (offTrackPenaltyPoints < 0)
                lines.Add(new ScoreLine { Label = "Off Track", Amount = offTrackPenaltyPoints, Count = offTrackPenaltyHits });

            if (finishBonus > 0)
            {
                var label = placement switch
                {
                    1 => "P1 Finish",
                    2 => "P2 Finish",
                    3 => "P3 Finish",
                    _ => "Finish"
                };
                lines.Add(new ScoreLine { Label = label, Amount = finishBonus, Count = 1 });
            }

            return lines;
        }

        public string BuildMultilineText(int placement)
        {
            var lines = BuildLines(placement);
            if (lines.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                    builder.Append('\n');
                builder.Append(lines[i].Format());
            }

            return builder.ToString();
        }
    }
}

using System;

namespace NeonLap.Environment
{
    public enum CrowdReactionKind
    {
        Mild = 0,
        Cheer = 1,
        Celebration = 2,
        Groan = 3,
    }

    /// <summary>
    /// Race-wide crowd reactions (audio + stadium visuals).
    /// </summary>
    public static class CrowdReactionHub
    {
        public static event Action<CrowdReactionKind> Reaction;

        public static void Emit(CrowdReactionKind kind) => Reaction?.Invoke(kind);

        public static CrowdReactionLevel ToVisualLevel(CrowdReactionKind kind) =>
            kind switch
            {
                CrowdReactionKind.Celebration => CrowdReactionLevel.Celebration,
                CrowdReactionKind.Cheer => CrowdReactionLevel.Strong,
                CrowdReactionKind.Groan => CrowdReactionLevel.Mild,
                _ => CrowdReactionLevel.Mild,
            };
    }
}

using System;

namespace NeonLap.Environment
{
    /// <summary>
    /// Lightweight incident bus for jumbotron ticker / stadium feed (broadcast tone).
    /// </summary>
    public static class StadiumIncidentHub
    {
        public static event Action<string> IncidentReported;

        public static void Report(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            IncidentReported?.Invoke(message.Trim().ToUpperInvariant());
        }
    }
}

using UnityEngine;

namespace NeonLap.Core
{
    public enum TrackWeatherChoice
    {
        Forecast = 0,
        ForceDry = 1,
        ForceRain = 2,
        ForceFog = 3,
        ForceSandstorm = 4,
    }

    public static class GameTrackOptions
    {
        const string ReverseKey = "NeonLap.Track.ReverseCircuit";
        const string NightKey = "NeonLap.Track.NightVariant";
        const string WeatherKey = "NeonLap.Track.WeatherChoice";

        public static bool ReverseCircuit { get; private set; }
        public static bool NightVariant { get; private set; }
        public static TrackWeatherChoice WeatherChoice { get; private set; } = TrackWeatherChoice.Forecast;

        public static void Load()
        {
            ReverseCircuit = PlayerPrefs.GetInt(ReverseKey, 0) == 1;
            NightVariant = PlayerPrefs.GetInt(NightKey, 0) == 1;
            WeatherChoice = (TrackWeatherChoice)Mathf.Clamp(
                PlayerPrefs.GetInt(WeatherKey, (int)TrackWeatherChoice.Forecast),
                0,
                (int)TrackWeatherChoice.ForceSandstorm);
        }

        public static void SetReverseCircuit(bool enabled)
        {
            ReverseCircuit = enabled;
            PlayerPrefs.SetInt(ReverseKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetNightVariant(bool enabled)
        {
            NightVariant = enabled;
            PlayerPrefs.SetInt(NightKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetWeatherChoice(TrackWeatherChoice choice)
        {
            WeatherChoice = choice;
            PlayerPrefs.SetInt(WeatherKey, (int)choice);
            PlayerPrefs.Save();
        }

        public static string GetWeatherDisplayName(TrackWeatherChoice choice)
        {
            return choice switch
            {
                TrackWeatherChoice.ForceDry => "DRY",
                TrackWeatherChoice.ForceRain => "RAIN",
                TrackWeatherChoice.ForceFog => "FOG",
                TrackWeatherChoice.ForceSandstorm => "SAND",
                _ => "FORECAST",
            };
        }

        public static string GetDirectionLabel() => ReverseCircuit ? "REVERSE" : "FORWARD";

        public static string FormatTrackName(string trackName)
        {
            if (string.IsNullOrWhiteSpace(trackName))
                return trackName;

            return ReverseCircuit ? $"{trackName} ↺" : trackName;
        }

        public static string GetReverseRaceHint()
        {
            return ReverseCircuit
                ? "Reverse circuit — run the layout counter-clockwise. Separate PBs and ghosts."
                : string.Empty;
        }

        public static string GetSummaryLine()
        {
            var parts = new System.Collections.Generic.List<string>();
            if (ReverseCircuit)
                parts.Add("REVERSE");
            if (NightVariant)
                parts.Add("NIGHT");
            if (WeatherChoice != TrackWeatherChoice.Forecast)
                parts.Add(GetWeatherDisplayName(WeatherChoice));
            return parts.Count > 0 ? string.Join("  •  ", parts) : "STANDARD";
        }
    }
}

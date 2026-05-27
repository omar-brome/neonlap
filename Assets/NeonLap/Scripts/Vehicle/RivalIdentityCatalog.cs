namespace NeonLap.Vehicle
{
    public static class RivalIdentityCatalog
    {
        public const int RivalCount = RivalRoster.Count;

        public static RivalIdentityProfile Get(int rivalIndex) => RivalRoster.GetProfile(rivalIndex);
    }
}

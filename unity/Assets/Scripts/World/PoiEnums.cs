namespace GameDev.World
{
    public enum PoiDiscoveryState
    {
        Unknown = 0,
        Hinted = 1,
        Marked = 2,
        Resolved = 3
    }

    public enum ActiveWindowClass
    {
        Brief = 0,
        Short = 1,
        Standard = 2,
        Long = 3,
        Persistent = 4
    }

    public enum PoiResolutionKind
    {
        None = 0,
        Participated = 1,
        Unattended = 2
    }
}

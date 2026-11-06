namespace GameDev.Core.Scenes
{
    /// <summary>
    /// A journey to another scene (17 I.2): the scene to load and the portal there the traveller arrives at. The
    /// arrival portal carries the pose, so going back through the same door returns the player where they entered.
    /// A shared scene that one scene file serves for many places (the slice's Dwelling) reads which place it is from
    /// <see cref="Context"/>.
    /// </summary>
    public readonly struct TravelRequest
    {
        public TravelRequest(string sceneName, string arrivalPortalId, string context = null)
        {
            SceneName = sceneName;
            ArrivalPortalId = arrivalPortalId;
            Context = context ?? string.Empty;
        }

        /// <summary>The scene's name as Build Settings list it.</summary>
        public string SceneName { get; }

        /// <summary>The id of the portal in that scene that places the traveller.</summary>
        public string ArrivalPortalId { get; }

        /// <summary>What the destination needs to know about where the traveller came from; empty for most journeys.</summary>
        public string Context { get; }

        public bool IsValid => !string.IsNullOrEmpty(SceneName) && !string.IsNullOrEmpty(ArrivalPortalId);

        public override string ToString() => $"{SceneName}#{ArrivalPortalId}";
    }
}

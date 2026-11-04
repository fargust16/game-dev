namespace GameDev.Core.Scenes
{
    /// <summary>
    /// A journey to another scene (17 I.2): the scene to load and the portal there the traveller arrives at. The
    /// arrival portal carries the pose, so going back through the same door returns the player where they entered.
    /// </summary>
    public readonly struct TravelRequest
    {
        public TravelRequest(string sceneName, string arrivalPortalId)
        {
            SceneName = sceneName;
            ArrivalPortalId = arrivalPortalId;
        }

        /// <summary>The scene's name as Build Settings list it.</summary>
        public string SceneName { get; }

        /// <summary>The id of the portal in that scene that places the traveller.</summary>
        public string ArrivalPortalId { get; }

        public bool IsValid => !string.IsNullOrEmpty(SceneName) && !string.IsNullOrEmpty(ArrivalPortalId);

        public override string ToString() => $"{SceneName}#{ArrivalPortalId}";
    }
}

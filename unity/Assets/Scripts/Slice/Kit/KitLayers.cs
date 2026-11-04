namespace GameDev.Slice.Kit
{
    /// <summary>The kit's layers (SLICE_MODULAR_KIT_STANDARD §5), in ProjectSettings at indices 8–14.</summary>
    public static class KitLayers
    {
        public const string Ground = "Ground";
        public const string Structure = "Structure";
        public const string Prop = "Prop";
        public const string Vegetation = "Vegetation";
        public const string Character = "Character";
        public const string Interactable = "Interactable";
        public const string NoGrid = "NoGrid";

        /// <summary>A block may stand on every kit layer except Character.</summary>
        public static bool IsBlockLayer(string layer)
        {
            return layer == Ground || layer == Structure || layer == Prop || layer == Vegetation
                   || layer == Interactable || layer == NoGrid;
        }

        /// <summary>Layers whose blocks are solid and must have a collider (§4); Interactable may be a trigger only.</summary>
        public static bool RequiresCollider(string layer)
        {
            return layer == Ground || layer == Structure || layer == Prop || layer == Vegetation;
        }
    }
}

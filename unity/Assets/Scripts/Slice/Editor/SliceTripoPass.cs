namespace GameDev.Slice.EditorTools
{
    /// <summary>
    /// Batch (owner, 2026-11-10): the owner's new Tripo pieces and people through every pass they touch, in order: the
    /// kit's changed wrappers, the seven interiors and the Dwellings with their pictures; the bronze statue among the
    /// ornaments; the mystic layer again, since its lamps now burn in the new street lamp's glass, and the lamps' views;
    /// then the people, whose walkers may meet the new lamps and the statue, and the interiors' pictures with them.
    /// -executeMethod GameDev.Slice.EditorTools.SliceTripoPass.Run
    /// </summary>
    public static class SliceTripoPass
    {
        public static void Run()
        {
            VeilmouthInteriors.EnrichAndRender();
            VeilmouthOrnaments.Place();
            VeilmouthMystic.PlaceAndRender();
            VeilmouthRenders.RenderLamps();
            SlicePeopleModels.Run();
            VeilmouthRenders.RenderInteriors();
        }

        /// <summary>
        /// The second run's fixes alone: the lamps' glowing glass made unlit, the people measured with the model's scale and
        /// kept in their first pose, and the walker that met a lamp planned again; then the views of both.
        /// -executeMethod GameDev.Slice.EditorTools.SliceTripoPass.Again
        /// </summary>
        public static void Again()
        {
            VeilmouthMystic.Place();
            VeilmouthRenders.RenderLamps();
            SlicePeopleModels.Run();
            VeilmouthRenders.RenderInteriors();
        }
    }
}

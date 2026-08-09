using UnityEngine;

namespace GameDev.World.Buildings.Shell
{
    [CreateAssetMenu(fileName = "EXT_Piece", menuName = "GameDev/Buildings/Exterior Piece Def")]
    public sealed class ExteriorPieceDef : ScriptableObject
    {
        public string Id;
        public ExteriorPieceCategory Category;
        public int WidthBays = 1;
        public int MinWealth = 1;
        public int MaxWealth = 5;
        public string StyleId = "VictIndustrial";
        public GameObject PrefabLod0;
        public GameObject PrefabLod1;
        public Color PlaceholderTint = Color.gray;
    }
}

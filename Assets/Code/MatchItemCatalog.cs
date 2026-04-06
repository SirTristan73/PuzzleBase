using UnityEngine;

[CreateAssetMenu(fileName = "MatchItemCatalog", menuName = "Match3/Catalog")]
public class MatchItemCatalog : ScriptableObject
{
    [System.Serializable]
    public struct ItemVisuals
    {
        public int Type;
        public Sprite Sprite;
        public Color Color; // На случай, если лень рисовать спрайты
    }

    public ItemVisuals[] Items;

    public ItemVisuals GetVisuals(int type) => 
        System.Array.Find(Items, x => x.Type == type);
}
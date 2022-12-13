using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Data", menuName = "2D/Tiles/Tile Wrapper", order = 1)]
public class TileWrapper : Tile
{
    [field: SerializeField]
    public int AdjacentTilesActionPointRegenAmount { get; private set; }
}
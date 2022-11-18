using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour
{
    public Grid grid;
    private Tilemap[] tilemaps;
    private Dictionary<Vector3Int, MapTile> map;

    Vector3Int minBounds = new Vector3Int();
    Vector3Int maxBounds = new Vector3Int();

    // Start is called before the first frame update
    void Start()
    {
        if (grid == null)
        {
            Debug.LogError("No Grid assigned to MapController!");
            return;
        }

        // First, grab the grid data and convert to map format
        tilemaps = grid.GetComponentsInChildren<Tilemap>();
        if (tilemaps == null)
        {
            Debug.LogError("Grid assigned to MapController is missing tilemap child!");
            return;
        }

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            minBounds.x = Mathf.Min(tilemap.cellBounds.xMin, minBounds.x);
            minBounds.y = Mathf.Min(tilemap.cellBounds.yMin, minBounds.y);
            minBounds.z = Mathf.Min(tilemap.cellBounds.zMin, minBounds.z);

            maxBounds.x = Mathf.Max(tilemap.cellBounds.xMax, maxBounds.x);
            maxBounds.y = Mathf.Max(tilemap.cellBounds.yMax, maxBounds.y);
            maxBounds.z = Mathf.Max(tilemap.cellBounds.zMax, maxBounds.z);
        }

        map = new Dictionary<Vector3Int, MapTile>();
        //[Mathf.Abs(maxBounds.x - minBounds.x), Mathf.Abs(maxBounds.y - minBounds.y), Mathf.Abs(maxBounds.z - minBounds.z)];

        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap tilemap = tilemaps[i];
            Tilemap nextTilemap = i < tilemaps.Length - 1 ? tilemaps[i + 1] : null;
            tilemap.CompressBounds();
            BoundsInt bounds = tilemap.cellBounds;


            foreach (Vector3Int position in bounds.allPositionsWithin)
            {
                if (!tilemap.HasTile(position))
                {
                    continue;
                }
                bool walkable = nextTilemap != null && !nextTilemap.HasTile(new Vector3Int(position.x, position.y, position.z + 1));
                MapTile tile = new MapTile(position, walkable, tilemap);
                map[new Vector3Int(position.x, position.y, position.z)] = tile;
            }
        }
       
    }

    private void Update()
    {
        foreach (Tilemap tilemap in tilemaps)
        {
            TilemapRenderer renderer = tilemap.gameObject.GetComponent<TilemapRenderer>();
            if (renderer == null || !renderer.material.HasProperty("_CutoutPositionLookup"))
            {
                continue;
            }
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBAFloat, -1, false);
            texture.filterMode = FilterMode.Point;
            Color color = new Color(mousePos.x, mousePos.y, mousePos.z);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            tilemap.gameObject.GetComponent<TilemapRenderer>().material.SetTexture("_CutoutPositionLookup", texture);
            tilemap.gameObject.GetComponent<TilemapRenderer>().material.SetFloat("_CutoutPositionLookupSize", 1);
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return grid.WorldToCell(worldPos);
    }

    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        return grid.CellToWorld(cellPos) + new Vector3(0f, 0.25f, 0f);
    }

    public List<MapTile> GetAllTilesInRange(Vector3Int position, int maxDistance, bool includeStart)
    {
        Dictionary<Vector3Int, MapTile> visited = new Dictionary<Vector3Int, MapTile>();
        DFS(visited, position, maxDistance);

        List<MapTile> result = new List<MapTile>();
        foreach (MapTile tile in visited.Values)
        {
            if (tile.GridPos == position && !includeStart)
            {
                continue;
            }
            result.Add(tile);
        }

        return result;
    }

    private void DFS(Dictionary<Vector3Int, MapTile> visited, Vector3Int position, int range)
    {
        visited.Add(position, GetTileAtGridCellPosition(position));
        if (range == 0)
        {
            return;
        }

        MapTile[] testTiles = new MapTile[4];
        testTiles[0] = GetTileAtGridCellPosition(position + Vector3Int.left);
        testTiles[1] = GetTileAtGridCellPosition(position + Vector3Int.down);
        testTiles[2] = GetTileAtGridCellPosition(position + Vector3Int.right);
        testTiles[3] = GetTileAtGridCellPosition(position + Vector3Int.up);

        foreach (MapTile test in testTiles)
        {
            if (test != null && !visited.ContainsKey(test.GridPos) && test.Walkable)
            {
                DFS(visited, test.GridPos, range - 1);
            }
        }
    }

    /***
     *  Given a tile map cell position, return the MapTile contained in the map 3d array 
     */
    private MapTile GetTileAtGridCellPosition(Vector3Int gridCellPosition)
    {
        return map.ContainsKey(gridCellPosition) ? map[gridCellPosition] : null;
    }
}

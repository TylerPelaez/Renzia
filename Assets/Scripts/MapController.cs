using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Util;

public class MapController : MonoBehaviour
{
    public Grid grid;
    public GameController gameController;
    public CameraController cameraController;
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
        
        cameraController.Bounds = GetWorldBounds();

        Unit[] units = FindObjectsOfType<Unit>();
        foreach (var unit in units)
        {
            InitializeUnit(unit);
        }
    }

    private void Update()
    {
        LinkedList<Unit> units = gameController.GetAllUnits();

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Texture2D texture = new Texture2D(units.Count + 1, 1, TextureFormat.RGBAFloat, -1, false);
        texture.filterMode = FilterMode.Point;
        Color color = new Color(mousePos.x, mousePos.y, mousePos.z);
        texture.SetPixel(0, 0, color);
        int index = 1;

        foreach (var unit in units)
        {
            var position = unit.transform.position;
            color = new Color(position.x, position.y + 0.25f, position.z);
            texture.SetPixel(index, 0, color);
            index++;
        }
        
        texture.Apply();
        
        foreach (Tilemap tilemap in tilemaps)
        {
            TilemapRenderer renderer = tilemap.gameObject.GetComponent<TilemapRenderer>();
            if (renderer == null || !renderer.material.HasProperty("_CutoutPositionLookup"))
            {
                continue;
            }


            tilemap.gameObject.GetComponent<TilemapRenderer>().material.SetTexture("_CutoutPositionLookup", texture);
            tilemap.gameObject.GetComponent<TilemapRenderer>().material.SetFloat("_CutoutPositionLookupSize", texture.width);
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        // TODO: This prevents us from being able to move units to tiles with z > 0
        return grid.WorldToCell(new Vector3(worldPos.x, worldPos.y, 0));
    }

    public Vector3 CellToWorld(Vector3Int cellPos)
    {
        return grid.CellToWorld(cellPos) + new Vector3(0f, 0.25f, 0f);
    }

    public List<MapTile> GetAllTilesInRange(Vector3Int cellPosition, int maxDistance, bool includeStart, bool includeOccupiedTiles)
    {
        Dictionary<Vector3Int, MapTile> visited = new Dictionary<Vector3Int, MapTile>();
        DFS(visited, cellPosition, maxDistance, includeOccupiedTiles);

        List<MapTile> result = new List<MapTile>();
        foreach (MapTile tile in visited.Values)
        {
            if (tile.GridPos == cellPosition && !includeStart)
            {
                continue;
            }
            result.Add(tile);
        }

        return result;
    }

    public List<MapTile> GetAllTilesInRange(Vector3 worldPosition, int maxDistance, bool includeStart = false, bool includeOccupiedTiles = false)
    {
        // TODO: Fiz Z coord here?
        Vector3Int cellPosition = WorldToCell(worldPosition);
        return GetAllTilesInRange(cellPosition, maxDistance, includeStart, includeOccupiedTiles);
    }

    private MapTile[] GetAdjacentTilesToPosition(Vector3Int cellPosition)
    {
        MapTile[] tiles = new MapTile[8];
        tiles[0] = GetTileAtGridCellPosition(cellPosition + Vector3Int.left);
        tiles[1] = GetTileAtGridCellPosition(cellPosition + Vector3Int.down);
        tiles[2] = GetTileAtGridCellPosition(cellPosition + Vector3Int.right);
        tiles[3] = GetTileAtGridCellPosition(cellPosition + Vector3Int.up);
        tiles[4] = GetTileAtGridCellPosition(cellPosition + Vector3Int.left + Vector3Int.down);
        tiles[5] = GetTileAtGridCellPosition(cellPosition + Vector3Int.right + Vector3Int.down);
        tiles[6] = GetTileAtGridCellPosition(cellPosition + Vector3Int.left + Vector3Int.up);
        tiles[7] = GetTileAtGridCellPosition(cellPosition + Vector3Int.right + Vector3Int.up);
        return tiles;
    }
    
    private void DFS(Dictionary<Vector3Int, MapTile> visited, Vector3Int position, int range, bool occupiedTilesAreWalkable)
    {
        if (!visited.ContainsKey(position))
        {
            visited.Add(position, GetTileAtGridCellPosition(position));
        }

        if (range == 0)
        {
            return;
        }

        MapTile[] testTiles = GetAdjacentTilesToPosition(position);

        foreach (MapTile test in testTiles)
        {
            if (test != null && test.Walkable && (occupiedTilesAreWalkable || test.CurrentUnit == null))
            {
                DFS(visited, test.GridPos, range - 1, occupiedTilesAreWalkable);
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

    private LinkedList<Vector3Int> RetracePath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        LinkedList<Vector3Int> path = new LinkedList<Vector3Int>();
        path.AddFirst(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.AddFirst(current);
        }

        return path;
    }

    private int GetCellDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    public List<Vector3Int> GetShortestPath(Vector3Int start, Vector3Int target)
    {
        var openSet = new PriorityQueue<Vector3Int, int>();
        var itemsInOpenSet = new HashSet<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int>();
        var fScore = new Dictionary<Vector3Int, int>();
        gScore[start] = 0;
        fScore[start] = GetCellDistance(start, target);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            itemsInOpenSet.Remove(current);
            if (current == target)
            {
                return new List<Vector3Int>(RetracePath(cameFrom, current));
            }

            var adjacentTiles = GetAdjacentTilesToPosition(current);
            foreach (var tile in adjacentTiles)
            {
                if (tile == null || !tile.Walkable)
                {
                    continue;
                }

                Vector3Int neighbor = tile.GridPos;

                var tentativeGScore = gScore[current] + 1;
                if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    // This path to neighbor is better than any previous one, record it.
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + GetCellDistance(neighbor, target);
                    if (!itemsInOpenSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor, fScore[neighbor]);
                        itemsInOpenSet.Add(neighbor);
                    }
                }
            }
        }

        // No path exists, apparently
        return null;
    }

    public bool CanUnitAttack(Unit attacker, Unit target)
    {
        Vector3Int attackerPosition = WorldToCell(attacker.transform.position);
        Vector3Int targetPosition = WorldToCell(target.transform.position);

        float distance = Mathf.Sqrt(Mathf.Pow(attackerPosition.x - targetPosition.x, 2) + Mathf.Pow(attackerPosition.y - targetPosition.y, 2));
        return distance < attacker.AttackRange && HasLineOfSight(attackerPosition, targetPosition);
    }

    public void InitializeUnit(Unit unit)
    {
        Vector3Int unitPosition = WorldToCell(unit.transform.position);
        MapTile tile = map[unitPosition];
        tile.CurrentUnit = unit;
        unit.CurrentTile = tile;
    }

    public void MoveUnit(Unit unit, Vector3Int newPosition)
    {
        MapTile currentTile = unit.CurrentTile;
        currentTile.CurrentUnit = null;
        MapTile newTile = map[newPosition];
        newTile.CurrentUnit = unit;
        unit.CurrentTile = newTile;
    }

    public void OnUnitDeath(Unit unit)
    {
        MapTile currentTile = unit.CurrentTile;
        currentTile.CurrentUnit = null;
        unit.CurrentTile = null;
    }

    public Bounds GetWorldBounds()
    {
        float minX = Single.MaxValue;
        float minY = Single.MaxValue;
        float maxX = Single.MinValue;
        float maxY = Single.MinValue;
        
        foreach (var tilemap in tilemaps)
        {
            Vector3 min = tilemap.transform.TransformPoint(tilemap.localBounds.min);
            Vector3 max = tilemap.transform.TransformPoint(tilemap.localBounds.max);
            if (min.x < minX)
            {
                minX = min.x;
            }
            if (min.y < minY)
            {
                minY = min.y;
            }
            if (max.x > maxX)
            {
                maxX = max.x;
            }
            if (max.y > maxY)
            {
                maxY = max.y;
            }
        }

        Bounds bounds = new Bounds();
        bounds.SetMinMax(new Vector3(minX, minY, 0), new Vector3(maxX, maxY, 0));
        return bounds;
    }
    
    public bool HasLineOfSight(Vector3Int origin, Vector3Int target)
    {
        List<Vector3Int> testPositions = new List<Vector3Int>();
        testPositions.Add(origin);
        testPositions.Add(origin + Vector3Int.left);
        testPositions.Add(origin + Vector3Int.right);
        testPositions.Add(origin + Vector3Int.up);
        testPositions.Add(origin + Vector3Int.down);

        List<Vector3> targetWorldPositions = new List<Vector3>();
        Vector3 centerPosition = CellToWorld(target);
        targetWorldPositions.Add(centerPosition + new Vector3(-0.5f, 0f, 0f));
        targetWorldPositions.Add(centerPosition + new Vector3(0.5f, 0f, 0f));
        targetWorldPositions.Add(centerPosition + new Vector3(0f, -0.25f, 0f));
        targetWorldPositions.Add(centerPosition + new Vector3(0f, 0.25f, 0f));

        // Iterate through adjacent tiles, and try to get 2 clear lines to the corners of the target tile.
        foreach (var position in testPositions)
        {
            MapTile tile = GetTileAtGridCellPosition(position);
            if (tile != null && !tile.Walkable)
            {
                continue;
            }

            Vector3 originWorldPosition = CellToWorld(position);

            int successCount = 0;
            foreach (var targetWorldPosition in targetWorldPositions)
            {
                float distance = Mathf.Sqrt(Mathf.Pow(originWorldPosition.x - targetWorldPosition.x, 2) +
                                            Mathf.Pow(originWorldPosition.y - targetWorldPosition.y, 2));


                // subtract .01 from distance to let us consider a corner shared with an obstructed cell. Without this, raycast would encounter a collision
                RaycastHit2D hit = Physics2D.Raycast(originWorldPosition, (targetWorldPosition - originWorldPosition).normalized, distance - 0.01f, LayerMask.GetMask("Terrain"));
                if (hit.collider == null)
                {
                    successCount++;
                }

                if (successCount >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

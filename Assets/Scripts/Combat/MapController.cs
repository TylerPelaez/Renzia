using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Util;
using Object = System.Object;

public class MapController : MonoBehaviour
{
    public Grid grid;
    public GameController gameController;
    public CameraController cameraController;
    public UIController uiController;
    
    private Tilemap[] tilemaps;
    private Dictionary<Vector3Int, MapTile> map;
    private Dictionary<Vector3Int, int> actionPointRegenPositions;

    public EventHandler OnMapStateChanged;

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

        actionPointRegenPositions = new Dictionary<Vector3Int, int>();

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
                
                
                TileBase tilemapTile = tilemap.GetTile(position);
                if (tilemapTile is TileWrapper wrapper)
                {
                    if (wrapper.AdjacentTilesActionPointRegenAmount > 0)
                    {
                        actionPointRegenPositions[position - Vector3Int.forward] = wrapper.AdjacentTilesActionPointRegenAmount;
                    }
                }
            }
        }
        
        ResetTileActionPoints();

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
        // I don't know why the offset is necessary, but it does help 
        return grid.CellToWorld(cellPos) + new Vector3(0f, 0.26f, 0f);
    }

    public List<MapTile> GetAllTilesInRange(Vector3Int cellPosition, int maxDistance, bool includeStart, bool includeOccupiedTiles)
    {
        // Dictionary<Vector3Int, MapTile> visited = new Dictionary<Vector3Int, MapTile>();
        // DFS(visited, cellPosition, maxDistance, includeOccupiedTiles);
        HashSet<Vector3Int> tilesInRange = BreadthFirstSearchInRange(GetTileAtGridCellPosition(cellPosition), maxDistance, includeOccupiedTiles);
        
        List<MapTile> result = new List<MapTile>();
        foreach (var position in tilesInRange)
        {
            if (position == cellPosition && !includeStart)
            {
                continue;
            }
            result.Add(GetTileAtGridCellPosition(position));
        }

        return result;
    }

    public List<MapTile> GetAllTilesInRange(Vector3 worldPosition, int maxDistance, bool includeStart = false, bool includeOccupiedTiles = false)
    {
        // TODO: Fiz Z coord here?
        Vector3Int cellPosition = WorldToCell(worldPosition);
        return GetAllTilesInRange(cellPosition, maxDistance, includeStart, includeOccupiedTiles);
    }
    
    public List<MapTile> GetAllTilesInAttackRange(Vector3Int cellPosition, int range)
    {
        List<MapTile> results = new List<MapTile>();
        
        for (int x = cellPosition.x - range; x <= cellPosition.x + range; x++)
        {
            for (int y = cellPosition.y - range; y <= cellPosition.y + range; y++)
            {
                Vector3Int testPosition = new Vector3Int(x, y, 0);
                if (testPosition == cellPosition)
                {
                    continue;
                }

                // TODO: Should this be a circular range check?
                MapTile tile = GetTileAtGridCellPosition(testPosition);
                if (tile != null && tile.Walkable)
                {
                    results.Add(tile);
                }
            }
        }

        return results;
    }

    public List<MapTile> GetAdjacentTilesToPosition(Vector3Int cellPosition)
    {
        List<MapTile> tiles = new List<MapTile>();
        MapTile tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.left);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.down);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.right);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.up);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.left + Vector3Int.down);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.right + Vector3Int.down);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.left + Vector3Int.up);
        if (tile != null)
            tiles.Add(tile);
        
        tile = GetTileAtGridCellPosition(cellPosition + Vector3Int.right + Vector3Int.up);
        if (tile != null)
            tiles.Add(tile);
        
        return tiles;
    }

    private HashSet<Vector3Int> BreadthFirstSearchInRange(MapTile origin, int range, bool occupiedTilesAreWalkable)
    {
        Queue<MapTile> currentDistanceQueue = new Queue<MapTile>();
        Queue<MapTile> nextDistanceQueue = new Queue<MapTile>();

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        int distanceFromOrigin = 0;
        
        currentDistanceQueue.Enqueue(origin);
        visited.Add(origin.GridPos);

        while (true)
        {
            while (currentDistanceQueue.Count > 0)
            {
                MapTile currentTile = currentDistanceQueue.Dequeue();
                
                List<MapTile> testTiles = GetAdjacentTilesToPosition(currentTile.GridPos);

                foreach (MapTile test in testTiles)
                {
                    if (test.Walkable && !visited.Contains(test.GridPos) && (occupiedTilesAreWalkable || test.CurrentUnit == null))
                    {
                        nextDistanceQueue.Enqueue(test);
                        visited.Add(test.GridPos);
                    }
                }
            }

            if (nextDistanceQueue.Count == 0)
            {
                break;
            }

            currentDistanceQueue = nextDistanceQueue;
            nextDistanceQueue = new Queue<MapTile>();
            distanceFromOrigin++;
            if (distanceFromOrigin == range)
            {
                break;
            }
        }

        return visited;
    }

    /***
     *  Given a tile map cell position, return the MapTile contained in the map 3d array 
     */
    public MapTile GetTileAtGridCellPosition(Vector3Int gridCellPosition)
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

    private float GetCellDistance(Vector3Int a, Vector3Int b)
    {
        //Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        return Vector3Int.Distance(a, b); 
    }

    public List<Vector3Int> GetShortestPath(Vector3Int start, Vector3Int target)
    {
        var openSet = new PriorityQueue<Vector3Int, float>();
        var itemsInOpenSet = new HashSet<Vector3Int>();
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int>();
        var fScore = new Dictionary<Vector3Int, float>();
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
                if (!tile.Walkable || tile.CurrentUnit != null)
                {
                    continue;
                }

                Vector3Int neighbor = tile.GridPos;

                var tentativeGScore = gScore[current] + 1;
                // We want to always prefer the lowest G Score. But when we have a tie, try to keep the path in a straight line
                if (gScore.ContainsKey(neighbor) && tentativeGScore > gScore[neighbor])
                {
                    continue;
                }


                if (gScore.ContainsKey(neighbor) && tentativeGScore == gScore[neighbor])
                {
                    if (!cameFrom.ContainsKey(current))
                    {
                        continue;
                    }

                    Vector3 previous = cameFrom[current];

                    int previousToCurrentDirection = DirectionUtil.GetAnimationSuffixForDirection(previous, current);
                    int currentToNeighborDirection = DirectionUtil.GetAnimationSuffixForDirection(current, neighbor);
                    
                    if (previousToCurrentDirection != currentToNeighborDirection)
                    {
                        continue;
                    }
                }
                
                
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

        // No path exists, apparently
        return null;
    }

    public bool CanUnitAttack(Unit attacker, Unit target, Weapon weaponUsed)
    {
        if (!attacker.CanUseWeapon(weaponUsed, gameController.RoundCount))
        {
            return false;
        }
        
        Vector3Int attackerPosition = WorldToCell(attacker.transform.position);
        Vector3Int targetPosition = WorldToCell(target.transform.position);

        List<MapTile> tilesInRange = GetAllTilesInAttackRange(attackerPosition, weaponUsed.Range);
        
        foreach (var tile in tilesInRange)
        {
            if (tile.GridPos == targetPosition)
            {
                return HasLineOfSight(attackerPosition, targetPosition);
            }
        }

        return false;
    }

    private void InitializeUnit(Unit unit)
    {
        Vector3Int unitPosition = WorldToCell(unit.transform.position);
        MapTile tile = map[unitPosition];
        tile.CurrentUnit = unit;
        unit.CurrentTile = tile;
        unit.OnTileEntered += OnUnitEnteredTile;
    }

    public void ResetTileActionPoints()
    {
        foreach (var (position, actionPointsGained) in actionPointRegenPositions)
        {
            List<MapTile> adjacentTiles = GetAdjacentTilesToPosition(position);
            Unit unitCollectingAPImmediately = null;
            foreach (var tile in adjacentTiles)
            {
                tile.ActionPointsGainedOnEntry = actionPointsGained;
                tile.ActionPointSource = map[position];

                if (tile.CurrentUnit != null && tile.CurrentUnit.Team == Team.PLAYER)
                {
                    unitCollectingAPImmediately = tile.CurrentUnit;
                }
            }

            if (map[position].CollectibleActionPointIndicator == null)
            {
                Vector3 worldPos = CellToWorld(position);
                map[position].CollectibleActionPointIndicator = uiController.SpawnCollectibleActionPointIndicator(actionPointsGained, worldPos);
            }

            if (unitCollectingAPImmediately != null)
            {
                OnUnitEnteredTile(unitCollectingAPImmediately, unitCollectingAPImmediately.transform.position);
            }
        }
    }

    public void MoveUnit(Unit unit, Vector3Int newPosition)
    {
        MapTile currentTile = unit.CurrentTile;
        currentTile.CurrentUnit = null;
        MapTile newTile = map[newPosition];
        newTile.CurrentUnit = unit;
        unit.CurrentTile = newTile;
        OnMapStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OnUnitEnteredTile(Object caller, Vector3 worldPosition)
    {
        if (caller is not Unit unit)
        {
            Debug.LogError("OnUnitEnteredTile did not receive Unit");
            return;
        }

        if (unit.Team != Team.PLAYER)
        {
            return;
        }

        Vector3Int mapPosition = WorldToCell(worldPosition);
        MapTile tile = GetTileAtGridCellPosition(mapPosition);
        if (tile.ActionPointsGainedOnEntry > 0)
        {
            int actionPointsGained = tile.ActionPointsGainedOnEntry;
            gameController.AddActionPoints(actionPointsGained);
            MapTile actionPointSource = tile.ActionPointSource;
            List<MapTile> adjacentMapTiles = GetAdjacentTilesToPosition(actionPointSource.GridPos);
            foreach (var adjacentTile in adjacentMapTiles)
            {
                adjacentTile.ActionPointsGainedOnEntry = 0;
            }
            
            Destroy(actionPointSource.CollectibleActionPointIndicator);
        }
    }

    public void OnUnitDeath(Unit unit)
    {
        MapTile currentTile = unit.CurrentTile;
        currentTile.CurrentUnit = null;
        unit.CurrentTile = null;
        OnMapStateChanged?.Invoke(this, EventArgs.Empty);
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

﻿using UnityEngine;
using UnityEngine.Tilemaps;

public class MapTile
{
    public Vector3Int GridPos { get; }
    public bool Walkable { get; }

    public Tilemap Tilemap { get; }
    
    public Unit CurrentUnit { get; set; }
    
    public int ActionPointsGainedOnEntry { get; set; }
    public MapTile ActionPointSource { get; set; }
    
    public GameObject CollectibleActionPointIndicator { get; set; }
    
    public MapTile(Vector3Int gridPos, bool walkable, Tilemap tilemap)
    {
        GridPos = gridPos;
        Walkable = walkable;
        Tilemap = tilemap;
    }
}
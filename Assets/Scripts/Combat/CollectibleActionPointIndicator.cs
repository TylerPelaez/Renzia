using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = System.Object;

public class CollectibleActionPointIndicator : MonoBehaviour
{
    public Vector3 defaultOffset;
    public GameObject textIndicatorPrefab;
    public GameObject adjacentTileIndicatorPrefab;
    private List<GameObject> adjacentTileIndicators;
    private MapController mapController;
    
    private GameObject instantiatedTextIndicator;
    
    // Start is called before the first frame update
    public void Initialize(int actionPoints, Vector3 startingWorldPosition, CanvasScaler scaler, RectTransform canvas, MapController map)
    {
        mapController = map;
        transform.position = new Vector3(startingWorldPosition.x, startingWorldPosition.y, 5f);
        instantiatedTextIndicator = Instantiate(textIndicatorPrefab, canvas);
        instantiatedTextIndicator.GetComponent<Popup>().Initialize("+" + actionPoints + " AP", startingWorldPosition + defaultOffset, scaler, canvas, Color.white);
        instantiatedTextIndicator.transform.SetAsFirstSibling();

        mapController.OnMapStateChanged += UpdateMovementIndicators;
        UpdateMovementIndicators(this, EventArgs.Empty);
    }


    private void UpdateMovementIndicators(Object caller, EventArgs args)
    {
        if (adjacentTileIndicators != null)
        {
            foreach (var indicator in adjacentTileIndicators)
            {
                Destroy(indicator);
            }
        }
        
        adjacentTileIndicators = new List<GameObject>();
        Vector3Int gridPos = mapController.WorldToCell(transform.position);
        List<MapTile> adjacentTiles = mapController.GetAdjacentTilesToPosition(gridPos);
        foreach (var tile in adjacentTiles)
        {
            if (tile.Walkable && tile.CurrentUnit == null)
            {
                GameObject indicator = Instantiate(adjacentTileIndicatorPrefab, mapController.CellToWorld(tile.GridPos), Quaternion.identity);
                adjacentTileIndicators.Add(indicator);
            }
        }
    }

    private void OnDestroy()
    {
        mapController.OnMapStateChanged -= UpdateMovementIndicators;
        if (instantiatedTextIndicator != null)
        {
            Destroy(instantiatedTextIndicator);
        }

        if (adjacentTileIndicators != null)
        {
            foreach (var indicator in adjacentTileIndicators)
            {
                Destroy(indicator);
            }
        }
    }
}

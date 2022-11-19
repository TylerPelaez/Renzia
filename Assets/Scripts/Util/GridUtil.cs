using UnityEngine;

namespace Util
{
    public static class GridUtil
    {
        public static Vector3 PositionToGridCellCenter(Grid grid, Vector3 position)
        {
            // instantiate movement indicator
            Vector3Int cellPos = grid.WorldToCell(new Vector3(position.x, position.y, 0));
            Vector3 result = grid.CellToWorld(cellPos);

            //// idk, the value from CellToWorld is offset by .25... 
            result.y += 0.25f;
            // return with original z... TODO: better way to figure out Z value??
            result.z = position.z;
            return result;
        }
    }
}
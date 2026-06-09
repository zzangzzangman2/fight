using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class TacticalGridOverlay : MonoBehaviour
{
    [SerializeField] private Vector2Int origin;
    [SerializeField] private Vector2Int size = new Vector2Int(16, 12);
    [SerializeField] private List<TacticalGridCellData> cells = new List<TacticalGridCellData>();

    private readonly Dictionary<Vector2Int, TacticalGridCellData> cellLookup =
        new Dictionary<Vector2Int, TacticalGridCellData>();

    public Vector2Int Origin => origin;
    public Vector2Int Size => size;
    public IReadOnlyList<TacticalGridCellData> Cells => cells;

    public void Configure(Vector2Int newOrigin, Vector2Int newSize)
    {
        origin = newOrigin;
        size = newSize;
        cells.Clear();
        cellLookup.Clear();
    }

    public void SetCell(TacticalGridCellData data)
    {
        if (data == null)
        {
            return;
        }

        if (cellLookup.TryGetValue(data.cell, out TacticalGridCellData existing))
        {
            int index = cells.IndexOf(existing);
            if (index >= 0)
            {
                cells[index] = data;
            }
        }
        else
        {
            cells.Add(data);
        }

        cellLookup[data.cell] = data;
    }

    public bool TryGetCell(Vector2Int cell, out TacticalGridCellData data)
    {
        if (cellLookup.Count != cells.Count)
        {
            RebuildLookup();
        }

        return cellLookup.TryGetValue(cell, out data);
    }

    public void CopyTo(BattleMapData mapData)
    {
        if (mapData == null)
        {
            return;
        }

        mapData.origin = origin;
        mapData.size = size;
        mapData.cells.Clear();

        foreach (TacticalGridCellData source in cells)
        {
            mapData.cells.Add(new BattleCellData
            {
                cell = source.cell,
                displayName = source.displayName,
                worldPosition = source.worldPosition,
                terrainType = source.terrainType,
                moveCost = source.moveCost,
                walkable = source.walkable,
                blocksMovement = source.blocksMovement,
                blocksLineOfSight = source.blocksLineOfSight,
                isChokePoint = source.isChokePoint,
                capacity = source.capacity,
                elevation = source.elevation,
                coverType = source.coverType,
                hazardType = source.hazardType,
                northEdge = source.northEdge,
                eastEdge = source.eastEdge,
                southEdge = source.southEdge,
                westEdge = source.westEdge,
                zoneId = source.zoneId,
                laneId = source.laneId,
                visualTileKey = source.visualTileKey,
                decorSetKey = source.decorSetKey,
            });
        }
    }

    private void RebuildLookup()
    {
        cellLookup.Clear();
        foreach (TacticalGridCellData data in cells)
        {
            if (data != null)
            {
                cellLookup[data.cell] = data;
            }
        }
    }
}

[Serializable]
public sealed class TacticalGridCellData
{
    public Vector2Int cell;
    public string displayName;
    public Vector3 worldPosition;
    public TerrainType terrainType = TerrainType.Stone;
    public int moveCost = 1;
    public bool walkable = true;
    public bool blocksMovement;
    public bool blocksLineOfSight;
    public bool isChokePoint;
    public int capacity = 1;
    public int elevation;
    public CoverType coverType;
    public HazardType hazardType;
    public EdgeType northEdge;
    public EdgeType eastEdge;
    public EdgeType southEdge;
    public EdgeType westEdge;
    public string zoneId;
    public string laneId;
    public string visualTileKey;
    public string decorSetKey;
}
}

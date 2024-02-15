using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BlockManager : MonoBehaviour
{
    public GameObject BeforeMirror;
    public GameObject InMirror;
    public GameObject BehindMirror;
    public MirrorPlane Mirror;
    public UnityEvent OnBlockGraphRebuilt = new();

    // 保存所有方块的世界坐标
    [NonSerialized] public HashSet<Vector3Int> AllBlocksInWorld = new();

    public static BlockManager Instance { get; private set; }

    private void Start()
    {
        RebuildBlockGraph();
        Instance = this;
    }

    private uint _disableInteract = 0;

    public void EnableInteract() => _disableInteract--;

    public void DisableInteract() => _disableInteract++;

    public bool CanInteract => _disableInteract == 0;

    public void RebuildBlockGraph()
    {
        AllBlocksInWorld.Clear();

        Block[] beforeMirrorBlocks = BeforeMirror.GetComponentsInChildren<Block>();
        Block[] inMirrorBlocks = InMirror.GetComponentsInChildren<Block>();
        Block[] behindMirrorBlocks = BehindMirror.GetComponentsInChildren<Block>();

        Matrix4x4 viewMat = Camera.main.worldToCameraMatrix;
        Vector2 axisX = viewMat.MultiplyVector(Vector3.right);
        Vector2 axisZ = viewMat.MultiplyVector(Vector3.forward);
        Vector2 origin = viewMat.MultiplyPoint(beforeMirrorBlocks[0].UpperCenter); // 原点随便找一个
        Debug.Log($"Origin Block {beforeMirrorBlocks[0].transform.position.RoundToInt()}", beforeMirrorBlocks[0]);

        // View Space 到屏幕上斜坐标系的过渡矩阵
        float det = axisX.x * axisZ.y - axisZ.x * axisX.y;
        Matrix4x4 transientMat = new Matrix4x4(
            new Vector4(axisZ.y, -axisX.y) / det,
            new Vector4(-axisZ.x, axisX.x) / det,
            Vector4.zero,
            Vector4.zero);

        Dictionary<Vector2Int, BlockGroup> bMap = new();

        foreach (var mapBlock in beforeMirrorBlocks.Concat(inMirrorBlocks).Concat(behindMirrorBlocks))
        {
            Vector3 vp = viewMat.MultiplyPoint(mapBlock.UpperCenter);
            Vector2 p = transientMat * ((Vector2)vp - origin);

            int x = Mathf.RoundToInt(p.x);
            int y = Mathf.RoundToInt(p.y);
            Vector2Int key = new(x, y);

            AllBlocksInWorld.Add(mapBlock.transform.position.RoundToInt());

            if (!bMap.TryGetValue(key, out BlockGroup group))
            {
                group = new BlockGroup();
                bMap[key] = group;
            }

            mapBlock.ProjectedXY = key;
            mapBlock.ViewSpaceUpperCenterZ = vp.z;
            group.AddBlock(mapBlock);
        }

        Vector2 planeMaxPoint = viewMat.MultiplyPoint(Mirror.PlaneMaxPosition);
        planeMaxPoint = transientMat * (planeMaxPoint - origin);
        int lineVMaxB = Mathf.RoundToInt(planeMaxPoint.y + planeMaxPoint.x); // 镜子左右两条边界线的截距（较大者）
        float lineHMaxB = Mathf.RoundToInt(planeMaxPoint.y + 0.5f) - 0.5f; // 镜子上下两条边界线的截距（较大者）
        Debug.Log($"VMax: {lineVMaxB}");
        Debug.Log($"HMax: {lineHMaxB}");

        CullBlocksByMirror(lineVMaxB, lineHMaxB, beforeMirrorBlocks, BlockCategory.BeforeMirror);
        CullBlocksByMirror(lineVMaxB, lineHMaxB, inMirrorBlocks, BlockCategory.InMirror);
        CullBlocksByMirror(lineVMaxB, lineHMaxB, behindMirrorBlocks, BlockCategory.BehindMirror);
        CullBlocksByViewSpaceZ(bMap);
        ConnectBlocks(bMap);

        OnBlockGraphRebuilt.Invoke();
    }

    private void CullBlocksByMirror(int lineVMaxB, float lineHMaxB, Block[] blocks, BlockCategory cat)
    {
        foreach (var mapBlock in blocks)
        {
            mapBlock.ProjectedShapes = BlockProjectedShapes.None;

            if (cat == BlockCategory.BeforeMirror)
            {
                mapBlock.ProjectedShapes = BlockProjectedShapes.FullHexagon;
            }
            else if (cat == BlockCategory.InMirror)
            {
                if (IsInMirror(mapBlock.ProjectedXY + new Vector2(1 / 6f, 1 / 6f), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.MiddleUpperTriangle;
                }

                if (IsInMirror(mapBlock.ProjectedXY - new Vector2(1 / 6f, 1 / 6f), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.LeftUpperTriangle;
                }

                if (IsInMirror(mapBlock.ProjectedXY - new Vector2(1 / 6f - 1, 1 / 6f), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.RightUpperTriangle;
                }

                if (IsInMirror(mapBlock.ProjectedXY + new Vector2(1 / 6f, 1 / 6f - 1), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.LeftLowerTriangle;
                }

                if (IsInMirror(mapBlock.ProjectedXY + new Vector2(1 / 6f + 1, 1 / 6f - 1), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.RightLowerTriangle;
                }

                if (IsInMirror(mapBlock.ProjectedXY - new Vector2(1 / 6f - 1, 1 / 6f + 1), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.MiddleLowerTriangle;
                }
            }
            else
            {
                if (!IsInMirror(mapBlock.ProjectedXY + new Vector2(1 / 6f, 1 / 6f), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.MiddleUpperTriangle;
                }

                if (!IsInMirror(mapBlock.ProjectedXY - new Vector2(1 / 6f, 1 / 6f), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.LeftUpperTriangle;
                }

                if (!IsInMirror(mapBlock.ProjectedXY - new Vector2(1 / 6f - 1, 1 / 6f), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.RightUpperTriangle;
                }

                if (!IsInMirror(mapBlock.ProjectedXY + new Vector2(1 / 6f, 1 / 6f - 1), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.LeftLowerTriangle;
                }

                if (!IsInMirror(mapBlock.ProjectedXY + new Vector2(1 / 6f + 1, 1 / 6f - 1), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.RightLowerTriangle;
                }

                if (!IsInMirror(mapBlock.ProjectedXY - new Vector2(1 / 6f - 1, 1 / 6f + 1), lineVMaxB, lineHMaxB))
                {
                    mapBlock.ProjectedShapes |= BlockProjectedShapes.MiddleLowerTriangle;
                }
            }
        }
    }

    private bool IsInMirror(Vector2 point, int lineVMaxB, float lineHMaxB)
    {
        float lineHMinB = lineHMaxB - Mirror.Height;
        if (point.y < lineHMinB || point.y > lineHMaxB)
        {
            return false;
        }

        int lineVMinB = lineVMaxB - Mirror.Width;
        float pos1 = -point.x - point.y + lineVMinB;
        float pos2 = -point.x - point.y + lineVMaxB;
        return pos1 * pos2 < 0;
    }

    private void CullBlocksByViewSpaceZ(Dictionary<Vector2Int, BlockGroup> bMap)
    {
        Dictionary<Vector2Int, float> zMapLower = new();
        Dictionary<Vector2Int, float> zMapUpper = new();

        foreach (var block in bMap.Values.SelectMany(g => g))
        {
            if ((block.ProjectedShapes & BlockProjectedShapes.LeftUpperTriangle) != 0)
            {
                SetZMap(zMapLower, block.ProjectedXY, block.ViewSpaceUpperCenterZ);
            }

            if ((block.ProjectedShapes & BlockProjectedShapes.MiddleUpperTriangle) != 0)
            {
                SetZMap(zMapUpper, block.ProjectedXY, block.ViewSpaceUpperCenterZ);
            }

            if ((block.ProjectedShapes & BlockProjectedShapes.RightUpperTriangle) != 0)
            {
                SetZMap(zMapLower, block.ProjectedXY + new Vector2Int(1, 0), block.ViewSpaceUpperCenterZ);
            }

            if ((block.ProjectedShapes & BlockProjectedShapes.LeftLowerTriangle) != 0)
            {
                SetZMap(zMapUpper, block.ProjectedXY + new Vector2Int(0, -1), block.ViewSpaceUpperCenterZ);
            }

            if ((block.ProjectedShapes & BlockProjectedShapes.MiddleLowerTriangle) != 0)
            {
                SetZMap(zMapLower, block.ProjectedXY + new Vector2Int(1, -1), block.ViewSpaceUpperCenterZ);
            }

            if ((block.ProjectedShapes & BlockProjectedShapes.RightLowerTriangle) != 0)
            {
                SetZMap(zMapUpper, block.ProjectedXY + new Vector2Int(1, -1), block.ViewSpaceUpperCenterZ);
            }
        }

        foreach (var block in bMap.Values.SelectMany(g => g))
        {
            if ((block.ProjectedShapes & BlockProjectedShapes.LeftUpperTriangle) != 0 && block.ViewSpaceUpperCenterZ < zMapLower[block.ProjectedXY])
            {
                block.ProjectedShapes &= ~BlockProjectedShapes.LeftUpperTriangle;
            }

            if ((block.ProjectedShapes & BlockProjectedShapes.MiddleUpperTriangle) != 0 && block.ViewSpaceUpperCenterZ < zMapUpper[block.ProjectedXY])
            {
                block.ProjectedShapes &= ~BlockProjectedShapes.MiddleUpperTriangle;
            }
        }
    }

    private static void SetZMap(Dictionary<Vector2Int, float> zMap, Vector2Int key, float z)
    {
        if (!zMap.TryGetValue(key, out float depth))
        {
            zMap[key] = z;
        }
        else
        {
            zMap[key] = Mathf.Max(depth, z);
        }
    }

    private void ConnectBlocks(Dictionary<Vector2Int, BlockGroup> bMap)
    {
        foreach (var kvp in bMap)
        {
            kvp.Value.ClearAdjBlocks();
            if (!kvp.Value.IsWalkable)
            {
                continue;
            }

            BlockGroup adjBlocks;

            // 右
            if (bMap.TryGetValue(kvp.Key + Vector2Int.right, out adjBlocks) && adjBlocks.IsWalkable)
            {
                kvp.Value.AddAdjBlocks(adjBlocks);
            }

            // 左
            if (bMap.TryGetValue(kvp.Key + Vector2Int.left, out adjBlocks) && adjBlocks.IsWalkable)
            {
                kvp.Value.AddAdjBlocks(adjBlocks);
            }

            // 前
            if (bMap.TryGetValue(kvp.Key + Vector2Int.up, out adjBlocks) && adjBlocks.IsWalkable)
            {
                kvp.Value.AddAdjBlocks(adjBlocks);
            }

            // 后
            if (bMap.TryGetValue(kvp.Key + Vector2Int.down, out adjBlocks) && adjBlocks.IsWalkable)
            {
                kvp.Value.AddAdjBlocks(adjBlocks);
            }
        }
    }
}

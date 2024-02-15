using System.Collections;
using System.Collections.Generic;

public class BlockGroup : IEnumerable<Block>
{
    private readonly List<Block> _blocks = new();

    public bool IsWalkable
    {
        get
        {
            BlockProjectedShapes shapes = BlockProjectedShapes.None;

            foreach (var b in _blocks)
            {
                shapes |= b.ProjectedShapes;

                if ((shapes & BlockProjectedShapes.Walkable) == BlockProjectedShapes.Walkable)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void AddBlock(Block block)
    {
        _blocks.Add(block);
    }

    public void ClearAdjBlocks()
    {
        foreach (var block in _blocks)
        {
            block.AdjBlocks.Clear();
        }
    }

    public void AddAdjBlocks(BlockGroup adjBlocks)
    {
        foreach (var block in _blocks)
        {
            foreach (var adjBlock in adjBlocks._blocks)
            {
                if ((adjBlock.ProjectedShapes & BlockProjectedShapes.Walkable) != 0)
                {
                    block.AdjBlocks.Add(adjBlock);
                }
            }
        }
    }

    public List<Block>.Enumerator GetEnumerator()
    {
        return _blocks.GetEnumerator();
    }

    IEnumerator<Block> IEnumerable<Block>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

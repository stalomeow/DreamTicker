using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    public Vector3 PositionOffset = new Vector3(0, 1.5f, 0);
    public float MoveTimePerBlock = 0.1f;
    public Trail PathDisplayTrail;
    public Block CurrentBlock;
    public GameObject GoalHintPrefab;
    public Block[] GoalBlocks;

    private bool _isMoving = false;
    private int _moveGoalIndex = 0;
    private readonly HashSet<Block> _moveVis = new();
    private readonly Queue<Block> _moveQueue = new();
    private readonly Dictionary<Block, Block> _moveNext = new();

    private void Start()
    {
        SetPlayerPosition(CurrentBlock.transform.position);
        TryPlaceGoalHint();
    }

    public void TryMove()
    {
        if (_isMoving || _moveGoalIndex >= GoalBlocks.Length)
        {
            return;
        }

        _moveVis.Clear();
        _moveQueue.Clear();
        _moveNext.Clear();

        // 倒过来 BFS，找最短路径
        bool ok = false;
        Block goal = GoalBlocks[_moveGoalIndex];
        _moveQueue.Enqueue(goal);

        while (_moveQueue.TryDequeue(out Block top))
        {
            _moveVis.Add(top);

            if (top == CurrentBlock)
            {
                ok = true;
                break;
            }

            foreach (var adj in top.AdjBlocks)
            {
                if (!_moveVis.Contains(adj))
                {
                    _moveQueue.Enqueue(adj);
                    _moveNext[adj] = top;
                }
            }
        }

        if (ok)
        {
            _moveGoalIndex++;
            StartCoroutine(Move(_moveNext, goal));
        }
    }

    private IEnumerator Move(Dictionary<Block, Block> next, Block goal)
    {
        _isMoving = true;
        BlockManager.Instance.DisableInteract();

        yield return StartCoroutine(PathDisplayTrail.Move(next, CurrentBlock, goal));

        while (CurrentBlock != goal)
        {
            Block block = next[CurrentBlock];

            float time = 0;
            while (time < MoveTimePerBlock)
            {
                float p = time / MoveTimePerBlock;
                Vector3 pos = Vector3.Lerp(CurrentBlock.transform.position, block.transform.position, p);
                SetPlayerPosition(pos);
                time += Time.deltaTime;
                yield return null;
            }

            CurrentBlock = block;
        }

        for (int i = 0; i < CurrentBlock.transform.childCount; i++)
        {
            Destroy(CurrentBlock.transform.GetChild(i).gameObject);
        }

        TryPlaceGoalHint();
        _isMoving = false;
        BlockManager.Instance.EnableInteract();

        // 再递归一次，看看能不能再移动
        TryMove();
    }

    private void SetPlayerPosition(Vector3 blockPosition)
    {
        transform.position = blockPosition + PositionOffset;
    }

    private void TryPlaceGoalHint()
    {
        if (_moveGoalIndex >= GoalBlocks.Length)
        {
            return;
        }

        Transform goal = GoalBlocks[_moveGoalIndex].transform;
        Instantiate(GoalHintPrefab, goal, false);
    }
}

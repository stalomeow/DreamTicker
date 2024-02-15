using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class MovableBlock : MonoBehaviour
{
    [Serializable]
    public class MoveConfig
    {
        public Block Block;
        public int MoveXMin;
        public int MoveXMax;
        public int MoveZMin;
        public int MoveZMax;
    }

    public MovableBlock Mirrored;
    public bool IsPassive;

    [Header("Move")]
    public float MoveSpeed = 0.05f;
    public MoveConfig[] MoveConfigs;

    [Header("MouseHover Highlight")]
    [Range(0, 1)] public float HighlightMix = 0.2f;

    private Vector2? _mousePos = null;
    private readonly HashSet<Vector3Int> _worldBlocks = new();
    private readonly int _highlightMixPropId = Shader.PropertyToID("_HighlightMix");

    private void Start()
    {
        if (!IsPassive)
        {
            foreach (var config in MoveConfigs)
            {
                var block = config.Block;
                block.MouseDownEvent += _ => OnMouseDownHandler();
                block.MouseDragEvent += _ => OnMouseDragHandler();
                block.MouseUpEvent += _ => OnMouseUpHandler();
                block.MouseEnterEvent += _ => SetHighlightMix(HighlightMix);
                block.MouseExitEvent += _ => SetHighlightMix(0);
            }
        }
    }

    private void SetHighlightMix(float value)
    {
        foreach (var config in MoveConfigs)
        {
            config.Block.Renderer.material.SetFloat(_highlightMixPropId, value);
        }

        foreach (var config in Mirrored.MoveConfigs)
        {
            config.Block.Renderer.material.SetFloat(_highlightMixPropId, value);
        }
    }

    private void OnMouseDownHandler()
    {
        if (!BlockManager.Instance.CanInteract)
        {
            return;
        }

        BlockManager.Instance.DisableInteract();

        _mousePos = Input.mousePosition;
        _worldBlocks.Clear();
        _worldBlocks.UnionWith(BlockManager.Instance.AllBlocksInWorld);

        // 移除自己下面所有方块
        foreach (var config in MoveConfigs)
        {
            _worldBlocks.Remove(config.Block.transform.position.RoundToInt());
        }
    }

    private void OnMouseDragHandler()
    {
        if (_mousePos == null)
        {
            return;
        }

        float move = MoveSpeed * Math.Sign(Input.mousePosition.x - _mousePos.Value.x);

        if (CanMove(move))
        {
            foreach (var config in MoveConfigs)
            {
                Vector3 pos = config.Block.transform.localPosition;
                pos.x = Mathf.Clamp(pos.x + move, config.MoveXMin, config.MoveXMax);
                pos.z = Mathf.Clamp(pos.z + move, config.MoveZMin, config.MoveZMax);
                config.Block.transform.localPosition = pos;
            }

            foreach (var config in Mirrored.MoveConfigs)
            {
                Vector3 pos = config.Block.transform.localPosition;
                pos.x = Mathf.Clamp(pos.x + move, config.MoveXMin, config.MoveXMax);
                pos.z = Mathf.Clamp(pos.z - move, config.MoveZMin, config.MoveZMax);
                config.Block.transform.localPosition = pos;
            }
        }

        _mousePos = Input.mousePosition;
    }

    private bool CanMove(float move)
    {
        // 检查移动后周围是否有方块。有的话就没法移动
        foreach (var config in MoveConfigs)
        {
            Vector3 pos = config.Block.transform.localPosition;
            pos.x = Mathf.Clamp(pos.x + move, config.MoveXMin, config.MoveXMax);
            pos.z = Mathf.Clamp(pos.z + move, config.MoveZMin, config.MoveZMax);

            if (config.Block.transform.parent != null)
            {
                pos = config.Block.transform.parent.TransformPoint(pos);
            }

            if (_worldBlocks.Contains(pos.FloorToInt()))
            {
                return false;
            }

            if (_worldBlocks.Contains(pos.CeilToInt()))
            {
                return false;
            }
        }

        return true;
    }

    private void OnMouseUpHandler()
    {
        if (_mousePos == null)
        {
            return;
        }

        foreach (var config in MoveConfigs)
        {
            Vector3 pos = config.Block.transform.localPosition;
            pos.x = Mathf.Clamp(Mathf.RoundToInt(pos.x), config.MoveXMin, config.MoveXMax);
            pos.z = Mathf.Clamp(Mathf.RoundToInt(pos.z), config.MoveZMin, config.MoveZMax);
            config.Block.transform.localPosition = pos;
        }

        foreach (var config in Mirrored.MoveConfigs)
        {
            Vector3 pos = config.Block.transform.localPosition;
            pos.x = Mathf.Clamp(Mathf.RoundToInt(pos.x), config.MoveXMin, config.MoveXMax);
            pos.z = Mathf.Clamp(Mathf.RoundToInt(pos.z), config.MoveZMin, config.MoveZMax);
            config.Block.transform.localPosition = pos;
        }

        _mousePos = null;
        BlockManager.Instance.EnableInteract();
        BlockManager.Instance.RebuildBlockGraph();
    }
}

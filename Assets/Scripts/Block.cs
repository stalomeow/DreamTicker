using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(MeshRenderer))]
public class Block : MonoBehaviour
{
    [Header("States (DO NOT EDIT)")]

    public Vector2Int ProjectedXY;
    public float ViewSpaceUpperCenterZ;
    public BlockProjectedShapes ProjectedShapes;
    public List<Block> AdjBlocks = new();

    [Header("Events")]

    [SerializeField] private UnityEvent<Block> _onMouseDown = new();
    [SerializeField] private UnityEvent<Block> _onMouseUp = new();
    [SerializeField] private UnityEvent<Block> _onMouseDrag = new();
    [SerializeField] private UnityEvent<Block> _onMouseEnter = new();
    [SerializeField] private UnityEvent<Block> _onMouseExit = new();

    private MeshRenderer _renderer;
    public MeshRenderer Renderer => _renderer ??= GetComponent<MeshRenderer>();

    public Vector3 UpperCenter => transform.position + Vector3.up * 0.5f;

    public event UnityAction<Block> MouseDownEvent
    {
        add => _onMouseDown.AddListener(value);
        remove => _onMouseDown.RemoveListener(value);
    }

    public event UnityAction<Block> MouseUpEvent
    {
        add => _onMouseUp.AddListener(value);
        remove => _onMouseUp.RemoveListener(value);
    }

    public event UnityAction<Block> MouseDragEvent
    {
        add => _onMouseDrag.AddListener(value);
        remove => _onMouseDrag.RemoveListener(value);
    }

    public event UnityAction<Block> MouseEnterEvent
    {
        add => _onMouseEnter.AddListener(value);
        remove => _onMouseEnter.RemoveListener(value);
    }

    public event UnityAction<Block> MouseExitEvent
    {
        add => _onMouseExit.AddListener(value);
        remove => _onMouseExit.RemoveListener(value);
    }

    private void OnMouseDown() => _onMouseDown.Invoke(this);

    private void OnMouseUp() => _onMouseUp.Invoke(this);

    private void OnMouseDrag() => _onMouseDrag.Invoke(this);

    private void OnMouseEnter() => _onMouseEnter.Invoke(this);

    private void OnMouseExit() => _onMouseExit.Invoke(this);

    private void OnDrawGizmos()
    {
        Color color = Gizmos.color;

        if (!Application.isPlaying)
        {
            Gizmos.color = Color.grey;
        }
        else if ((ProjectedShapes & BlockProjectedShapes.Walkable) == BlockProjectedShapes.Walkable)
        {
            Gizmos.color = Color.green;
        }
        else if ((ProjectedShapes & BlockProjectedShapes.Walkable) != 0)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            return;
        }

        Gizmos.DrawSphere(UpperCenter, 0.1f);
        Gizmos.color = Color.green;
        foreach (var adj in AdjBlocks)
        {
            Gizmos.DrawLine(UpperCenter, adj.UpperCenter);
        }

        Gizmos.color = color;
    }
}

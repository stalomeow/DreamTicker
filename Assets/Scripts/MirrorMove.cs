using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class MirrorMove : MonoBehaviour
{
    public MirrorPlane Plane;
    public float MoveSpeed = 0.05f;
    [Range(0, 1)] public float HighlightMix = 0.2f;

    private Vector2? _mousePos = null;
    private MeshRenderer _renderer;
    private readonly int _highlightMixPropId = Shader.PropertyToID("_HighlightMix");

    private void Start()
    {
        _renderer = GetComponent<MeshRenderer>();
    }

    private void OnMouseEnter()
    {
        _renderer.material.SetFloat(_highlightMixPropId, HighlightMix);
    }

    private void OnMouseExit()
    {
        _renderer.material.SetFloat(_highlightMixPropId, 0);
    }

    private void OnMouseDown()
    {
        if (!BlockManager.Instance.CanInteract)
        {
            return;
        }

        _mousePos = Input.mousePosition;
        BlockManager.Instance.DisableInteract();
    }

    private void OnMouseDrag()
    {
        if (_mousePos == null)
        {
            return;
        }

        float move = MoveSpeed * Math.Sign(Input.mousePosition.x - _mousePos.Value.x);
        Vector3 pos = Plane.transform.localPosition;
        pos.x = Mathf.Clamp(pos.x + move, Plane.MoveMinX, Plane.MoveMaxX);
        Plane.transform.localPosition = pos;

        _mousePos = Input.mousePosition;
    }

    private void OnMouseUp()
    {
        if (_mousePos == null)
        {
            return;
        }

        Vector3 pos = Plane.transform.localPosition;
        pos.x = Mathf.Clamp(Mathf.RoundToInt(pos.x + 0.5f) - 0.5f, Plane.MoveMinX, Plane.MoveMaxX);
        Plane.transform.localPosition = pos;

        _mousePos = null;
        BlockManager.Instance.EnableInteract();
        BlockManager.Instance.RebuildBlockGraph();
    }
}

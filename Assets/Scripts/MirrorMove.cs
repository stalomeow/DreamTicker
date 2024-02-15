using System;
using UnityEngine;

public class MirrorMove : MonoBehaviour
{
    public MirrorPlane Plane;
    public float MoveSpeed = 0.05f;
    private Vector2? _mousePos = null;

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

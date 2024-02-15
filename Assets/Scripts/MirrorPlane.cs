using UnityEngine;
using UnityEngine.Serialization;

public class MirrorPlane : MonoBehaviour
{
    [Header("Move")]

    [FormerlySerializedAs("MinX")] public float MoveMinX = 3;
    [FormerlySerializedAs("MaxX")] public float MoveMaxX = 7;

    [Header("Shape")]

    [Min(1)] public int Width = 7;
    [Min(1)] public int Height = 12;

    private void OnValidate()
    {
        // Unity 的 Plane 是 10x10 的
        transform.localScale = new Vector3(Width * 0.1f, Height * 0.1f, 1);
    }

    public Vector3 PlaneMaxPosition => transform.position;
}

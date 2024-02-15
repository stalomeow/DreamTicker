using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[DisallowMultipleComponent]
public class RotatableBlock : MonoBehaviour
{
    public Transform PivotBlock;
    public Vector3 Axis = Vector3.left;
    [FormerlySerializedAs("Mirror")] public RotatableBlock Mirrored;
    public bool IsPassive;

    [Header("Rotate Animation")]
    public float RotateTime = 0.5f;

    [Header("Shake Animation")]
    public float ShakeTime = 0.2f;
    public float ShakeIntensity = 0.1f;

    [Header("MouseHover Highlight")]
    [Range(0, 1)] public float HighlightMix = 0.2f;

    private Block[] _blocks;
    private int _rotateAngle = 90;
    private readonly int _highlightMixPropId = Shader.PropertyToID("_HighlightMix");

    private void Start()
    {
        _blocks = GetComponentsInChildren<Block>();

        if (!IsPassive)
        {
            foreach (var block in _blocks)
            {
                block.MouseDownEvent += _ => StartCoroutine(TryRotate());
                block.MouseEnterEvent += _ => SetHighlightMix(HighlightMix);
                block.MouseExitEvent += _ => SetHighlightMix(0);
            }
        }
    }

    private void SetHighlightMix(float value)
    {
        foreach (var block in _blocks)
        {
            block.Renderer.material.SetFloat(_highlightMixPropId, value);
        }

        foreach (var block in Mirrored._blocks)
        {
            block.Renderer.material.SetFloat(_highlightMixPropId, value);
        }
    }

    private IEnumerator TryRotate()
    {
        if (!BlockManager.Instance.CanInteract)
        {
            yield break;
        }

        if (!CanRotate())
        {
            // 换一个方向尝试
            _rotateAngle = -_rotateAngle;

            if (!CanRotate())
            {
                Debug.LogWarning("卡住了！");
                BlockManager.Instance.DisableInteract();
                yield return StartCoroutine(DoShakeAnimation());
                BlockManager.Instance.EnableInteract();
                yield break;
            }
        }

        BlockManager.Instance.DisableInteract();
        yield return StartCoroutine(DoRotateAnimation(_rotateAngle));
        BlockManager.Instance.EnableInteract();
        BlockManager.Instance.RebuildBlockGraph();
    }

    private IEnumerator DoShakeAnimation()
    {
        float time = 0;
        Vector3 pos = transform.position;
        Vector3 posMirrored = Mirrored.transform.position;
        while (time < ShakeTime)
        {
            Vector3 randomMove = Random.insideUnitSphere * ShakeIntensity;
            transform.position = pos + randomMove;
            Mirrored.transform.position = posMirrored - randomMove;
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = pos;
        Mirrored.transform.position = posMirrored;
    }

    private IEnumerator DoRotateAnimation(int angle)
    {
        List<Vector3> pos = ListPool<Vector3>.Get();
        List<Vector3> posMirrored = ListPool<Vector3>.Get();

        try
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                pos.Add(transform.GetChild(i).position);
            }

            for (int i = 0; i < Mirrored.transform.childCount; i++)
            {
                posMirrored.Add(Mirrored.transform.GetChild(i).position);
            }

            float time = 0;
            while (time < RotateTime)
            {
                float progress = time / RotateTime;

                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    child.position = GetPositionAfterRotation(pos[i], angle, progress, out Quaternion rot);
                    child.localRotation = rot;
                }

                for (int i = 0; i < Mirrored.transform.childCount; i++)
                {
                    Transform child = Mirrored.transform.GetChild(i);
                    child.position = Mirrored.GetPositionAfterRotation(posMirrored[i], -angle, progress, out Quaternion rot);
                    child.localRotation = rot;
                }

                time += Time.deltaTime;
                yield return null;
            }

            // 最后重新设置一次，减少误差

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                child.position = GetPositionAfterRotation(pos[i], angle, 1, out Quaternion rot);
                child.localRotation = rot;
            }

            for (int i = 0; i < Mirrored.transform.childCount; i++)
            {
                Transform child = Mirrored.transform.GetChild(i);
                child.position = Mirrored.GetPositionAfterRotation(posMirrored[i], -angle, 1, out Quaternion rot);
                child.localRotation = rot;
            }
        }
        finally
        {
            ListPool<Vector3>.Release(pos);
            ListPool<Vector3>.Release(posMirrored);
        }
    }

    private Vector3 GetPositionAfterRotation(Vector3 position, int angle, float slerpT = 1)
    {
        return GetPositionAfterRotation(position, angle, slerpT, out _);
    }

    private Vector3 GetPositionAfterRotation(Vector3 position, int angle, float slerpT, out Quaternion rot)
    {
        Vector3 pivot = PivotBlock.position;
        rot = Quaternion.AngleAxis(angle, Axis);

        if (slerpT < 1)
        {
            rot = Quaternion.Slerp(Quaternion.identity, rot, slerpT);
        }

        return pivot + rot * (position - pivot);
    }

    private bool CanRotate()
    {
        HashSet<Vector3Int> cubes = HashSetPool<Vector3Int>.Get();
        cubes.UnionWith(BlockManager.Instance.AllBlocksInWorld);

        // 移除自己下面所有方块
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            cubes.Remove(child.position.RoundToInt());
        }

        try
        {
            // 检查旋转后周围是否有方块。有的话就没法旋转
            Vector3Int min = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            Vector3Int max = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                Vector3Int pos = GetPositionAfterRotation(child.position, _rotateAngle).RoundToInt();
                min = Vector3Int.Min(pos, min);
                max = Vector3Int.Max(pos, max);
            }

            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        if (cubes.Contains(new Vector3Int(x, y, z)))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        finally
        {
            HashSetPool<Vector3Int>.Release(cubes);
        }
    }
}

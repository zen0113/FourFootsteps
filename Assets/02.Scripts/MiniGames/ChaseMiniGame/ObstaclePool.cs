using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstaclePool : MonoBehaviour
{
    [SerializeField] private List<ThrowableObstacle> prefabs = new List<ThrowableObstacle>();
    [SerializeField] private int prewarm = 8;
    private readonly List<ThrowableObstacle> pool = new();
    private bool isPlaying = true;
    public bool IsPlaying => isPlaying;

    void Awake()
    {
        for (int i = 0; i < prewarm; i++)
        {
            pool.Add(CreateRandomPrefab());
        }
    }

    public ThrowableObstacle Get()
    {
        if (!isPlaying) return null;
        foreach (var o in pool) if (!o.gameObject.activeSelf) return o;
        var n = CreateRandomPrefab();
        pool.Add(n);
        return n;
    }

    private ThrowableObstacle CreateRandomPrefab()
    {
        if (!isPlaying) return null;
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogError("[ObstaclePool] Prefabs 리스트가 비어있습니다.");
            return null;
        }

        int idx = UnityEngine.Random.Range(0, prefabs.Count);
        var p = Instantiate(prefabs[idx], transform);
        p.gameObject.SetActive(false);
        return p;
    }

    public void DisableAllObstacle()
    {
        isPlaying = false;
        foreach (var o in pool)
            o.gameObject.SetActive(false);
    }
}

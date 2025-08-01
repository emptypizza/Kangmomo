using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 스폰을 담당하는 클래스. Static은 셀 내부, Walker는 외곽에서 스폰됨.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("적 프리팹")]
    public GameObject[] enemyPrefabs; // 0: Static, 1: Walker

    [Header("스폰 설정")]
    public float spawnInterval = 4.0f;
    public int maxSpawnCount = 2;
    public int maxTotalEnemies = 4;

    private float nextSpawnTime;

    void Update()
    {
        float elapsed = Time.timeSinceLevelLoad;
        if (elapsed < 60f) { maxTotalEnemies = 4; maxSpawnCount = 2; }
        else if (elapsed < 120f) { maxTotalEnemies = 7; maxSpawnCount = 3; }
        else { maxTotalEnemies = 10; maxSpawnCount = 4; }

        if (Time.time > nextSpawnTime)
        {
            SpawnEnemyWave();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void SpawnEnemyWave()
    {
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (currentEnemies >= maxTotalEnemies) return;

        int maxCanSpawn = Mathf.Min(maxSpawnCount, maxTotalEnemies - currentEnemies);
        if (maxCanSpawn < 1) return;

        int spawnCount = Random.Range(1, maxCanSpawn + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            if (Random.value < 0.4f)
                SpawnStaticEnemy_NoOverlap();
            else
                SpawnWalkerAtEdge();
        }
    }

    void SpawnStaticEnemy_NoOverlap()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Enemy e = enemy.GetComponent<Enemy>();
            if (e != null && e.enemyType == EnemyType.Static)
            {
                Vector2Int hex = gm.player.WorldToHex(enemy.transform.position);
                occupied.Add(hex);
            }
        }

        occupied.Add(gm.player.WorldToHex(gm.player.transform.position));

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < gm.gridWidth; x++)
        {
            for (int y = 0; y < gm.gridHeight; y++)
            {
                Vector2Int hex = new Vector2Int(x, y);
                if (!occupied.Contains(hex))
                    candidates.Add(hex);
            }
        }

        if (candidates.Count == 0) return;

        Vector2Int spawnHex = candidates[Random.Range(0, candidates.Count)];
        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);

        GameObject prefab = enemyPrefabs.Length > 0 ? enemyPrefabs[0] : null;
        if (prefab == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(spawnHex, EnemyType.Static, Vector2Int.zero); // ✅ 호출 복원됨
    }

    void SpawnWalkerAtEdge()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        // 랜덤 가장자리 스폰
        List<(Vector2Int, Vector2Int)> edgeSpawns = new List<(Vector2Int, Vector2Int)>();

        int width = gm.gridWidth;
        int height = gm.gridHeight;

        for (int x = 0; x < width; x++)
        {
            edgeSpawns.Add((new Vector2Int(x, 0), new Vector2Int(0, 1))); // 아래
            edgeSpawns.Add((new Vector2Int(x, height - 1), new Vector2Int(0, -1))); // 위
        }
        for (int y = 0; y < height; y++)
        {
            edgeSpawns.Add((new Vector2Int(0, y), new Vector2Int(1, 0))); // 왼쪽
            edgeSpawns.Add((new Vector2Int(width - 1, y), new Vector2Int(-1, 0))); // 오른쪽
        }

        var pair = edgeSpawns[Random.Range(0, edgeSpawns.Count)];
        Vector2Int spawnHex = pair.Item1;
        Vector2Int moveDir = pair.Item2;

        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);
        GameObject prefab = enemyPrefabs.Length > 1 ? enemyPrefabs[1] : null;
        if (prefab == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(spawnHex, EnemyType.Walker, moveDir); //  호출 복원됨
    }
}

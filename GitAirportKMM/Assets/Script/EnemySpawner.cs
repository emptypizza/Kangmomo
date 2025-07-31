using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 적 스폰을 담당하는 클래스. Static 타입은 셀 중앙에, Walker 타입은 그리드 가장자리에서 생성합니다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("적 프리팹")]
    public GameObject[] enemyPrefabs; // 0: Static(Enemy_fix), 1: Walker(Enemy_move(1))

    [Header("스폰 설정")]
    public float spawnInterval = 4.0f;
    public int maxSpawnCount = 2;
    public int maxTotalEnemies = 4;
    private float nextSpawnTime;

    // GameManager로부터 그리드 크기를 받아오므로 내부 변수는 필요 없습니다.
    // public int gridWidth = 9;
    // public int gridHeight = 9;

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

    /// <summary>
    /// 적 생성 웨이브를 시작합니다. 40% 확률로 Static, 60% 확률로 Walker를 생성합니다.
    /// </summary>
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

    /// <summary>
    /// Static 타입 적(Enemy_fix)을 다른 적과 겹치지 않게 셀 중앙에 생성합니다.
    /// </summary>
    void SpawnStaticEnemy_NoOverlap()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        // 1. 현재 Static 적들이 차지한 셀 좌표 수집
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Enemy e = enemy.GetComponent<Enemy>();
            if (e != null && e.enemyType == EnemyType.Static)
            {
                // ★★★ 수정된 부분: GameManager의 좌표 변환 함수를 사용합니다. ★★★
                Vector2Int hex = gm.player.WorldToHex(enemy.transform.position);
                occupied.Add(hex);
            }
        }

        // 2. 플레이어가 있는 셀도 차지한 것으로 간주 (플레이어 바로 위에 스폰 방지)
        occupied.Add(gm.player.WorldToHex(gm.player.transform.position));

        // 3. 빈 셀 리스트 생성
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

        if (candidates.Count == 0) return; // 빈 셀이 없으면 생성 스킵

        // 4. 빈 셀 중 랜덤 하나를 선택하여 생성
        Vector2Int spawnHex = candidates[Random.Range(0, candidates.Count)];
        // ★★★ 수정된 부분: GameManager의 좌표 변환 함수를 사용합니다. ★★★
        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);

        GameObject prefab = enemyPrefabs.Length > 0 ? enemyPrefabs[0] : null;
        if (prefab == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(EnemyType.Static, Vector2Int.zero);
    }

    /// <summary>
    /// Walker 타입 적을 그리드 가장자리에서 생성하여 반대편으로 이동시킵니다.
    /// </summary>
    void SpawnWalkerAtEdge()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;

        // 50% 확률로 왼쪽 또는 오른쪽 가장자리에서 생성
        bool spawnLeft = Random.value < 0.5f;
        int y = Random.Range(0, gm.gridHeight);

        Vector2Int spawnHex;
        Vector2 moveDir;
        if (spawnLeft)
        {
            spawnHex = new Vector2Int(0, y);
            moveDir = Vector2.right;
        }
        else
        {
            spawnHex = new Vector2Int(gm.gridWidth - 1, y);
            moveDir = Vector2.left;
        }

        // ★★★ 수정된 부분: GameManager의 좌표 변환 함수를 사용합니다. ★★★
        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);

        GameObject prefab = enemyPrefabs.Length > 1 ? enemyPrefabs[1] : null;
        if (prefab == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(EnemyType.Walker, Vector2Int.RoundToInt(moveDir));
    }

    // --- 아래 좌표 변환 함수들은 GameManager의 것을 사용하므로 삭제합니다. ---
    // public Vector2Int WorldToHex(Vector3 pos) { ... }
    // public Vector3 HexToWorld(Vector2Int hex) { ... }
}
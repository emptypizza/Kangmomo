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
                                   /// 적 웨이브를 생성합니다.
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

    void SpawnStaticEnemy_NoOverlap() //Static 타입 적을 겹치지 않게 생성합니다.
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null) return;
        // 1. 스폰 불가능한 위치(다른 Static 적, 플레이어 위치) 목록 생성
        HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();// ㅎㅐ쉬셋

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

        List<Vector2Int> candidates = new List<Vector2Int>();// 2. 스폰 가능한 빈 셀 목록 생성
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
        // 3. 생성 및 초기화
        Vector2Int spawnHex = candidates[Random.Range(0, candidates.Count)];  
        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);

        GameObject prefab = enemyPrefabs.Length > 0 ? enemyPrefabs[0] : null;
        if (prefab == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(spawnHex, EnemyType.Static, Vector2Int.zero); // 호출 복원됨
    }

    void SpawnWalkerAtEdge()    /// Walker 타입 적을 그리드 가장자리에서 생성합니다.
    {
        GameManager gm = GameManager.Instance;

        // 1. 모든 가장자리 위치와 해당 방향 목록 생성
        List<(Vector2Int, Vector2Int)> edgePoints = new List<(Vector2Int, Vector2Int)>(); // 랜덤 가장자리 스폰
        int width = gm.gridWidth;
        int height = gm.gridHeight;

       // if (gm == null || gm.player == null) return;

       
   
        for (int x = 0; x < width; x++)
        {
            edgePoints.Add((new Vector2Int(x, 0), Vector2Int.up)); // 아래에서 위
            edgePoints.Add((new Vector2Int(x, height - 1), Vector2Int.down)); // 위에서아 
        }
        for (int y = 1; y < height - 1; y++)
        { // 모서리는 중복되므로 제외
            edgePoints.Add((new Vector2Int(0, y), Vector2Int.right));   // 왼쪽에서 오른쪽으로
            edgePoints.Add((new Vector2Int(width - 1, y), Vector2Int.left)); // 오른쪽에서 왼쪽으로
        }


        if (edgePoints.Count == 0) return;


        // 2. 랜덤한 가장자리 선택
        var (spawnHex, moveDir) = edgePoints[Random.Range(0, edgePoints.Count)];


        // 3. 생성 및 초기화
        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);
        GameObject prefab = enemyPrefabs[1];
        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";


        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(spawnHex, EnemyType.Walker, moveDir);



        /* var pair = edgeSpawns[Random.Range(0, edgeSpawns.Count)];
         Vector2Int spawnHex = pair.Item1;
        Vector2Int moveDir = pair.Item2;

        Vector3 spawnWorldPos = gm.player.HexToWorld(spawnHex);
        GameObject prefab = enemyPrefabs.Length > 1 ? enemyPrefabs[1] : null;
        if (prefab == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnWorldPos, Quaternion.identity);
        enemyObj.tag = "Enemy";

        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
            enemyScript.Init(spawnHex, EnemyType.Walker, moveDir); */ // GPT4o code
    }
}

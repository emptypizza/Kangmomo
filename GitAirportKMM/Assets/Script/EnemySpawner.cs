using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EnemySpawner: 적을 일정 주기로 안전거리, 중복 방지, 난이도 곡선에 따라 생성하는 스크립트.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("생성할 적 프리팹 (0:약한 적, 1:중간, 2:강한 적...)")]
    public GameObject[] enemyPrefabs;

    [Header("스폰 설정")]
    public float spawnInterval = 4.0f;      // 웨이브 간격(초)
    public int maxSpawnCount = 2;           // 웨이브당 최대 스폰 수(난이도에 따라 자동 증가)
    public int maxTotalEnemies = 4;         // 맵에 존재할 수 있는 적 최대 수(난이도에 따라 자동 증가)

    private float nextSpawnTime;

    // 헥사 타일 6방향(Flat-Top 기준)
    private static readonly Vector2Int[] hexDirections = {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };

    void Update()
    {
        float elapsed = Time.timeSinceLevelLoad;

        // --- 난이도 곡선(시간 경과에 따라 적 개수/웨이브 증가) ---
        if (elapsed < 60f)
        {
            maxTotalEnemies = 4;   // 1분 전까지는 소수만
            maxSpawnCount = 2;
        }
        else if (elapsed < 120f)
        {
            maxTotalEnemies = 7;   // 2분까지 중간
            maxSpawnCount = 3;
        }
        else
        {
            maxTotalEnemies = 10;  // 이후 최대치
            maxSpawnCount = 4;
        }

        // --- 적 스폰 타이밍 ---
        if (Time.time > nextSpawnTime)
        {
            SpawnEnemyWave();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    /// <summary>
    /// 헥사 그리드 두 좌표간 거리 계산 (큐브 좌표 변환)
    /// </summary>
    private int HexDistance(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        int dz = (a.x + a.y) - (b.x + b.y);
        return (Mathf.Abs(dx) + Mathf.Abs(dy) + Mathf.Abs(dz)) / 2;
    }

    /// <summary>
    /// 플레이어로부터 최소 minDistance 떨어진 셀만 후보로 반환
    /// </summary>
    private List<Vector2Int> GetSafeAvailableCells(GameManager gm, int minDistance = 3)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int playerHex = gm.player.WorldToHex(gm.player.transform.position);

        for (int x = 0; x < gm.gridWidth; x++)
            for (int y = 0; y < gm.gridHeight; y++)
            {
                Vector2Int hex = new Vector2Int(x, y);
                if (HexDistance(playerHex, hex) >= minDistance)
                    cells.Add(hex);
            }
        return cells;
    }

    /// <summary>
    /// Walker 타입 적: 플레이어로부터 가장 멀어지는 방향 계산
    /// </summary>
    private Vector2Int GetWalkerDirection(Vector2Int spawnHex, Vector2Int playerHex)
    {
        Vector2Int[] directions = hexDirections;
        Vector2Int bestDir = directions[0];
        int maxDist = -1;
        foreach (var dir in directions)
        {
            Vector2Int testHex = spawnHex + dir;
            int dist = HexDistance(playerHex, testHex);
            if (dist > maxDist)
            {
                maxDist = dist;
                bestDir = dir;
            }
        }
        return bestDir;
    }

    /// <summary>
    /// 적 한 웨이브 스폰(중복 방지, Walker 방향, 안전거리 포함)
    /// </summary>
    public void SpawnEnemyWave()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // 현재 맵의 살아있는 적 개수 체크("Enemy" 태그 필수!)
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (currentEnemies >= maxTotalEnemies) return;

        int maxCanSpawn = Mathf.Min(maxSpawnCount, maxTotalEnemies - currentEnemies);
        if (maxCanSpawn < 1) return;
        int spawnCount = Random.Range(1, maxCanSpawn + 1);

        List<Vector2Int> spawnableCells = GetSafeAvailableCells(gm, 3); // 최소 3칸 이상 거리
        Vector2Int playerHex = gm.player.WorldToHex(gm.player.transform.position);

        // 이번 웨이브에서 스폰한 셀은 중복 방지
        HashSet<Vector2Int> usedCells = new HashSet<Vector2Int>();
        for (int i = 0; i < spawnCount && spawnableCells.Count > 0; i++)
        {
            int idx = Random.Range(0, spawnableCells.Count);
            Vector2Int spawnHex = spawnableCells[idx];
            spawnableCells.RemoveAt(idx);
            usedCells.Add(spawnHex);

            GameObject prefabToSpawn = SelectEnemyByTime();
            if (prefabToSpawn == null) continue;

            Vector3 spawnPosition = gm.player.HexToWorld(spawnHex);
            GameObject enemyObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            enemyObj.tag = "Enemy";

            Enemy enemyScript = enemyObj.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                if (Random.value > 0.5f)
                {
                    // Walker - 가장 멀어지는 방향으로 설정
                    Vector2Int walkerDir = GetWalkerDirection(spawnHex, playerHex);
                    enemyScript.Init(spawnHex, EnemyType.Walker, walkerDir);
                }
                else
                {
                    // Static
                    enemyScript.Init(spawnHex, EnemyType.Static, Vector2Int.zero);
                }
            }
        }
    }

    /// <summary>
    /// 게임 시간에 따라 적 프리팹(강약) 다르게 등장시키기
    /// </summary>
    private GameObject SelectEnemyByTime()
    {
        float gameTime = Time.timeSinceLevelLoad;
        if (enemyPrefabs.Length == 0) return null;
        if (gameTime < 60f)
            return enemyPrefabs[0]; // 약한 적만
        else if (gameTime < 120f && enemyPrefabs.Length >= 2)
            return enemyPrefabs[Random.Range(0, 2)]; // 0~1번 랜덤
        else
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)]; // 전체 랜덤
    }
}

using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("생성할 적 프리팹 (여러 종류)")]
    public GameObject[] enemyPrefabs; // 0: 약한 적, 1: 중간, 2: 강한 적 등등

    [Header("스폰 설정")]
    public float spawnInterval = 4.0f;
    public int minSpawnCount = 1;
    public int maxSpawnCount = 4;
    public int maxTotalEnemies = 10;

    private float nextSpawnTime;

    private static readonly Vector2Int[] hexDirections = {
        new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1),
        new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };

    void Update()
    {
        if (Time.time > nextSpawnTime)
        {
            SpawnEnemyWave();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    public void SpawnEnemyWave()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.player == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // 현재 맵에 살아있는 적 수 체크(Enemy 태그 필수!)
        int currentEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (currentEnemies >= maxTotalEnemies) return;

        int maxCanSpawn = Mathf.Min(maxSpawnCount, maxTotalEnemies - currentEnemies);
        if (maxCanSpawn < minSpawnCount) return;
        int spawnCount = Random.Range(minSpawnCount, maxCanSpawn + 1);

        List<Vector2Int> availableCells = GetAvailableCells(gm);

        for (int i = 0; i < spawnCount; i++)
        {
            if (availableCells.Count == 0) break;

            int randomIndex = Random.Range(0, availableCells.Count);
            Vector2Int spawnHex = availableCells[randomIndex];
            availableCells.RemoveAt(randomIndex);

            // ▶ 시간에 따라 등장 적이 달라짐!
            GameObject prefabToSpawn = SelectEnemyByTime();
            if (prefabToSpawn == null) continue;

            Vector3 spawnPosition = gm.player.HexToWorld(spawnHex);
            GameObject enemyObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            enemyObj.tag = "Enemy"; // ★ 태그 필수!

            // Enemy 스크립트 초기화 (Static/Walker 구분)
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                if (Random.value > 0.5f)
                {
                    Vector2Int randomDir = hexDirections[Random.Range(0, hexDirections.Length)];
                    enemyScript.Init(spawnHex, EnemyType.Walker, randomDir);
                }
                else
                {
                    enemyScript.Init(spawnHex, EnemyType.Static, Vector2Int.zero);
                }
            }
        }
    }

    // 시간에 따라 스폰할 적 프리팹 결정
    private GameObject SelectEnemyByTime()
    {
        float gameTime = Time.timeSinceLevelLoad;
        if (enemyPrefabs.Length == 0) return null;
        if (gameTime < 5f)
        {
            return enemyPrefabs[0]; // 0~60초: 약한 적만
        }
        else if (gameTime < 6f && enemyPrefabs.Length >= 2)
        {
            int idx = Random.Range(0, 2); // 0~1 중 랜덤 (두 종류)
            return enemyPrefabs[idx];
        }
        else
        {
            int idx = Random.Range(0, enemyPrefabs.Length); // 모든 적 등장
            return enemyPrefabs[idx];
        }
    }

    // 플레이어 위치를 제외한 가능한 셀을 반환
    private List<Vector2Int> GetAvailableCells(GameManager gm)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        for (int x = 0; x < gm.gridWidth; x++)
            for (int y = 0; y < gm.gridHeight; y++)
                cells.Add(new Vector2Int(x, y));
        Vector2Int playerHex = gm.player.WorldToHex(gm.player.transform.position);
        cells.Remove(playerHex);
        return cells;
    }
}

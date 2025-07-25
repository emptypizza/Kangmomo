// ItemSpawner_Grid.cs
// 아이템을 특정 조건(점수, 시간 등)에 따라 Hex Grid 내 무작위 셀에 생성하는 스포너

using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner_Grid : MonoBehaviour
{
    [Header("아이템 프리팹")]
    public GameObject itemPrefab;

    [Header("스폰 주기 (초)")]
    public float spawnInterval = 5f;

    [Header("스폰 조건")]
    public int requiredScore = 3;
    public float requiredTime = 10f;

    private float nextSpawnTime = 0f;

    void Update()
    {
        if (Time.time >= nextSpawnTime && ShouldSpawnItem())
        {
            SpawnItem();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private bool ShouldSpawnItem()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            return false;

        return gm.CurrentScore >= requiredScore && gm.GameTime >= requiredTime;
    }

    /// <summary>
    /// 그리드 내 랜덤 셀에 아이템을 중복 없이, 플레이어 위치 제외하고 생성
    /// </summary>
    private void SpawnItem()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.player == null)
            return;

        int spawnCount = 3;
        int gridWidth = gm.gridWidth;
        int gridHeight = gm.gridHeight;
        Vector2Int playerHex = gm.player.WorldToHex(gm.player.transform.position);

        // 1. 전체 셀 좌표 리스트 생성
        List<Vector2Int> availableCells = new List<Vector2Int>();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (cell == playerHex)
                    continue;
                availableCells.Add(cell);
            }
        }

        // 2. (선택) 이미 아이템 존재 셀 제외 로직 필요시 여기에 구현

        // 3. 랜덤하게 N개 셀 선택하여 스폰 (중복 없음)
        for (int i = 0; i < spawnCount && availableCells.Count > 0; i++)
        {
            int idx = Random.Range(0, availableCells.Count);
            Vector2Int chosenCell = availableCells[idx];
            availableCells.RemoveAt(idx);

            Vector3 worldPos = gm.player.HexToWorld(chosenCell);
            Instantiate(itemPrefab, worldPos, Quaternion.identity);
        }
    }

    /// <summary>
    /// 임의 셀 한 곳에 즉시 아이템 생성 (참고/디버깅용)
    /// </summary>
    public void SpawnItem0()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.SpawnItemOnRandomCell();
    }

    /// <summary>
    /// 플레이어 주변 셀에 아이템 여러 개 생성 (예시/미사용)
    /// </summary>
    private void SpawnItem1()
    {
        int spawnCount = 2;
        float spawnRadius = 3f;
        // gm.SpawnItemsNearPlayer(spawnCount, spawnRadius);
    }
}

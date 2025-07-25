using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 전체 게임 진행을 관리하는 싱글톤 매니저.
/// Flat-Top 헥사곤 타일 그리드를 중앙 정렬로 생성합니다.
/// </summary>
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    // 게임 진행 시간 (외부 접근용)
    public float GameTime => Time.timeSinceLevelLoad;

    public Player player;  // 인스펙터에서 직접 드래그 or Start에서 찾기
    public static GameManager Instance; // 싱글톤 인스턴스

    [Header("Grid Settings")]
    public GameObject gridCellPrefab; // 셀 프리팹
    public int gridWidth = 9;         // 가로 셀 수
    public int gridHeight = 9;        // 세로 셀 수
    public Transform gridParent;      // 셀을 담을 부모 오브젝트

    [Header("Item Settings")]
    public GameObject itemPrefab;     // 생성할 아이템 프리팹

    private GameObject[,] gridObjects; // 셀 위치 참조

    
    private int itemCount = 0;         // 아이템 획득 수
    public Vector3 GetPlayerPosition()
    {
        if (player != null)
            return player.transform.position;
        else
            return Vector3.zero; // fallback
    }
    private void Awake()
    {

        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        GenerateGrid(); // 그리드 생성
                player = GetComponent<Player>();
        if (player == null)
            player = FindObjectOfType<Player>();

    }

    /// <summary>
    /// 그리드 셀 생성 (육각형 기준, 중심 정렬)
    /// </summary>
    void GenerateGrid()
    {
        gridObjects = new GameObject[gridWidth, gridHeight];

        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;

        // 중앙 정렬 offset 계산
        Vector2 offset = new Vector2(
            -width * 0.75f * (gridWidth - 1) / 2f,
            -height * (gridHeight - 1) / 2f
        );

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float xPos = width * 0.75f * x + offset.x;
                float yPos = height * (y + 0.5f * (x & 1)) + offset.y;

                Vector3 cellPos = new Vector3(xPos, yPos, 0);
                var obj = Instantiate(gridCellPrefab, cellPos, Quaternion.identity, gridParent);
                gridObjects[x, y] = obj;
            }
        }
    }

    /// <summary>
    /// 아이템을 먹었을 때 호출. HP와 점수 증가
    /// </summary>
    public void CollectItem()
    {
        itemCount++;
        //  playerHP += 1;

        Debug.Log($"[Item] 획득: {itemCount}개,");
    }

    /// <summary>
    /// 현재 점수 반환용 프로퍼티
    /// </summary>
    public int CurrentScore => itemCount;

    /// <summary>
    /// 현재 그리드 크기 반환 (외부 참조용)
    /// </summary>
    public Vector2Int GetGridDimensions() => new Vector2Int(gridWidth, gridHeight);

    /// <summary>
    /// 중앙 셀 위치 반환
    /// </summary>
    public Vector2Int GetGridCenter() => new Vector2Int(gridWidth / 2, gridHeight / 2);

    /// <summary>
    /// 주어진 좌표가 유효한 셀인지 검사
    /// </summary>
    public bool IsCellExists(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    /// <summary>
    /// 플레이어 주변(6방향 + 자기 위치)에 아이템을 스폰함
    /// </summary>
    public void SpawnItemsNearPlayer()
    {
        var player = FindObjectOfType<Player>();
        Vector3 playerPos = player.transform.position;
        Vector2Int playerHex = player.WorldToHex(playerPos);

        List<Vector2Int> spawnHexes = new List<Vector2Int>();
        Vector2Int[] dirs = {
            new(0,0), new(1,0), new(-1,0), new(0,1), new(0,-1), new(1,1), new(-1,-1)
        };

        foreach (var dir in dirs)
        {
            Vector2Int candidate = playerHex + dir;
            if (IsCellExists(candidate))
                spawnHexes.Add(candidate);
        }

        foreach (var hex in spawnHexes)
        {
            Vector3 pos = player.HexToWorld(hex);
            Instantiate(itemPrefab, pos, Quaternion.identity);
        }
    }

    /// <summary>
    /// 그리드 내 랜덤 셀에 아이템을 1개 스폰
    /// </summary>
    public void SpawnItemOnRandomCell()
    {
        var player = FindObjectOfType<Player>();
        int x = Random.Range(0, gridWidth);
        int y = Random.Range(0, gridHeight);
        Vector3 pos = player.HexToWorld(new Vector2Int(x, y));
        Instantiate(itemPrefab, pos, Quaternion.identity);
    }
}

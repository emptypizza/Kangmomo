using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
// UnityEngine.UI 네임스페이스를 추가해야 Text, Button 등 UI 관련 컴포넌트를 코드에서 사용할 수 있습니다.
using UnityEngine.UI;

/// <summary>
/// 게임의 전반적인 진행, 상태, 규칙을 관리하는 싱글톤(Singleton) 클래스입니다.
/// 헥사곤 타일맵 생성, 아이템 관리, 게임 클리어 조건 판정 등 핵심 로직을 담당합니다.
/// </summary>
public class GameManager : MonoBehaviour
{

    // --- 외부 참조 변수 --- //

    /// <summary>
    /// 게임 레벨이 시작된 후 흐른 시간을 반환합니다. (읽기 전용)
    /// </summary>
    public float GameTime => Time.timeSinceLevelLoad;

    /// <summary>
    /// 플레이어 오브젝트의 참조입니다. 인스펙터에서 직접 할당하거나, 없을 경우 Start()에서 자동으로 찾습니다.
    /// </summary>
    public Player player;

    /// <summary>
    /// 싱글톤 패턴을 위한 static 인스턴스입니다. 
    /// 다른 스크립트에서 'GameManager.Instance'를 통해 이 스크립트의 public 멤버에 쉽게 접근할 수 있습니다.
    /// </summary>
    public static GameManager Instance;

    // --- 그리드 설정 --- //
    [Header("Grid Settings")] // 인스펙터에서 구역을 나누어 보기 편하게 합니다.
    public GameObject gridCellPrefab; // 헥사곤 타일(셀)로 사용될 프리팹입니다.
    public int gridWidth = 9;         // 그리드의 가로 셀 개수입니다.
    public int gridHeight = 9;        // 그리드의 세로 셀 개수입니다.
    public Transform gridParent;      // 생성된 셀들을 담을 부모 오브젝트의 Transform입니다. (Hierarchy 정리용)

    // --- 아이템 설정 --- //
    [Header("Item Settings")]
    public GameObject itemPrefab;     // '폐지' 등 플레이어가 수집할 아이템 프리팹입니다.

    // --- 게임 클리어 조건 설정 --- //
    [Header("Game Clear Settings")]
    public Text clearText; // "Day1 Stage Clear"와 같은 메시지를 표시할 UI Text 컴포넌트입니다.
    public int clearItemCount = 10; // Day 클리어를 위해 필요한 아이템(폐지) 획득 개수입니다.
    public float clearTime = 600f; // Day 클리어를 위한 생존 시간 목표 (600초 = 10분) 입니다.

    // --- 내부 상태 변수 --- //
    private GameObject[,] gridObjects; // 생성된 그리드 셀 오브젝트들을 2차원 배열로 저장하여 관리합니다.
    private int itemCount = 0;         // 현재까지 획득한 아이템의 개수를 저장합니다.
    private bool isGameCleared = false; // 게임 클리어 상태인지 여부를 저장하는 플래그(flag)입니다. (중복 처리 방지용)

    /// <summary>
    /// 플레이어의 현재 월드 좌표를 반환합니다.
    /// </summary>
    public Vector3 GetPlayerPosition()
    {
        if (player != null)
            return player.transform.position;
        else
            return Vector3.zero; // 플레이어가 없으면 (0,0,0)을 반환합니다.
    }

    /// <summary>
    /// 씬이 시작될 때 가장 먼저 한번 호출되는 Awake() 함수입니다. 싱글톤 인스턴스를 설정합니다.
    /// </summary>
    private void Awake()
    {
        // 만약 Instance가 아직 할당되지 않았다면, 이 스크립트의 인스턴스를 할당합니다.
        if (Instance == null)
            Instance = this;
        // 이미 Instance가 존재하고, 그것이 이 인스턴스와 다르다면 이 오브젝트는 파괴합니다. (싱글톤 중복 방지)
        else if (Instance != this)
            Destroy(gameObject);
    }

    /// <summary>
    /// Awake() 다음, 첫 프레임 업데이트 전에 한번 호출되는 Start() 함수입니다.
    /// </summary>
    private void Start()
    {
        GenerateGrid(); // 그리드맵을 생성합니다.

        // player 변수가 인스펙터에서 할당되지 않았을 경우를 대비해 씬에서 찾아봅니다.
        if (player == null)
            player = FindObjectOfType<Player>();

        // clearText UI가 할당되었다면, 게임 시작 시에는 보이지 않도록 비활성화합니다.
        if (clearText != null)
            clearText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 매 프레임마다 호출되는 Update() 함수입니다.
    /// </summary>
    private void Update()
    {
        // 만약 이미 게임이 클리어된 상태라면, 더 이상 아래 로직을 실행하지 않고 함수를 종료합니다.
        if (isGameCleared) return;

        // 게임 클리어 조건 1: 생존 시간이 목표 시간을 초과했는지 확인합니다.
        if (GameTime >= clearTime)
        {
            GameClear(); // 조건을 만족하면 게임 클리어 처리 함수를 호출합니다.
        }
    }


    /// <summary>
    /// 헥사곤 그리드(육각형 타일맵)를 생성하는 함수입니다.
    /// </summary>
    void GenerateGrid()
    {
        gridObjects = new GameObject[gridWidth, gridHeight];

        // 헥사곤 타일의 크기 및 간격 계산 (Flat-Top 기준)
        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;

        // 그리드를 화면 중앙에 정렬하기 위한 오프셋(offset)을 계산합니다.
        Vector2 offset = new Vector2(
            -width * 0.75f * (gridWidth - 1) / 2f,
            -height * (gridHeight - 1) / 2f
        );

        // 이중 for문을 사용하여 정해진 가로, 세로 개수만큼 타일을 생성합니다.
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // 헥사곤 좌표 규칙에 따라 각 셀의 월드 좌표(xPos, yPos)를 계산합니다.
                float xPos = width * 0.75f * x + offset.x;
                float yPos = height * (y + 0.5f * (x & 1)) + offset.y; // (x & 1)은 x가 홀수일 때만 1을 반환하여, 홀수 열을 반 칸 아래로 내립니다.

                Vector3 cellPos = new Vector3(xPos, yPos, 0); // 최종 생성 위치입니다.
                // Instantiate 함수로 gridCellPrefab을 생성하고, 위치와 회전, 부모를 지정해줍니다.
                var obj = Instantiate(gridCellPrefab, cellPos, Quaternion.identity, gridParent);
                // 생성된 셀 오브젝트를 gridObjects 배열에 저장하여 나중에 참조할 수 있게 합니다.
                gridObjects[x, y] = obj;
            }
        }
    }

    /// <summary>
    /// 플레이어가 아이템을 획득했을 때 호출되는 함수입니다. (주로 Player.cs에서 호출)
    /// </summary>
    public void CollectItem()
    {
        // 게임이 클리어된 후에는 아이템을 획득할 수 없도록 막습니다.
        if (isGameCleared) return;

        itemCount++; // 아이템 카운트를 1 증가시킵니다.
        Debug.Log($"[Item] 획득: {itemCount}개"); // 콘솔에 로그를 출력하여 확인합니다.

        // 게임 클리어 조건 2: 획득한 아이템 개수가 목표 개수 이상인지 확인합니다.
        if (itemCount >= clearItemCount)
        {
            GameClear(); // 조건을 만족하면 게임 클리어 처리 함수를 호출합니다.
        }
    }

    /// <summary>
    /// 게임 클리어 조건을 만족했을 때 실행되는 함수입니다.
    /// </summary>
    private void GameClear()
    {
        isGameCleared = true; // 게임을 클리어 상태로 변경합니다.
        Time.timeScale = 0f; // 게임의 시간을 멈춥니다. (모든 움직임과 업데이트가 정지됨)

        // clearText UI가 할당되어 있다면,
        if (clearText != null)
        {
            clearText.text = "Day1 Stage Clear"; // 텍스트 내용을 설정하고,
            clearText.gameObject.SetActive(true); // UI를 활성화하여 화면에 보이게 합니다.
        }
        Debug.Log("Day1 Stage Clear!"); // 콘솔에도 클리어 메시지를 출력합니다.
    }

    // --- 외부 제공 함수 및 프로퍼티들 --- //

    /// <summary>
    /// 현재 획득한 아이템 개수(점수)를 반환하는 읽기 전용 프로퍼티입니다.
    /// </summary>
    public int CurrentScore => itemCount;

    /// <summary>
    /// 그리드의 크기(가로, 세로 개수)를 Vector2Int 형태로 반환합니다.
    /// </summary>
    public Vector2Int GetGridDimensions() => new Vector2Int(gridWidth, gridHeight);

    /// <summary>
    /// 그리드의 중앙 셀 좌표를 반환합니다.
    /// </summary>
    public Vector2Int GetGridCenter() => new Vector2Int(gridWidth / 2, gridHeight / 2);

    /// <summary>
    /// 주어진 좌표(pos)가 그리드 범위 내에 있는지 확인하는 함수입니다.
    /// </summary>
    public bool IsCellExists(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }

    /// <summary>
    /// 플레이어 주변(자신 포함 7개 셀)에 아이템을 스폰합니다. (현재 데모에서는 미사용)
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
    /// 그리드 내의 랜덤한 위치에 아이템을 1개 스폰합니다. (현재 데모에서는 미사용, 필요시 아이템 스포너에서 활용 가능)
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
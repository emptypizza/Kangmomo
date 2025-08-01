using System.Collections;
using UnityEngine;

/// <summary>
/// 적 타입 열거형 - 고정형 Static, 이동형 Walker
/// </summary>
public enum EnemyType
{
    Static,
    Walker
}

/// <summary>
/// Enemy 클래스 - Static은 일정 시간 후 제거, Walker는 그리드를 따라 이동
/// 역할: 적의 생명 주기(생성, 이동, 소멸)와 플레이어와의 상호작용을 담당합니다.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("공통 설정")]
    public EnemyType enemyType = EnemyType.Static;

    [Header("Static 타입 설정")]
    [Tooltip("Static 타입의 적이 활성화된 후 사라지기까지의 시간")]
    public float staticLifeTime = 3f;

    [Header("Walker 타입 설정")]
    [Tooltip("Walker의 초당 이동 속도")]
    public float walkerSpeed = 2f;
    [Tooltip("이동 후 다음 행동까지의 짧은 딜레이")]
    public float postMoveDelay = 0.05f;

    // --- 내부 상태 변수 ---
    private Vector2Int hexPos;
    private Vector2Int moveDir;
    private Coroutine walkCoroutine;

    // --- 캐싱된 참조 ---
    private GameManager gameManager;
    private Player player;

    #region Unity Lifecycle

    private void Awake()
    {
        // 핵심 참조 미리 캐싱하여 안정성 및 성능 확보
        gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            player = gameManager.player;
        }
    }

    private void OnDisable()
    {
        // 비활성화될 때 실행 중인 코루틴이 있다면 확실하게 중지
        if (walkCoroutine != null)
        {
            StopCoroutine(walkCoroutine);
            walkCoroutine = null;
        }
    }

    void Update()
    {
        // 방어 코드: 핵심 참조가 없다면 아무것도 실행하지 않음
        if (gameManager == null || player == null)
        {
            Debug.LogError("GameManager 또는 Player 참조를 찾을 수 없습니다. Enemy를 비활성화합니다.");
            gameObject.SetActive(false); // Destroy 대신 비활성화 (오브젝트 풀링 대비)
            return;
        }

        if (enemyType == EnemyType.Static)
        {
            // Static 타입은 간단하게 수명만 관리
            // Init에서 설정된 staticLifeTime을 별도 타이머 없이 직접 감소
            staticLifeTime -= Time.deltaTime;
            if (staticLifeTime <= 0f)
            {
                gameObject.SetActive(false); // Destroy 대신 비활성화
            }
        }
    }

    #endregion

    /// <summary>
    /// 외부(EnemySpawner)에서 적 생성 시 호출되는 초기화 함수
    /// </summary>
    public void Init(Vector2Int spawnHex, EnemyType type, Vector2Int dir)
    {
        // 방어 코드
        if (gameManager == null || player == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // 상태 초기화
        hexPos = spawnHex;
        enemyType = type;
        moveDir = dir;
        transform.position = player.HexToWorld(hexPos);

        if (enemyType == EnemyType.Walker)
        {
            // Walker일 경우, 즉시 다음 목적지로 이동 시작
            TryWalkToNextHex();
        }
        // Static 타입은 Update에서 수명을 관리하므로 별도 로직 불필요
    }

    /// <summary>
    /// Walker: 다음 헥사 타일로 이동을 시도합니다.
    /// </summary>
    private void TryWalkToNextHex()
    {
        Vector2Int nextHex = hexPos + moveDir;

        // 다음 목적지가 그리드 내에 존재하는지 확인
        if (gameManager.IsCellExists(nextHex))
        {
            // 이미 실행 중인 코루틴이 있다면 중복 실행 방지
            if (walkCoroutine != null) StopCoroutine(walkCoroutine);
            walkCoroutine = StartCoroutine(WalkToHex(nextHex));
        }
        else
        {
            // 그리드 밖으로 벗어났으므로 비활성화
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Walker가 한 칸 부드럽게 이동하는 코루틴
    /// </summary>
    private IEnumerator WalkToHex(Vector2Int targetHex)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = player.HexToWorld(targetHex);

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / walkerSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        hexPos = targetHex;

        // 이동 완료 후 짧은 딜레이
        if (postMoveDelay > 0)
        {
            yield return new WaitForSeconds(postMoveDelay);
        }

        walkCoroutine = null; // 코루틴 완료 처리

        // 다음 이동 시도
        TryWalkToNextHex();
    }

    #region Collision & Interaction

    /// <summary>
    /// 플레이어와 충돌 시 넉백 및 HP 감소 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 태그가 아니거나, 캐싱된 player 객체와 다르면 무시
        if (!collision.CompareTag("Player") || collision.gameObject != player.gameObject)
        {
            return;
        }

        // Enemy → Player 방향을 기준으로 반대방향 넉백
        Vector2 hitDir = (player.transform.position - transform.position).normalized;
        Vector2Int knockbackDir = GetClosestHexDirection(hitDir);

        player.Knockback(knockbackDir);
        player.Hit(1); // 데미지는 상수로 관리하면 더 좋습니다. e.g., private const int DAMAGE = 1;

        Debug.Log($"적 충돌: 플레이어 넉백({knockbackDir}) 및 HP 감소");

        // 플레이어와 충돌 시 즉시 사라지도록 처리
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 벡터 방향을 가장 가까운 헥사 6방향 중 하나로 변환합니다.
    /// </summary>
    private Vector2Int GetClosestHexDirection(Vector2 direction)
    {
        // 참고: Axial 좌표계의 6방향 벡터
        Vector2Int[] hexDirections = {
            new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1)
        };

        float maxDot = -Mathf.Infinity;
        Vector2Int bestDirection = Vector2Int.zero;

        foreach (var hexDir in hexDirections)
        {
            // 정규화된 벡터 간의 내적을 통해 가장 유사한 방향을 찾음
            float dot = Vector2.Dot(direction.normalized, ((Vector2)hexDir).normalized);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestDirection = hexDir;
            }
        }
        return bestDirection;
    }
    #endregion
}
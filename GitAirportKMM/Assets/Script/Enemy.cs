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
/// </summary>
public class Enemy : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Static;

    [Header("Static 타입 설정")]
    public float staticLifeTime = 3f;

    [Header("Walker 설정")]
    public float walkerSpeed = 2f;             // Walker 속도 (유닛/초)
    public float outOfGridDestroyDelay = 3f;   // 그리드 밖 나간 후 제거 대기 시간

    // 내부 상태
    private Vector2Int hexPos;        // 현재 헥사 좌표
    private Vector2Int moveDir;       // 이동 방향 (Walker용)
    private float timer = 0f;         // Static 생존 시간
    private float outTimer = 0f;      // 화면 밖 대기 타이머
    private bool isOutOfGrid = false; // Walker가 그리드 밖으로 나갔는지
    private bool isWalking = false;   // Walker가 현재 이동 중인지

    /// <summary>
    /// 외부에서 적 생성 시 호출되는 초기화 함수
    /// </summary>
    public void Init(Vector2Int spawnHex, EnemyType type, Vector2Int dir)
    {
        hexPos = spawnHex;
        enemyType = type;
        moveDir = dir;

        transform.position = GameManager.Instance.player.HexToWorld(hexPos);

        if (enemyType == EnemyType.Static)
            timer = staticLifeTime;
        else
            timer = 9999f;

        isOutOfGrid = false;
        outTimer = outOfGridDestroyDelay;
    }

    void Update()
    {
        if (enemyType == EnemyType.Static)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
                Destroy(gameObject);
        }
        else if (enemyType == EnemyType.Walker)
        {
            WalkerUpdate();
        }
    }

    /// <summary>
    /// Walker 전용 Update 로직: 한 칸씩 이동하며 그리드 밖 나가면 제거 대기
    /// </summary>
    void WalkerUpdate()
    {
        if (isWalking) return;

        if (!isOutOfGrid)
        {
            Vector2Int nextHex = hexPos + moveDir;
            if (GameManager.Instance.IsCellExists(nextHex))
            {
                StartCoroutine(WalkToHex(nextHex));
            }
            else
            {
                isOutOfGrid = true;
            }
        }
        else
        {
            outTimer -= Time.deltaTime;
            if (outTimer <= 0f)
                Destroy(gameObject);
        }
    }

    /// <summary>
    /// Walker가 한 칸 부드럽게 이동하는 코루틴 (속도 반영)
    /// </summary>
    private IEnumerator WalkToHex(Vector2Int targetHex)
    {
        isWalking = true;

        Vector3 start = transform.position;
        Vector3 end = GameManager.Instance.player.HexToWorld(targetHex);

        float dist = Vector3.Distance(start, end);
        float travelTime = dist / walkerSpeed;
        float elapsed = 0f;

        while (elapsed < travelTime)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / travelTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        hexPos = targetHex;

        isWalking = false;

        yield return new WaitForSeconds(0.05f); // 살짝 텀 두고 다음 이동
    }

    /// <summary>
    /// 플레이어와 충돌 시 넉백 및 HP 감소 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                // Enemy → Player 방향을 기준으로 반대방향 넉백
                Vector2 hitDir = (player.transform.position - transform.position).normalized;
                Vector2Int knockbackDir = GetClosestHexDirection(hitDir);

                player.Knockback(knockbackDir);
                player.Hit(1);

                Debug.Log("적 충돌: 넉백 + HP 감소");
            }
        }
    }

    /// <summary>
    /// 벡터 방향을 가장 가까운 헥사 6방향으로 변환
    /// </summary>
    Vector2Int GetClosestHexDirection(Vector2 hitDir)
    {
        Vector2Int[] hexDirs = {
            new Vector2Int(1, 0),  new Vector2Int(0, 1),  new Vector2Int(-1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1)
        };

        float maxDot = -Mathf.Infinity;
        Vector2Int bestDir = hexDirs[0];

        foreach (var dir in hexDirs)
        {
            float dot = Vector2.Dot(hitDir, ((Vector2)dir).normalized);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestDir = dir;
            }
        }

        return bestDir;
    }
}

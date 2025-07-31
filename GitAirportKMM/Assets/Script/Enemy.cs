using UnityEngine;

/// <summary>
/// 적 타입 정의: Static(고정), Walker(직선 이동)
/// </summary>
public enum EnemyType
{
    Static, // 일정 시간 후 사라지는 적
    Walker  // 직선 이동 후 화면 밖에서 사라지는 적
}

/// <summary>
/// Enemy 클래스: Static은 제자리, Walker는 한 방향으로 쭉 이동
/// </summary>
public class Enemy : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Static;

    [Header("Static 타입 설정")]
    public float staticLifeTime = 3f; // Static 타입 수명

    [Header("Walker 타입 설정")]
    public float walkerSpeed = 2f;    // Walker 타입 이동 속도
    private Vector2 moveDirection;    // Walker 이동 방향(단위 벡터)

    /// <summary>
    /// EnemySpawner에서 적 생성시 타입/방향 지정
    /// </summary>
    /// <param name="type">적 타입</param>
    /// <param name="direction">Walker 이동 방향(Vector2Int)</param>
    public void Init(EnemyType type, Vector2Int direction)
    {
        this.enemyType = type;
        // 반드시 Vector2로 변환 후 normalized!
        this.moveDirection = ((Vector2)direction).normalized;

        if (this.enemyType == EnemyType.Static)
        {
            Destroy(gameObject, staticLifeTime); // Static은 일정 시간 후 Destroy
        }
    }

    void Update()
    {
        // Walker 타입이면 매 프레임 지정 방향으로 이동
        if (enemyType == EnemyType.Walker)
        {
            transform.Translate(moveDirection * walkerSpeed * Time.deltaTime, Space.World);
        }
    }

    /// <summary>
    /// Walker 타입이 카메라 밖으로 벗어나면 자동 Destroy
    /// </summary>
    private void OnBecameInvisible()
    {
        if (enemyType == EnemyType.Walker)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 플레이어와 충돌 체크 (필요에 따라 구현)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                // 충돌방향 = Enemy → Player 방향 (즉, Player가 Enemy 반대방향으로 팅김)
                Vector2 hitDir = (player.transform.position - transform.position).normalized;

                // 가장 가까운 6방향 중 하나로 변환 (헥사)
                Vector2Int knockbackDir = GetClosestHexDirection(hitDir);

                player.Knockback(knockbackDir);
                player.Hit(1); // HP 1 감소

                Debug.Log("Enemy가 Player와 충돌! 한 칸 팅김 + HP 1 깎임");
            }
        }
    }

    /// <summary>
    /// 실수방향을 헥사 그리드 6방향(Vector2Int) 중 가장 가까운 방향으로 변환
    /// </summary>
    private Vector2Int GetClosestHexDirection(Vector2 hitDir)
    {
        Vector2Int[] hexDirs = {
        new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(-1,1),
        new Vector2Int(-1,0), new Vector2Int(0,-1), new Vector2Int(1,-1)
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

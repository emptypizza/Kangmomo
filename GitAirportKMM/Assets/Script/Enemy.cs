using System.Collections;
using UnityEngine;

public enum EnemyType
{
    Static,
    Walker
}

public class Enemy : MonoBehaviour
{
    public EnemyType enemyType = EnemyType.Static;
    public float staticLifeTime = 3f;
    public float walkerSpeed = 2f;
    public float outOfGridDestroyDelay = 3f;

    private Vector2Int hexPos;
    private Vector2Int moveDir;
    private float timer = 0f;
    private bool isOutOfGrid = false;
    private float outTimer = 0f;

    public void Init(Vector2Int spawnHex, EnemyType type, Vector2Int dir)
    {
        hexPos = spawnHex;
        enemyType = type;
        moveDir = dir;
        transform.position = GameManager.Instance.player.HexToWorld(hexPos);
        timer = (enemyType == EnemyType.Static) ? staticLifeTime : 9999f;
        isOutOfGrid = false;
        outTimer = outOfGridDestroyDelay;
    }

    void Update()
    {
        if (enemyType == EnemyType.Static)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) Destroy(gameObject);
        }
        else if (enemyType == EnemyType.Walker)
        {
            WalkerUpdate();
        }
    }

    void WalkerUpdate()
    {
        if (!isOutOfGrid)
        {
            Vector2Int nextHex = hexPos + moveDir;
            if (GameManager.Instance.IsCellExists(nextHex))
            {
                hexPos = nextHex;
                transform.position = GameManager.Instance.player.HexToWorld(hexPos);
            }
            else
            {
                isOutOfGrid = true;
                // 여기서 바로 사라지지 않고 3초 기다림
            }
        }
        else
        {
            outTimer -= Time.deltaTime;
            if (outTimer <= 0f)
                Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var player = collision.GetComponent<Player>();
            if (player != null)
                player.OnEnemyHit(hexPos);
        }
    }
}

/*
public enum EnemyType
{
    Static, // 고정형
    Walker  // 한 방향 이동형
}

public class Enemy : MonoBehaviour
{
    [Header("타입별 설정")]
    public EnemyType enemyType = EnemyType.Static;
    public float staticLifeTime = 3f;           // 고정형일 때 생존 시간
    public float walkerSpeed = 2f;              // 이동형일 때 이동 속도 (현재 로직에서는 즉시 이동)
    public float outOfGridDestroyDelay = 3f;    // 이동형이 그리드를 벗어난 후 사라지기까지의 시간

    [Header("내부 상태 변수")]
    private Vector2Int hexPos;                  // 현재 그리드 좌표
    private Vector2Int moveDir;                 // 이동 방향 (Walker 타입용)
    private float lifeTimer = 0f;               // 생존 시간 타이머
    private bool isOutOfGrid = false;           // 그리드 이탈 여부
    private float outOfGridTimer = 0f;          // 그리드 이탈 후 타이머

    /// <summary>
    /// 스포너에서 적을 생성할 때 호출하는 초기화 함수
    /// </summary>
    public void Init(Vector2Int spawnHex, EnemyType type, Vector2Int dir)
    {
        // 기본 정보 설정
        hexPos = spawnHex;
        enemyType = type;
        moveDir = dir;
        transform.position = GameManager.Instance.player.HexToWorld(hexPos);

        // 타입에 따른 타이머 초기화
        if (enemyType == EnemyType.Static)
        {
            lifeTimer = staticLifeTime;
        }

        // 상태 초기화
        isOutOfGrid = false;
        outOfGridTimer = outOfGridDestroyDelay;
    }

    void Update()
    {
        // 타입에 따라 다른 업데이트 로직 실행
        if (enemyType == EnemyType.Static)
        {
            StaticTypeUpdate();
        }
        else if (enemyType == EnemyType.Walker)
        {
            WalkerTypeUpdate();
        }
    }

    /// <summary>
    /// 고정형(Static) 타입의 업데이트 로직
    /// </summary>
    private void StaticTypeUpdate()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 이동형(Walker) 타입의 업데이트 로직
    /// </summary>
    private void WalkerTypeUpdate()
    {
        // 아직 그리드 안에 있을 때
        if (!isOutOfGrid)
        {
            // 한 프레임에 한 칸씩 즉시 이동 (부드러운 이동을 원하면 수정 필요)
            Vector2Int nextHex = hexPos + moveDir;
            if (GameManager.Instance.IsCellExists(nextHex))
            {
                hexPos = nextHex;
                transform.position = GameManager.Instance.player.HexToWorld(hexPos);
            }
            else
            {
                // 그리드를 벗어남
                isOutOfGrid = true;
            }
        }
        // 그리드를 벗어났을 때
        else
        {
            outOfGridTimer -= Time.deltaTime;
            if (outOfGridTimer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// 플레이어와 충돌했을 때 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.OnEnemyHit(hexPos);
            }
        }
    }
}
*/
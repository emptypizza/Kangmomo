using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 플레이어: 육각형 그리드 기반 이동 + HP 및 적과의 충돌 처리
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Hex 이동 설정")]
    public float moveSpeed = 5f;

    [Header("기본 스탯")]
    public int nHP = 3;

    private bool isMoving = false;
    private Rigidbody2D rb;

    private Vector2Int hexPos = Vector2Int.zero;     // 현재 헥사 좌표
    private Vector2Int lastSafeHexPos = Vector2Int.zero; // 적과 부딪히기 전 위치

    // Flat-Top 기준 6방향
    private static readonly Vector2Int[] hexDirections = {
        new Vector2Int(1, 0),    // E
        new Vector2Int(0, 1),    // NE
        new Vector2Int(-1, 1),   // NW
        new Vector2Int(-1, 0),   // W
        new Vector2Int(0, -1),   // SW
        new Vector2Int(1, -1)    // SE
    };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 중앙에서 시작
        if (GameManager.Instance != null)
        {
            Vector2Int centerHex = GameManager.Instance.GetGridCenter();
            hexPos = centerHex;
            lastSafeHexPos = hexPos;
            transform.position = HexToWorld(hexPos);
        }
        else
        {
            hexPos = WorldToHex(transform.position);
            lastSafeHexPos = hexPos;
            transform.position = HexToWorld(hexPos);
        }
    }

    void Update()
    {
        if (isMoving) return;

        // 키패드로 6방향 이동
        if (Input.GetKeyDown(KeyCode.Keypad6)) TryMove(0); // E
        if (Input.GetKeyDown(KeyCode.Keypad9)) TryMove(1); // NE
        if (Input.GetKeyDown(KeyCode.Keypad7)) TryMove(2); // NW
        if (Input.GetKeyDown(KeyCode.Keypad4)) TryMove(3); // W
        if (Input.GetKeyDown(KeyCode.Keypad1)) TryMove(4); // SW
        if (Input.GetKeyDown(KeyCode.Keypad3)) TryMove(5); // SE
    }

    /// <summary>
    /// 외부에서 방향 인덱스로 이동 요청
    /// </summary>
    public void MoveByIndex(int dirIndex)
    {
        TryMove(dirIndex);
    }

    private void TryMove(int dirIndex)
    {
        if (isMoving || dirIndex < 0 || dirIndex > 5) return;

        Vector2Int nextHex = hexPos + hexDirections[dirIndex];
        if (GameManager.Instance != null && !GameManager.Instance.IsCellExists(nextHex))
            return;

        // 이동 시작 전에 안전지점 저장
        lastSafeHexPos = hexPos;

        Vector3 worldTarget = HexToWorld(nextHex);
        StartCoroutine(MoveRoutine(worldTarget));
        hexPos = nextHex;
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
        isMoving = false;
    }

    /// <summary>
    /// 적과 충돌했을 때 호출되는 함수 (적이 직접 호출)
    /// </summary>
    public void OnEnemyHit(Vector2Int enemyHexPos)
    {
        StopAllCoroutines();
        isMoving = false;

        nHP -= 1;
        Debug.Log($"적과 충돌! HP 감소: {nHP}");

        // UIManager.Instance.UpdateHPUI(nHP); // UI 갱신이 필요하다면

        // 마지막 안전지점으로 되돌림
        hexPos = lastSafeHexPos;
        transform.position = HexToWorld(lastSafeHexPos);
    }

    /// <summary>
    /// 헥사 → 월드 변환 (GameManager의 중앙정렬 오프셋 적용)
    /// </summary>
    public Vector3 HexToWorld(Vector2Int hex)
    {
        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;

        float offsetX = -width * 0.75f * (GameManager.Instance.GetGridDimensions().x - 1) / 2f;
        float offsetY = -height * (GameManager.Instance.GetGridDimensions().y - 1) / 2f;

        float x = width * (0.75f * hex.x) + offsetX;
        float y = height * (hex.y + 0.5f * (hex.x & 1)) + offsetY;
        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// 월드 → 헥사 변환 (중앙정렬 offset 적용)
    /// </summary>
    public Vector2Int WorldToHex(Vector3 pos)
    {
        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;

        float offsetX = -width * 0.75f * (GameManager.Instance.GetGridDimensions().x - 1) / 2f;
        float offsetY = -height * (GameManager.Instance.GetGridDimensions().y - 1) / 2f;

        float px = pos.x - offsetX;
        float py = pos.y - offsetY;

        int q = Mathf.RoundToInt(px / (width * 0.75f));
        int r = Mathf.RoundToInt((py - (q & 1) * height * 0.5f) / height);
        return new Vector2Int(q, r);
    }

    /// <summary>
    /// 트리거 충돌 처리(아이템 등)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("GridCell"))
        {
            var cell = collision.GetComponent<Cell>();
            cell?.ActivateCell();
        }
        if (collision.CompareTag("Trash"))
        {
            GameManager.Instance.CollectItem();
            nHP += 1; // 아이템 획득 시 HP 증가 예시
            Destroy(collision.gameObject);
        }
    }
    public void MoveByPath(List<Vector2Int> path)
    {
        // 예시: 경로를 따라 이동하는 코루틴 호출
        StartCoroutine(MoveByPathCoroutine(path));
    }

    private IEnumerator MoveByPathCoroutine(List<Vector2Int> path)
    {
        isMoving = true;
        foreach (var hex in path)
        {
            Vector3 worldTarget = HexToWorld(hex);
            while (Vector3.Distance(transform.position, worldTarget) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, worldTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = worldTarget;
            hexPos = hex;
            yield return new WaitForSeconds(0.05f);
        }
        isMoving = false;
    }

}

/*
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Hex 이동 설정")]
    public float moveSpeed = 5f;

    [Header("기본 스탯")]
    public int nHP = 3;

    private bool isMoving = false;
    private Rigidbody2D rb;
    private Vector2Int hexPos = Vector2Int.zero; // 현재 헥사 좌표


    public void OnEnemyHit(Vector2Int enemyHex)
    {
        nHP = Mathf.Max(nHP - 1, 0);

        // 무작위 이동 취소: 이전 hexPos로 강제 이동
        // hexPrevPos는 직접 구현(이동하기 전에 저장해두면 됨)
        if (hexPrevPos != null)
            hexPos = hexPrevPos;

        transform.position = HexToWorld(hexPos);
        // 충돌 효과, 무적 타임 등 추가 가능
    }

    // Flat-Top 기준 방향 (E, NE, NW, W, SW, SE)
    private static readonly Vector2Int[] hexDirections = new Vector2Int[]
    {
        new Vector2Int(1, 0),    // E
        new Vector2Int(0, 1),    // NE
        new Vector2Int(-1, 1),   // NW
        new Vector2Int(-1, 0),   // W
        new Vector2Int(0, -1),   // SW
        new Vector2Int(1, -1)    // SE
    };

    void Start()
    {   rb = GetComponent<Rigidbody2D>();

        // 중앙 그리드에서 시작
        if (GameManager.Instance != null)
        {
            Vector2Int centerHex = GameManager.Instance.GetGridCenter();
            hexPos = centerHex;
            transform.position = HexToWorld(hexPos);
        }
        else
        {
            // GameManager 없을 때 위치 맞춤
            hexPos = WorldToHex(transform.position);
            transform.position = HexToWorld(hexPos);
        }
    }


    public void MoveByPath(List<Vector2Int> path)
    {
        // 예시: 경로를 따라 이동하는 코루틴 호출
        StartCoroutine(MoveByPathCoroutine(path));
    }

    private IEnumerator MoveByPathCoroutine(List<Vector2Int> path)
    {
        isMoving = true;
        foreach (var hex in path)
        {
            Vector3 worldTarget = HexToWorld(hex);
            while (Vector3.Distance(transform.position, worldTarget) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, worldTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = worldTarget;
            hexPos = hex;
            yield return new WaitForSeconds(0.05f);
        }
        isMoving = false;
    }
 
    private IEnumerator MoveByPathCoroutine(List<Vector2Int> path)
    {
        isMoving = true;
        foreach (var hex in path)
        {
            Vector3 worldTarget = HexToWorld(hex);
            while (Vector3.Distance(transform.position, worldTarget) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, worldTarget, moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = worldTarget;
            hexPos = hex;
            yield return new WaitForSeconds(0.05f);
        }
        isMoving = false;
    }
   // Player.cs 안에 추가
    public void MoveByPath(List<Vector2Int> path)
    {
        if (isMoving || path == null || path.Count == 0) return;
        StartCoroutine(MoveByPathCoroutine(path));
    }

    void Update()
    {
        if (isMoving) return;

        // 키패드 숫자 키로 이동
        if (Input.GetKeyDown(KeyCode.Keypad6)) TryMove(0); // E
        if (Input.GetKeyDown(KeyCode.Keypad9)) TryMove(1); // NE
        if (Input.GetKeyDown(KeyCode.Keypad7)) TryMove(2); // NW
        if (Input.GetKeyDown(KeyCode.Keypad4)) TryMove(3); // W
        if (Input.GetKeyDown(KeyCode.Keypad1)) TryMove(4); // SW
        if (Input.GetKeyDown(KeyCode.Keypad3)) TryMove(5); // SE
    }

    /// <summary>
    /// 외부에서 방향 인덱스로 이동 요청
    /// </summary>
    public void MoveByIndex(int dirIndex)
    {
        TryMove(dirIndex);
    }

    /// <summary>
    /// Hex 좌표 기준으로 이동 시도
    /// </summary>
    private void TryMove(int dirIndex)
    {
        if (isMoving || dirIndex < 0 || dirIndex > 5) return;

        Vector2Int nextHex = hexPos + hexDirections[dirIndex];

        // 그리드 범위 체크
        if (GameManager.Instance != null && !GameManager.Instance.IsCellExists(nextHex))
            return;

        Vector3 worldTarget = HexToWorld(nextHex);
        StartCoroutine(MoveRoutine(worldTarget));
        hexPos = nextHex;
    }

    private IEnumerator MoveRoutine(Vector3 destination)
    {
        isMoving = true;
        while (Vector3.Distance(transform.position, destination) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = destination;
        isMoving = false;
    }

    /// <summary>
    /// Hex → 월드 변환 (GameManager 그리드 중앙 정렬과 완벽 동기화)
    /// </summary>
    public Vector3 HexToWorld(Vector2Int hex)
    {
        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;

        // GameManager에서 사용하는 offset 계산식과 동일
        float offsetX = -width * 0.75f * (GameManager.Instance.GetGridDimensions().x - 1) / 2f;
        float offsetY = -height * (GameManager.Instance.GetGridDimensions().y - 1) / 2f;

        float x = width * (0.75f * hex.x) + offsetX;
        float y = height * (hex.y + 0.5f * (hex.x & 1)) + offsetY;

        return new Vector3(x, y, 0);


/// <summary>
    /// 월드 → Hex 좌표 변환 (중앙정렬 offset 감안)
    /// </summary>
    public Vector2Int WorldToHex(Vector3 pos)
    {
        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;

        float offsetX = -width * 0.75f * (GameManager.Instance.GetGridDimensions().x - 1) / 2f;
        float offsetY = -height * (GameManager.Instance.GetGridDimensions().y - 1) / 2f;

        float px = pos.x - offsetX;
        float py = pos.y - offsetY;

        int q = Mathf.RoundToInt(px / (width * 0.75f));
        int r = Mathf.RoundToInt((py - (q & 1) * height * 0.5f) / height);

        return new Vector2Int(q, r);
    }

    /// <summary>
    /// 외부에서 Hex 위치로 이동 (스냅)
    /// </summary>
    public void MoveToHex(Vector2Int targetHex)
    {
        if (isMoving) return;

        Vector3 targetWorld = HexToWorld(targetHex);
        StartCoroutine(MoveRoutine(targetWorld));
        hexPos = targetHex;
    }

    /// <summary>
    /// 외부에서 월드 위치로 이동 요청시 (자동 셀스냅)
    /// </summary>
    public void MoveToWorldPosition(Vector3 worldPos)
    {
        if (isMoving) return;

        Vector2Int targetHex = WorldToHex(worldPos);
        Vector3 targetWorld = HexToWorld(targetHex);

        StartCoroutine(MoveRoutine(targetWorld));
        hexPos = targetHex;
    }

    /// <summary>
    /// 트리거 충돌 처리 (그리드 셀/아이템 등)
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("GridCell"))
        {
            var cell = collision.GetComponent<Cell>();
            cell?.ActivateCell();
        }

        if (collision.CompareTag("Trash"))
        {
            GameManager.Instance.CollectItem();
            nHP += 1; // HP 증가
            Destroy(collision.gameObject);
        }
    }
}

    }*/
/*    public void MoveToWorldPosition(Vector3 worldPos)
    {
        if (isMoving) return;

        Vector2Int targetHex = WorldToHex(worldPos);
        Vector3 targetWorld = HexToWorld(targetHex);

        StartCoroutine(MoveRoutine(targetWorld));
        hexPos = targetHex; } */
/* 

// GameManager의 Offset을 동일하게 적용
public Vector3 HexToWorld(Vector2Int hex)
{
   float width = 1f;
   float height = Mathf.Sqrt(3f) / 2f * width;

   float offsetX = -width * 0.75f * (GameManager.Instance.GetGridDimensions().x - 1) / 2f;
   float offsetY = -height * (GameManager.Instance.GetGridDimensions().y - 1) / 2f;

   float x = width * (3f / 4f * hex.x) + offsetX;
   float y = height * (hex.y + 0.5f * (hex.x & 1)) + offsetY;

   return new Vector3(x, y, 0);
}
// 기존: private → 수정 후: public

// 육각형 좌표 → 월드 좌표 변환 (Flat-Top 기준)

public Vector3 HexToWorld(Vector2Int hex)
{
   float width = 1f;
   float height = Mathf.Sqrt(3f) / 2f * width;

   float x = width * (3f / 4f * hex.x);
   float y = height * (hex.y + 0.5f * (hex.x & 1));

   return new Vector3(x, y, 0);
}
*/



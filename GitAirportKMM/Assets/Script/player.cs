using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;

    [Header("플레이어 스탯")]
    public int nHP = 3;

    private Rigidbody2D rb;
    public bool isMoving = false;
    private Vector2Int currentHex; // 플레이어의 현재 헥사 좌표

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.isKinematic = true; // 물리 효과 대신 스크립트로 위치를 제어하므로 Kinematic으로 설정
    }

    void Start()
    {
        // 게임 시작 시 그리드 중앙에 플레이어 배치
        if (GameManager.Instance != null)
        {
            currentHex = GameManager.Instance.GetGridCenter();
            transform.position = HexToWorld(currentHex);
        }
    }

    /// <summary>
    /// UIcode로부터 받은 경로(path)를 따라 한 칸씩 이동하는 코루틴을 시작합니다.
    /// </summary>
    public void MoveByPath(List<Vector2Int> path)
    {
        if (isMoving || path == null || path.Count == 0)
            return;

        StartCoroutine(MovePathCoroutine(path));
    }

    /// <summary>
    /// 경로를 따라 실제로 이동을 처리하는 코루틴입니다.
    /// </summary>
    private IEnumerator MovePathCoroutine(List<Vector2Int> path)
    {
        isMoving = true;

        foreach (var targetHex in path)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = HexToWorld(targetHex);
            float journey = 0f;

            // 한 칸을 부드럽게 이동
            while (journey < 0.2f) // 0.2초 동안 한 칸 이동
            {
                journey += Time.deltaTime;
                float percent = Mathf.Clamp01(journey / 0.2f);
                transform.position = Vector3.Lerp(startPos, endPos, percent);
                yield return null;
            }
            transform.position = endPos;
            currentHex = targetHex; // 현재 위치 갱신
        }

        isMoving = false;
    }

    /// <summary>
    /// 적과 충돌 시 HP 감소 및 넉백 처리
    /// </summary>
    public void Hit(int damage)
    {
        nHP -= damage;
        Debug.Log($"HP 감소! 현재 HP: {nHP}");
        if (nHP <= 0)
        {
            Debug.Log("게임 오버");
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }



    public void Knockback(Vector2Int knockbackDir)
    {

        //if (isMoving) return;
        // 현재 위치에서 한 칸 밀림
        Vector2Int targetHex = currentHex + knockbackDir;
        // 넉백될 위치가 맵 안인지 확인
        if (GameManager.Instance.IsCellExists(targetHex))
        {
            /* currentHex = targetHex;
            transform.position = HexToWorld(currentHex);*/
            StopAllCoroutines(); // 이동 중이면 중단
            StartCoroutine(KnockbackCoroutine(targetHex));
        }
        else
        {
            Debug.Log("넉백 불가: 바깥 셀");
            // 필요 시 여기서 피격 애니메이션만 재생 가능
        }
    }
    private IEnumerator KnockbackCoroutine(Vector2Int targetHex)
    {
        isMoving = true; // 기존 이동과 충돌 방지

        Vector3 start = transform.position;
        Vector3 end = HexToWorld(targetHex);

        float duration = 0.15f; // 0.15초 동안 넉백
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        currentHex = targetHex;

        isMoving = false;
    }


    /// <summary>
    /// 아이템 획득 등 트리거 충돌 처리
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash"))
        {
            GameManager.Instance.CollectItem();
           
            Destroy(other.gameObject);
            nHP += 1;
        }
        else if (other.CompareTag("GridCell"))
        {
            other.GetComponent<Cell>()?.ActivateCell();
        }
    }


    #region Coordinate Conversion
    // 이 좌표 변환 함수들은 GameManager의 것과 완벽히 동일해야 합니다.
    public Vector3 HexToWorld(Vector2Int hex)
    {
        if (GameManager.Instance == null) return Vector3.zero;

        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;
        Vector2Int gridDim = GameManager.Instance.GetGridDimensions();

        Vector2 offset = new Vector2(
            -width * 0.75f * (gridDim.x - 1) / 2f,
            -height * (gridDim.y - 1) / 2f
        );

        float xPos = width * 0.75f * hex.x + offset.x;
        float yPos = height * (hex.y + 0.5f * (hex.x & 1)) + offset.y;
        return new Vector3(xPos, yPos, 0);
    }

    public Vector2Int WorldToHex(Vector3 worldPos)
    {
        if (GameManager.Instance == null) return Vector2Int.zero;

        float width = 1f;
        float height = Mathf.Sqrt(3f) / 2f * width;
        Vector2Int gridDim = GameManager.Instance.GetGridDimensions();

        Vector2 offset = new Vector2(
            -width * 0.75f * (gridDim.x - 1) / 2f,
            -height * (gridDim.y - 1) / 2f
        );

        float px = worldPos.x - offset.x;
        float py = worldPos.y - offset.y;

        int q = Mathf.RoundToInt(px / (width * 0.75f));
        int r = Mathf.RoundToInt((py - height * 0.5f * (q & 1)) / height);
        return new Vector2Int(q, r);
    }
    #endregion
}
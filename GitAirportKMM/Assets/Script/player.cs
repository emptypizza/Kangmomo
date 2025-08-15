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
    private SpriteRenderer spriteRenderer;
    public bool isMoving = false;
    private bool isKnockback = false;
    private bool isInvincible = false;
    private Vector2Int currentHex;

    [Header("넉백 설정")]
    public int knockbackDistance = 0; // 넉백 칸 수 (0: 한 칸만 튕김)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.isKinematic = true;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            currentHex = GameManager.Instance.GetGridCenter();
            transform.position = HexToWorld(currentHex);
        }
    }

    public void MoveByPath(List<Vector2Int> path)
    {
        if (isMoving || path == null || path.Count == 0)
            return;

        StartCoroutine(MovePathCoroutine(path));
    }

    private IEnumerator MovePathCoroutine(List<Vector2Int> path)
    {
        isMoving = true;

        // Activate the starting cell
        GameManager.Instance.GetCellAt(currentHex)?.ActivateTrail();

        foreach (var targetHex in path)
        {
            Cell targetCell = GameManager.Instance.GetCellAt(targetHex);

            // Check for loop closure
            if (targetCell != null && targetCell.currentState == CellState.PlayerTrail)
            {
                GameManager.Instance.ProcessLoop();
                break; // Stop movement and end the path traversal
            }

            // --- Movement Lerp ---
            Vector3 startPos = transform.position;
            Vector3 endPos = HexToWorld(targetHex);
            float journey = 0f;
            float moveDuration = 0.2f;

            while (journey < moveDuration)
            {
                journey += Time.deltaTime;
                float percent = Mathf.Clamp01(journey / moveDuration);
                transform.position = Vector3.Lerp(startPos, endPos, percent);
                yield return null;
            }
            transform.position = endPos;
            // --- End Movement Lerp ---

            currentHex = targetHex;

            // Activate the new cell after moving
            targetCell?.ActivateTrail();
        }

        isMoving = false;
    }

    public void Hit(int damage)
    {
        if (isInvincible) return;

        nHP -= damage;
        Debug.Log($"HP 감소! 현재 HP: {nHP}");
        if (nHP <= 0)
        {
            Debug.Log("게임 오버");
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }

        StartCoroutine(InvincibilityCoroutine());
    }

    /// <summary>
    /// 넉백 방향 한 칸만 적용되도록 고정 구현
    /// </summary>
    public void Knockback(Vector2Int knockbackDir)
    {
        StopAllCoroutines();
        StartCoroutine(KnockbackCoroutine(knockbackDir));
    }

    /// <summary>
    /// 넉백 1칸만 적용되며 확장 가능성은 유지
    /// </summary>
    private IEnumerator KnockbackCoroutine(Vector2Int knockbackDir)
    {
        isMoving = true;
        isKnockback = true;

        Vector2Int nextHex = currentHex + knockbackDir; // 한 칸만 넉백
        if (!GameManager.Instance.IsCellExists(nextHex))
        {
            Debug.Log("넉백 종료: 맵 바깥");
            yield break;
        }

        Vector3 start = transform.position;
        Vector3 end = HexToWorld(nextHex);
        float duration = 0.27f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(start, end, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;
        currentHex = nextHex;

        isMoving = false;
        isKnockback = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Trash"))
        {
            GameManager.Instance.CollectItem();
            Destroy(other.gameObject);
            nHP += 1;
        }
        // The logic for activating cells is now handled in MovePathCoroutine
        // to have more control over the loop detection.
    }

    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        bool visible = true;
        while (elapsed < 2f)
        {
            if (spriteRenderer != null)
            {
                visible = !visible;
                spriteRenderer.enabled = visible;
            }
            yield return new WaitForSeconds(0.2f);
            elapsed += 0.2f;
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        isInvincible = false;
    }

    #region Coordinate Conversion
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

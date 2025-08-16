using UnityEngine;

public enum CellState
{
    Neutral,
    PlayerTrail,
    PlayerCaptured
}

public class Cell : MonoBehaviour
{
    [Header("State")]
    public CellState currentState = CellState.Neutral;
    public Vector2Int hexCoords;

    [Header("Settings")]
    public float trailDuration = 3f; // 트레일 상태가 유지되는 시간

    private float trailTimer = 0f;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateColor();
    }

    private void Update()
    {
        // 트레일 상태일 때만 타이머를 작동시킵니다.
        if (currentState == CellState.PlayerTrail)
        {
            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0)
            {
                // 시간이 다 되면 중립 상태로 되돌립니다.
                SetState(CellState.Neutral);
            }
        }
    }

    /// <summary>
    /// 플레이어가 셀을 밟았을 때 호출됩니다.
    /// 중립 상태일 경우에만 트레일 상태로 변경합니다.
    /// </summary>
    public void ActivateTrail()
    {
        if (currentState == CellState.Neutral)
        {
            SetState(CellState.PlayerTrail);
        }
    }

    /// <summary>
    /// 셀의 상태를 지정하고, 상태에 따른 부가적인 처리를 합니다.
    /// </summary>
    public void SetState(CellState newState)
    {
        if (currentState == newState) return;

        CellState oldState = currentState;
        currentState = newState;

        // 상태 변경에 따른 처리
        if (oldState == CellState.PlayerTrail)
        {
            // 트레일 상태에서 벗어날 때 GameManager에 알림
            GameManager.Instance?.DeregisterTrailCell(this);
        }

        if (newState == CellState.PlayerTrail)
        {
            // 트레일 상태가 될 때 타이머를 설정하고 GameManager에 등록
            trailTimer = trailDuration;
            GameManager.Instance?.RegisterTrailCell(this);
        }
        else
        {
            // 다른 상태가 되면 타이머를 리셋
            trailTimer = 0f;
        }

        UpdateColor();
    }

    /// <summary>
    /// 현재 상태에 따라 셀의 색상을 업데이트합니다.
    /// </summary>
    private void UpdateColor()
    {
        switch (currentState)
        {
            case CellState.Neutral:
                spriteRenderer.color = Color.white;
                break;
            case CellState.PlayerTrail:
                spriteRenderer.color = new Color(0.5f, 0.8f, 1f); // Light Blue
                break;
            case CellState.PlayerCaptured:
                spriteRenderer.color = new Color(0.2f, 0.5f, 1f); // Dark Blue
                break;
        }
    }
}

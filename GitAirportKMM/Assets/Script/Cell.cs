using UnityEngine;

// 셀의 상태를 나타내는 열거형입니다.
public enum CellState
{
    Neutral,      // 중립 상태 (기본)
    PlayerTrail,  // 플레이어가 지나간 길
    PlayerCaptured // 플레이어가 점령한 영역
}

public class Cell : MonoBehaviour
{
    public Vector2Int hexCoords; // 셀의 헥사 그리드 좌표
    public CellState currentState = CellState.Neutral; // 현재 셀의 상태
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateColor(); // 초기 색상 설정
    }

    /// <summary>
    /// 플레이어가 이 셀에 들어왔을 때 호출됩니다.
    /// 상태를 '지나간 길'로 변경합니다.
    /// </summary>
    public void ActivateCell()
    {
        // 이미 점령된 땅은 다시 길로 만들 수 없습니다.
        if (currentState == CellState.PlayerCaptured) return;

        SetState(CellState.PlayerTrail);
    }

    /// <summary>
    /// 셀의 상태를 변경하고 그에 맞는 색상으로 업데이트합니다.
    /// </summary>
    /// <param name="newState">새로운 셀 상태</param>
    public void SetState(CellState newState)
    {
        currentState = newState;
        UpdateColor();
    }

    /// <summary>
    /// 현재 상태에 따라 셀의 색상을 변경합니다.
    /// </summary>
    private void UpdateColor()
    {
        switch (currentState)
        {
            case CellState.Neutral:
                spriteRenderer.color = Color.white; // 중립: 흰색
                break;
            case CellState.PlayerTrail:
                spriteRenderer.color = new Color(0.5f, 0.8f, 1f); // 길: 연한 파란색
                break;
            case CellState.PlayerCaptured:
                spriteRenderer.color = new Color(0.2f, 0.5f, 1f); // 점령지: 진한 파란색
                break;
        }
    }
}

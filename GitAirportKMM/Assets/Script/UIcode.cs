using UnityEngine;
using System.Collections.Generic;

public class UIcode : MonoBehaviour
{
    public Player player;

    void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.GetComponent<Player>();
        }
    }

    void Update()
    {
        if (player == null || GameManager.Instance == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Vector3 clickWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            clickWorldPos.z = 0f;

            Vector2Int currentHex = player.WorldToHex(player.transform.position);

            // 클릭한 곳에서 플레이어 위치로의 방향벡터
            Vector3 diff = clickWorldPos - player.transform.position;

            // 6방향 정의 (Flat-Top Hex 기준)
            Vector2Int[] hexDirections = {
                new Vector2Int(1, 0),    // E
                new Vector2Int(0, 1),    // NE
                new Vector2Int(-1, 1),   // NW
                new Vector2Int(-1, 0),   // W
                new Vector2Int(0, -1),   // SW
                new Vector2Int(1, -1)    // SE
            };

            // 각 방향별 월드 좌표 벡터 계산
            float width = 1f;
            float height = Mathf.Sqrt(3f) / 2f * width;
            Vector3[] dirVectors = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                dirVectors[i] = player.HexToWorld(currentHex + hexDirections[i]) - player.HexToWorld(currentHex);
            }

            // 클릭한 벡터와 각 방향의 내적값(유사도) 최대인 방향 고르기
            int bestDirIndex = 0;
            float bestDot = -Mathf.Infinity;
            for (int i = 0; i < 6; i++)
            {
                float dot = Vector3.Dot(diff.normalized, dirVectors[i].normalized);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDirIndex = i;
                }
            }
            Vector2Int direction = hexDirections[bestDirIndex];

            // 최대 이동 거리: Shift 누르면 3칸, 기본 1칸
            int maxStep = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 3 : 1;

            // 경로 생성(한 칸씩, 막히면 중단)
            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int next = currentHex;
            for (int step = 1; step <= maxStep; step++)
            {
                Vector2Int candidate = currentHex + direction * step;
                if (GameManager.Instance.IsCellExists(candidate))
                    path.Add(candidate);
                else
                    break; // 못가는 셀이면 여기서 멈춤
            }
            if (path.Count > 0)
                player.MoveByPath(path);
        }
    }
}

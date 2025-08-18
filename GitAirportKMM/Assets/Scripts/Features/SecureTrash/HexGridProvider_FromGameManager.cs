using System.Reflection;
using UnityEngine;

/// Drop-in replacement:
/// - 1순위: GameManager의 WorldToHex/HexToWorld(or WorldToAxial/AxialToWorld) 호출
/// - 2순위: Player의 WorldToHex/HexToWorld 호출
/// - 둘 다 없으면 1회 경고 후 안전한 기본값 반환(게임은 계속 진행)
[DefaultExecutionOrder(-5000)]
public class HexGridProvider_FromGameManager : MonoBehaviour, IHexGridProvider
{
    [Header("References (auto-bound if null)")]
    [SerializeField] private GameManager gm;
    [SerializeField] private Player player;

    MethodInfo gmWorldToHex, gmHexToWorld;
    bool warned;

    void Awake()
    {
        // Auto-bind
        if (!gm) gm = FindObjectOfType<GameManager>();
        if (!player)
        {
            var pt = GameObject.FindGameObjectWithTag("Player");
            if (pt) player = pt.GetComponent<Player>();
            if (!player) player = FindObjectOfType<Player>();
        }

        CacheGmMethods();
    }

    void CacheGmMethods()
    {
        if (gm == null) return;
        var t = gm.GetType();
        gmWorldToHex = t.GetMethod("WorldToHex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?? t.GetMethod("WorldToAxial", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        gmHexToWorld = t.GetMethod("HexToWorld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                      ?? t.GetMethod("AxialToWorld", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    public Vector2Int WorldToHex(Vector3 world)
    {
        // 1) GameManager 변환 우선
        if (gm != null && gmWorldToHex != null)
            return (Vector2Int)gmWorldToHex.Invoke(gm, new object[] { world });

        // 2) Player 변환으로 폴백 (당신의 Player에 이미 구현되어 있음)
        if (player != null)
            return player.WorldToHex(world);

        WarnOnce("WorldToHex");
        return Vector2Int.zero;
    }

    public Vector3 HexToWorld(Vector2Int hex)
    {
        if (gm != null && gmHexToWorld != null)
            return (Vector3)gmHexToWorld.Invoke(gm, new object[] { hex });

        if (player != null)
            return player.HexToWorld(hex);

        WarnOnce("HexToWorld");
        return Vector3.zero;
    }

    public Vector2Int PlayerCurrentHex
    {
        get
        {
            if (player != null)
                return WorldToHex(player.transform.position);
            WarnOnce("PlayerCurrentHex");
            return Vector2Int.zero;
        }
    }

    void WarnOnce(string where)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning($"HexGridProvider_FromGameManager fallback used (no GM methods; no Player). Method: {where}", this);
    }
}

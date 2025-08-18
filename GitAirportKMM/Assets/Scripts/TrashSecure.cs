using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Core logic for the "Secure Trash" mechanic. Attach this to a Trash prefab.
/// When a trash item spawns, it designates nearby hexes as "required". The player
/// must step on all required hexes within a time window to capture the trash.
/// </summary>
public class TrashSecure : MonoBehaviour
{
    // Helper enum for selecting hex directions in the Inspector.
    public enum HexDir { E, NE, NW, W, SW, SE }

    // Axial coordinate offsets for each hex direction, as per the prompt.
    private static readonly Dictionary<HexDir, Vector2Int> hexDirectionOffsets = new Dictionary<HexDir, Vector2Int>
    {
        { HexDir.E,  new Vector2Int(1, 0) },
        { HexDir.NE, new Vector2Int(1, -1) },
        { HexDir.NW, new Vector2Int(0, -1) },
        { HexDir.W,  new Vector2Int(-1, 0) },
        { HexDir.SW, new Vector2Int(-1, 1) },
        { HexDir.SE, new Vector2Int(0, 1) }
    };

    [Header("Configuration")]
    [Tooltip("Directions relative to this trash item that become required cells.")]
    public List<HexDir> requiredDirs = new List<HexDir> { HexDir.NW, HexDir.SW };

    [Tooltip("Time in seconds the player has to step on all required cells after stepping on the first one.")]
    public float secureWindow = 3.0f;

    [Tooltip("Delay in seconds before the trash object is destroyed after being captured.")]
    public float captureFxDelay = 0.25f;

    [Header("Visuals")]
    public Color baseColor = Color.yellow;
    public Color progressColor = Color.blue;
    public Color doneColor = Color.green;

    [Header("Dependencies")]
    [SerializeField]
    [Tooltip("A component that implements IHexGridProvider. This is required for the script to function.")]
    private MonoBehaviour gridProviderSource;

    // --- Private State ---
    private IHexGridProvider gridProvider;
    private Vector2Int itemHex;
    private readonly List<Vector2Int> requiredHexes = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> visitedHexes = new HashSet<Vector2Int>();

    private enum State { Idle, Countdown, Captured }
    private State currentState = State.Idle;
    private float countdownTimer = 0f;
    private Vector2Int lastPlayerHex;

    private void Start()
    {
        // 1. Find and validate the Grid Provider
        if (gridProviderSource != null && gridProviderSource is IHexGridProvider provider)
        {
            gridProvider = provider;
        }
        else
        {
            Debug.LogError("TrashSecure requires a valid IHexGridProvider to be assigned in the 'Grid Provider Source' field. Disabling component.", this);
            enabled = false;
            return;
        }

        // 2. Determine required hexes
        itemHex = gridProvider.WorldToHex(transform.position);
        foreach (var dir in requiredDirs)
        {
            if (hexDirectionOffsets.TryGetValue(dir, out var offset))
            {
                requiredHexes.Add(itemHex + offset);
            }
        }

        if (requiredHexes.Count == 0)
        {
            Debug.LogWarning("TrashSecure: No valid required directions set. The trash will be un-securable.", this);
            enabled = false;
            return;
        }

        // 3. Set initial state and visuals
        ResetStateVisuals(baseColor);
        lastPlayerHex = gridProvider.PlayerCurrentHex;
    }

    private void Update()
    {
        if (currentState == State.Captured || !enabled) return;

        // Handle countdown timer
        if (currentState == State.Countdown)
        {
            countdownTimer -= Time.deltaTime;
            if (countdownTimer <= 0f)
            {
                ResetToIdle();
            }
        }

        // Check for player movement onto a required hex
        Vector2Int playerHex = gridProvider.PlayerCurrentHex;
        if (playerHex != lastPlayerHex)
        {
            lastPlayerHex = playerHex;
            if (requiredHexes.Contains(playerHex) && !visitedHexes.Contains(playerHex))
            {
                OnPlayerEnterRequiredHex(playerHex);
            }
        }
    }

    private void OnPlayerEnterRequiredHex(Vector2Int hex)
    {
        visitedHexes.Add(hex);
        GridHighlighter.Instance?.SetColor(hex, progressColor);

        // Start countdown on first interaction
        if (currentState == State.Idle)
        {
            currentState = State.Countdown;
            countdownTimer = secureWindow;
        }

        // Check for completion
        if (visitedHexes.Count == requiredHexes.Count)
        {
            StartCoroutine(CaptureRoutine());
        }
    }

    private void ResetToIdle()
    {
        currentState = State.Idle;
        visitedHexes.Clear();
        countdownTimer = 0f;
        ResetStateVisuals(baseColor);
    }

    private void ResetStateVisuals(Color color)
    {
        if (GridHighlighter.Instance == null) return;
        foreach (var hex in requiredHexes)
        {
            GridHighlighter.Instance.SetColor(hex, color);
        }
    }

    private IEnumerator CaptureRoutine()
    {
        currentState = State.Captured;

        ResetStateVisuals(doneColor);
        GameEvents.RaiseTrashCaptured(transform.position);

        yield return new WaitForSeconds(captureFxDelay);

        // The object will be destroyed, triggering OnDestroy for cleanup.
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // Ensure highlights are cleared when this object is destroyed for any reason.
        if (GridHighlighter.Instance != null && requiredHexes.Count > 0)
        {
            foreach (var hex in requiredHexes)
            {
                GridHighlighter.Instance.Clear(hex);
            }
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // This code only runs in the Unity Editor
        if (gridProviderSource == null || !(gridProviderSource is IHexGridProvider))
        {
            // Try to find one in the component itself for edit-time feedback
            if (GetComponent<IHexGridProvider>() != null)
                 gridProviderSource = GetComponent<MonoBehaviour>();
            else
                return; // Can't draw gizmos without a provider
        }

        IHexGridProvider gizmoProvider = (IHexGridProvider)gridProviderSource;
        Vector2Int currentItemHex = gizmoProvider.WorldToHex(transform.position);

        Gizmos.color = baseColor;
        foreach (var dir in requiredDirs)
        {
            if (hexDirectionOffsets.TryGetValue(dir, out var offset))
            {
                Vector3 worldPos = gizmoProvider.HexToWorld(currentItemHex + offset);
                Gizmos.DrawWireSphere(worldPos, 0.5f); // Use WireSphere for better visibility over solid objects
            }
        }
    }
    #endif
}

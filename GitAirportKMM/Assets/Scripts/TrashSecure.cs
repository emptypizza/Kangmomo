// --- WHAT CHANGED ---
// 1. No longer pre-highlights cells in Start(). Highlights are now created only when the player steps on a cell.
// 2. Enforces exactly 3 required cells, auto-filling or trimming the inspector list as needed.
// 3. Added logic in Start() to handle cases where the player begins on a required cell.
// 4. On success/timeout, uses GridHighlighter.ClearMany() for cleaner highlight removal.
// 5. Colors are now mapped as per the prompt: progressColor (Yellow on step), doneColor (Green on success).

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrashSecure : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The directions relative to this object that must be secured. Will be forced to exactly 3.")]
    public List<HexDir> requiredDirs = new List<HexDir> { HexDir.NW, HexDir.SW };
    [Tooltip("Time in seconds to secure all cells after the first one is entered.")]
    public float secureWindow = 3.0f;
    [Tooltip("Delay after capture before the object is destroyed.")]
    public float captureFxDelay = 0.25f;

    [Header("Visuals")]
    [Tooltip("Color for cells that have not been visited yet (should be transparent).")]
    public Color baseColor = new Color(1, 1, 1, 0); // Default to transparent
    [Tooltip("Color for a cell the player has stepped on.")]
    public Color progressColor = Color.yellow;
    [Tooltip("Color for all cells upon successful capture.")]
    public Color doneColor = Color.green;

    [Header("Dependencies")]
    [SerializeField]
    private MonoBehaviour gridProviderSource;
    private IHexGridProvider _gridProvider;

    // Internal State
    private Vector2Int _itemHex;
    private readonly List<Vector2Int> _requiredHexes = new List<Vector2Int>();
    private readonly HashSet<Vector2Int> _visitedHexes = new HashSet<Vector2Int>();
    private float _secureTimer = -1f;
    private Vector2Int _lastPlayerHex;

    // --- Unity Lifecycle ---

    void Start()
    {
        _gridProvider = gridProviderSource as IHexGridProvider;

        if (_gridProvider == null)
        {
            var providerComponent = FindObjectOfType<HexGridProvider_FromGameManager>();
            if(providerComponent != null) _gridProvider = providerComponent;
        }

        if (_gridProvider == null)
        {
            Debug.LogError("TrashSecure: IHexGridProvider is not assigned and could not be found!", this);
            enabled = false;
            return;
        }

        InitializeRequiredHexes();

        _lastPlayerHex = _gridProvider.PlayerCurrentHex;

        // Edge Case: Check if player starts on a required hex.
        if (_requiredHexes.Contains(_lastPlayerHex))
        {
            OnPlayerEnterRequiredHex(_lastPlayerHex);
        }
    }

    void Update()
    {
        if (_gridProvider == null) return;

        HandlePlayerMovement();
        HandleSecureTimer();
    }

    void OnDestroy()
    {
        if (GridHighlighter.Instance != null && _requiredHexes.Count > 0)
        {
            GridHighlighter.Instance.ClearMany(_requiredHexes);
        }
    }

    // --- Core Logic ---

    private void InitializeRequiredHexes()
    {
        _itemHex = _gridProvider.WorldToHex(transform.position);

        var finalDirs = new HashSet<HexDir>(requiredDirs);
        var allDirs = System.Enum.GetValues(typeof(HexDir)).Cast<HexDir>().ToList();

        while(finalDirs.Count < 3 && allDirs.Count > 0)
        {
            int randIndex = Random.Range(0, allDirs.Count);
            if (!finalDirs.Contains(allDirs[randIndex]))
            {
                finalDirs.Add(allDirs[randIndex]);
            }
            allDirs.RemoveAt(randIndex);
        }

        _requiredHexes.Clear();
        foreach (var dir in finalDirs.Take(3))
        {
            _requiredHexes.Add(_itemHex + GetHexOffset(dir));
        }
    }

    private void HandlePlayerMovement()
    {
        Vector2Int currentPlayerHex = _gridProvider.PlayerCurrentHex;
        if (currentPlayerHex != _lastPlayerHex)
        {
            if (_requiredHexes.Contains(currentPlayerHex))
            {
                OnPlayerEnterRequiredHex(currentPlayerHex);
            }
            _lastPlayerHex = currentPlayerHex;
        }
    }

    private void OnPlayerEnterRequiredHex(Vector2Int hex)
    {
        if (_visitedHexes.Contains(hex)) return;

        _visitedHexes.Add(hex);
        GridHighlighter.Instance.SetColor(hex, progressColor);

        if (_visitedHexes.Count == 1)
        {
            _secureTimer = secureWindow;
        }

        if (_visitedHexes.Count >= _requiredHexes.Count)
        {
            OnCaptureSuccess();
        }
    }

    private void HandleSecureTimer()
    {
        if (_secureTimer > 0)
        {
            _secureTimer -= Time.deltaTime;
            if (_secureTimer <= 0)
            {
                OnCaptureTimeout();
            }
        }
    }

    private void OnCaptureSuccess()
    {
        _secureTimer = -1f;
        enabled = false;

        foreach (var hex in _requiredHexes)
        {
            GridHighlighter.Instance.SetColor(hex, doneColor);
        }

        GameEvents.RaiseTrashCaptured(transform.position);
        Invoke(nameof(CleanupAfterCapture), captureFxDelay);
    }

    private void OnCaptureTimeout()
    {
        _secureTimer = -1f;
        GridHighlighter.Instance.ClearMany(_visitedHexes);
        _visitedHexes.Clear();
    }

    private void CleanupAfterCapture()
    {
        if (GridHighlighter.Instance != null)
        {
            GridHighlighter.Instance.ClearMany(_requiredHexes);
        }
        Destroy(gameObject);
    }

    private Vector2Int GetHexOffset(HexDir dir)
    {
        switch (dir)
        {
            case HexDir.E: return new Vector2Int(1, 0);
            case HexDir.NE: return new Vector2Int(1, -1);
            case HexDir.NW: return new Vector2Int(0, -1);
            case HexDir.W: return new Vector2Int(-1, 0);
            case HexDir.SW: return new Vector2Int(-1, 1);
            case HexDir.SE: return new Vector2Int(0, 1);
            default: return Vector2Int.zero;
        }
    }
}

public enum HexDir { E, NE, NW, W, SW, SE }

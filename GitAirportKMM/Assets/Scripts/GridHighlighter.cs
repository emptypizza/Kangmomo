// --- WHAT CHANGED ---
// 1. Added ClearMany() method to efficiently clear multiple highlights at once.
// 2. Implemented a simple object pool for highlight GameObjects to prevent garbage collection spikes.

using System.Collections.Generic;
using UnityEngine;

public class GridHighlighter : MonoBehaviour
{
    public static GridHighlighter Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] public GameObject highlightPrefab;
    [SerializeField] private MonoBehaviour gridProviderSource;
    private IHexGridProvider _gridProvider;

    private readonly Dictionary<Vector2Int, SpriteRenderer> _activeHighlights = new Dictionary<Vector2Int, SpriteRenderer>();
    private readonly Queue<SpriteRenderer> _highlightPool = new Queue<SpriteRenderer>();
    private Transform _poolContainer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _gridProvider = gridProviderSource as IHexGridProvider;
    }

    void Start()
    {
        if (_gridProvider == null)
        {
             var providerComponent = FindObjectOfType<HexGridProvider_FromGameManager>();
             if(providerComponent != null) _gridProvider = providerComponent;
        }

        if (_gridProvider == null)
        {
            Debug.LogError("GridHighlighter: IHexGridProvider is not assigned!", this);
            enabled = false;
        }
    }

    public void SetColor(Vector2Int hex, Color color)
    {
        if (_gridProvider == null) return;

        if (_activeHighlights.TryGetValue(hex, out var renderer))
        {
            renderer.color = color;
        }
        else
        {
            SpriteRenderer newRenderer = GetFromPool();
            newRenderer.transform.position = _gridProvider.HexToWorld(hex);
            newRenderer.color = color;
            newRenderer.gameObject.SetActive(true);
            _activeHighlights[hex] = newRenderer;
        }
    }

    public void Clear(Vector2Int hex)
    {
        if (_activeHighlights.TryGetValue(hex, out var renderer))
        {
            ReturnToPool(renderer);
            _activeHighlights.Remove(hex);
        }
    }

    public void ClearMany(IEnumerable<Vector2Int> hexes)
    {
        foreach (var hex in hexes)
        {
            Clear(hex);
        }
    }

    private SpriteRenderer GetFromPool()
    {
        if (_highlightPool.Count > 0)
        {
            return _highlightPool.Dequeue();
        }

        if (_poolContainer == null)
        {
            _poolContainer = new GameObject("HighlightPoolContainer").transform;
            _poolContainer.SetParent(transform);
        }

        if (highlightPrefab == null)
        {
            Debug.LogError("GridHighlighter: highlightPrefab is not assigned! Ensure SecureTrashBootstrap is in the scene.", this);
            var go = new GameObject("FallbackHighlight");
            go.transform.SetParent(_poolContainer);
            return go.AddComponent<SpriteRenderer>();
        }

        var instance = Instantiate(highlightPrefab, _poolContainer);
        return instance.GetComponent<SpriteRenderer>();
    }

    private void ReturnToPool(SpriteRenderer renderer)
    {
        renderer.gameObject.SetActive(false);
        _highlightPool.Enqueue(renderer);
    }
}

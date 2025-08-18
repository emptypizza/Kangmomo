using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A singleton visualizer for the hex grid. It manages and reuses highlight
/// objects to color individual hex cells without performance overhead.
/// </summary>
public class GridHighlighter : MonoBehaviour
{
    public static GridHighlighter Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField]
    [Tooltip("The prefab to use for highlighting a cell. Must have a SpriteRenderer component.")]
    private GameObject highlightPrefab;

    [SerializeField]
    [Tooltip("A component that implements IHexGridProvider. This is required for the highlighter to work.")]
    private MonoBehaviour gridProviderSource;

    private IHexGridProvider gridProvider;
    private readonly Dictionary<Vector2Int, SpriteRenderer> activeHighlights = new Dictionary<Vector2Int, SpriteRenderer>();
    private readonly Queue<SpriteRenderer> highlightPool = new Queue<SpriteRenderer>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // --- Grid Provider Initialization ---
        if (gridProviderSource != null && gridProviderSource is IHexGridProvider provider)
        {
            gridProvider = provider;
        }
        else
        {
            // Unlike the first attempt, we won't fall back to finding GameManager automatically,
            // because we know it's not compatible. An explicit assignment is required.
            Debug.LogError("GridHighlighter requires a valid IHexGridProvider to be assigned in the 'Grid Provider Source' field.", this);
            enabled = false; // Disable this component if the dependency is not met.
            return;
        }
    }

    /// <summary>
    /// Sets the color of a specific hex cell.
    /// </summary>
    /// <param name="hex">The axial coordinate of the hex to color.</param>
    /// <param name="color">The color to apply.</param>
    public void SetColor(Vector2Int hex, Color color)
    {
        if (gridProvider == null || !enabled) return;

        if (activeHighlights.TryGetValue(hex, out SpriteRenderer renderer))
        {
            // If already highlighting this hex, just update the color.
            renderer.color = color;
        }
        else
        {
            // Otherwise, grab a highlighter from the pool or create a new one.
            renderer = GetOrCreateHighlightRenderer();
            if (renderer == null) return; // Stop if prefab is missing.

            renderer.transform.position = gridProvider.HexToWorld(hex);
            renderer.color = color;
            renderer.gameObject.SetActive(true);
            activeHighlights[hex] = renderer;
        }
    }

    /// <summary>
    /// Clears any highlight from a specific hex cell, returning it to the pool.
    /// </summary>
    /// <param name="hex">The axial coordinate of the hex to clear.</param>
    public void Clear(Vector2Int hex)
    {
        if (activeHighlights.TryGetValue(hex, out SpriteRenderer renderer))
        {
            renderer.gameObject.SetActive(false);
            highlightPool.Enqueue(renderer);
            activeHighlights.Remove(hex);
        }
    }

    private SpriteRenderer GetOrCreateHighlightRenderer()
    {
        if (highlightPool.Count > 0)
        {
            return highlightPool.Dequeue();
        }

        if (highlightPrefab == null)
        {
            Debug.LogError("Highlight Prefab is not assigned in GridHighlighter.", this);
            return null;
        }
        GameObject newInstance = Instantiate(highlightPrefab, transform);
        return newInstance.GetComponent<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        // Clean up all instantiated highlight objects to prevent scene leaks.
        foreach (var pair in activeHighlights)
        {
            if (pair.Value != null) Destroy(pair.Value.gameObject);
        }
        activeHighlights.Clear();

        foreach(var pooledRenderer in highlightPool)
        {
            if (pooledRenderer != null) Destroy(pooledRenderer.gameObject);
        }
        highlightPool.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Ensures Secure Trash feature has all required runtime components.
/// Drop one instance in the scene; it will auto-wire dependencies.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SecureTrashBootstrap : MonoBehaviour
{
    private static GameObject runtimePrefab;
    private static readonly List<TrashSecure> trashBuffer = new List<TrashSecure>();

    private void Awake()
    {
        var provider = FindObjectOfType<HexGridProvider_FromGameManager>();
        if (provider == null)
            provider = new GameObject("HexGridProvider").AddComponent<HexGridProvider_FromGameManager>();

        var highlighter = FindObjectOfType<GridHighlighter>();
        if (highlighter == null)
            highlighter = new GameObject("GridHighlighter").AddComponent<GridHighlighter>();

        BindGridHighlighter(highlighter, provider);
        AssignTrashSecures(provider);

        // Periodically re-bind to catch newly spawned trash objects.
        InvokeRepeating(nameof(AssignTrashSecuresDelayed), 1f, 1f);
    }

    private void BindGridHighlighter(GridHighlighter highlighter, HexGridProvider_FromGameManager provider)
    {
        var providerField = typeof(GridHighlighter).GetField("gridProviderSource", BindingFlags.NonPublic | BindingFlags.Instance);
        if (providerField != null && providerField.GetValue(highlighter) == null)
        {
            providerField.SetValue(highlighter, provider);
            if (!highlighter.enabled)
            {
                highlighter.enabled = true;
                typeof(GridHighlighter).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(highlighter, null);
            }
        }

        var prefabField = typeof(GridHighlighter).GetField("highlightPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
        if (prefabField != null && prefabField.GetValue(highlighter) == null)
        {
            GameObject prefab = Resources.Load<GameObject>("HighlightCell");
            if (prefab == null)
            {
                if (runtimePrefab == null)
                {
                    runtimePrefab = CreateRuntimePrefab();
                    DontDestroyOnLoad(runtimePrefab);
                    runtimePrefab.hideFlags = HideFlags.HideInHierarchy;
                }
                prefab = runtimePrefab;
            }
            prefabField.SetValue(highlighter, prefab);
        }
    }

    private static GameObject CreateRuntimePrefab()
    {
        var go = new GameObject("HighlightCellPrefab");
        var sr = go.AddComponent<SpriteRenderer>();
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        sr.sortingOrder = 100;
        go.transform.localScale = Vector3.one;
        go.SetActive(false);
        return go;
    }

    private void AssignTrashSecuresDelayed() => AssignTrashSecures(FindObjectOfType<HexGridProvider_FromGameManager>());

    private void AssignTrashSecures(HexGridProvider_FromGameManager provider)
    {
        if (provider == null) return;
        var field = typeof(TrashSecure).GetField("gridProviderSource", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return;

        trashBuffer.Clear();
        trashBuffer.AddRange(FindObjectsOfType<TrashSecure>());
        foreach (var ts in trashBuffer)
        {
            if (field.GetValue(ts) == null)
                field.SetValue(ts, provider);
        }
    }
}

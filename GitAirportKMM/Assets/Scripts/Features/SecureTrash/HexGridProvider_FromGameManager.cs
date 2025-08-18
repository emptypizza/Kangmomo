using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Provides hex-grid conversions by delegating to an existing GameManager.
/// </summary>
public class HexGridProvider_FromGameManager : MonoBehaviour, IHexGridProvider
{
    [Tooltip("Explicit reference to the GameManager. If left null, one will be searched at runtime.")]
    public GameManager gm;

    private MethodInfo _worldToHex;
    private MethodInfo _hexToWorld;
    private PropertyInfo _playerHexProperty;
    private Transform _playerTransform;

    private bool _loggedWorldToHexMissing;
    private bool _loggedHexToWorldMissing;
    private bool _loggedPlayerMissing;

    private void Awake()
    {
        if (gm == null)
            gm = FindObjectOfType<GameManager>();

        CacheRefs();
    }

    private void CacheRefs()
    {
        if (gm == null)
            return;

        var type = gm.GetType();
        _worldToHex = type.GetMethod("WorldToHex") ?? type.GetMethod("WorldToAxial");
        _hexToWorld = type.GetMethod("HexToWorld") ?? type.GetMethod("AxialToWorld");
        _playerHexProperty = type.GetProperty("PlayerCurrentHex");

        // Try to grab the player's transform for fallback calculations.
        var playerField = type.GetField("player") as FieldInfo;
        var playerProp = type.GetProperty("player");
        object playerObj = playerField?.GetValue(gm) ?? playerProp?.GetValue(gm);
        if (playerObj is Component comp)
            _playerTransform = comp.transform;
    }

    public Vector2Int WorldToHex(Vector3 world)
    {
        if (gm == null)
        {
            if (!_loggedWorldToHexMissing)
            {
                Debug.LogError("HexGridProvider_FromGameManager: GameManager not found.", this);
                _loggedWorldToHexMissing = true;
            }
            return Vector2Int.zero;
        }

        if (_worldToHex != null)
        {
            try
            {
                object result = _worldToHex.Invoke(gm, new object[] { world });
                if (result is Vector2Int v) return v;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HexGridProvider_FromGameManager: WorldToHex invocation failed - {ex.Message}", this);
            }
        }
        else if (!_loggedWorldToHexMissing)
        {
            Debug.LogError("HexGridProvider_FromGameManager: GameManager lacks WorldToHex/WorldToAxial method.", this);
            _loggedWorldToHexMissing = true;
        }

        return Vector2Int.zero;
    }

    public Vector3 HexToWorld(Vector2Int hex)
    {
        if (gm == null)
        {
            if (!_loggedHexToWorldMissing)
            {
                Debug.LogError("HexGridProvider_FromGameManager: GameManager not found.", this);
                _loggedHexToWorldMissing = true;
            }
            return Vector3.zero;
        }

        if (_hexToWorld != null)
        {
            try
            {
                object result = _hexToWorld.Invoke(gm, new object[] { hex });
                if (result is Vector3 v) return v;
            }
            catch (Exception ex)
            {
                Debug.LogError($"HexGridProvider_FromGameManager: HexToWorld invocation failed - {ex.Message}", this);
            }
        }
        else if (!_loggedHexToWorldMissing)
        {
            Debug.LogError("HexGridProvider_FromGameManager: GameManager lacks HexToWorld/AxialToWorld method.", this);
            _loggedHexToWorldMissing = true;
        }

        return Vector3.zero;
    }

    public Vector2Int PlayerCurrentHex
    {
        get
        {
            if (gm == null)
            {
                if (!_loggedPlayerMissing)
                {
                    Debug.LogError("HexGridProvider_FromGameManager: GameManager not found.", this);
                    _loggedPlayerMissing = true;
                }
                return Vector2Int.zero;
            }

            if (_playerHexProperty != null)
            {
                object val = _playerHexProperty.GetValue(gm, null);
                if (val is Vector2Int v) return v;
            }

            if (_playerTransform != null)
            {
                return WorldToHex(_playerTransform.position);
            }

            if (!_loggedPlayerMissing)
            {
                Debug.LogError("HexGridProvider_FromGameManager: Unable to determine player hex.", this);
                _loggedPlayerMissing = true;
            }
            return Vector2Int.zero;
        }
    }
}

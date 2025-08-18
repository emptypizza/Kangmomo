using UnityEngine;

/// <summary>
/// Defines a standard interface for interacting with a hexagonal grid system
/// that uses axial coordinates. This allows components like TrashSecure to be
/// decoupled from a specific GameManager or grid implementation.
/// </summary>
public interface IHexGridProvider
{
    /// <summary>
    /// Converts a world space position to an axial hex coordinate (q, r).
    /// </summary>
    Vector2Int WorldToHex(Vector3 world);

    /// <summary>
    /// Converts an axial hex coordinate (q, r) to a world space position (center of the hex).
    /// </summary>
    Vector3 HexToWorld(Vector2Int hex);

    /// <summary>
    /// Gets the axial hex coordinate the player is currently on.
    /// </summary>
    Vector2Int PlayerCurrentHex { get; }
}

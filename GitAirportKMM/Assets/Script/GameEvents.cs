using System;
using UnityEngine;

/// <summary>
/// A static event hub for loosely-coupled game-wide communication.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Fired when a Trash item is successfully secured by the player.
    /// The Vector3 payload is the world position of the captured trash.
    /// </summary>
    public static event Action<Vector3> OnTrashCaptured;

    /// <summary>
    /// Raises the OnTrashCaptured event.
    /// </summary>
    /// <param name="worldPos">The world position of the trash that was captured.</param>
    public static void RaiseTrashCaptured(Vector3 worldPos) => OnTrashCaptured?.Invoke(worldPos);
}

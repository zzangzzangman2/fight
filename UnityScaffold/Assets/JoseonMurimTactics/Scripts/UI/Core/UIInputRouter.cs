using System;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Centralizes common UI keys while the project transitions from IMGUI to Canvas.</summary>
[DisallowMultipleComponent]
public sealed class UIInputRouter : MonoBehaviour
{
    public event Action BackPressed;
    public event Action SubmitPressed;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackPressed?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitPressed?.Invoke();
        }
    }
}
}

using System;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Reusable confirm modal state. A prefab can bind buttons to Confirm and Cancel.</summary>
[DisallowMultipleComponent]
public sealed class ModalDialog : MonoBehaviour
{
    private Action onConfirm;
    private Action onCancel;

    public string Title { get; private set; }
    public string Message { get; private set; }
    public bool IsOpen { get; private set; }

    public void Open(string title, string message, Action confirm, Action cancel = null)
    {
        Title = title;
        Message = message;
        onConfirm = confirm;
        onCancel = cancel;
        IsOpen = true;
        gameObject.SetActive(true);
    }

    public void Confirm()
    {
        Action callback = onConfirm;
        Close();
        callback?.Invoke();
    }

    public void Cancel()
    {
        Action callback = onCancel;
        Close();
        callback?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        onConfirm = null;
        onCancel = null;
        gameObject.SetActive(false);
    }
}
}

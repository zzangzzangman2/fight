using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class ConfirmPopup : UIScreenBase
{
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private TMP_Text bodyText;
    [SerializeField]
    private Button confirmButton;
    [SerializeField]
    private Button cancelButton;

    private Action onConfirm;
    private Action onCancel;

    protected override void Awake()
    {
        base.Awake();
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(Confirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(Cancel);
        }
    }

    public void Open(string title, string body, Action confirm, Action cancel = null)
    {
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.text = body ?? string.Empty;
        }

        onConfirm = confirm;
        onCancel = cancel;
        Show();
    }

    private void Confirm()
    {
        Hide();
        onConfirm?.Invoke();
    }

    private void Cancel()
    {
        Hide();
        onCancel?.Invoke();
    }
}
}

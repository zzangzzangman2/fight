using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Simple toast queue used by hub/save/settings screens.</summary>
[DisallowMultipleComponent]
public sealed class ToastQueue : MonoBehaviour
{
    private readonly Queue<string> pending = new Queue<string>();

    [SerializeField]
    private float duration = 2f;

    private float timer;
    private GameRoot root;

    public string Current { get; private set; }

    private void Awake()
    {
        root = GameRoot.EnsureExists();
    }

    private void Update()
    {
        if (timer > 0f)
        {
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f)
            {
                return;
            }
        }

        if (pending.Count > 0)
        {
            Current = pending.Dequeue();
            timer = Mathf.Max(0.2f, duration);
        }
        else if (root != null && root.Notifications != null && root.Notifications.TryDequeue(out UiNotification note))
        {
            Current = note.message;
            timer = Mathf.Max(0.2f, note.durationSeconds > 0f ? note.durationSeconds : duration);
        }
        else
        {
            Current = null;
        }
    }

    public void Push(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            pending.Enqueue(message);
        }
    }
}
}

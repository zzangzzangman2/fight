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

    public string Current { get; private set; }

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

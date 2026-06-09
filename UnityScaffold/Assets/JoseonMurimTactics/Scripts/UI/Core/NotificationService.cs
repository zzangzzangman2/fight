using System.Collections.Generic;

namespace JoseonMurimTactics
{
public enum NotificationKind
{
    Info,
    Success,
    Warning,
    Error,
    Autosave
}

public sealed class UiNotification
{
    public string message;
    public NotificationKind kind;
    public float durationSeconds;

    public UiNotification(string message, NotificationKind kind, float durationSeconds)
    {
        this.message = message;
        this.kind = kind;
        this.durationSeconds = durationSeconds;
    }
}

public sealed class NotificationService
{
    private readonly Queue<UiNotification> queue = new Queue<UiNotification>();

    public string CurrentSceneTitle { get; private set; }

    public void Push(string message, NotificationKind kind = NotificationKind.Info, float durationSeconds = 2f)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        queue.Enqueue(new UiNotification(message, kind, durationSeconds));
    }

    public void PushAutosave(bool success)
    {
        Push(success ? "자동 저장 완료" : "자동 저장 실패", success ? NotificationKind.Autosave : NotificationKind.Error,
             2.4f);
    }

    public bool TryDequeue(out UiNotification notification)
    {
        if (queue.Count > 0)
        {
            notification = queue.Dequeue();
            return true;
        }

        notification = null;
        return false;
    }

    public void SetSceneTitle(string title)
    {
        CurrentSceneTitle = title ?? string.Empty;
    }

    public void Clear()
    {
        queue.Clear();
        CurrentSceneTitle = string.Empty;
    }
}
}

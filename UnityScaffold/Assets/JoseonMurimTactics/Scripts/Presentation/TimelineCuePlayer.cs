using UnityEngine;
using UnityEngine.Playables;

namespace JoseonMurimTactics
{
public sealed class TimelineCuePlayer : MonoBehaviour
{
    public PlayableDirector defaultDirector;

    public void Play(TimelineCue cue)
    {
        if (cue == TimelineCue.None)
        {
            return;
        }

        Debug.Log("[TimelineCue] " + cue);
        if (defaultDirector != null)
        {
            defaultDirector.Stop();
            defaultDirector.Play();
        }
    }
}
}

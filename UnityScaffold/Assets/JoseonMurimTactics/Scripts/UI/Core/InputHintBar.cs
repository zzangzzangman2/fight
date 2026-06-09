using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Data holder for bottom input hints: Enter select, Esc back, arrows move.</summary>
[DisallowMultipleComponent]
public sealed class InputHintBar : MonoBehaviour
{
    [SerializeField]
    private string hintText = "Enter 선택 / Esc 뒤로 / 방향키 이동";

    public string HintText => hintText;

    public void SetHint(string text)
    {
        hintText = string.IsNullOrEmpty(text) ? string.Empty : text;
    }
}
}

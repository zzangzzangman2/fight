using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
[RequireComponent(typeof(Button))]
public sealed class HubHotspot : MonoBehaviour
{
    [SerializeField]
    private HubLocation location;
    [SerializeField]
    private TMP_Text labelText;
    [SerializeField]
    private TMP_Text badgeText;
    [SerializeField]
    private Button button;

    public HubLocation Location => location;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void Bind(HubMapController controller, HubLocation hubLocation, string label, string badge)
    {
        location = hubLocation;
        if (labelText != null)
        {
            labelText.text = label ?? string.Empty;
        }

        if (badgeText != null)
        {
            badgeText.text = badge ?? string.Empty;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => controller.Select(location));
        }
    }
}
}

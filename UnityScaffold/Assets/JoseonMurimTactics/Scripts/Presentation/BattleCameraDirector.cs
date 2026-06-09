using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class BattleCameraDirector : MonoBehaviour
{
    public Camera battleCamera;
    public float moveSpeed = 8f;
    public float zoomSpeed = 5f;
    public float defaultOrthographicSize = 7f;
    public float closeOrthographicSize = 4.5f;

    private Vector3 targetPosition;
    private float targetSize;

    private void Awake()
    {
        if (battleCamera == null)
        {
            battleCamera = Camera.main;
        }

        if (battleCamera != null)
        {
            targetPosition = battleCamera.transform.position;
            targetSize = battleCamera.orthographicSize;
        }
    }

    private void LateUpdate()
    {
        if (battleCamera == null)
        {
            return;
        }

        battleCamera.transform.position =
            Vector3.Lerp(battleCamera.transform.position, targetPosition, Time.deltaTime * moveSpeed);
        battleCamera.orthographicSize =
            Mathf.Lerp(battleCamera.orthographicSize, targetSize, Time.deltaTime * zoomSpeed);
    }

    public void Focus(Vector3 worldPosition, bool closeUp)
    {
        if (battleCamera == null)
        {
            return;
        }

        targetPosition = new Vector3(worldPosition.x, worldPosition.y, battleCamera.transform.position.z);
        targetSize = closeUp ? closeOrthographicSize : defaultOrthographicSize;
    }
}
}

using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera mainCamera;
    public float cameraZoomSpeed = 5f;
    public float cameraOffsetY = 2f;
    public float cameraOffsetZ = -5f;

    void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public Coroutine AdjustCamera(int tallestStack)
    {
        return StartCoroutine(AdjustCameraRoutine(tallestStack));
    }

    private IEnumerator AdjustCameraRoutine(int tallest)
    {
        float targetY = (tallest * 0.5f) + cameraOffsetY;
        float targetZ = cameraOffsetZ - (tallest * 0.5f);
        Vector3 targetPos = new Vector3(mainCamera.transform.position.x, targetY, targetZ);

        float duration = 1.0f; // Smooth transition duration
        float time = 0f;
        Vector3 startPos = mainCamera.transform.position;

        while (time < duration)
        {
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = targetPos;
    }
}

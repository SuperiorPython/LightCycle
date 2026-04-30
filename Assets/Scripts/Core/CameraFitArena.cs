using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFitArena : MonoBehaviour
{
    public ArenaGrid arena;
    public float padding = 1.0f; // extra space around arena

    void Start()
    {
        Fit();
    }

    void Fit()
    {
        Camera cam = GetComponent<Camera>();
        cam.orthographic = true;

        float arenaWidthWorld = arena.width * arena.cellSize;
        float arenaHeightWorld = arena.height * arena.cellSize;

        float aspect = (float)Screen.width / Screen.height;

        float sizeByHeight = arenaHeightWorld / 2f;
        float sizeByWidth = arenaWidthWorld / (2f * aspect);

        cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth) + padding;

        // Center camera on arena
        cam.transform.position = new Vector3(0f, 0f, cam.transform.position.z);
    }
}

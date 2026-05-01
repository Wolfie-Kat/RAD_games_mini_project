using UnityEngine;

[ExecuteAlways]
public class FullscreenOverlay : MonoBehaviour
{
    public Camera targetCamera;

    [Range(0f, 1f)]
    public float contamination = 0f;

    [Tooltip("Higher = faster catch-up. ~5 is a gentle ramp, ~15 is snappy.")]
    public float smoothingSpeed = 5f;

    private float targetContamination = 0f;
    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int ContaminationID = Shader.PropertyToID("_Contamination");

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        propertyBlock = new MaterialPropertyBlock();
        targetContamination = contamination;
    }

    void LateUpdate()
    {
        // Resize to fill camera
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null || !cam.orthographic) return;

        float height = cam.orthographicSize * 2f;
        float width  = height * cam.aspect;
        transform.localScale = new Vector3(width, height, 1f);

        // Smoothly approach the target each frame
        contamination = Mathf.Lerp(contamination, targetContamination, 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime));

        // Push contamination value to shader via property block (per-renderer, doesn't mutate the material asset)
        if (spriteRenderer != null)
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(ContaminationID, contamination);
            spriteRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    void Update()
    {
        // In editor, allow real-time updates to the target contamination
        if (!Application.isPlaying)
        {
            targetContamination = contamination;
        }
    }

    public void SetContamination(float value)
    {
        targetContamination = Mathf.Clamp01(value);
    }
}
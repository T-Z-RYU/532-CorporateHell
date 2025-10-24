using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;     // 玩家
    [SerializeField] private PlayerMovement player;

    [Header("Follow")]
    [Range(0.01f, 0.6f)][SerializeField] private float smoothTime = 0.18f;
    [Range(0.01f, 0.3f)][SerializeField] private float dashSmoothTime = 0.06f;

    [Header("Velocity Look-Ahead")]
    [SerializeField] private bool useVelocityLookAhead = true;
    [Range(0f, 3f)][SerializeField] private float baseLookAhead = 0.6f;
    [Range(0f, 3f)][SerializeField] private float speedLookAheadMultiplier = 0.8f;
    [Range(0f, 6f)][SerializeField] private float dashLookAhead = 2.2f;

    [Header("Mouse Aim Settings")]
    [SerializeField] private bool useMouseAim = true;
    [SerializeField] private float minAimRadius = 0.5f;
    [SerializeField] private float maxAimRadius = 6f;
    [SerializeField] private float aimMaxOffset = 3f;
    [SerializeField] private float aimDeadZone = 0.3f;
    [Range(0f, 1f)][SerializeField] private float aimBiasWeight = 0.7f;

    [Header("Z Axis")]
    [SerializeField] private bool lockZ = true;
    [SerializeField] private float cameraZ = -10f;

    private Camera cam;
    private Vector3 velocity;
    private float currentSmooth;

    // 对外可用
    public Vector2 AimDirection { get; private set; }
    public Vector3 AimPoint { get; private set; }
    public float AimDistance { get; private set; }

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (player == null) player = FindObjectOfType<PlayerMovement>();
        if (target == null && player != null) target = player.transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position;
        bool isDashing = player != null && player.IsDashing;
        currentSmooth = isDashing ? dashSmoothTime : smoothTime;

        // 1. 鼠标虚拟准星
        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        Vector3 delta = mouseWorld - target.position;
        Vector2 planar = new Vector2(delta.x, delta.y);
        float rawDist = planar.magnitude;

        float clampedDist = Mathf.Clamp(rawDist, minAimRadius, maxAimRadius);
        Vector2 dir = rawDist > 0.001f ? planar / rawDist : Vector2.right;

        AimDirection = dir;
        AimDistance = clampedDist;
        AimPoint = target.position + (Vector3)(dir * clampedDist);

        // 2. 偏移量（基于鼠标虚拟准星）
        Vector2 aimAhead = Vector2.zero;
        if (useMouseAim && AimDistance > aimDeadZone)
        {
            float t = (AimDistance - aimDeadZone) / (maxAimRadius - aimDeadZone);
            t = Mathf.Clamp01(t);
            aimAhead = dir * (t * aimMaxOffset);
        }

        // 3. 速度 Look-Ahead
        Vector2 velAhead = Vector2.zero;
        if (useVelocityLookAhead && player != null)
        {
            Vector2 vel2D = player.Velocity;
            float speed = vel2D.magnitude;
            Vector2 vdir = speed > 0.001f ? vel2D / speed : Vector2.zero;

            float lookDist = isDashing
                ? dashLookAhead
                : baseLookAhead + Mathf.Clamp01(speed / player.MoveSpeed) * speedLookAheadMultiplier;

            velAhead = vdir * lookDist;
        }

        // 4. 混合
        Vector2 combinedAhead = Vector2.Lerp(velAhead, aimAhead, aimBiasWeight);
        desired += (Vector3)combinedAhead;

        if (lockZ) desired.z = cameraZ;

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, currentSmooth);
    }
}



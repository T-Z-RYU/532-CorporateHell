using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Default Shake Settings")]
    [SerializeField] private float defaultIntensity = 1f;
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // 内部
    private bool isShaking = false;
    private float timeLeft = 0f;
    private float totalDuration = 0f;
    private float currentIntensity = 0f;

    // 重要：把“原点”当成子物体的 localPosition = Vector3.zero
    // 不要记录 Awake 的世界位置，避免与跟随脚本打架
    private Vector3 shakeOffset = Vector3.zero;

    void LateUpdate()
    {
        if (!isShaking) return;

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            EndShake();
            return;
        }

        // 归一化进度 [0..1]
        float t = 1f - (timeLeft / totalDuration);
        float envelope = shakeCurve.Evaluate(t);             // 包络随时间衰减
        float magnitude = currentIntensity * envelope;

        // 每帧随机偏移（也可以换成柏林噪声以获得更“顺滑”的抖动）
        shakeOffset.x = Random.Range(-1f, 1f) * magnitude;
        shakeOffset.y = Random.Range(-1f, 1f) * magnitude;
        shakeOffset.z = 0f;

        // 关键：只改 localPosition，在父物体位置基础上叠加
        transform.localPosition = shakeOffset;
    }

    public void Shake() => Shake(defaultIntensity, defaultDuration);

    public void Shake(float intensity, float duration)
    {
        // 叠加策略：谁更强/更久用谁
        if (isShaking)
        {
            currentIntensity = Mathf.Max(currentIntensity, intensity);
            timeLeft = Mathf.Max(timeLeft, duration);
            totalDuration = Mathf.Max(totalDuration, duration);
        }
        else
        {
            isShaking = true;
            currentIntensity = intensity;
            timeLeft = duration;
            totalDuration = Mathf.Max(0.0001f, duration);
        }
    }

    public void StopShake() => EndShake();

    private void EndShake()
    {
        isShaking = false;
        timeLeft = 0f;
        totalDuration = 0f;
        currentIntensity = 0f;
        shakeOffset = Vector3.zero;
        transform.localPosition = Vector3.zero; // 复位到父物体原点
    }

    public bool IsShaking() => isShaking;
}

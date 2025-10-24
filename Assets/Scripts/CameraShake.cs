using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Default Shake Settings")]
    [SerializeField] private float defaultIntensity = 1f;
    [SerializeField] private float defaultDuration = 0.5f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // �ڲ�
    private bool isShaking = false;
    private float timeLeft = 0f;
    private float totalDuration = 0f;
    private float currentIntensity = 0f;

    // ��Ҫ���ѡ�ԭ�㡱����������� localPosition = Vector3.zero
    // ��Ҫ��¼ Awake ������λ�ã����������ű����
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

        // ��һ������ [0..1]
        float t = 1f - (timeLeft / totalDuration);
        float envelope = shakeCurve.Evaluate(t);             // ������ʱ��˥��
        float magnitude = currentIntensity * envelope;

        // ÿ֡���ƫ�ƣ�Ҳ���Ի��ɰ��������Ի�ø���˳�����Ķ�����
        shakeOffset.x = Random.Range(-1f, 1f) * magnitude;
        shakeOffset.y = Random.Range(-1f, 1f) * magnitude;
        shakeOffset.z = 0f;

        // �ؼ���ֻ�� localPosition���ڸ�����λ�û����ϵ���
        transform.localPosition = shakeOffset;
    }

    public void Shake() => Shake(defaultIntensity, defaultDuration);

    public void Shake(float intensity, float duration)
    {
        // ���Ӳ��ԣ�˭��ǿ/������˭
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
        transform.localPosition = Vector3.zero; // ��λ��������ԭ��
    }

    public bool IsShaking() => isShaking;
}

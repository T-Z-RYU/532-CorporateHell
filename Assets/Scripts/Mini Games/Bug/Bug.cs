using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class Bug : MonoBehaviour, IPointerClickHandler
{
    public static event Action<Bug> OnAnyBugKilled;

    [Header("Movement (�ڲ�)")]
    [SerializeField] Vector2 waypointIntervalRange = new Vector2(0.8f, 1.6f);
    [SerializeField] float jitter = 25f;      // �������ȣ�����/�룩
    [SerializeField] float padding = 20f;     // �����ݱ߽�Ļ��壨���أ�

    [Header("Hitbox��������п�����ӦͼƬ��")]
    public float hitPadding = 20f;
    public float minHitSize = 60f;

    [Header("Orientation������/ת��")]
    public bool faceMovement = true;          // �Ƿ�������ͼ�����ƶ�����
    public bool useUpAsForward = true;        // ǰ���᣺��=���ϣ���=����
    public bool randomInitialRotation = false;// ��ʼ�������
    public float rotateSmoothDeg = 720f;      // ��תƽ���ٶȣ���/�룩
    public float minSpeedToRotate = 5f;       // ���ڸ��ٶȲ����³���

    [Header("Flip & Wobble����ѡȤζЧ����")]
    public bool flipXByDirection = false;     // ����ˮƽ�ٶȷ������ҷ�ת���������п�
    public bool enableWobbleRotation = true;  // ҡͷ���ڳ��������С���ڶ���
    public float wobbleAngleDeg = 8f;         // ҡͷ�Ƕȣ��ȣ�
    public float wobbleSpeed = 6f;            // ҡͷ�ٶȣ�Hz���ƣ�
    public bool enableWobblePosition = false; // λ�ðڶ��������п���С��Χ������
    public Vector2 wobblePosAmplitude = new Vector2(4f, 2f);
    public float wobblePosSpeed = 2.5f;

    [Header( "Audio Clips" )]
    public AudioClip sqaushSound;
    // ���� ������ ���� //
    RectTransform moveArea;        // �ƶ���Χ��Content��
    RectTransform _rt;             // ���ڵ� Rect�����п�
    RectTransform _artRt;          // �ӽڵ� Art Rect������ͼ��

    float _baseSpeed = 250f;       // ����ʱ�ɹ�����ע��
    float _runtimeMul = 1f;        // �����ڱ��ʣ���ʱ�����/�Ѷȣ�
    float _currentSpeed => Mathf.Max(1f, _baseSpeed * _runtimeMul);

    float _waypointInterval;       // ÿֻ���Ӷ����Ļ�Ŀ����
    float _timer;                  // ��һ�λ�Ŀ�굹��ʱ
    Vector2 _targetPos;            // ��ǰĿ���
    Vector2 _prevPos;              // ��һ֡λ�ã����ڼ����ٶ�������

    // Wobble/Flip ����
    Vector3 _artBaseLocalPos;
    Quaternion _artBaseLocalRot;
    Vector3 _artBaseLocalScale;
    float _wobblePhaseRot;
    float _wobblePhasePos;

    //References
    private AudioEffectsScript effectsScript;
    void Awake()
    {
        _rt = transform as RectTransform;

        // �Զ����Ӽ� Art������ͼƬ�ڵ㣬���ƽ���Ϊ "Art"��
        var art = transform.Find("Art");
        if (art != null) _artRt = art as RectTransform;

        // ȷ�����ڵ�ɽ��յ������һ��͸�� Image
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // ��ȫ͸��
        }
        img.raycastTarget = true;

        if (_artRt != null)
        {
            _artBaseLocalPos = _artRt.localPosition;
            _artBaseLocalRot = _artRt.localRotation;
            _artBaseLocalScale = _artRt.localScale;
        }

        effectsScript = GameObject.FindGameObjectWithTag("Audio Effect Object").GetComponent<AudioEffectsScript>();
    }

    /// <summary>�ɹ�����ע���ƶ���Χ�������ٶȡ��߽绺�����ʼλ��</summary>
    public void Configure(RectTransform area, float baseSpeed, float edgePadding, Vector2? startPos = null)
    {
        moveArea = area;
        _baseSpeed = baseSpeed;
        padding = edgePadding;

        // ÿֻ���Ӳ�ͬ�Ļ�Ŀ���� & �������λ��������ˢˢ����
        _waypointInterval = Mathf.Max(0.1f, UnityEngine.Random.Range(waypointIntervalRange.x, waypointIntervalRange.y));
        _timer = UnityEngine.Random.Range(0f, _waypointInterval);

        AutoFitHitboxToArt();

        // UI ê��ͳһΪ����
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);

        _rt.anchoredPosition = startPos.HasValue ? ClampInsideArea(startPos.Value) : RandomInsideArea();
        _prevPos = _rt.anchoredPosition;
        _targetPos = RandomInsideArea();

        // ��ʼ�������ֻ��ת Art��
        if (randomInitialRotation && _artRt != null)
        {
            float z = UnityEngine.Random.Range(0f, 360f);
            _artRt.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        // ��� wobble ��λ
        _wobblePhaseRot = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        _wobblePhasePos = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>�������ٶȱ��ʣ���ʱ����ѣ�</summary>
    public void SetRuntimeMultiplier(float mul) => _runtimeMul = Mathf.Max(0.05f, mul);

    /// <summary>���� Art �ߴ��Զ��������п��С����ͼ/���ź�����ֶ����ã�</summary>
    public void AutoFitHitboxToArt()
    {
        if (_rt == null) _rt = transform as RectTransform;

        Vector2 size = _rt.sizeDelta;
        if (_artRt != null)
        {
            // ����ʹ�� sizeDelta * lossyScale ��ȡ���ӳߴ�
            Vector2 artSize = Vector2.Scale(_artRt.sizeDelta, _artRt.lossyScale);
            size = artSize + Vector2.one * (hitPadding * 2f);
        }
        size.x = Mathf.Max(size.x, minHitSize);
        size.y = Mathf.Max(size.y, minHitSize);
        _rt.sizeDelta = size;
    }

    void Update()
    {
        if (moveArea == null) return;

        // ������ʱ��Ŀ�꣨����ƶ���
        _timer -= Time.unscaledDeltaTime;
        if (_timer <= 0f)
        {
            _targetPos = RandomInsideArea();
            _timer = _waypointInterval;
        }

        // �����ƶ�
        Vector2 current = _rt.anchoredPosition;
        Vector2 dir = (_targetPos - current).normalized;
        Vector2 next = current + dir * _currentSpeed * Time.unscaledDeltaTime;
        next += UnityEngine.Random.insideUnitCircle * jitter * Time.unscaledDeltaTime;
        next = ClampInsideArea(next);

        // ����λ��
        _rt.anchoredPosition = next;

        // �ٶ����������ڳ���/��ת��
        Vector2 v = (next - _prevPos) / Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
        float spd = v.magnitude;

        // ���� ����ֻ��ת Art������
        Quaternion facingRot = (_artRt != null) ? _artRt.localRotation : Quaternion.identity;
        if (faceMovement && _artRt != null && spd >= minSpeedToRotate)
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (useUpAsForward) ang -= 90f; // �زġ����ϡ�Ϊǰ������ʱ����
            Quaternion target = Quaternion.Euler(0f, 0f, ang);
            facingRot = Quaternion.RotateTowards(
                _artRt.localRotation, target, rotateSmoothDeg * Time.unscaledDeltaTime
            );
        }

        // ���� ��ת�����Ҿ��񣩣����ı����п� ���� 
        if (_artRt != null && flipXByDirection && spd > 0.01f)
        {
            float sign = (v.x >= 0f) ? 1f : -1f;
            Vector3 s = _artBaseLocalScale;
            s.x = Mathf.Abs(s.x) * sign;
            _artRt.localScale = s;
        }
        else if (_artRt != null)
        {
            _artRt.localScale = _artBaseLocalScale;
        }

        // ���� ҡͷ/�ڶ��������ڳ�����&λ���ϣ�����
        if (_artRt != null)
        {
            Quaternion finalRot = facingRot;

            if (enableWobbleRotation && wobbleAngleDeg > 0f)
            {
                float wob = Mathf.Sin(_wobblePhaseRot + Time.unscaledTime * wobbleSpeed) * wobbleAngleDeg;
                finalRot = finalRot * Quaternion.Euler(0f, 0f, wob);
            }
            _artRt.localRotation = finalRot;

            if (enableWobblePosition && (wobblePosAmplitude.sqrMagnitude > 0.0001f))
            {
                float sx = Mathf.Sin(_wobblePhasePos + Time.unscaledTime * wobblePosSpeed) * wobblePosAmplitude.x;
                float sy = Mathf.Cos(_wobblePhasePos + Time.unscaledTime * wobblePosSpeed * 1.13f) * wobblePosAmplitude.y;
                _artRt.localPosition = _artBaseLocalPos + new Vector3(sx, sy, 0f);
            }
            else
            {
                _artRt.localPosition = _artBaseLocalPos;
            }
        }

        _prevPos = next;
    }

    Vector2 RandomInsideArea()
    {
        Vector2 halfSize = _rt.rect.size * 0.5f;
        float halfW = moveArea.rect.width * 0.5f - halfSize.x - padding;
        float halfH = moveArea.rect.height * 0.5f - halfSize.y - padding;
        float x = UnityEngine.Random.Range(-halfW, halfW);
        float y = UnityEngine.Random.Range(-halfH, halfH);
        return new Vector2(x, y);
    }

    Vector2 ClampInsideArea(Vector2 pos)
    {
        Vector2 halfSize = _rt.rect.size * 0.5f;
        float halfW = moveArea.rect.width * 0.5f - halfSize.x - padding;
        float halfH = moveArea.rect.height * 0.5f - padding;
        pos.x = Mathf.Clamp(pos.x, -halfW, halfW);
        pos.y = Mathf.Clamp(pos.y, -halfH, halfH);
        return pos;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnAnyBugKilled?.Invoke(this);
        GameObject.Find( "BugGame" ).GetComponent<BugMiniGame>().PlayClip(sqaushSound);
        Destroy(gameObject);
    }
}

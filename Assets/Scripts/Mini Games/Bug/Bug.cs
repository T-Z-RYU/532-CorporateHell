using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class Bug : MonoBehaviour, IPointerClickHandler
{
    public static event Action<Bug> OnAnyBugKilled;

    [Header("Movement (内部)")]
    [SerializeField] Vector2 waypointIntervalRange = new Vector2(0.8f, 1.6f);
    [SerializeField] float jitter = 25f;      // 抖动幅度（像素/秒）
    [SerializeField] float padding = 20f;     // 离内容边界的缓冲（像素）

    [Header("Hitbox（点击命中框，自适应图片）")]
    public float hitPadding = 20f;
    public float minHitSize = 60f;

    [Header("Orientation（朝向/转向）")]
    public bool faceMovement = true;          // 是否让美术图朝向移动方向
    public bool useUpAsForward = true;        // 前进轴：勾=向上，关=向右
    public bool randomInitialRotation = false;// 初始随机朝向
    public float rotateSmoothDeg = 720f;      // 旋转平滑速度（度/秒）
    public float minSpeedToRotate = 5f;       // 低于该速度不更新朝向

    [Header("Flip & Wobble（可选趣味效果）")]
    public bool flipXByDirection = false;     // 根据水平速度方向左右翻转（不改命中框）
    public bool enableWobbleRotation = true;  // 摇头（在朝向基础上小幅摆动）
    public float wobbleAngleDeg = 8f;         // 摇头角度（度）
    public float wobbleSpeed = 6f;            // 摇头速度（Hz近似）
    public bool enableWobblePosition = false; // 位置摆动（在命中框内小范围抖动）
    public Vector2 wobblePosAmplitude = new Vector2(4f, 2f);
    public float wobblePosSpeed = 2.5f;

    [Header( "Audio Clips" )]
    public AudioClip sqaushSound;
    // —— 运行期 —— //
    RectTransform moveArea;        // 移动范围（Content）
    RectTransform _rt;             // 根节点 Rect（命中框）
    RectTransform _artRt;          // 子节点 Art Rect（可视图像）

    float _baseSpeed = 250f;       // 生成时由管理器注入
    float _runtimeMul = 1f;        // 运行期倍率（随时间加速/难度）
    float _currentSpeed => Mathf.Max(1f, _baseSpeed * _runtimeMul);

    float _waypointInterval;       // 每只虫子独立的换目标间隔
    float _timer;                  // 下一次换目标倒计时
    Vector2 _targetPos;            // 当前目标点
    Vector2 _prevPos;              // 上一帧位置（用于计算速度向量）

    // Wobble/Flip 基线
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

        // 自动找子级 Art（可视图片节点，名称建议为 "Art"）
        var art = transform.Find("Art");
        if (art != null) _artRt = art as RectTransform;

        // 确保根节点可接收点击：加一个透明 Image
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // 完全透明
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

    /// <summary>由管理器注入移动范围、基础速度、边界缓冲与初始位置</summary>
    public void Configure(RectTransform area, float baseSpeed, float edgePadding, Vector2? startPos = null)
    {
        moveArea = area;
        _baseSpeed = baseSpeed;
        padding = edgePadding;

        // 每只虫子不同的换目标间隔 & 随机起步相位（避免齐刷刷换向）
        _waypointInterval = Mathf.Max(0.1f, UnityEngine.Random.Range(waypointIntervalRange.x, waypointIntervalRange.y));
        _timer = UnityEngine.Random.Range(0f, _waypointInterval);

        AutoFitHitboxToArt();

        // UI 锚点统一为中心
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);

        _rt.anchoredPosition = startPos.HasValue ? ClampInsideArea(startPos.Value) : RandomInsideArea();
        _prevPos = _rt.anchoredPosition;
        _targetPos = RandomInsideArea();

        // 初始随机朝向（只旋转 Art）
        if (randomInitialRotation && _artRt != null)
        {
            float z = UnityEngine.Random.Range(0f, 360f);
            _artRt.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        // 随机 wobble 相位
        _wobblePhaseRot = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        _wobblePhasePos = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
    }

    /// <summary>运行期速度倍率（随时间变难）</summary>
    public void SetRuntimeMultiplier(float mul) => _runtimeMul = Mathf.Max(0.05f, mul);

    /// <summary>根据 Art 尺寸自动调整命中框大小（换图/缩放后可再手动调用）</summary>
    public void AutoFitHitboxToArt()
    {
        if (_rt == null) _rt = transform as RectTransform;

        Vector2 size = _rt.sizeDelta;
        if (_artRt != null)
        {
            // 近似使用 sizeDelta * lossyScale 获取可视尺寸
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

        // 独立计时换目标（错峰移动）
        _timer -= Time.unscaledDeltaTime;
        if (_timer <= 0f)
        {
            _targetPos = RandomInsideArea();
            _timer = _waypointInterval;
        }

        // 计算移动
        Vector2 current = _rt.anchoredPosition;
        Vector2 dir = (_targetPos - current).normalized;
        Vector2 next = current + dir * _currentSpeed * Time.unscaledDeltaTime;
        next += UnityEngine.Random.insideUnitCircle * jitter * Time.unscaledDeltaTime;
        next = ClampInsideArea(next);

        // 更新位置
        _rt.anchoredPosition = next;

        // 速度向量（用于朝向/翻转）
        Vector2 v = (next - _prevPos) / Mathf.Max(Time.unscaledDeltaTime, 1e-4f);
        float spd = v.magnitude;

        // —— 朝向（只旋转 Art）——
        Quaternion facingRot = (_artRt != null) ? _artRt.localRotation : Quaternion.identity;
        if (faceMovement && _artRt != null && spd >= minSpeedToRotate)
        {
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (useUpAsForward) ang -= 90f; // 素材“朝上”为前进方向时修正
            Quaternion target = Quaternion.Euler(0f, 0f, ang);
            facingRot = Quaternion.RotateTowards(
                _artRt.localRotation, target, rotateSmoothDeg * Time.unscaledDeltaTime
            );
        }

        // —— 翻转（左右镜像），不改变命中框 —— 
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

        // —— 摇头/摆动（叠加在朝向上&位置上）——
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

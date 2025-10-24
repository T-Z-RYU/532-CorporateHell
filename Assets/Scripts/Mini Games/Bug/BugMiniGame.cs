using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class BugMiniGame : MonoBehaviour
{
    // ===== 持久化：供 Store 读取 =====
    public class MiniSession : MonoBehaviour
    {
        public static MiniSession Instance { get; private set; }

        public int BugsClearedLastRun { get; private set; }  // 已消灭虫子数（=已清行）
        public int BugsTotalLastRun { get; private set; }    // 本局总目标（假评论行）
        public int Gold { get; private set; }

        [System.Serializable]
        public class PostStat
        {
            public string id;
            public Sprite avatar;
            public string name;
            public int totalFakeLines;
            public int clearedFakeLines;
            public int Percent => (totalFakeLines > 0)
                ? Mathf.RoundToInt(100f * clearedFakeLines / (float)totalFakeLines)
                : 0;
        }
        public readonly List<PostStat> PostStats = new List<PostStat>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetBugsClearedLastRun(int count) => BugsClearedLastRun = Mathf.Max(0, count);
        public void SetBugsTotalLastRun(int total)   => BugsTotalLastRun   = Mathf.Max(0, total);
        public void AddGold(int amount)              => Gold = Mathf.Max(0, Gold + amount);

        public PostStat GetOrCreatePost(string id, Sprite avatar, string name, int totalFakeLines)
        {
            var ps = PostStats.Find(p => p.id == id);
            if (ps == null)
            {
                ps = new PostStat
                {
                    id = id,
                    avatar = avatar,
                    name = name,
                    totalFakeLines = Mathf.Max(0, totalFakeLines),
                    clearedFakeLines = 0
                };
                PostStats.Add(ps);
            }
            else
            {
                if (ps.avatar == null && avatar != null) ps.avatar = avatar;
                if (string.IsNullOrEmpty(ps.name) && !string.IsNullOrEmpty(name)) ps.name = name;
                if (ps.totalFakeLines == 0 && totalFakeLines > 0) ps.totalFakeLines = totalFakeLines;
            }
            return ps;
        }

        public void IncrementPostCleared(string id)
        {
            var ps = PostStats.Find(p => p.id == id);
            if (ps != null && ps.clearedFakeLines < ps.totalFakeLines) ps.clearedFakeLines++;
        }
    }
    // =================================

    [Header("Main Settings")]
    public RectTransform contentArea;   // ScrollView Content（必须）
    public GameObject bugPrefab;        // 预制体（顶层 UI + 挂 Bug.cs）
    public float bugSpeed = 240f;
    public float edgePadding = 20f;

    [Header("点击后表现")]
    public bool shakeOnExpose = true;
    public Vector2 shakeDurationRange = new Vector2(1f, 2f);
    public float shakeAngleDeg = 6f;
    public float shakeScaleAmp = 0.05f;
    public float shakeFrequency = 10f;
    public bool fadeOutLine = true;
    public float fadeOutDuration = 0.25f;

    [Header("UI（可选）")]
    public TMP_Text remainingText;   // 剩余目标（未清）
    public TMP_Text timerText;       // 剩余时间（秒）
    public float gameDuration = 60f;
    public Button exitButton;

    [Header("AudioClips")]
    public AudioClip bugSkitter;

    // ===== 手动 Scroll 范围控制 =====
    [Header("Scroll 控制（手动）")]
    [Tooltip("挂 ScrollRect（用于归一化控制或查找 Viewport）")]
    public ScrollRect scrollRect;

    [Tooltip("Viewport（可留空，自动取 scrollRect.viewport）")]
    public RectTransform viewport;

    [Tooltip("启用：按像素裁掉上下可滚范围；关闭：用归一化 0~1 控制")]
    public bool clampByPixels = true;

    [Tooltip("底部裁掉的像素（不允许滚到更下）")]
    public float bottomTrimPixels = 0f;

    [Tooltip("顶部裁掉的像素（不允许滚到更上）")]
    public float topTrimPixels = 0f;

    [Tooltip("是否启用手动 Scroll 限制（无论用像素还是归一化）")]
    public bool enableManualScrollClamp = true;

    [Range(0f, 1f), Tooltip("归一化顶部（1=最上）")]
    public float topNormalized = 1f;

    [Range(0f, 1f), Tooltip("归一化底部（0=最下）")]
    public float bottomNormalized = 0f;

    // 运行态
    float _timeLeft;
    int _totalFakes;   // 总目标（所有 isFake 行数）
    int _cleared;      // 已清（= 已消灭虫子数）
    bool _running;

    private AudioSource audioSource;

    void Awake()
    {
        // 确保会话存在
        if (FindObjectOfType<MiniSession>() == null)
        {
            var go = new GameObject("BugMiniGame.MiniSession");
            go.AddComponent<MiniSession>();
        }

        // 统计总目标
        var lines = GetComponentsInChildren<CommentBodyLine>(true);
        _totalFakes = 0;
        foreach (var line in lines)
        {
            line.manager = this;
            if (line.isFake) _totalFakes++;
        }

        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitUI);
        }

        _timeLeft = gameDuration;
        _running = true;
        RefreshHUD();

        audioSource = GetComponent<AudioSource>();

        // Scroll 相关引用修正
        if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (viewport == null && scrollRect != null) viewport = scrollRect.viewport;
    }

    void OnEnable()  { Bug.OnAnyBugKilled += OnBugKilled; }
    void OnDisable() { Bug.OnAnyBugKilled -= OnBugKilled; }

    void Update()
    {
        if (!_running) return;

        _timeLeft -= Time.unscaledDeltaTime;
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            _running = false;
            CommitResultToSession();   // 时间到也写入（不会成 100%，因为未杀的虫不计入）
        }

        if (timerText != null)
            timerText.text = Mathf.CeilToInt(_timeLeft).ToString();
    }

    void LateUpdate()
    {
        // 在 ScrollRect 更新之后做限制，避免被惯性/拖拽覆盖
        if (enableManualScrollClamp)
            ClampScrollRange();
    }

    // === 手动限制滚动范围（像素 / 归一化 二选一） ===
    void ClampScrollRange()
    {
        if (scrollRect == null || contentArea == null) return;

        if (clampByPixels)
        {
            // 以 anchoredPosition.y 为基准（默认 Content 顶对齐 pivot=1）
            if (viewport == null) viewport = scrollRect.viewport;
            if (viewport == null) return;

            float contentH  = contentArea.rect.height;
            float viewportH = viewport.rect.height;
            float scrollable = Mathf.Max(0f, contentH - viewportH);

            // 允许的 y 范围： [topTrimPixels, scrollable - bottomTrimPixels]
            float yMin = Mathf.Clamp(topTrimPixels, 0f, scrollable);
            float yMax = Mathf.Clamp(scrollable - bottomTrimPixels, 0f, scrollable);
            if (yMax < yMin) { var t = yMin; yMin = yMax; yMax = t; } // 兜底

            Vector2 pos = contentArea.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y, yMin, yMax);
            contentArea.anchoredPosition = pos;

            // 可选：把 ScrollRect 的 normalized 同步一下（不是必须）
            float v = (scrollable > 0.0001f) ? 1f - Mathf.Clamp01(pos.y / scrollable) : 1f;
            scrollRect.verticalNormalizedPosition = v;
        }
        else
        {
            // 归一化 0..1 范围：1=顶部，0=底部
            float hi = Mathf.Clamp01(topNormalized);
            float lo = Mathf.Clamp01(bottomNormalized);
            if (hi < lo) { var t = hi; hi = lo; lo = t; } // 确保 lo<=hi

            float v = scrollRect.verticalNormalizedPosition;
            float clamped = Mathf.Clamp(v, lo, hi);
            if (!Mathf.Approximately(v, clamped))
                scrollRect.verticalNormalizedPosition = clamped;
        }
    }

    /// <summary>
    /// 点中“假评论行”时：生成虫子 + 抖动淡出该行 + 登记帖子（不计清除！）
    /// </summary>
    public void SpawnBugAtLine(RectTransform lineRT)
    {
        if (!_running || lineRT == null) return;

        // 1) 同 Content 下生成虫子
        Vector2 localPos = lineRT.anchoredPosition;
        var go = Instantiate(bugPrefab, contentArea);
        var bug = go.GetComponent<Bug>();

        var bugRT = go.transform as RectTransform;
        bugRT.anchorMin = bugRT.anchorMax = new Vector2(0.5f, 0.5f);
        bugRT.pivot = new Vector2(0.5f, 0.5f);
        bugRT.anchoredPosition = localPos + new Vector2(0f, lineRT.rect.height * 0.5f + 12f);

        if (bug != null)
            bug.Configure(contentArea, bugSpeed, edgePadding);

        // 2) 给虫子打上“属于哪个帖子”的标记（供 OnBugKilled 结算）
        string postId = GetPostIdFromLine(lineRT, out Sprite avatar, out string name, out int totalFakeLines);
        if (!string.IsNullOrEmpty(postId))
        {
            MiniSession.Instance?.GetOrCreatePost(postId, avatar, name, totalFakeLines);

            var tag = go.GetComponent<PostTag>();
            if (tag == null) tag = go.AddComponent<PostTag>();
            tag.postId = postId;
        }

        // 3) 抖动/淡出该行（仅表现，不计清除）
        if (shakeOnExpose) StartCoroutine(ShakeAndHideLine(lineRT));
    }

    // 轻量标签：把虫子与一个 postId 关联起来
    private class PostTag : MonoBehaviour { public string postId; }

    // 查找卡片根并取 ID / 头像 / 名字 / 该帖总假行数
    string GetPostIdFromLine(RectTransform lineRT, out Sprite avatar, out string name, out int totalFakeLines)
    {
        avatar = null; name = "Post"; totalFakeLines = 0;
        Transform card = FindCardRoot(lineRT);
        if (card == null) return null;

        var avatarImg = card.Find("Header/Avatar")?.GetComponent<Image>();
        var nameTx = card.Find("Header/NameText")?.GetComponent<TMP_Text>();
        if (avatarImg) avatar = avatarImg.sprite;
        // 名字兜底：Header 下第一个 TMP_Text
        if (nameTx == null)
        {
            var header = card.Find("Header");
            if (header != null)
            {
                var arr = header.GetComponentsInChildren<TMP_Text>(true);
                if (arr != null && arr.Length > 0) nameTx = arr[0];
            }
        }
        if (nameTx) name = StripRichTags(nameTx.text).Trim();

        // 统计该帖总假行数
        var lines = card.GetComponentsInChildren<CommentBodyLine>(true);
        foreach (var l in lines) if (l.isFake) totalFakeLines++;

        // 用对象名+实例ID作为唯一 ID（如有 PostMeta，可替换）
        return $"{card.name}_{card.GetInstanceID()}";
    }

    Transform FindCardRoot(Transform t)
    {
        Transform cur = t;
        for (int i = 0; i < 10 && cur != null; i++)
        {
            bool hasHeader = cur.Find("Header") != null;
            bool hasBody = cur.Find("BodyGroup") != null || cur.Find("BodyText") != null;
            if (hasHeader && hasBody) return cur;
            if (cur == (Transform)contentArea) break;
            cur = cur.parent;
        }
        return null;
    }

    IEnumerator ShakeAndHideLine(RectTransform lineRT)
    {
        Quaternion rot0 = lineRT.localRotation;
        Vector3 s0 = lineRT.localScale;

        float dur = Random.Range(shakeDurationRange.x, shakeDurationRange.y);
        float t = 0f;

        var cg = lineRT.GetComponent<CanvasGroup>();
        if (cg == null) cg = lineRT.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            float damp = 1f - k;
            float w = shakeFrequency * 2f * Mathf.PI * Time.unscaledTime;

            float ang = Mathf.Sin(w) * shakeAngleDeg * damp;
            float scl = 1f + Mathf.Sin(w * 0.9f) * shakeScaleAmp * damp;

            lineRT.localRotation = Quaternion.Euler(0f, 0f, ang);
            lineRT.localScale = s0 * scl;
            yield return null;
        }

        lineRT.localRotation = rot0;
        lineRT.localScale = s0;

        if (fadeOutLine && cg != null)
        {
            float t2 = 0f;
            while (t2 < fadeOutDuration)
            {
                t2 += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t2 / fadeOutDuration);
                yield return null;
            }
            cg.alpha = 0f;
        }

        lineRT.gameObject.SetActive(false);
        // 不在这里计数！只在 OnBugKilled 里计数
        RefreshHUD();
    }

    // 只有虫子被消灭时才计清除
    void OnBugKilled(Bug b)
    {
        if (!_running || b == null) return;

        _cleared = Mathf.Min(_totalFakes, _cleared + 1);
        RefreshHUD();

        var tag = b.GetComponent<PostTag>();
        if (tag != null && !string.IsNullOrEmpty(tag.postId))
            MiniSession.Instance?.IncrementPostCleared(tag.postId);

        if (_cleared >= _totalFakes)
        {
            _running = false;
            CommitResultToSession();
        }
    }

    void RefreshHUD()
    {
        if (remainingText != null)
            remainingText.text = Mathf.Max(0, _totalFakes - _cleared).ToString();
    }

    void CommitResultToSession()
    {
        if (MiniSession.Instance == null) return;
        MiniSession.Instance.SetBugsClearedLastRun(_cleared);
        MiniSession.Instance.SetBugsTotalLastRun(_totalFakes);
    }

    public void ExitUI()
    {
        CommitResultToSession(); // 退出时写当前进度（没杀虫不会是100%）
        gameObject.SetActive(false);

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var ps = player.GetComponent<PlayerScript>();
            if (ps != null) ps.UnFreezePlayer();
        }
    }

    // 文本净化（去富文本标签）
    static string StripRichTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
        bool inside = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '<') { inside = true; continue; }
            if (inside)
            {
                if (c == '>') inside = false;
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    public void PlayClip(AudioClip clip)
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null || clip == null) return;
        audioSource.clip = clip;
        audioSource.Play();
    }
}

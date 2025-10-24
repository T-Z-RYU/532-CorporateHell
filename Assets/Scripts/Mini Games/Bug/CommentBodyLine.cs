using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 挂在每条评论正文（TMP_Text）上：
/// - 勾 isFake → 点击生成虫子（BugMiniGame.SpawnBugAtLine）
/// - 悬停高亮：
///   · highlightWholeCard = true  → 高亮整张卡片（按卡片Rect，支持Padding）
///   · highlightWholeCard = false → 精确高亮这条文本的 textBounds（支持Padding）
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class CommentBodyLine : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("是否是假评论（点击才会生成虫子）")]
    public bool isFake = false;

    [Header("Hover 高亮设置")]
    public bool highlightOnHover = true;
    [Tooltip("true=整张评论卡高亮；false=仅高亮当前行文本区域")]
    public bool highlightWholeCard = true;

    [Tooltip("高亮颜色（建议半透明）")]
    public Color highlightColor = new Color(1f, 1f, 0.4f, 0.18f);

    [Tooltip("搜索/创建的高亮对象名（同名可复用）")]
    public string highlightObjectName = "Highlight";

    [Tooltip("高亮边距（x=水平内边距，y=垂直内边距）单位=像素")]
    public Vector2 highlightPadding = new Vector2(8f, 4f);

    [HideInInspector] public BugMiniGame manager;

    // 缓存
    Image _cardOverlay; // 整卡覆盖
    Image _lineOverlay; // 行覆盖
    TMP_Text _tmp;
    RectTransform _lineRT;

    void Awake()
    {
        _tmp = GetComponent<TMP_Text>();
        _lineRT = transform as RectTransform;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isFake || manager == null) return;
        manager.SpawnBugAtLine(_lineRT);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!highlightOnHover) return;
        SetHighlight(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!highlightOnHover) return;
        SetHighlight(false);
    }

    void SetHighlight(bool on)
    {
        if (highlightWholeCard)
        {
            var card = FindCardRoot(transform);
            if (!card) return;

            if (!_cardOverlay)
                _cardOverlay = GetOrCreateOverlay(card as RectTransform, isCard:true);

            if (_cardOverlay)
            {
                _cardOverlay.enabled = on;
                if (on) UpdateCardOverlayGeometry(_cardOverlay.rectTransform, card as RectTransform);
            }
        }
        else
        {
            // 仅高亮该行的文本区域（使用 TMP 的 textBounds）
            if (!_lineOverlay)
                _lineOverlay = GetOrCreateOverlay(_lineRT, isCard:false);

            if (_lineOverlay)
            {
                _lineOverlay.enabled = on;
                if (on) UpdateLineOverlayGeometry(_lineOverlay.rectTransform, _tmp);
            }
        }
    }

    /// <summary>整卡高亮：覆盖卡片 rect，加入 Padding。</summary>
    void UpdateCardOverlayGeometry(RectTransform overlay, RectTransform card)
    {
        // 铺满卡片，再按 padding 外扩
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.pivot = new Vector2(0.5f, 0.5f);
        overlay.offsetMin = new Vector2(-highlightPadding.x, -highlightPadding.y);
        overlay.offsetMax = new Vector2(+highlightPadding.x, +highlightPadding.y);
        overlay.SetAsFirstSibling();
    }

    /// <summary>行高亮：精确贴合 TMP_Text 的 textBounds（局部空间），并支持 Padding。</summary>
    void UpdateLineOverlayGeometry(RectTransform overlay, TMP_Text text)
    {
        // 强制刷新布局/渲染数据，保证 textBounds 最新
        Canvas.ForceUpdateCanvases();
        text.ForceMeshUpdate();

        var bounds = text.textBounds;            // 局部空间的包围盒
        Vector2 size = bounds.size;
        Vector2 center = bounds.center;

        // 叠加 padding（扩大高亮）
        size += highlightPadding * 2f;

        overlay.anchorMin = new Vector2(0.5f, 0.5f);
        overlay.anchorMax = new Vector2(0.5f, 0.5f);
        overlay.pivot     = new Vector2(0.5f, 0.5f);

        overlay.sizeDelta = size;
        overlay.anchoredPosition = center;       // textBounds 的中心就是局部坐标系中心偏移
        overlay.SetAsFirstSibling();
    }

    /// <summary>获取或创建一个全覆盖用的 Image 作为高亮层。</summary>
    Image GetOrCreateOverlay(RectTransform parent, bool isCard)
    {
        // 优先复用同名子节点
        var exist = parent.Find(highlightObjectName);
        if (exist)
        {
            var img = exist.GetComponent<Image>();
            if (img) return SetupOverlay(img);
        }

        // 没有就新建
        var go = new GameObject(highlightObjectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var imgNew = go.GetComponent<Image>();
        return SetupOverlay(imgNew);
    }

    Image SetupOverlay(Image img)
    {
        img.color = highlightColor;
        img.raycastTarget = false;

        var cg = img.GetComponent<CanvasGroup>();
        if (!cg) cg = img.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false; cg.interactable = false;

        // 缓存：根据父级判断是卡层还是行层
        if (img.transform.parent == transform)
            _lineOverlay = img;
        else
            _cardOverlay = img;

        return img;
    }

    /// <summary>寻找“评论卡根”：包含 Header 与 BodyGroup（或 BodyText）的节点</summary>
    Transform FindCardRoot(Transform t)
    {
        var cur = t;
        for (int i = 0; i < 10 && cur != null; i++)
        {
            bool hasHeader = cur.Find("Header") != null;
            bool hasBody   = cur.Find("BodyGroup") != null || cur.Find("BodyText") != null;
            if (hasHeader && hasBody) return cur;
            cur = cur.parent;
        }
        return null;
    }

    void OnDisable()
    {
        if (_cardOverlay) _cardOverlay.enabled = false;
        if (_lineOverlay) _lineOverlay.enabled = false;
    }

    // 当 Rect 或 文本发生改变时再次更新（避免动态换行/布局变化带来的错位）
    void OnRectTransformDimensionsChange()
    {
        if (!highlightOnHover) return;

        if (_lineOverlay && _lineOverlay.enabled && !highlightWholeCard)
            UpdateLineOverlayGeometry(_lineOverlay.rectTransform, _tmp);
        if (_cardOverlay && _cardOverlay.enabled && highlightWholeCard)
            UpdateCardOverlayGeometry(_cardOverlay.rectTransform, FindCardRoot(transform) as RectTransform);
    }

    // 如果你在运行中频繁改文本，可在渲染前再算一次
    void OnPreRenderText() // 只对 TMP_Text 有效（Unity 2021+）
    {
        if (_lineOverlay && _lineOverlay.enabled && !highlightWholeCard)
            UpdateLineOverlayGeometry(_lineOverlay.rectTransform, _tmp);
    }
}


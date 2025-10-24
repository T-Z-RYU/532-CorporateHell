using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreGameController : MonoBehaviour
{
    [Header("Pages (三个页面)")]
    public GameObject storePage;           // 左侧商店主页面
    public GameObject marketPage;          // 右侧Market页面
    public GameObject resultPage;          // 卖出结果页（你预制好的）

    [Header("Manual 4 Slots (手动排位)")]
    public PostSlot[] slots = new PostSlot[4];

    [Header("Store Top UI")]
    public TMP_Text goldText;
    public Button exitButton;

    [Header("Market Page UI")]
    public Image marketAvatar;
    public TMP_Text marketCaption;
    public TMP_Text marketHatersCleared;
    public TMP_Text txtCurrentMarketValue;
    public TMP_Text txtPostValue;
    public TMP_Text txtPrice;
    public Button btnMinus;
    public Button btnPlus;
    public Button btnSell;

    [Header("Result Page UI")]
    public Image resultAvatar;
    public TMP_Text resultCaption;
    public TMP_Text resultSoldPrice;
    public Button btnBackToStore;
    public Button btnExitAll;

    [Header("Other GameObject (Result Page 出现时关闭)")]
    public GameObject otherGameObject;     // ✅ 你希望在 Result Page 出现时关闭的物体

    [Header("Price Settings")]
    public int priceStep = 5;
    public int minPrice = 0;
    public int maxPrice = 99999;

    [Header("Color Settings")]
    public Color goldColor = new Color(1f, 0.85f, 0f); // 默认金色

    // runtime
    private BugMiniGame.MiniSession.PostStat _selected;
    private int _selectedIndex = -1;
    private int _sellPrice = 0;

    void OnEnable()
    {
        // 默认显示 Store，隐藏 Market 和 Result
        SetPageActive(storePage, true);
        SetPageActive(marketPage, false);
        SetPageActive(resultPage, false);

        BuildSlotsFromSession();
        BindSlotClicks();
        RefreshGold();

        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitUI);
        }

        // 结果页按钮
        if (btnBackToStore)
        {
            btnBackToStore.onClick.RemoveAllListeners();
            btnBackToStore.onClick.AddListener(BackToStorePage);
        }
        if (btnExitAll)
        {
            btnExitAll.onClick.RemoveAllListeners();
            btnExitAll.onClick.AddListener(ExitUI);
        }
    }

    // ========== 左侧槽位 ==========
    void BuildSlotsFromSession()
    {
        var ses = BugMiniGame.MiniSession.Instance;
        if (ses == null || ses.PostStats == null || ses.PostStats.Count == 0)
        {
            ClearAllSlots();
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i < ses.PostStats.Count)
            {
                var p = ses.PostStats[i];
                slots[i].Setup(p.avatar, SafeText(p.name), p.Percent);
                slots[i].gameObject.SetActive(true);
            }
            else
            {
                slots[i].Clear();
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    void BindSlotClicks()
    {
        var ses = BugMiniGame.MiniSession.Instance;
        if (ses == null || ses.PostStats == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            if (slots[i] == null) continue;

            slots[i].Bind(() =>
            {
                if (idx >= 0 && idx < ses.PostStats.Count)
                    OpenMarketFor(ses.PostStats[idx], idx);
            });
        }
    }

    void ClearAllSlots()
    {
        foreach (var s in slots)
            if (s != null) { s.Clear(); s.gameObject.SetActive(false); }
    }

    // ========== Market ==========
    void OpenMarketFor(BugMiniGame.MiniSession.PostStat post, int index)
    {
        _selected = post;
        _selectedIndex = index;

        SetPageActive(storePage, true);
        SetPageActive(marketPage, true);
        SetPageActive(resultPage, false);

        if (marketAvatar) marketAvatar.sprite = post.avatar;
        if (marketCaption) marketCaption.text = SafeText(post.name);
        if (marketHatersCleared) marketHatersCleared.text = $"Hater's Cleared: {post.Percent}%";

        // 线性映射 40±20
        int mapped = Mathf.RoundToInt(20f + 0.4f * post.Percent);
        string hex = ColorUtility.ToHtmlStringRGB(goldColor);

        if (txtCurrentMarketValue) txtCurrentMarketValue.text = $"Current Market Value: <color=#{hex}>G{mapped}</color>";
        if (txtPostValue) txtPostValue.text = $"Post Value: <color=#{hex}>G{mapped}</color>";

        _sellPrice = Mathf.Clamp(mapped, minPrice, maxPrice);
        RefreshPriceLabel();

        if (btnMinus)
        {
            btnMinus.onClick.RemoveAllListeners();
            btnMinus.onClick.AddListener(() =>
            {
                _sellPrice = Mathf.Clamp(_sellPrice - priceStep, minPrice, maxPrice);
                RefreshPriceLabel();
            });
        }
        if (btnPlus)
        {
            btnPlus.onClick.RemoveAllListeners();
            btnPlus.onClick.AddListener(() =>
            {
                _sellPrice = Mathf.Clamp(_sellPrice + priceStep, minPrice, maxPrice);
                RefreshPriceLabel();
            });
        }
        if (btnSell)
        {
            btnSell.onClick.RemoveAllListeners();
            btnSell.onClick.AddListener(SellSelectedPost);
        }
    }

    void RefreshPriceLabel()
    {
        string hex = ColorUtility.ToHtmlStringRGB(goldColor);
        if (txtPrice) txtPrice.text = $"Sell Price：\n<color=#{hex}>G{_sellPrice}</color>";
    }

    void SellSelectedPost()
    {
        if (_selected == null) return;
        var ses = BugMiniGame.MiniSession.Instance;
        if (ses == null) return;

        // 加金币
        ses.AddGold(_sellPrice);
        RefreshGold();

        // 清空槽位
        if (_selectedIndex >= 0 && _selectedIndex < slots.Length && slots[_selectedIndex] != null)
            slots[_selectedIndex].Clear();

        // ✅ 切到结果页，关闭 otherGameObject
        SetPageActive(storePage, false);
        SetPageActive(marketPage, false);
        SetPageActive(resultPage, true);

        if (otherGameObject) otherGameObject.SetActive(false);

        // 结果页内容
        string hex = ColorUtility.ToHtmlStringRGB(goldColor);
        if (resultAvatar) resultAvatar.sprite = _selected.avatar;
        if (resultCaption) resultCaption.text = SafeText(_selected.name);
        if (resultSoldPrice) resultSoldPrice.text = $"Sold for <color=#{hex}>G{_sellPrice}</color>";

        _selected = null;
        _selectedIndex = -1;
    }

    // ========== 返回商店 ==========
    void BackToStorePage()
    {
        // 重新打开 otherGameObject
        if (otherGameObject) otherGameObject.SetActive(true);

        SetPageActive(resultPage, false);
        SetPageActive(marketPage, false);
        SetPageActive(storePage, true);

        // 若希望刷新左侧Slot，可打开下一行
        // BuildSlotsFromSession();
    }

    // ========== 顶部 & 退出 ==========
    void RefreshGold()
    {
        var ses = BugMiniGame.MiniSession.Instance;
        if (goldText && ses != null)
        {
            string hex = ColorUtility.ToHtmlStringRGB(goldColor);
            goldText.text = $"Gold: <color=#{hex}>G{ses.Gold}</color>";
        }
    }

    public void ExitUI()
    {
        if (resultPage) resultPage.SetActive(false);
        if (marketPage) marketPage.SetActive(false);
        if (storePage) storePage.SetActive(false);

        // 重新打开 otherGameObject（保证退出时回到正常界面）
        if (otherGameObject) otherGameObject.SetActive(true);

        gameObject.SetActive(false);

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var ps = player.GetComponent<PlayerScript>();
            if (ps != null) ps.UnFreezePlayer();
        }
    }

    // ========== 工具 ==========
    void SetPageActive(GameObject page, bool on)
    {
        if (page != null) page.SetActive(on);
    }

    static string SafeText(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Post";
        return StripRichTags(s).Trim();
    }

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
}

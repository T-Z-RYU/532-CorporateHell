using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PostSlot : MonoBehaviour
{
    [Header("UI")]
    public Image avatar;            // 左侧头像
    public TMP_Text caption;        // 名字
    public TMP_Text hatersCleared;  // "Hater's Cleared: XX%"

    [Tooltip("可选：如果你手动放了 Button，就拖进来；否则脚本会自动添加")]
    public Button selectButton;

    private Action _onClick;

    // ---------- 初始化与绑定 ----------
    public void Setup(Sprite avatarSprite, string name, int percent)
    {
        if (avatar) avatar.sprite = avatarSprite;
        if (caption) caption.text = string.IsNullOrEmpty(name) ? "Post" : name;
        if (hatersCleared) hatersCleared.text = $"Hater's Cleared: {percent}%";
    }

    public void Bind(Action onClick)
    {
        _onClick = onClick;

        // 如果没手动指定按钮，就自动给自己加一个
        if (selectButton == null)
        {
            selectButton = GetComponent<Button>();
            if (selectButton == null)
                selectButton = gameObject.AddComponent<Button>();
        }

        // 按钮透明：让点击区域覆盖整个 Slot
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0);  // 全透明
        }
        selectButton.transition = Selectable.Transition.None;

        // 绑定事件
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => _onClick?.Invoke());
    }

    public void Clear()
    {
        if (avatar) avatar.sprite = null;
        if (caption) caption.text = "";
        if (hatersCleared) hatersCleared.text = "Hater's Cleared: 0%";
        if (selectButton) selectButton.onClick.RemoveAllListeners();
        _onClick = null;
    }
}

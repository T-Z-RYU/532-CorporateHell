using UnityEngine;

/// <summary>
/// 随机生成一个完整的页面（预制体），
/// 每次运行只会出现其中一个。
/// </summary>
public class RandomPageSpawner : MonoBehaviour
{
    [Header("页面预制体（BugMiniGame 页面）")]
    [Tooltip("拖入你预制好的 BugMiniGame 页面，例如 BugPage_Food、BugPage_Travel、BugPage_Game 等")]
    public GameObject[] pagePrefabs;  // 多个完整页面预制体

    [Header("生成设置")]
    [Tooltip("生成到哪里（通常是 Canvas）")]
    public Transform spawnParent;     // 放到哪个父物体下面（例如 Canvas）
    public bool autoSpawnOnStart = true; // 是否在启动时自动生成

    private GameObject currentPage;   // 当前生成的页面实例

    void Start()
    {
        if (autoSpawnOnStart)
            SpawnRandomPage();
    }

    [ContextMenu("Spawn Random Page")]
    public void SpawnRandomPage()
    {
        // 清理旧页面
        if (currentPage != null)
            Destroy(currentPage);

        if (pagePrefabs == null || pagePrefabs.Length == 0)
        {
            Debug.LogWarning("[RandomPageSpawner] 没有页面预制体可用！");
            return;
        }

        // 随机挑选一个页面预制体
        int index = Random.Range(0, pagePrefabs.Length);
        var prefab = pagePrefabs[index];

        // 实例化到 Canvas 下
        currentPage = Instantiate(prefab, spawnParent != null ? spawnParent : transform);
        currentPage.SetActive(true);

        Debug.Log($"[RandomPageSpawner] 已生成随机页面：{prefab.name}");
    }

    // 如果你希望重新随机生成一个新页面，可以通过调用这个方法实现
    public void Regenerate()
    {
        SpawnRandomPage();
    }
}

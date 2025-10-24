using UnityEngine;

/// <summary>
/// �������һ��������ҳ�棨Ԥ���壩��
/// ÿ������ֻ���������һ����
/// </summary>
public class RandomPageSpawner : MonoBehaviour
{
    [Header("ҳ��Ԥ���壨BugMiniGame ҳ�棩")]
    [Tooltip("������Ԥ�ƺõ� BugMiniGame ҳ�棬���� BugPage_Food��BugPage_Travel��BugPage_Game ��")]
    public GameObject[] pagePrefabs;  // �������ҳ��Ԥ����

    [Header("��������")]
    [Tooltip("���ɵ����ͨ���� Canvas��")]
    public Transform spawnParent;     // �ŵ��ĸ����������棨���� Canvas��
    public bool autoSpawnOnStart = true; // �Ƿ�������ʱ�Զ�����

    private GameObject currentPage;   // ��ǰ���ɵ�ҳ��ʵ��

    void Start()
    {
        if (autoSpawnOnStart)
            SpawnRandomPage();
    }

    [ContextMenu("Spawn Random Page")]
    public void SpawnRandomPage()
    {
        // �����ҳ��
        if (currentPage != null)
            Destroy(currentPage);

        if (pagePrefabs == null || pagePrefabs.Length == 0)
        {
            Debug.LogWarning("[RandomPageSpawner] û��ҳ��Ԥ������ã�");
            return;
        }

        // �����ѡһ��ҳ��Ԥ����
        int index = Random.Range(0, pagePrefabs.Length);
        var prefab = pagePrefabs[index];

        // ʵ������ Canvas ��
        currentPage = Instantiate(prefab, spawnParent != null ? spawnParent : transform);
        currentPage.SetActive(true);

        Debug.Log($"[RandomPageSpawner] ���������ҳ�棺{prefab.name}");
    }

    // �����ϣ�������������һ����ҳ�棬����ͨ�������������ʵ��
    public void Regenerate()
    {
        SpawnRandomPage();
    }
}

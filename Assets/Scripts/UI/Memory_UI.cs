using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 常驻管理器：只在全局数据就绪和通关记录保存成功时填充列表。
public class Memory_UI : MonoBehaviour
{
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject memoryEntry;
    [SerializeField] private Button memoryButton;
    [SerializeField] private GameObject memoryPanel;
    [SerializeField] private ScrollRect scrollRect;

    private SaveManager saveManager;
    private List<EndingData> catalog;
    private readonly List<GameObject> rows = new List<GameObject>();
    private readonly List<string> defaultTitles = new List<string>();
    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (memoryEntry != null) memoryEntry.SetActive(false);
        if (memoryPanel != null) memoryPanel.SetActive(false);
    }

    private void Start()
    {
        saveManager = SaveManager.instance;
        if (saveManager == null) { Debug.LogError("回忆面板缺少 SaveManager。", this); return; }
        saveManager.GlobalEndingsChanged += RefreshMemory;
        // 兼容各组件 Start 执行顺序；未加载时等待事件，已加载时直接使用缓存。
        if (saveManager.IsGlobalProgressLoaded) RefreshMemory();
    }

    private void OnDestroy()
    {
        if (saveManager != null) saveManager.GlobalEndingsChanged -= RefreshMemory;
    }

    private void RefreshMemory()
    {
        bool unlocked = saveManager.IsGlobalProgressLoaded && saveManager.HasCompletedGame;
        if (memoryEntry != null) memoryEntry.SetActive(unlocked);
        if (memoryButton != null) memoryButton.interactable = unlocked;
        if (!unlocked && memoryPanel != null) memoryPanel.SetActive(false);
        if (!saveManager.IsGlobalProgressLoaded) return;
        if (memoryPanel == null || scrollRect == null || scrollRect.content == null)
            throw new InvalidOperationException("回忆面板需要绑定 Panel 和 ScrollRect。");

        if (catalog == null)
        {
            var csv = Resources.Load<TextAsset>("CSV/CSV-End");
            if (csv == null) throw new InvalidOperationException("找不到 CSV/CSV-End。");
            catalog = EndingRules.Load(csv.text).FindAll(ending => ending.Priority < 4);
        }

        var byId = new Dictionary<string, EndingData>(StringComparer.Ordinal);
        foreach (var ending in catalog) byId.Add(ending.结局ID.Trim(), ending);
        var achieved = new List<EndingData>();
        var included = new HashSet<string>(StringComparer.Ordinal);
        // 全局记录已经按优先级、首次达成顺序排序。
        foreach (var record in saveManager.GetAchievedEndings())
            if (byId.TryGetValue(record.endingId.Trim(), out var ending) && included.Add(ending.结局ID.Trim()))
                achieved.Add(ending);

        if (rows.Count == 0)
        {
            // 使用编辑器中摆放的条目，按 Content 中的兄弟顺序更新。
            for (int index = 0; index < scrollRect.content.childCount; index++)
            {
                var row = scrollRect.content.GetChild(index).gameObject;
                var text = row.GetComponentInChildren<TMP_Text>(true);
                if (text == null) throw new InvalidOperationException("EndList 缺少 TMP 结局标题。");
                rows.Add(row);
                defaultTitles.Add(text.text);
            }
        }
        if (rows.Count != catalog.Count)
            throw new InvalidOperationException($"回忆 Content 有 {rows.Count} 个条目，但 CSV 中有 {catalog.Count} 个剧情结局，请调整场景中的 EndList 数量。");
        for (int index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var imageTransform = row.transform.Find("Image");
            var image = imageTransform != null ? imageTransform.GetComponent<Image>() : null;
            var text = row.GetComponentInChildren<TMP_Text>(true);
            if (image == null || text == null)
                throw new InvalidOperationException("EndList 需要 Image 子物体及 TMP 结局标题。");
            var color = image.color;
            if (index < achieved.Count)
            {
                var ending = achieved[index];
                image.sprite = LoadSprite(ending.image);
                image.preserveAspect = true;
                color.a = image.sprite != null ? 1f : 0f;
                text.text = ending.结局标题;
            }
            else
            {
                image.sprite = null;
                color.a = 0f;
                // 占位项保留这个场景条目最初的文字。
                text.text = defaultTitles[index];
            }
            image.color = color;
        }

        scrollRect.StopMovement();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private Sprite LoadSprite(string resource)
    {
        if (string.IsNullOrWhiteSpace(resource)) return null;
        string path = resource.Trim().Replace('\\', '/');
        if (path.StartsWith("Assets/Resources/", StringComparison.Ordinal)) path = path.Substring(17);
        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 4);
        if (!path.Contains("/")) path = "Sprites/" + path;
        if (!sprites.TryGetValue(path, out var sprite))
        {
            sprite = Resources.Load<Sprite>(path);
            sprites.Add(path, sprite);
            if (sprite == null) Debug.LogWarning($"回忆结局图片不存在：{resource}", this);
        }
        return sprite;
    }

    public void OnMemoryClicked()
    {
        if (saveManager == null || !saveManager.HasCompletedGame || memoryPanel == null) return;
        if (mainPanel != null) mainPanel.SetActive(false);
        memoryPanel.SetActive(true);
    }

    public void OnBackClicked()
    {
        if (memoryPanel != null) memoryPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }
}

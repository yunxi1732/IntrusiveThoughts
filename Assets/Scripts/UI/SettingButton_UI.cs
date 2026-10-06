using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class SettingButton_UI : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    private Button button;
    private CanvasGroup visibility;

    private void Awake()
    {
        button = GetComponent<Button>();
        visibility = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UpdateVisibility(SceneManager.GetActiveScene());
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene previous, Scene current)
    {
        UpdateVisibility(current);
    }

    private void UpdateVisibility(Scene scene)
    {
        bool show = scene.name != "MainScene";
        // 不停用自身，常驻按钮才能继续接收场景变化事件。
        visibility.alpha = show ? 1f : 0f;
        visibility.interactable = show;
        visibility.blocksRaycasts = show;
    }

    public void OnSettingsClicked()
    {
        if (SceneManager.GetActiveScene().name == "MainScene") 
            return;

        settingsPanel.SetActive(true);
    }
}

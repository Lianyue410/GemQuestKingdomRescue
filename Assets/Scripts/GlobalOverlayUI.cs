using UnityEngine;
using UnityEngine.UI;

public class GlobalOverlayUI : MonoBehaviour
{
  [Header("Buttons")]
  public Button toolButton;
  public Button musicButton;

  [Header("Panels")]
  public GameObject toolPanel;
  public GameObject musicPanel;

  [Header("Audio Sliders")]
  public Slider musicSlider;
  public Slider sfxSlider;

  private CanvasGroup toolPanelCanvasGroup;
  private CanvasGroup musicPanelCanvasGroup;

  void Awake()
  {
    toolPanelCanvasGroup = EnsureCanvasGroup(toolPanel);
    musicPanelCanvasGroup = EnsureCanvasGroup(musicPanel);

    MakePanelOnlyControlsClickable(toolPanel);
    MakePanelOnlyControlsClickable(musicPanel);

    if (toolButton != null)
    {
      toolButton.onClick.RemoveListener(ToggleToolPanel);
      toolButton.onClick.AddListener(ToggleToolPanel);
    }

    if (musicButton != null)
    {
      musicButton.onClick.RemoveListener(ToggleMusicPanel);
      musicButton.onClick.AddListener(ToggleMusicPanel);
    }

    if (musicSlider != null)
    {
      musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
      musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
    }

    if (sfxSlider != null)
    {
      sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
      sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
    }
  }

  void Start()
  {
    SyncFromAudioManager();
  }

  public void ToggleToolPanel()
  {
    if (toolPanel == null) return;

    bool newState = !toolPanel.activeSelf;
    toolPanel.SetActive(newState);

    if (newState && musicPanel != null)
    {
      musicPanel.SetActive(false);
    }
  }

  public void ToggleMusicPanel()
  {
    if (musicPanel == null) return;

    bool newState = !musicPanel.activeSelf;
    musicPanel.SetActive(newState);

    if (newState && toolPanel != null)
    {
      toolPanel.SetActive(false);
    }

    if (newState)
    {
      SyncFromAudioManager();
    }
  }

  public void CloseToolPanel()
  {
    if (toolPanel != null)
    {
      toolPanel.SetActive(false);
    }
  }

  public void CloseMusicPanel()
  {
    if (musicPanel != null)
    {
      musicPanel.SetActive(false);
    }
  }

  public void SyncFromAudioManager()
  {
    if (AudioManager.Instance == null) return;

    if (musicSlider != null)
    {
      musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
    }

    if (sfxSlider != null)
    {
      sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);
    }
  }

  void OnMusicSliderChanged(float value)
  {
    AudioManager.Instance?.SetMusicVolume(value);
  }

  void OnSfxSliderChanged(float value)
  {
    AudioManager.Instance?.SetSfxVolume(value);
  }

  public void SetOverlayPanelsInteractable(bool interactable)
  {
    SetCanvasGroupInteractable(toolPanelCanvasGroup, interactable);
    SetCanvasGroupInteractable(musicPanelCanvasGroup, interactable);
  }

  CanvasGroup EnsureCanvasGroup(GameObject target)
  {
    if (target == null) return null;

    CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

    if (canvasGroup == null)
    {
      canvasGroup = target.AddComponent<CanvasGroup>();
    }

    canvasGroup.interactable = true;
    canvasGroup.blocksRaycasts = true;

    return canvasGroup;
  }

  void SetCanvasGroupInteractable(CanvasGroup canvasGroup, bool interactable)
  {
    if (canvasGroup == null) return;

    canvasGroup.interactable = interactable;
    canvasGroup.blocksRaycasts = interactable;
  }

  void MakePanelOnlyControlsClickable(GameObject panel)
  {
    if (panel == null) return;

    Graphic[] graphics = panel.GetComponentsInChildren<Graphic>(true);

    foreach (Graphic graphic in graphics)
    {
      graphic.raycastTarget = false;
    }

    Button[] buttons = panel.GetComponentsInChildren<Button>(true);

    foreach (Button button in buttons)
    {
      EnableControlRaycast(button.gameObject);
    }

    Slider[] sliders = panel.GetComponentsInChildren<Slider>(true);

    foreach (Slider slider in sliders)
    {
      EnableControlRaycast(slider.gameObject);
    }

    Scrollbar[] scrollbars = panel.GetComponentsInChildren<Scrollbar>(true);

    foreach (Scrollbar scrollbar in scrollbars)
    {
      EnableControlRaycast(scrollbar.gameObject);
    }
  }

  void EnableControlRaycast(GameObject control)
  {
    if (control == null) return;

    Graphic[] graphics = control.GetComponentsInChildren<Graphic>(true);

    foreach (Graphic graphic in graphics)
    {
      graphic.raycastTarget = true;
    }
  }
}
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlowManager : MonoBehaviour
{
  private static GameFlowManager instance;

  public static GameFlowManager Instance
  {
    get
    {
      if (instance == null)
      {
        instance = FindFirstObjectByType<GameFlowManager>();
      }

      return instance;
    }
  }

  public static bool BlocksGameplayInput => Instance != null && Instance.modalState != ModalState.None;

  private const string MainMenuSceneName = "MainMenu";
  private const string FirstLevelSceneName = "Level_01";

  private enum ModalState
  {
    None,
    Pause,
    Win,
    Lose
  }

  private ModalState modalState;
  private string currentSceneName;

  private GameObject pausePanel;
  private GameObject winPanel;
  private GameObject losePanel;
  private Button[] sceneButtons;
  private GlobalOverlayUI overlayUI;

  void Awake()
  {
    if (instance != null && instance != this)
    {
      Destroy(gameObject);
      return;
    }

    instance = this;
    SceneManager.sceneLoaded += OnSceneLoaded;
  }

  void Start()
  {
    SetupScene(SceneManager.GetActiveScene());
  }

  void OnDestroy()
  {
    if (instance == this)
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
      instance = null;
    }
  }

  void OnSceneLoaded(Scene scene, LoadSceneMode mode)
  {
    SetupScene(scene);
  }

  void SetupScene(Scene scene)
  {
    currentSceneName = scene.name;
    modalState = ModalState.None;
    Time.timeScale = 1f;

    CacheSceneReferences();
    HideAllPanels();
    overlayUI?.SetOverlayPanelsInteractable(true);
    UpdateButtonInteractivity();
    UnlockCursor();

    if (LevelObjectiveManager.Instance != null && winPanel != null)
    {
      LevelObjectiveManager.Instance.levelCompletePanel = winPanel;
    }

    if (CoinProgress.Instance != null)
    {
      CoinProgress.Instance.RefreshFromSavedProgress();
    }
  }

  public void PauseGame()
  {
    if (IsMainMenuScene() || modalState != ModalState.None || pausePanel == null) return;
    ShowModal(ModalState.Pause, pausePanel);
  }

  public void ResumeGame()
  {
    modalState = ModalState.None;
    HideAllPanels();
    Time.timeScale = 1f;
    overlayUI?.SetOverlayPanelsInteractable(true);
    UpdateButtonInteractivity();
  }

  public void ShowWinPanel()
  {
    if (modalState != ModalState.None) return;
    if (IsMainMenuScene() || winPanel == null) return;

    CoinProgress.Instance?.CommitLevelProgress();
    ShowModal(ModalState.Win, winPanel);
    AudioManager.Instance?.PlayFinalWinSfx();
  }

  public void ShowLosePanel()
  {
    if (modalState != ModalState.None) return;
    if (IsMainMenuScene() || losePanel == null) return;

    ShowModal(ModalState.Lose, losePanel);
    AudioManager.Instance?.PlayLoseSfx();
  }

  public void StartNewGame()
  {
    modalState = ModalState.None;
    Time.timeScale = 1f;
    CoinProgress.ResetAllSavedProgress();
    SceneManager.LoadScene(FirstLevelSceneName);
  }

  public void RestartLevel()
  {
    modalState = ModalState.None;
    Time.timeScale = 1f;
    CoinProgress.Instance?.RestoreLevelStartProgress();
    SceneManager.LoadScene(currentSceneName);
  }

  public void LoadNextLevel()
  {
    modalState = ModalState.None;
    Time.timeScale = 1f;

    string nextScene = GetNextSceneName();
    if (string.IsNullOrEmpty(nextScene))
    {
      SceneManager.LoadScene(MainMenuSceneName);
      return;
    }

    SceneManager.LoadScene(nextScene);
  }

  public void GoToMainMenu()
  {
    modalState = ModalState.None;
    Time.timeScale = 1f;
    SceneManager.LoadScene(MainMenuSceneName);
  }

  public void QuitGame()
  {
    modalState = ModalState.None;
    Time.timeScale = 1f;

#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
  }

  void ShowModal(ModalState state, GameObject panel)
  {
    if (panel == null) return;

    modalState = state;
    HideAllPanels();
    panel.SetActive(true);

    Time.timeScale = 0f;
    UnlockCursor();
    overlayUI?.SetOverlayPanelsInteractable(false);
    UpdateButtonInteractivity();
  }

  void HideAllPanels()
  {
    SetPanelVisible(pausePanel, false);
    SetPanelVisible(winPanel, false);
    SetPanelVisible(losePanel, false);
  }

  void SetPanelVisible(GameObject panel, bool visible)
  {
    if (panel != null)
    {
      panel.SetActive(visible);
    }
  }

  void CacheSceneReferences()
  {
    sceneButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    overlayUI = FindFirstObjectByType<GlobalOverlayUI>(FindObjectsInactive.Include);

    pausePanel = FindObjectByName("PausePanel");
    winPanel = FindObjectByName("WinPanel");
    losePanel = FindObjectByName("LosePanel");

    if (winPanel == null)
    {
      winPanel = FindObjectByName("LevelCompletePanel");
    }
  }

  void UpdateButtonInteractivity()
  {
    if (sceneButtons == null) return;

    GameObject activePanel = GetActivePanel();

    foreach (Button button in sceneButtons)
    {
      if (button == null) continue;

      if (modalState == ModalState.None)
      {
        button.interactable = true;
        continue;
      }

      button.interactable = activePanel != null && IsInsidePanel(button.transform, activePanel.transform);
    }
  }

  GameObject GetActivePanel()
  {
    switch (modalState)
    {
      case ModalState.Pause:
        return pausePanel;
      case ModalState.Win:
        return winPanel;
      case ModalState.Lose:
        return losePanel;
      default:
        return null;
    }
  }

  bool IsInsidePanel(Transform target, Transform panelRoot)
  {
    if (target == null || panelRoot == null) return false;
    return target == panelRoot || target.IsChildOf(panelRoot);
  }

  GameObject FindObjectByName(string objectName)
  {
    Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    foreach (Transform item in transforms)
    {
      if (item.name == objectName)
      {
        return item.gameObject;
      }
    }

    return null;
  }

  string GetNextSceneName()
  {
    switch (currentSceneName)
    {
      case "Level_01":
        return "Level_02";
      case "Level_02":
        return "Level_03";
      case "Level_03":
        return MainMenuSceneName;
      default:
        return null;
    }
  }

  bool IsMainMenuScene()
  {
    return currentSceneName == MainMenuSceneName;
  }

  void UnlockCursor()
  {
    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
  }
}

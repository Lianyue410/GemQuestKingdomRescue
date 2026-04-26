using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelObjectiveManager : MonoBehaviour
{
  public static LevelObjectiveManager Instance;

  [Header("Objectives")]
  public int requiredGems = 2;
  public int collectedGems;

  [Header("Scene Names")]
  public string nextLevelSceneName = "Level_02";
  public string mainMenuSceneName = "MainMenu";

  [Header("UI")]
  public TMP_Text gemsText;
  public TMP_Text messageText;
  public GameObject levelCompletePanel;

  private bool levelComplete;
  private float messageTimer;

  public bool IsLevelComplete => levelComplete;

  void Awake()
  {
    Instance = this;
    ApplyLevelDefaults();
    AutoAssignReferences();

    if (levelCompletePanel != null)
    {
      levelCompletePanel.SetActive(false);
    }

    Time.timeScale = 1f;
    UpdateUI();
  }

  void Update()
  {
    if (messageTimer <= 0f) return;

    messageTimer -= Time.unscaledDeltaTime;
    if (messageTimer <= 0f && messageText != null && !levelComplete)
    {
      messageText.text = "";
    }
  }

  public void CollectGem()
  {
    if (levelComplete) return;

    collectedGems = Mathf.Clamp(collectedGems + 1, 0, requiredGems);
    UpdateUI();
    ShowMessage($"Gem collected: {collectedGems}/{requiredGems}");
  }

  public void TryFinishLevel()
  {
    if (levelComplete) return;

    if (collectedGems < requiredGems)
    {
      ShowMessage($"Collect all gems first: {collectedGems}/{requiredGems}");
      return;
    }

    levelComplete = true;
    UpdateUI();

    if (messageText != null)
    {
      messageText.text = "Level Complete!";
    }

    if (GameFlowManager.Instance != null)
    {
      GameFlowManager.Instance.ShowWinPanel();
      return;
    }

    if (levelCompletePanel != null)
    {
      levelCompletePanel.SetActive(true);
    }

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    Time.timeScale = 0f;
  }

  public void LoadNextLevel()
  {
    if (GameFlowManager.Instance != null)
    {
      GameFlowManager.Instance.LoadNextLevel();
      return;
    }

    Time.timeScale = 1f;
    SceneManager.LoadScene(nextLevelSceneName);
  }

  public void BackToMainMenu()
  {
    if (GameFlowManager.Instance != null)
    {
      GameFlowManager.Instance.GoToMainMenu();
      return;
    }

    Time.timeScale = 1f;
    SceneManager.LoadScene(mainMenuSceneName);
  }

  void UpdateUI()
  {
    AutoAssignReferences();

    if (gemsText != null)
    {
      gemsText.text = $"Gems: {collectedGems}/{requiredGems}";
    }
  }

  void ShowMessage(string message)
  {
    if (messageText == null) return;

    messageText.text = message;
    messageTimer = 2f;
  }

  void AutoAssignReferences()
  {
    if (gemsText == null)
    {
      gemsText = FindTextByName("GemsText");
    }

    if (levelCompletePanel == null)
    {
      GameObject panel = FindObjectByName("WinPanel");
      if (panel == null)
      {
        panel = FindObjectByName("LevelCompletePanel");
      }

      if (panel != null)
      {
        levelCompletePanel = panel;
      }
    }

    if (messageText == null)
    {
      messageText = FindTextByName("ObjectiveMessageText");
    }
  }

  void ApplyLevelDefaults()
  {
    string sceneName = SceneManager.GetActiveScene().name;

    if (sceneName == "Level_03")
    {
      requiredGems = 3;
      return;
    }

    if (sceneName == "Level_01" || sceneName == "Level_02")
    {
      requiredGems = 2;
    }
  }

  TMP_Text FindTextByName(string objectName)
  {
    TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    foreach (TMP_Text text in texts)
    {
      if (text.name == objectName)
      {
        return text;
      }
    }

    return null;
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
}

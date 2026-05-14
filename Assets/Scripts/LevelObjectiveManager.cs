using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class LevelObjectiveManager : MonoBehaviour
{
  public static LevelObjectiveManager Instance;

  [Header("Objectives")]
  public int requiredGems = 2;
  public int collectedGems;

  [Header("Countdown")]
  public float levelTimeLimitSeconds = 40f;
  public TMP_Text timerText;
  public Color normalTimerColor = Color.white;
  public Color warningTimerColor = new Color(1f, 0.32f, 0.32f, 1f);
  public float warningThresholdSeconds = 10f;

  [Header("Scene Names")]
  public string nextLevelSceneName = "Level_02";
  public string mainMenuSceneName = "MainMenu";

  [Header("UI")]
  public TMP_Text gemsText;
  public TMP_Text messageText;
  public GameObject levelCompletePanel;

  private bool levelComplete;
  private bool timerExpired;
  private float messageTimer;
  private float remainingTime;

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
    InitializeTimer();
    UpdateUI();
  }

  void OnEnable()
  {
    ApplyLevelDefaults();
    AutoAssignReferences();

    if (!Application.isPlaying)
    {
      remainingTime = Mathf.Max(1f, levelTimeLimitSeconds);
      UpdateUI();
    }
  }

  void Update()
  {
    UpdateMessageTimer();
    UpdateCountdownTimer();
  }

  void UpdateMessageTimer()
  {
    if (messageTimer <= 0f) return;

    messageTimer -= Time.unscaledDeltaTime;
    if (messageTimer <= 0f && messageText != null && !levelComplete && !timerExpired)
    {
      messageText.text = "";
    }
  }

  void UpdateCountdownTimer()
  {
    if (levelComplete || timerExpired) return;
    if (GameFlowManager.BlocksGameplayInput) return;

    remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
    UpdateTimerUI();

    if (remainingTime > 0f) return;

    timerExpired = true;

    if (messageText != null)
    {
      messageText.text = "Time's up!";
    }

    if (GameFlowManager.Instance != null)
    {
      GameFlowManager.Instance.ShowLosePanel();
      return;
    }

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
    Time.timeScale = 0f;
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
    if (levelComplete || timerExpired) return;

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

    UpdateTimerUI();
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

    if (timerText == null)
    {
      timerText = FindTextByName("TimerText");
    }

    if (timerText == null && gemsText != null)
    {
      timerText = CreateTimerText(gemsText);
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
      levelTimeLimitSeconds = 90f;
      return;
    }

    if (sceneName == "Level_01" || sceneName == "Level_02")
    {
      requiredGems = 2;
      levelTimeLimitSeconds = sceneName == "Level_01" ? 40f : 60f;
    }
  }

  void InitializeTimer()
  {
    timerExpired = false;
    remainingTime = Mathf.Max(1f, levelTimeLimitSeconds);
    UpdateTimerUI();
  }

  void UpdateTimerUI()
  {
    AutoAssignReferences();
    if (timerText == null) return;

    int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
    int minutes = totalSeconds / 60;
    int seconds = totalSeconds % 60;
    timerText.text = $"Time: {minutes:00}:{seconds:00}";
    timerText.color = remainingTime <= warningThresholdSeconds ? warningTimerColor : normalTimerColor;
  }

  TMP_Text CreateTimerText(TMP_Text sourceText)
  {
    if (sourceText == null || sourceText.transform.parent == null) return null;

    GameObject timerObject = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
    timerObject.transform.SetParent(sourceText.transform.parent, false);

    RectTransform sourceRect = sourceText.rectTransform;
    RectTransform timerRect = timerObject.GetComponent<RectTransform>();
    timerRect.anchorMin = sourceRect.anchorMin;
    timerRect.anchorMax = sourceRect.anchorMax;
    timerRect.pivot = sourceRect.pivot;
    timerRect.sizeDelta = new Vector2(Mathf.Max(sourceRect.sizeDelta.x, 170f), sourceRect.sizeDelta.y);
    timerRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(sourceRect.sizeDelta.x + 35f, 0f);
    timerRect.localScale = Vector3.one;

    TextMeshProUGUI timerLabel = timerObject.GetComponent<TextMeshProUGUI>();
    timerLabel.font = sourceText.font;
    timerLabel.fontSharedMaterial = sourceText.fontSharedMaterial;
    timerLabel.fontSize = sourceText.fontSize;
    normalTimerColor = sourceText.color;
    timerLabel.color = normalTimerColor;
    timerLabel.alignment = sourceText.alignment;
    timerLabel.textWrappingMode = TextWrappingModes.NoWrap;
    timerLabel.raycastTarget = false;
    timerLabel.text = "Time: 00:00";
    return timerLabel;
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

#if UNITY_EDITOR
  void OnValidate()
  {
    ApplyLevelDefaults();
    levelTimeLimitSeconds = Mathf.Max(1f, levelTimeLimitSeconds);
    AutoAssignReferences();
    remainingTime = Mathf.Max(1f, levelTimeLimitSeconds);
    UpdateUI();
  }
#endif
}

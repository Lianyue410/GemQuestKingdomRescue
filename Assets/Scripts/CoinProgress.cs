using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CoinProgress : MonoBehaviour
{
  public static CoinProgress Instance;
  private const string SavedCoinsKey = "Coursework_TotalCoins";
  private static int savedTotalCoins = -1;
  private int levelStartCoins;

  [Header("Progress")]
  public int totalCoins;
  public int maxDisplayedCoins = 100;
  public int attack2UnlockCoins = 20;
  public int attack3UnlockCoins = 40;

  [Header("Refs")]
  public PlayerController playerController;
  public RectTransform fillRect;
  public TMP_Text progressText;
  public TMP_Text attack2Text;
  public TMP_Text attack3Text;
  public Image attack2Marker;
  public Image attack3Marker;

  [Header("Colors")]
  public Color lockedMarkerColor = new Color(1f, 1f, 1f, 0.45f);
  public Color unlockedMarkerColor = new Color(1f, 0.86f, 0.12f, 1f);

  private float lastParentWidth;

  void Awake()
  {
    if (Instance != null && Instance != this)
    {
      Destroy(gameObject);
      return;
    }

    Instance = this;
    LoadSavedProgress();
    AutoAssignReferences();
    ApplyUnlocks();
    UpdateUI();
  }

  void Start()
  {
    AutoAssignReferences();

    if (playerController == null)
    {
      playerController = FindFirstObjectByType<PlayerController>();
    }

    ApplyUnlocks();
    UpdateUI();
  }

  void Update()
  {
    AutoAssignReferences();

    if (playerController == null)
    {
      playerController = FindFirstObjectByType<PlayerController>();
      ApplyUnlocks();
    }

    RefreshIfBarWasResized();
  }

  public void AddCoin(int amount)
  {
    if (amount <= 0) return;

    totalCoins += amount;
    ApplyUnlocks();
    UpdateUI();
  }

  public void ResetProgress()
  {
    ResetAllSavedProgress();
    RefreshFromSavedProgress();
  }

  public void CommitLevelProgress()
  {
    savedTotalCoins = totalCoins;
    levelStartCoins = totalCoins;
    SaveProgress();
  }

  public void RestoreLevelStartProgress()
  {
    totalCoins = levelStartCoins;
    ApplyUnlocks();
    UpdateUI();
  }

  public void RefreshFromSavedProgress()
  {
    LoadSavedProgress();
    ApplyUnlocks();
    UpdateUI();
  }

  public static void ResetAllSavedProgress()
  {
    savedTotalCoins = 0;
    PlayerPrefs.SetInt(SavedCoinsKey, 0);
    PlayerPrefs.Save();
  }

  void LoadSavedProgress()
  {
    if (savedTotalCoins < 0)
    {
      savedTotalCoins = PlayerPrefs.GetInt(SavedCoinsKey, totalCoins);
    }

    totalCoins = savedTotalCoins;
    levelStartCoins = savedTotalCoins;
  }

  void SaveProgress()
  {
    savedTotalCoins = totalCoins;
    PlayerPrefs.SetInt(SavedCoinsKey, totalCoins);
    PlayerPrefs.Save();
  }

  void OnDestroy()
  {
    if (Instance == this)
    {
      Instance = null;
    }
  }

  void AutoAssignReferences()
  {
    if (playerController == null)
    {
      playerController = FindFirstObjectByType<PlayerController>();
    }

    if (fillRect == null)
    {
      RectTransform fill = FindRectByName("Fill_Yellow_CollectedCoins");
      if (fill == null) fill = FindRectByName("CoinProgressFill");
      if (fill == null) fill = FindRectByName("Fill");
      fillRect = fill;
    }

    if (progressText == null)
    {
      progressText = FindTextByName("CoinsText_Left");
    }

    if (attack2Text == null)
    {
      attack2Text = FindTextByName("Attack2Text_Center");
    }

    if (attack3Text == null)
    {
      attack3Text = FindTextByName("Attack3Text_Right");
    }

    if (attack2Marker == null)
    {
      attack2Marker = FindImageByName("Attack2_Marker_50Coins");
    }

    if (attack3Marker == null)
    {
      attack3Marker = FindImageByName("Attack3_Marker_100Coins");
    }
  }

  RectTransform FindRectByName(string objectName)
  {
    RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    foreach (RectTransform rect in rects)
    {
      if (rect.name == objectName)
      {
        return rect;
      }
    }

    return null;
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

  Image FindImageByName(string objectName)
  {
    Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    foreach (Image image in images)
    {
      if (image.name == objectName)
      {
        return image;
      }
    }

    return null;
  }

  void ApplyUnlocks()
  {
    if (playerController == null) return;

    playerController.attack2Unlocked = totalCoins >= attack2UnlockCoins;
    playerController.attack3Unlocked = totalCoins >= attack3UnlockCoins;

    if (!playerController.attack2Unlocked && playerController.currentAttackType == 2)
    {
      playerController.currentAttackType = 1;
    }

    if (!playerController.attack3Unlocked && playerController.currentAttackType == 3)
    {
      playerController.currentAttackType = 1;
    }
  }

  void UpdateUI()
  {
    UpdateFill();
    UpdateText();
    UpdateMarkers();
  }

  void UpdateFill()
  {
    if (fillRect == null) return;

    RectTransform parentRect = fillRect.parent as RectTransform;
    float fullWidth = parentRect != null ? parentRect.rect.width : fillRect.sizeDelta.x;
    fullWidth = Mathf.Max(0f, fullWidth);

    float progress = maxDisplayedCoins <= 0 ? 0f : Mathf.Clamp01((float)totalCoins / maxDisplayedCoins);
    fillRect.sizeDelta = new Vector2(fullWidth * progress, fillRect.sizeDelta.y);
    lastParentWidth = fullWidth;
  }

  void UpdateText()
  {
    if (progressText != null)
    {
      progressText.text = $"Coins {totalCoins}/{maxDisplayedCoins}";
    }

    string attack2 = totalCoins >= attack2UnlockCoins ? "Attack 2 Unlocked" : $"Attack 2 {totalCoins}/{attack2UnlockCoins}";
    string attack3 = totalCoins >= attack3UnlockCoins ? "Attack 3 Unlocked" : $"Attack 3 {totalCoins}/{attack3UnlockCoins}";

    if (attack2Text != null)
    {
      attack2Text.text = attack2;
    }

    if (attack3Text != null)
    {
      attack3Text.text = attack3;
    }
  }

  void UpdateMarkers()
  {
    if (attack2Marker != null)
    {
      attack2Marker.color = totalCoins >= attack2UnlockCoins ? unlockedMarkerColor : lockedMarkerColor;
    }

    if (attack3Marker != null)
    {
      attack3Marker.color = totalCoins >= attack3UnlockCoins ? unlockedMarkerColor : lockedMarkerColor;
    }
  }

  void RefreshIfBarWasResized()
  {
    if (fillRect == null) return;

    RectTransform parentRect = fillRect.parent as RectTransform;
    if (parentRect == null) return;

    float currentWidth = parentRect.rect.width;
    if (Mathf.Abs(currentWidth - lastParentWidth) > 0.1f)
    {
      UpdateFill();
    }
  }
}

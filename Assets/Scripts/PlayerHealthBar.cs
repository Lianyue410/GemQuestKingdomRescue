using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PlayerHealthBar : MonoBehaviour
{
  [Header("Preview")]
  public int previewCurrentHealth = 100;
  public int previewMaxHealth = 100;

  [Header("Style")]
  public Color borderColor = new Color(0.45f, 0.45f, 0.45f, 1f);
  public Color fillColor = new Color(0.05f, 0.8f, 0.2f, 1f);
  public Color textColor = Color.white;

  [Header("Size")]
  public float barWidth = 260f;
  public float barHeight = 28f;
  public float borderThickness = 1.5f;
  public float fillPadding = 5f;

  private RectTransform fillRect;
  private TMP_Text healthText;

  void OnEnable()
  {
    BuildBar();
    SetHealth(previewCurrentHealth, previewMaxHealth);
  }

  void OnValidate()
  {
    BuildBar();
    SetHealth(previewCurrentHealth, previewMaxHealth);
  }

  public void SetHealth(int currentHealth, int maxHealth)
  {
    BuildBar();

    currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    previewCurrentHealth = currentHealth;
    previewMaxHealth = maxHealth;

    float ratio = maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;

    if (fillRect != null)
    {
      float fullFillWidth = barWidth - fillPadding * 2f;
      fillRect.sizeDelta = new Vector2(fullFillWidth * ratio, barHeight - fillPadding * 2f);
    }

    if (healthText != null)
    {
      healthText.text = currentHealth + "/" + maxHealth;
    }
  }

  void BuildBar()
  {
    Canvas canvas = GetComponent<Canvas>();
    if (canvas == null)
    {
      canvas = gameObject.AddComponent<Canvas>();
    }

    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 50;

    if (GetComponent<CanvasScaler>() == null)
    {
      CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920f, 1080f);
      scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
      scaler.matchWidthOrHeight = 0.5f;
    }

    if (GetComponent<GraphicRaycaster>() == null)
    {
      gameObject.AddComponent<GraphicRaycaster>();
    }

    RectTransform root = GetComponent<RectTransform>();
    root.anchorMin = new Vector2(0f, 1f);
    root.anchorMax = new Vector2(0f, 1f);
    root.pivot = new Vector2(0f, 1f);
    root.anchoredPosition = new Vector2(30f, -30f);
    root.sizeDelta = new Vector2(barWidth + 20f, barHeight + 20f);

    Image top = GetOrCreateImage("Border_Top");
    SetCenteredRect(top.rectTransform, new Vector2(barWidth * 0.5f, -borderThickness * 0.5f), new Vector2(barWidth, borderThickness));
    top.color = borderColor;

    Image bottom = GetOrCreateImage("Border_Bottom");
    SetCenteredRect(bottom.rectTransform, new Vector2(barWidth * 0.5f, -barHeight + borderThickness * 0.5f), new Vector2(barWidth, borderThickness));
    bottom.color = borderColor;

    Image left = GetOrCreateImage("Border_Left");
    SetCenteredRect(left.rectTransform, new Vector2(borderThickness * 0.5f, -barHeight * 0.5f), new Vector2(borderThickness, barHeight));
    left.color = borderColor;

    Image right = GetOrCreateImage("Border_Right");
    SetCenteredRect(right.rectTransform, new Vector2(barWidth - borderThickness * 0.5f, -barHeight * 0.5f), new Vector2(borderThickness, barHeight));
    right.color = borderColor;

    Image fill = GetOrCreateImage("Fill");
    fillRect = fill.rectTransform;
    SetLeftRect(fillRect, new Vector2(fillPadding, -barHeight * 0.5f), new Vector2(barWidth - fillPadding * 2f, barHeight - fillPadding * 2f));
    fill.color = fillColor;
    fill.raycastTarget = false;

    healthText = GetOrCreateText("HealthText");
    SetCenteredRect(healthText.rectTransform, new Vector2(barWidth * 0.5f, -barHeight * 0.5f), new Vector2(barWidth, barHeight));
    healthText.text = "100/100";
    healthText.fontSize = 18f;
    healthText.alignment = TextAlignmentOptions.Center;
    healthText.color = textColor;
    healthText.raycastTarget = false;

    top.transform.SetSiblingIndex(0);
    bottom.transform.SetSiblingIndex(1);
    left.transform.SetSiblingIndex(2);
    right.transform.SetSiblingIndex(3);
    fill.transform.SetSiblingIndex(4);
    healthText.transform.SetSiblingIndex(5);
  }

  Image GetOrCreateImage(string objectName)
  {
    Transform child = transform.Find(objectName);

    if (child == null)
    {
      GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
      obj.transform.SetParent(transform, false);
      child = obj.transform;
    }

    return child.GetComponent<Image>();
  }

  TMP_Text GetOrCreateText(string objectName)
  {
    Transform child = transform.Find(objectName);

    if (child == null)
    {
      GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
      obj.transform.SetParent(transform, false);
      child = obj.transform;
    }

    return child.GetComponent<TMP_Text>();
  }

  void SetCenteredRect(RectTransform rect, Vector2 position, Vector2 size)
  {
    rect.anchorMin = new Vector2(0f, 1f);
    rect.anchorMax = new Vector2(0f, 1f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = position;
    rect.sizeDelta = size;
    rect.localRotation = Quaternion.identity;
    rect.localScale = Vector3.one;
  }

  void SetLeftRect(RectTransform rect, Vector2 position, Vector2 size)
  {
    rect.anchorMin = new Vector2(0f, 1f);
    rect.anchorMax = new Vector2(0f, 1f);
    rect.pivot = new Vector2(0f, 0.5f);
    rect.anchoredPosition = position;
    rect.sizeDelta = size;
    rect.localRotation = Quaternion.identity;
    rect.localScale = Vector3.one;
  }
}

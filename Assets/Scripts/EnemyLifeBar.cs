using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyLifeBar : MonoBehaviour
{
  [Header("Life")]
  public int previewCurrentLife = 3;
  public int previewMaxLife = 3;

  [Header("Style")]
  public Color borderColor = new Color(0.45f, 0.45f, 0.45f, 1f);
  public Color fillColor = new Color(0.85f, 0.05f, 0.05f, 1f);
  public Color textColor = Color.white;

  [Header("Size")]
  public float barWidth = 145f;
  public float barHeight = 18f;
  public float borderThickness = 1.5f;
  public float fillPadding = 4f;

  private RectTransform fillRect;
  private Image fillImage;
  private TMP_Text lifeText;
  private bool isBuilt;

  void Start()
  {
    BuildBar();
    SetLife(previewCurrentLife, previewMaxLife);
  }

  public void SetLife(int currentLife, int maxLife)
  {
    if (!isBuilt)
    {
      BuildBar();
    }

    currentLife = Mathf.Clamp(currentLife, 0, maxLife);
    previewCurrentLife = currentLife;
    previewMaxLife = maxLife;

    float ratio = maxLife <= 0 ? 0f : (float)currentLife / maxLife;

    if (fillRect != null)
    {
      float fullFillWidth = barWidth - fillPadding * 2f;
      fillRect.sizeDelta = new Vector2(fullFillWidth * ratio, barHeight - fillPadding * 2f);
    }

    if (lifeText != null)
    {
      lifeText.text = currentLife + "/" + maxLife;
    }
  }

  void BuildBar()
  {
    RemoveOldObject("BG");

    Canvas canvas = GetComponent<Canvas>();
    if (canvas == null)
    {
      canvas = gameObject.AddComponent<Canvas>();
    }

    canvas.renderMode = RenderMode.WorldSpace;
    canvas.sortingOrder = 20;

    if (GetComponent<CanvasScaler>() == null)
    {
      gameObject.AddComponent<CanvasScaler>();
    }

    if (GetComponent<GraphicRaycaster>() == null)
    {
      gameObject.AddComponent<GraphicRaycaster>();
    }

    RectTransform rootRect = GetComponent<RectTransform>();
    if (rootRect != null)
    {
      rootRect.sizeDelta = new Vector2(barWidth + 10f, barHeight + 10f);
    }

    Image top = GetOrCreateImage("Border_Top");
    SetCenteredRect(top.rectTransform, new Vector2(0f, barHeight * 0.5f), new Vector2(barWidth, borderThickness));
    top.color = borderColor;

    Image bottom = GetOrCreateImage("Border_Bottom");
    SetCenteredRect(bottom.rectTransform, new Vector2(0f, -barHeight * 0.5f), new Vector2(barWidth, borderThickness));
    bottom.color = borderColor;

    Image left = GetOrCreateImage("Border_Left");
    SetCenteredRect(left.rectTransform, new Vector2(-barWidth * 0.5f, 0f), new Vector2(borderThickness, barHeight));
    left.color = borderColor;

    Image right = GetOrCreateImage("Border_Right");
    SetCenteredRect(right.rectTransform, new Vector2(barWidth * 0.5f, 0f), new Vector2(borderThickness, barHeight));
    right.color = borderColor;

    fillImage = GetOrCreateImage("Fill");
    fillRect = fillImage.rectTransform;
    SetLeftRect(fillRect, new Vector2(-barWidth * 0.5f + fillPadding, 0f), new Vector2(barWidth - fillPadding * 2f, barHeight - fillPadding * 2f));
    fillImage.color = fillColor;
    fillImage.type = Image.Type.Simple;
    fillImage.raycastTarget = false;

    lifeText = GetOrCreateText("LifeText");
    SetCenteredRect(lifeText.rectTransform, Vector2.zero, new Vector2(barWidth, barHeight));
    lifeText.alignment = TextAlignmentOptions.Center;
    lifeText.fontSize = 13f;
    lifeText.color = textColor;
    lifeText.raycastTarget = false;

    top.transform.SetSiblingIndex(0);
    bottom.transform.SetSiblingIndex(1);
    left.transform.SetSiblingIndex(2);
    right.transform.SetSiblingIndex(3);
    fillImage.transform.SetSiblingIndex(4);
    lifeText.transform.SetSiblingIndex(5);

    isBuilt = true;
  }

  void RemoveOldObject(string objectName)
  {
    Transform child = transform.Find(objectName);
    if (child != null)
    {
      if (Application.isPlaying)
      {
        Destroy(child.gameObject);
      }
      else
      {
        DestroyImmediate(child.gameObject);
      }
    }
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
    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.anchoredPosition = position;
    rect.sizeDelta = size;
    rect.localRotation = Quaternion.identity;
    rect.localScale = Vector3.one;
  }

  void SetLeftRect(RectTransform rect, Vector2 position, Vector2 size)
  {
    rect.anchorMin = new Vector2(0.5f, 0.5f);
    rect.anchorMax = new Vector2(0.5f, 0.5f);
    rect.pivot = new Vector2(0f, 0.5f);
    rect.anchoredPosition = position;
    rect.sizeDelta = size;
    rect.localRotation = Quaternion.identity;
    rect.localScale = Vector3.one;
  }
}

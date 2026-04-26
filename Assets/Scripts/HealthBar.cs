using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
  public Image fillImage;

  public void SetHealth(float currentHealth, float maxHealth)
  {
    if (fillImage == null) return;

    float value = 0f;

    if (maxHealth > 0f)
    {
      value = currentHealth / maxHealth;
    }

    fillImage.fillAmount = Mathf.Clamp01(value);
  }
}

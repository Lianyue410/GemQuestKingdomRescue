using UnityEngine;

public class WorldHealthBar3D : MonoBehaviour
{
  public Transform fill;
  public float fullWidth = 1.1f;

  public void SetHealth(float currentHealth, float maxHealth)
  {
    if (fill == null) return;

    float ratio = 0f;

    if (maxHealth > 0f)
    {
      ratio = Mathf.Clamp01(currentHealth / maxHealth);
    }

    Vector3 scale = fill.localScale;
    scale.x = fullWidth * ratio;
    fill.localScale = scale;

    Vector3 position = fill.localPosition;
    position.x = -(fullWidth * (1f - ratio)) * 0.5f;
    fill.localPosition = position;
  }
}

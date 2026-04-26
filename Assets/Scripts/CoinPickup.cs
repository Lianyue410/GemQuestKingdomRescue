using UnityEngine;

public class CoinPickup : MonoBehaviour
{
  public int coinValue = 1;
  public float rotateSpeed = 120f;

  private bool collected;

  void Awake()
  {
    Collider col = GetComponent<Collider>();

    if (col == null)
    {
      SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
      sphere.radius = 0.5f;
      col = sphere;
    }

    col.isTrigger = true;
  }

  void Update()
  {
    transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
  }

  void OnTriggerEnter(Collider other)
  {
    if (collected) return;

    PlayerController player = other.GetComponentInParent<PlayerController>();
    if (player == null) return;

    collected = true;

    AudioManager.Instance?.PlayPickupSfx();

    if (CoinProgress.Instance != null)
    {
      CoinProgress.Instance.AddCoin(coinValue);
    }

    Destroy(gameObject);
  }
}

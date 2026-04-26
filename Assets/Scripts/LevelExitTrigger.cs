using UnityEngine;

public class LevelExitTrigger : MonoBehaviour
{
  public float rotateSpeed = 45f;

  void Awake()
  {
    Collider col = GetComponent<Collider>();
    if (col == null)
    {
      SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
      sphere.radius = 0.8f;
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
    if (other.GetComponentInParent<PlayerController>() == null) return;
    if (LevelObjectiveManager.Instance == null) return;

    LevelObjectiveManager.Instance.TryFinishLevel();
  }
}

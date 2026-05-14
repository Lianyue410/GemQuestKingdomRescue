using UnityEngine;

public class HealthPickup : MonoBehaviour
{
  [Header("Heal")]
  public int healAmount = 2;

  [Header("Optional Feedback")]
  public AudioClip pickupSound;
  public ParticleSystem pickupEffect;
  public float effectLifetime = 2f;

  private bool collected;

  void Reset()
  {
    ConfigureTriggerCollider();
  }

  void Awake()
  {
    ConfigureTriggerCollider();
  }

  void OnTriggerEnter(Collider other)
  {
    TryCollect(other);
  }

  void OnTriggerStay(Collider other)
  {
    TryCollect(other);
  }

  void TryCollect(Collider other)
  {
    if (collected) return;

    PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
    if (playerHealth == null || playerHealth.IsDead) return;

    collected = true;

    playerHealth.Heal(healAmount);

    if (pickupSound != null)
    {
      AudioSource.PlayClipAtPoint(pickupSound, transform.position);
    }
    else
    {
      AudioManager.Instance?.PlayPickupSfx();
    }

    SpawnPickupEffect(playerHealth.transform);

    Destroy(gameObject);
  }

  void SpawnPickupEffect(Transform playerTransform)
  {
    if (pickupEffect == null) return;

    GameObject effectObject = Instantiate(pickupEffect.gameObject, playerTransform);

    effectObject.transform.localPosition = Vector3.up * 1.0f;
    effectObject.transform.localRotation = Quaternion.identity;

    ConfigureOneShotEffect(effectObject, true);
    PlayAllParticleSystems(effectObject);

    Destroy(effectObject, effectLifetime);
  }

  void ConfigureOneShotEffect(GameObject rootEffectObject, bool useLocalSpace)
  {
    ParticleSystem[] particleSystems =
        rootEffectObject.GetComponentsInChildren<ParticleSystem>(true);

    foreach (ParticleSystem particleSystem in particleSystems)
    {
      // Stop first because some particle prefabs may have Play On Awake enabled.
      // Do not change duration while the particle system is already playing.
      particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

      var main = particleSystem.main;
      main.loop = false;
      main.simulationSpace = useLocalSpace
          ? ParticleSystemSimulationSpace.Local
          : ParticleSystemSimulationSpace.World;

      particleSystem.Clear(true);
    }
  }

  void PlayAllParticleSystems(GameObject rootEffectObject)
  {
    ParticleSystem[] particleSystems =
        rootEffectObject.GetComponentsInChildren<ParticleSystem>(true);

    foreach (ParticleSystem particleSystem in particleSystems)
    {
      particleSystem.Clear(true);
      particleSystem.Play(true);
    }
  }

  void ConfigureTriggerCollider()
  {
    Collider pickupCollider = GetComponent<Collider>();

    if (pickupCollider == null)
    {
      SphereCollider sphereCollider = gameObject.AddComponent<SphereCollider>();
      sphereCollider.radius = 0.6f;
      pickupCollider = sphereCollider;
    }

    pickupCollider.isTrigger = true;
  }
}
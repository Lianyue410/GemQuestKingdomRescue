using UnityEngine;

public class GemObjectivePickup : MonoBehaviour
{
  public float rotateSpeed = 90f;

  [Header("Feedback")]
  public ParticleSystem pickupEffect;
  public float effectLifetime = 1f;

  private bool collected;

  void Awake()
  {
    Collider col = GetComponent<Collider>();
    if (col == null)
    {
      SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
      sphere.radius = 0.6f;
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
    if (other.GetComponentInParent<PlayerController>() == null) return;

    LevelObjectiveManager manager = LevelObjectiveManager.Instance;
    if (manager == null)
    {
      manager = FindFirstObjectByType<LevelObjectiveManager>();
    }

    if (manager == null)
    {
      return;
    }

    collected = true;

    AudioManager.Instance?.PlayPickupSfx();

    SpawnPickupEffect();

    manager.CollectGem();

    Destroy(gameObject);
  }

  void SpawnPickupEffect()
  {
    if (pickupEffect == null) return;

    GameObject effectObject = Instantiate(
        pickupEffect.gameObject,
        transform.position,
        Quaternion.identity
    );

    ConfigureOneShotEffect(effectObject);
    PlayAllParticleSystems(effectObject);

    Destroy(effectObject, effectLifetime);
  }

  void ConfigureOneShotEffect(GameObject rootEffectObject)
  {
    ParticleSystem[] particleSystems =
        rootEffectObject.GetComponentsInChildren<ParticleSystem>(true);

    foreach (ParticleSystem particleSystem in particleSystems)
    {
      // Stop first, because some particle prefabs may have Play On Awake enabled.
      // This prevents Unity's warning about changing particle settings while playing.
      particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

      var main = particleSystem.main;
      main.loop = false;
      main.simulationSpace = ParticleSystemSimulationSpace.World;

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
}
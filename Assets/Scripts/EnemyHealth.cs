using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.AI;
using UnityEngine.Playables;

public class EnemyHealth : MonoBehaviour
{
  [Header("Life")]
  public int maxHealth = 3;
  public int currentHealth = 3;

  [Header("Death")]
  public string dieState = "Die";
  public AnimationClip deathClip;
  public float destroyDelay = 3f;

  [Header("UI")]
  public EnemyLifeBar lifeBar;

  private Animator animator;
  private NavMeshAgent agent;
  private EnemyAI enemyAI;
  private Collider[] colliders;
  private PlayableGraph deathGraph;
  private bool isDead;

  void Awake()
  {
    currentHealth = maxHealth;

    animator = GetComponentInChildren<Animator>();
    agent = GetComponent<NavMeshAgent>();
    enemyAI = GetComponent<EnemyAI>();
    colliders = GetComponentsInChildren<Collider>();

    EnsureLifeBar();

    AutoAssignDeathClipInEditor();
  }

  void Start()
  {
    UpdateLifeBar();
  }

  public void TakeDamage(float damage)
  {
    TakeHit(damage);
  }

  public void TakeHit(float damage = 1f)
  {
    if (isDead) return;

    int damageAmount = Mathf.Max(1, Mathf.RoundToInt(damage));
    currentHealth -= damageAmount;
    currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

    UpdateLifeBar();

    if (currentHealth <= 0)
    {
      Die();
    }
  }

  void UpdateLifeBar()
  {
    EnsureLifeBar();

    if (lifeBar != null)
    {
      lifeBar.SetLife(currentHealth, maxHealth);
    }
    else
    {
    }
  }

  void EnsureLifeBar()
  {
    if (lifeBar != null) return;

    EnemyLifeBar[] bars = GetComponentsInChildren<EnemyLifeBar>(true);
    if (bars.Length > 0)
    {
      lifeBar = bars[0];
    }
  }

  void Die()
  {
    if (isDead) return;

    isDead = true;
    currentHealth = 0;
    UpdateLifeBar();

    if (enemyAI != null)
    {
      enemyAI.enabled = false;
    }

    if (agent != null)
    {
      agent.isStopped = true;
      agent.enabled = false;
    }

    foreach (Collider col in colliders)
    {
      col.enabled = false;
    }

    PlayDeathAnimation();

    Destroy(gameObject, destroyDelay);
  }

  void PlayDeathAnimation()
  {
    if (animator == null)
    {
      return;
    }

    if (animator.HasState(0, Animator.StringToHash(dieState)))
    {
      animator.Play(dieState, 0, 0f);
      return;
    }

    if (deathClip != null)
    {
      if (deathGraph.IsValid())
      {
        deathGraph.Destroy();
      }

      deathGraph = PlayableGraph.Create(name + "_DeathGraph");
      deathGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

      AnimationPlayableOutput output = AnimationPlayableOutput.Create(deathGraph, "DeathAnimation", animator);
      AnimationClipPlayable playable = AnimationClipPlayable.Create(deathGraph, deathClip);
      playable.SetApplyFootIK(false);
      playable.SetSpeed(1f);

      output.SetSourcePlayable(playable);
    deathGraph.Play();
    return;
    }
  }

  void OnDestroy()
  {
    if (deathGraph.IsValid())
    {
      deathGraph.Destroy();
    }
  }

  void AutoAssignDeathClipInEditor()
  {
#if UNITY_EDITOR
    if (deathClip != null) return;

    deathClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(
      "Assets/DogKnight/Animations/Die.anim"
    );
#endif
  }
}



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

  [Header("Hit Reaction")]
  public string hitState = "GetHit";
  public float hitReactionDuration = 0.2f;

  [Header("UI")]
  public EnemyLifeBar lifeBar;

  private Animator animator;
  private NavMeshAgent agent;
  private EnemyAI enemyAI;
  private Collider[] colliders;
  private PlayableGraph deathGraph;
  private bool isDead;
  private float lastHitReactionTime;

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
      return;
    }

    PlayHitReaction();
    enemyAI?.NotifyDamaged(FindFirstObjectByType<PlayerHealth>()?.transform);
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

  void PlayHitReaction()
  {
    if (animator == null) return;
    if (Time.time - lastHitReactionTime < hitReactionDuration) return;

    lastHitReactionTime = Time.time;

    if (!string.IsNullOrEmpty(hitState) && animator.HasState(0, Animator.StringToHash(hitState)))
    {
      animator.CrossFadeInFixedTime(hitState, 0.04f, 0);
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
    if (animator != null && animator.runtimeAnimatorController != null)
    {
      foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
      {
        if (clip == null) continue;
        if (string.Equals(clip.name, dieState, System.StringComparison.OrdinalIgnoreCase))
        {
          deathClip = clip;
          return;
        }
      }
    }

    string[] candidatePaths =
    {
      "Assets/DogKnight/Animations/Die.anim",
      "Assets/Mini Legion Lich PBR HP Polyart/Animations/Lich/Die/die.anim",
      "Assets/WizardPolyArt/Animations/Die.fbx"
    };

    foreach (string candidatePath in candidatePaths)
    {
      AnimationClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>(candidatePath);
      if (clip != null)
      {
        deathClip = clip;
        return;
      }
    }
#endif
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
}

using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerHealth : MonoBehaviour
{
  public int maxHealth = 10;
  public int currentHealth = 10;

  [Header("Death")]
  public string deathState = "ko_big";
  public AnimationClip deathClip;
  public float losePanelDelay = 2f;

  public EnemyLifeBar healthBar;
  public Animator animator;
  public PlayerController playerController;
  public PlayerAttackDamage playerAttackDamage;

  private CharacterController characterController;
  private PlayableGraph deathGraph;
  private bool isDead;

  public bool IsDead => isDead;

  void Awake()
  {
    currentHealth = maxHealth;

    if (animator == null) animator = GetComponentInChildren<Animator>();
    if (playerController == null) playerController = GetComponent<PlayerController>();
    if (playerAttackDamage == null) playerAttackDamage = GetComponent<PlayerAttackDamage>();

    characterController = GetComponent<CharacterController>();

    AutoAssignDeathClipInEditor();
  }

  void Start()
  {
    UpdateHealthBar();
  }

  public void TakeDamage(int damage)
  {
    if (isDead) return;

    currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
    UpdateHealthBar();

    if (currentHealth <= 0)
    {
      Die();
    }
  }

  public void Heal(int amount)
  {
    if (isDead) return;

    currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    UpdateHealthBar();
  }

  void UpdateHealthBar()
  {
    if (healthBar != null)
    {
      healthBar.SetLife(currentHealth, maxHealth);
    }
  }

  void Die()
  {
    if (isDead) return;

    isDead = true;

    if (playerController != null) playerController.enabled = false;
    if (playerAttackDamage != null) playerAttackDamage.enabled = false;
    if (characterController != null) characterController.enabled = false;

    PlayDeathAnimation();

    StartCoroutine(ShowLosePanelAfterDelay());
  }

  IEnumerator ShowLosePanelAfterDelay()
  {
    yield return new WaitForSecondsRealtime(losePanelDelay);

    GameFlowManager.Instance?.ShowLosePanel();
  }

  void PlayDeathAnimation()
  {
    if (animator == null)
    {
      return;
    }

    animator.SetFloat("Speed", 0f);

    if (deathClip != null)
    {
      if (deathGraph.IsValid())
      {
        deathGraph.Destroy();
      }

      deathGraph = PlayableGraph.Create("PlayerDeathGraph");
      deathGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

      AnimationPlayableOutput output =
        AnimationPlayableOutput.Create(deathGraph, "DeathAnimation", animator);

      AnimationClipPlayable playable =
        AnimationClipPlayable.Create(deathGraph, deathClip);

      playable.SetApplyFootIK(false);
      playable.SetSpeed(1f);

      output.SetSourcePlayable(playable);
      deathGraph.Play();
      return;
    }

    if (TryPlayAnimatorState(deathState)) return;
    if (TryPlayAnimatorState("ko_big")) return;
    if (TryPlayAnimatorState("KO_big")) return;
    if (TryPlayAnimatorState("ko")) return;
    if (TryPlayAnimatorState("KO")) return;
  }

  bool TryPlayAnimatorState(string stateName)
  {
    if (animator == null || string.IsNullOrEmpty(stateName)) return false;

    int shortHash = Animator.StringToHash(stateName);
    int fullHash = Animator.StringToHash("Base Layer." + stateName);

    if (animator.HasState(0, shortHash))
    {
      animator.Play(shortHash, 0, 0f);
      return true;
    }

    if (animator.HasState(0, fullHash))
    {
      animator.Play(fullHash, 0, 0f);
      return true;
    }

    return false;
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
      "Assets/SapphiArt/SapphiArtchan/Animation/KO_big.anim"
    );
#endif
  }
}
using System.Collections;
using UnityEngine;

public class PlayerAttackDamage : MonoBehaviour
{
  [Header("Attack")]
  public float attackRange = 3.0f;
  public float hitDelay = 0.18f;
  public float attackCooldown = 0.25f;
  public float clickAssistPixels = 80f;

  [Header("Attack Damage")]
  public int attack1Damage = 1;
  public int attack2Damage = 2;
  public int attack3Damage = 3;

  [Header("Attack VFX")]
  public ParticleSystem attack1EffectPrefab;
  public ParticleSystem attack2EffectPrefab;
  public ParticleSystem attack3EffectPrefab;
  public Vector3 groundEffectOffset = new Vector3(0f, 0.05f, 0f);
  public float attack1EffectLifetime = 0.9f;
  public float attack2EffectLifetime = 1.1f;
  public float attack3EffectLifetime = 1.1f;

  [Header("Refs")]
  public Camera playerCamera;
  public Animator animator;

  private EnemyHealth pendingEnemy;
  private EnemyHealth queuedEnemy;
  private bool isAttacking;
  private bool hasQueuedAttack;
  private PlayerController playerController;
  private Coroutine attackCoroutine;

  void Awake()
  {
    playerController = GetComponent<PlayerController>();

    if (playerCamera == null)
    {
      playerCamera = Camera.main;
    }

    if (animator == null)
    {
      animator = GetComponentInChildren<Animator>();
    }
  }

  void Update()
  {
    if (GameFlowManager.BlocksGameplayInput) return;

    if (Input.GetMouseButtonDown(0))
    {
      EnemyHealth clickedEnemy = GetClickedEnemy();

      if (clickedEnemy == null) return;

      if (isAttacking)
      {
        queuedEnemy = clickedEnemy;
        hasQueuedAttack = true;
        return;
      }

      StartAttack(clickedEnemy);
    }
  }

  void StartAttack(EnemyHealth enemy)
  {
    if (enemy == null) return;

    float distance = Vector3.Distance(transform.position, enemy.transform.position);

    if (distance > attackRange)
    {
      return;
    }

    if (attackCoroutine != null)
    {
      StopCoroutine(attackCoroutine);
    }

    attackCoroutine = StartCoroutine(AttackRoutine(enemy));
  }

  IEnumerator AttackRoutine(EnemyHealth enemy)
  {
    isAttacking = true;
    pendingEnemy = enemy;

    FaceEnemy(enemy.transform);

    int attackType = GetResolvedAttackType();

    if (animator != null)
    {
      string attackState = "hit01";

      if (playerController != null)
      {
        attackState = playerController.GetCurrentAttackStateName();
      }

      animator.speed = 1f;
      animator.Play(attackState, 0, 0f);
    }

    AudioManager.Instance?.PlayAttackSfx(attackType);

    yield return new WaitForSecondsRealtime(hitDelay);

    SpawnAttackEffect(enemy.transform, attackType);
    ApplyPendingHit();

    float cooldownLeft = Mathf.Max(0f, attackCooldown - hitDelay);

    if (cooldownLeft > 0f)
    {
      yield return new WaitForSecondsRealtime(cooldownLeft);
    }

    isAttacking = false;
    attackCoroutine = null;

    if (hasQueuedAttack)
    {
      EnemyHealth nextEnemy = queuedEnemy;
      queuedEnemy = null;
      hasQueuedAttack = false;

      StartAttack(nextEnemy);
    }
  }

  EnemyHealth GetClickedEnemy()
  {
    if (playerCamera == null) return null;

    Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

    if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
    {
      EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();

      if (enemy != null)
      {
        return enemy;
      }
    }

    return FindEnemyNearMouse();
  }

  EnemyHealth FindEnemyNearMouse()
  {
    if (playerCamera == null) return null;

    EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

    EnemyHealth bestEnemy = null;
    float bestScreenDistance = clickAssistPixels;

    foreach (EnemyHealth enemy in enemies)
    {
      if (enemy == null || enemy.currentHealth <= 0f) continue;

      Vector3 screenPosition =
        playerCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 0.8f);

      if (screenPosition.z < 0f) continue;

      float screenDistance = Vector2.Distance(
        new Vector2(Input.mousePosition.x, Input.mousePosition.y),
        new Vector2(screenPosition.x, screenPosition.y)
      );

      if (screenDistance < bestScreenDistance)
      {
        bestScreenDistance = screenDistance;
        bestEnemy = enemy;
      }
    }

    return bestEnemy;
  }

  void FaceEnemy(Transform enemy)
  {
    Vector3 direction = enemy.position - transform.position;
    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f) return;

    transform.rotation = Quaternion.LookRotation(direction);
  }

  void ApplyPendingHit()
  {
    if (pendingEnemy == null) return;

    float distance = Vector3.Distance(transform.position, pendingEnemy.transform.position);

    if (distance <= attackRange)
    {
      int damage = GetCurrentAttackDamage();
      pendingEnemy.TakeDamage(damage);
    }

    pendingEnemy = null;
  }

  int GetCurrentAttackDamage()
  {
    if (playerController == null)
    {
      return attack1Damage;
    }

    switch (playerController.CurrentAttackType)
    {
      case 2:
        return playerController.attack2Unlocked ? attack2Damage : attack1Damage;

      case 3:
        return playerController.attack3Unlocked ? attack3Damage : attack1Damage;

      default:
        return attack1Damage;
    }
  }

  int GetResolvedAttackType()
  {
    if (playerController == null)
    {
      return 1;
    }

    switch (playerController.CurrentAttackType)
    {
      case 2:
        return playerController.attack2Unlocked ? 2 : 1;

      case 3:
        return playerController.attack3Unlocked ? 3 : 1;

      default:
        return 1;
    }
  }

  void SpawnAttackEffect(Transform target, int attackType)
  {
    switch (attackType)
    {
      case 2:
        SpawnGroundEffect(attack2EffectPrefab, attack2EffectLifetime);
        break;

      case 3:
        SpawnGroundEffect(attack3EffectPrefab, attack3EffectLifetime);
        break;

      default:
        SpawnSlashEffect(target);
        break;
    }
  }

  void SpawnSlashEffect(Transform target)
  {
    if (attack1EffectPrefab == null || target == null) return;

    Vector3 origin = transform.position + transform.forward * 0.9f + Vector3.up * 1.0f;

    Vector3 direction = target.position - origin;
    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f)
    {
      direction = transform.forward;
    }

    Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

    ParticleSystem effect = Instantiate(attack1EffectPrefab, origin, rotation);
    effect.Play();

    Destroy(effect.gameObject, GetEffectLifetime(effect, attack1EffectLifetime));
  }

  void SpawnGroundEffect(ParticleSystem effectPrefab, float fallbackLifetime)
  {
    if (effectPrefab == null) return;

    Vector3 effectPosition = transform.position + groundEffectOffset;
    Quaternion rotation = Quaternion.Euler(-90f, transform.eulerAngles.y, 0f);

    ParticleSystem effect = Instantiate(effectPrefab, effectPosition, rotation);
    effect.Play();

    Destroy(effect.gameObject, GetEffectLifetime(effect, fallbackLifetime));
  }

  float GetEffectLifetime(ParticleSystem effect, float fallbackLifetime)
  {
    if (effect == null) return fallbackLifetime;

    var main = effect.main;

    float startLifetime =
      main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
        ? main.startLifetime.constantMax
        : main.startLifetime.constant;

    float duration = main.duration + startLifetime;

    return Mathf.Max(duration, fallbackLifetime);
  }
}

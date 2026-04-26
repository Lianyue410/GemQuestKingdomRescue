using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
  public Transform player;

  public float detectionRange = 12f;
  public float attackRange = 3.2f;
  public float minSeparation = 2.5f;
  public float walkSpeed = 1.35f;

  public int damage = 1;
  public float attackCooldown = 1.2f;
  public float hitDelay = 0.35f;
  public float attackAnimationDuration = 0.55f;

  public string idleState = "Idle_Battle";
  public string walkState = "WalkForwardBattle";
  public string attackState = "Attack01";

  private NavMeshAgent agent;
  private Animator animator;
  private PlayerHealth playerHealth;
  private bool isAttacking;

  void Awake()
  {
    agent = GetComponent<NavMeshAgent>();
    animator = GetComponentInChildren<Animator>();
    agent.stoppingDistance = minSeparation;
    agent.speed = walkSpeed;

    if (animator != null)
    {
      animator.applyRootMotion = false;
    }
  }

  void Start()
  {
    if (player == null)
    {
      GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
      if (playerObject != null) player = playerObject.transform;
    }

    if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
  }

  void Update()
  {
    if (GameFlowManager.BlocksGameplayInput)
    {
      StopEnemy();
      PlayLoopState(idleState);
      return;
    }

    if (player == null || playerHealth == null) return;

    if (playerHealth.IsDead)
    {
      StopEnemy();
      PlayLoopState(idleState);
      return;
    }

    float distance = Vector3.Distance(transform.position, player.position);

    if (distance > detectionRange)
    {
      StopEnemy();
      PlayLoopState(idleState);
      return;
    }

    if (distance > attackRange)
    {
      Chase();
      return;
    }

    if (!isAttacking)
    {
      StartCoroutine(AttackRoutine());
    }
  }

  void Chase()
  {
    if (isAttacking) return;

    agent.speed = walkSpeed;
    agent.isStopped = false;
    agent.stoppingDistance = minSeparation;
    agent.SetDestination(player.position);
    PlayLoopState(walkState);
  }

  IEnumerator AttackRoutine()
  {
    isAttacking = true;

    StopEnemy();
    FacePlayer();

    if (animator != null && !string.IsNullOrEmpty(attackState))
    {
      int attackHash = Animator.StringToHash(attackState);
      if (animator.HasState(0, attackHash))
      {
        animator.Play(attackHash, 0, 0f);
      }
    }

    yield return new WaitForSeconds(hitDelay);

    if (playerHealth != null && !playerHealth.IsDead)
    {
      float distance = Vector3.Distance(transform.position, player.position);
      if (distance <= attackRange + 0.4f)
      {
        playerHealth.TakeDamage(damage);
      }
    }

    float settleTime = Mathf.Max(0f, attackAnimationDuration - hitDelay);
    if (settleTime > 0f)
    {
      yield return new WaitForSeconds(settleTime);
    }

    float remainingCooldown = Mathf.Max(0f, attackCooldown - hitDelay - settleTime);
    if (remainingCooldown > 0f)
    {
      yield return new WaitForSeconds(remainingCooldown);
    }

    isAttacking = false;
  }

  void StopEnemy()
  {
    if (agent.enabled)
    {
      agent.isStopped = true;
      agent.ResetPath();
    }
  }

  void FacePlayer()
  {
    Vector3 direction = player.position - transform.position;
    direction.y = 0f;

    if (direction.sqrMagnitude < 0.001f) return;

    transform.rotation = Quaternion.LookRotation(direction);
  }

  void PlayLoopState(string stateName)
  {
    if (animator == null || string.IsNullOrEmpty(stateName)) return;

    AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(0);
    if (currentInfo.IsName(stateName))
    {
      return;
    }

    if (animator.IsInTransition(0))
    {
      AnimatorStateInfo nextInfo = animator.GetNextAnimatorStateInfo(0);
      if (nextInfo.IsName(stateName))
      {
        return;
      }
    }

    if (animator.HasState(0, Animator.StringToHash(stateName)))
    {
      animator.CrossFadeInFixedTime(stateName, 0.08f, 0);
    }
  }
}

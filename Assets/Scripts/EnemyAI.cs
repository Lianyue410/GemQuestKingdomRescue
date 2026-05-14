using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
  public enum EnemyArchetype
  {
    Auto,
    Dog,
    Lich,
    Wizard
  }

  enum EnemyState
  {
    Idle,
    Patrol,
    Alert,
    Chase,
    Search,
    Return,
    Attack
  }

  [Header("Target")]
  public Transform player;
  public EnemyArchetype enemyArchetype = EnemyArchetype.Auto;

  [Header("Detection")]
  public float detectionRange = 12f;
  [Range(10f, 180f)] public float fieldOfView = 110f;
  public float eyeHeight = 1.4f;
  public float loseSightGracePeriod = 1.1f;
  public LayerMask visionBlockers = ~0;

  [Header("Movement")]
  public float attackRange = 3.2f;
  public float minSeparation = 2.5f;
  public float walkSpeed = 1.35f;
  public float runSpeed = 2.7f;
  public float rotationSpeed = 12f;
  public bool patrolEnabled = true;
  public float patrolRadius = 7f;
  public float patrolPointTolerance = 0.9f;
  public float patrolPauseDuration = 1.2f;
  public float alertDuration = 0.45f;
  public float searchDuration = 3.2f;
  public bool returnToSpawnAfterSearch = true;
  public float preferredCombatDistance = 0f;
  public float retreatDistance = 0f;
  public float retreatSampleDistance = 4f;
  public float retreatRepathInterval = 0.35f;

  [Header("Patrol Route")]
  public bool useFixedPatrolRoute = false;
  public float fixedPatrolRouteRadius = 2.2f;
  public int fixedPatrolPointCount = 4;

  [Header("Stuck Recovery")]
  public float stuckVelocityThreshold = 0.08f;
  public float stuckDistanceThreshold = 1.2f;
  public float stuckRepathDelay = 1.1f;

  [Header("Combat")]
  public int damage = 1;
  public float attackCooldown = 1.2f;
  public float hitDelay = 0.35f;
  public float attackAnimationDuration = 0.55f;

  [Header("Wizard Phase 2")]
  public int wizardPhaseTwoTriggerHealth = 3;
  public int wizardPhaseTwoDamage = 5;
  public float wizardPhaseTwoAttackRange = 5.2f;
  public float wizardPhaseTwoRetreatDistance = 3f;
  public float wizardPhaseTwoRetreatDuration = 0.7f;
  public float wizardPhaseTwoAttackCooldown = 2.1f;
  public float wizardPhaseTwoHitDelay = 0.78f;
  public float wizardPhaseTwoAttackAnimationDuration = 1.35f;
  public string wizardPhaseTwoAttackState = "Attack04";

  [Header("Animation States")]
  public string idleState = "Idle_Battle";
  public string patrolState = "";
  public string alertState = "";
  public string walkState = "WalkForwardBattle";
  public string runState = "";
  public string searchState = "";
  public string returnState = "";
  public string attackState = "Attack01";
  public string[] attackStates;

  private NavMeshAgent agent;
  private Animator animator;
  private EnemyHealth enemyHealth;
  private PlayerHealth playerHealth;
  private EnemyState currentState;
  private Vector3 spawnPosition;
  private Vector3 spawnForward;
  private Vector3 patrolDestination;
  private Vector3 lastSeenPosition;
  private float stateTimer;
  private float lastSeenTime;
  private float nextAttackTime;
  private float stuckTimer;
  private bool hasPatrolDestination;
  private bool hasLastSeenPosition;
  private bool isAttacking;
  private bool playerDefeated;
  private bool wizardPhaseTwoActive;
  private bool wizardPhaseTwoRetreating;
  private float nextRetreatRepathTime;
  private float wizardPhaseTwoRetreatEndTime;
  private Coroutine attackRoutine;
  private Vector3 wizardPhaseTwoRetreatTarget;
  private int fixedPatrolIndex;
  private readonly List<string> resolvedAttackStates = new List<string>();

  void Awake()
  {
    agent = GetComponent<NavMeshAgent>();
    animator = GetComponentInChildren<Animator>();
    enemyHealth = GetComponent<EnemyHealth>();
    spawnPosition = transform.position;
    spawnForward = transform.forward.sqrMagnitude > 0.001f ? transform.forward.normalized : Vector3.forward;

    agent.stoppingDistance = minSeparation;
    agent.speed = walkSpeed;
    agent.angularSpeed = Mathf.Max(agent.angularSpeed, rotationSpeed * 60f);

    if (animator != null)
    {
      animator.applyRootMotion = false;
      animator.speed = 1f;
    }

    AutoConfigureArchetype();
    AutoConfigureAnimationProfile();
    ResolveAttackStates();
  }

  void Start()
  {
    ResolvePlayerReferences();
    TransitionToState(patrolEnabled ? EnemyState.Patrol : EnemyState.Idle, true);
  }

  void Update()
  {
    if (GameFlowManager.BlocksGameplayInput)
    {
      CancelAttack();
      StopEnemy();
      PlayBestLoopState(idleState, patrolState, walkState);
      return;
    }

    ResolvePlayerReferences();
    if (player == null || playerHealth == null) return;

    if (playerDefeated || playerHealth.IsDead)
    {
      hasLastSeenPosition = false;
      CancelAttack();
      TransitionToState(EnemyState.Idle);
      ForceIdleAnimation();
      return;
    }

    bool canSeePlayer = CanSeePlayer(out float distanceToPlayer);
    bool recentlySawPlayer = hasLastSeenPosition && Time.time - lastSeenTime <= loseSightGracePeriod;

    if (canSeePlayer)
    {
      lastSeenPosition = player.position;
      hasLastSeenPosition = true;
      lastSeenTime = Time.time;
    }

    UpdateWizardPhaseTwoState();

    if (HandleWizardPhaseTwoRetreat())
    {
      return;
    }

    if (isAttacking)
    {
      FacePlayer();
      return;
    }

    switch (currentState)
    {
      case EnemyState.Idle:
        TickIdle(canSeePlayer);
        break;
      case EnemyState.Patrol:
        TickPatrol(canSeePlayer);
        break;
      case EnemyState.Alert:
        TickAlert(canSeePlayer);
        break;
      case EnemyState.Chase:
        TickChase(canSeePlayer, recentlySawPlayer, distanceToPlayer);
        break;
      case EnemyState.Search:
        TickSearch(canSeePlayer);
        break;
      case EnemyState.Return:
        TickReturn(canSeePlayer);
        break;
      case EnemyState.Attack:
        TickAttack(canSeePlayer, recentlySawPlayer, distanceToPlayer);
        break;
    }
  }

  void ResolvePlayerReferences()
  {
    if (player == null)
    {
      GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
      if (playerObject != null) player = playerObject.transform;
    }

    if (player != null && playerHealth == null)
    {
      playerHealth = player.GetComponent<PlayerHealth>();
    }
  }

  void TickIdle(bool canSeePlayer)
  {
    StopEnemy();
    PlayBestLoopState(idleState, patrolState);

    if (canSeePlayer)
    {
      TransitionToState(EnemyState.Alert);
      return;
    }

    if (patrolEnabled)
    {
      stateTimer += Time.deltaTime;
      if (stateTimer >= patrolPauseDuration)
      {
        TransitionToState(EnemyState.Patrol);
      }
    }
  }

  void TickPatrol(bool canSeePlayer)
  {
    if (canSeePlayer)
    {
      TransitionToState(EnemyState.Alert);
      return;
    }

    if (!patrolEnabled || patrolRadius <= 0.1f)
    {
      TransitionToState(EnemyState.Idle);
      return;
    }

    agent.stoppingDistance = 0f;
    agent.speed = walkSpeed;
    agent.isStopped = false;

    if (!hasPatrolDestination)
    {
      if (!TryGetPatrolDestination(out patrolDestination))
      {
        TransitionToState(EnemyState.Idle);
        return;
      }

      hasPatrolDestination = true;
      agent.SetDestination(patrolDestination);
    }

    PlayBestLoopState(patrolState, walkState, idleState);

    if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
    {
      hasPatrolDestination = false;
      TransitionToState(EnemyState.Idle);
      return;
    }

    if (ShouldRecoverFromStuck())
    {
      hasPatrolDestination = false;
      agent.ResetPath();
      TransitionToState(EnemyState.Idle, true);
    }
  }

  void TickAlert(bool canSeePlayer)
  {
    StopEnemy();
    FacePlayer();
    PlayBestLoopState(alertState, idleState);

    if (!canSeePlayer && !hasLastSeenPosition)
    {
      TransitionToState(patrolEnabled ? EnemyState.Patrol : EnemyState.Idle);
      return;
    }

    stateTimer += Time.deltaTime;
    if (stateTimer >= alertDuration)
    {
      TransitionToState(EnemyState.Chase);
    }
  }

  void TickChase(bool canSeePlayer, bool recentlySawPlayer, float distanceToPlayer)
  {
    if (!canSeePlayer && !recentlySawPlayer)
    {
      TransitionToState(EnemyState.Search);
      return;
    }

    if (ShouldRetreatFromPlayer(canSeePlayer, distanceToPlayer))
    {
      RetreatFromPlayer();
      PlayBestLoopState(runState, walkState, patrolState, idleState);
      return;
    }

    if (CanStartAttack(canSeePlayer, distanceToPlayer))
    {
      TransitionToState(EnemyState.Attack);
      return;
    }

    agent.stoppingDistance = minSeparation;
    agent.speed = runSpeed > walkSpeed ? runSpeed : walkSpeed;
    agent.isStopped = false;

    Vector3 targetPosition = canSeePlayer ? player.position : lastSeenPosition;
    agent.SetDestination(targetPosition);
    PlayBestLoopState(runState, walkState, patrolState, idleState);
  }

  void TickSearch(bool canSeePlayer)
  {
    if (canSeePlayer)
    {
      TransitionToState(EnemyState.Alert);
      return;
    }

    if (!hasLastSeenPosition)
    {
      TransitionToState(returnToSpawnAfterSearch ? EnemyState.Return : EnemyState.Idle);
      return;
    }

    agent.stoppingDistance = 0f;
    agent.speed = walkSpeed;
    agent.isStopped = false;
    agent.SetDestination(lastSeenPosition);
    PlayBestLoopState(searchState, walkState, patrolState, idleState);

    stateTimer += Time.deltaTime;
    if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
    {
      StopEnemy();
      FaceDirection(lastSeenPosition - transform.position);
      PlayBestLoopState(searchState, idleState);
    }

    if (stateTimer >= searchDuration)
    {
      hasLastSeenPosition = false;
      TransitionToState(returnToSpawnAfterSearch ? EnemyState.Return : EnemyState.Idle);
      return;
    }

    if (ShouldRecoverFromStuck())
    {
      hasLastSeenPosition = false;
      agent.ResetPath();
      TransitionToState(returnToSpawnAfterSearch ? EnemyState.Return : EnemyState.Idle, true);
    }
  }

  void TickReturn(bool canSeePlayer)
  {
    if (canSeePlayer)
    {
      TransitionToState(EnemyState.Alert);
      return;
    }

    agent.stoppingDistance = 0f;
    agent.speed = walkSpeed;
    agent.isStopped = false;
    agent.SetDestination(spawnPosition);
    PlayBestLoopState(returnState, patrolState, walkState, idleState);

    if (!agent.pathPending && agent.remainingDistance <= patrolPointTolerance)
    {
      TransitionToState(patrolEnabled ? EnemyState.Patrol : EnemyState.Idle);
      return;
    }

    if (ShouldRecoverFromStuck())
    {
      agent.ResetPath();
      TransitionToState(patrolEnabled ? EnemyState.Patrol : EnemyState.Idle, true);
    }
  }

  void TickAttack(bool canSeePlayer, bool recentlySawPlayer, float distanceToPlayer)
  {
    StopEnemy();

    if (!canSeePlayer && !recentlySawPlayer)
    {
      TransitionToState(EnemyState.Search);
      return;
    }

    if (ShouldRetreatFromPlayer(canSeePlayer, distanceToPlayer))
    {
      TransitionToState(EnemyState.Chase);
      return;
    }

    if (distanceToPlayer > attackRange)
    {
      TransitionToState(EnemyState.Chase);
      return;
    }

    FacePlayer();

    if (Time.time >= nextAttackTime && attackRoutine == null)
    {
      attackRoutine = StartCoroutine(AttackRoutine());
      return;
    }

    PlayBestLoopState(idleState, alertState, patrolState);
  }

  IEnumerator AttackRoutine()
  {
    if (playerHealth == null || playerDefeated || playerHealth.IsDead)
    {
      isAttacking = false;
      attackRoutine = null;
      ForceIdleAnimation();
      yield break;
    }

    isAttacking = true;
    nextAttackTime = Time.time + attackCooldown;

    StopEnemy();
    FacePlayer();
    PlayAttackAnimation();

    yield return new WaitForSeconds(hitDelay);

    if (playerHealth != null && !playerHealth.IsDead && player != null)
    {
      float distance = Vector3.Distance(transform.position, player.position);
      if (distance <= attackRange + 0.5f && !ShouldRetreatFromPlayer(true, distance) && HasLineOfSightToPlayer())
      {
        playerHealth.TakeDamage(damage);

        if (playerHealth.IsDead || playerDefeated)
        {
          isAttacking = false;
          attackRoutine = null;
          ForceIdleAnimation();
          yield break;
        }
      }
    }

    float settleTime = Mathf.Max(0f, attackAnimationDuration - hitDelay);
    if (settleTime > 0f)
    {
      yield return new WaitForSeconds(settleTime);
    }

    isAttacking = false;
    attackRoutine = null;

    if (player == null || playerHealth == null || playerHealth.IsDead || playerDefeated)
    {
      TransitionToState(EnemyState.Idle, true);
      ForceIdleAnimation();
      yield break;
    }

    bool canSeePlayer = CanSeePlayer(out float distanceToPlayer);
    bool recentlySawPlayer = hasLastSeenPosition && Time.time - lastSeenTime <= loseSightGracePeriod;

    if (canSeePlayer && distanceToPlayer <= attackRange)
    {
      TransitionToState(EnemyState.Attack, true);
    }
    else if (canSeePlayer || recentlySawPlayer)
    {
      TransitionToState(EnemyState.Chase, true);
    }
    else
    {
      TransitionToState(EnemyState.Search, true);
    }
  }

  void CancelAttack()
  {
    if (attackRoutine != null)
    {
      StopCoroutine(attackRoutine);
      attackRoutine = null;
    }

    isAttacking = false;
    nextAttackTime = Time.time + 0.25f;
  }

  void TransitionToState(EnemyState nextState, bool force = false)
  {
    if (!force && currentState == nextState) return;

    currentState = nextState;
    stateTimer = 0f;
    stuckTimer = 0f;

    switch (currentState)
    {
      case EnemyState.Idle:
        StopEnemy();
        break;
      case EnemyState.Patrol:
        hasPatrolDestination = false;
        break;
      case EnemyState.Alert:
        StopEnemy();
        break;
      case EnemyState.Chase:
        agent.ResetPath();
        break;
      case EnemyState.Search:
        agent.ResetPath();
        break;
      case EnemyState.Return:
        agent.ResetPath();
        break;
      case EnemyState.Attack:
        StopEnemy();
        break;
    }
  }

  bool CanSeePlayer(out float distanceToPlayer)
  {
    distanceToPlayer = float.MaxValue;
    if (player == null) return false;

    Vector3 toPlayer = player.position - transform.position;
    distanceToPlayer = toPlayer.magnitude;
    if (distanceToPlayer > detectionRange) return false;

    if (distanceToPlayer > attackRange * 1.1f)
    {
      float angle = Vector3.Angle(transform.forward, toPlayer);
      if (angle > fieldOfView * 0.5f) return false;
    }

    return HasLineOfSightToPlayer();
  }

  bool HasLineOfSightToPlayer()
  {
    if (player == null) return false;

    Vector3 origin = transform.position + Vector3.up * eyeHeight;
    Vector3 target = player.position + Vector3.up * 1f;
    Vector3 direction = target - origin;
    float distance = direction.magnitude;
    if (distance <= 0.01f) return true;

    if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, visionBlockers, QueryTriggerInteraction.Ignore))
    {
      return hit.transform == player || hit.transform.IsChildOf(player);
    }

    return true;
  }

  bool TryGetPatrolDestination(out Vector3 destination)
  {
    if (useFixedPatrolRoute)
    {
      return TryGetFixedPatrolDestination(out destination);
    }

    float sampleRadius = Mathf.Max(1.5f, patrolRadius * 0.45f);

    for (int i = 0; i < 10; i++)
    {
      Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
      Vector3 sample = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
      if (NavMesh.SamplePosition(sample, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
      {
        if (Vector3.Distance(hit.position, transform.position) <= patrolPointTolerance + 0.5f)
        {
          continue;
        }

        destination = hit.position;
        return true;
      }
    }

    destination = spawnPosition;
    return false;
  }

  bool TryGetFixedPatrolDestination(out Vector3 destination)
  {
    int pointCount = Mathf.Max(2, fixedPatrolPointCount);
    float radius = Mathf.Max(0.6f, fixedPatrolRouteRadius);

    Vector3 forward = spawnForward.sqrMagnitude > 0.001f ? spawnForward : Vector3.forward;
    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
    float angleStep = 360f / pointCount;

    for (int i = 0; i < pointCount; i++)
    {
      int routePointIndex = (fixedPatrolIndex + i) % pointCount;
      Quaternion rotation = Quaternion.AngleAxis(routePointIndex * angleStep, Vector3.up);
      Vector3 offset = rotation * forward * radius;
      Vector3 samplePoint = spawnPosition + offset + right * Mathf.Sin(routePointIndex * Mathf.Deg2Rad * angleStep) * 0.35f;

      if (NavMesh.SamplePosition(samplePoint, out NavMeshHit hit, radius + 1f, NavMesh.AllAreas))
      {
        fixedPatrolIndex = (routePointIndex + 1) % pointCount;
        destination = hit.position;
        return true;
      }
    }

    destination = spawnPosition;
    return false;
  }

  bool ShouldRetreatFromPlayer(bool canSeePlayer, float distanceToPlayer)
  {
    return enemyArchetype == EnemyArchetype.Wizard
      && !wizardPhaseTwoActive
      && canSeePlayer
      && retreatDistance > 0.1f
      && distanceToPlayer < retreatDistance;
  }

  bool ShouldRecoverFromStuck()
  {
    if (!agent.enabled || agent.pathPending || !agent.hasPath || agent.isStopped)
    {
      stuckTimer = 0f;
      return false;
    }

    if (agent.remainingDistance <= Mathf.Max(patrolPointTolerance, 0.2f))
    {
      stuckTimer = 0f;
      return false;
    }

    if (agent.remainingDistance <= stuckDistanceThreshold)
    {
      stuckTimer = 0f;
      return false;
    }

    if (agent.velocity.sqrMagnitude > stuckVelocityThreshold * stuckVelocityThreshold)
    {
      stuckTimer = 0f;
      return false;
    }

    stuckTimer += Time.deltaTime;
    return stuckTimer >= stuckRepathDelay;
  }

  bool CanStartAttack(bool canSeePlayer, float distanceToPlayer)
  {
    if (!canSeePlayer) return false;
    if (distanceToPlayer > attackRange) return false;

    if (enemyArchetype == EnemyArchetype.Wizard && retreatDistance > 0.1f)
    {
      return distanceToPlayer >= retreatDistance;
    }

    return true;
  }

  void RetreatFromPlayer()
  {
    if (player == null) return;

    agent.stoppingDistance = Mathf.Max(attackRange * 0.8f, minSeparation);
    agent.speed = runSpeed > walkSpeed ? runSpeed : walkSpeed;
    agent.isStopped = false;

    if (Time.time < nextRetreatRepathTime && agent.hasPath)
    {
      return;
    }

    nextRetreatRepathTime = Time.time + retreatRepathInterval;

    Vector3 awayDirection = (transform.position - player.position).normalized;
    if (awayDirection.sqrMagnitude < 0.001f)
    {
      awayDirection = -transform.forward;
    }

    Vector3 desiredPoint = transform.position + awayDirection * retreatSampleDistance;
    if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, retreatSampleDistance, NavMesh.AllAreas))
    {
      agent.SetDestination(hit.position);
    }
    else
    {
      agent.SetDestination(transform.position + awayDirection * 1.5f);
    }
  }

  void UpdateWizardPhaseTwoState()
  {
    if (enemyArchetype != EnemyArchetype.Wizard || wizardPhaseTwoActive) return;
    if (enemyHealth == null || enemyHealth.currentHealth > wizardPhaseTwoTriggerHealth) return;

    wizardPhaseTwoActive = true;
    wizardPhaseTwoRetreating = true;
    wizardPhaseTwoRetreatEndTime = Time.time + wizardPhaseTwoRetreatDuration;

    damage = wizardPhaseTwoDamage;
    attackRange = Mathf.Max(attackRange, wizardPhaseTwoAttackRange);
    attackCooldown = Mathf.Max(attackCooldown, wizardPhaseTwoAttackCooldown);
    hitDelay = Mathf.Max(hitDelay, wizardPhaseTwoHitDelay);
    attackAnimationDuration = Mathf.Max(attackAnimationDuration, wizardPhaseTwoAttackAnimationDuration);
    attackState = FirstValidState(wizardPhaseTwoAttackState, "Attack04", "Attack03Start", attackState);
    attackStates = new[] { attackState };
    ResolveAttackStates();

    Vector3 awayDirection = player != null
      ? (transform.position - player.position).normalized
      : -transform.forward;

    if (awayDirection.sqrMagnitude < 0.001f)
    {
      awayDirection = -transform.forward;
    }

    Vector3 desiredPoint = transform.position + awayDirection * wizardPhaseTwoRetreatDistance;
    if (NavMesh.SamplePosition(desiredPoint, out NavMeshHit hit, wizardPhaseTwoRetreatDistance + 1f, NavMesh.AllAreas))
    {
      wizardPhaseTwoRetreatTarget = hit.position;
    }
    else
    {
      wizardPhaseTwoRetreatTarget = transform.position + awayDirection * 1.5f;
    }

    CancelAttack();
    TransitionToState(EnemyState.Chase, true);
  }

  bool HandleWizardPhaseTwoRetreat()
  {
    if (!wizardPhaseTwoRetreating) return false;

    if (Time.time >= wizardPhaseTwoRetreatEndTime)
    {
      wizardPhaseTwoRetreating = false;
      return false;
    }

    agent.stoppingDistance = 0f;
    agent.speed = runSpeed > walkSpeed ? runSpeed : walkSpeed;
    agent.isStopped = false;
    agent.SetDestination(wizardPhaseTwoRetreatTarget);
    PlayBestLoopState(runState, walkState, patrolState, idleState);
    return true;
  }

  void StopEnemy()
  {
    if (!agent.enabled) return;

    agent.isStopped = true;
    if (agent.hasPath)
    {
      agent.ResetPath();
    }
  }

  void FacePlayer()
  {
    if (player == null) return;
    FaceDirection(player.position - transform.position);
  }

  void FaceDirection(Vector3 direction)
  {
    direction.y = 0f;
    if (direction.sqrMagnitude < 0.001f) return;

    Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
    transform.rotation = Quaternion.Slerp(
      transform.rotation,
      targetRotation,
      Mathf.Clamp01(Time.deltaTime * rotationSpeed)
    );
  }

  void PlayAttackAnimation()
  {
    string chosenAttackState = attackState;

    if (resolvedAttackStates.Count > 0)
    {
      chosenAttackState = resolvedAttackStates[Random.Range(0, resolvedAttackStates.Count)];
    }

    if (animator == null || string.IsNullOrEmpty(chosenAttackState)) return;

    if (animator.HasState(0, Animator.StringToHash(chosenAttackState)))
    {
      animator.Play(chosenAttackState, 0, 0f);
    }
  }

  void PlayBestLoopState(params string[] candidates)
  {
    if (animator == null) return;

    foreach (string stateName in candidates)
    {
      if (string.IsNullOrEmpty(stateName)) continue;

      int hash = Animator.StringToHash(stateName);
      if (!animator.HasState(0, hash)) continue;

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

      animator.CrossFadeInFixedTime(stateName, 0.08f, 0);
      return;
    }
  }

  void ResolveAttackStates()
  {
    resolvedAttackStates.Clear();

    AddAttackStateCandidate(attackState);

    if (attackStates != null)
    {
      foreach (string candidate in attackStates)
      {
        AddAttackStateCandidate(candidate);
      }
    }

    if (attackStates != null && attackStates.Length > 0)
    {
      return;
    }

    AddAttackStateCandidate("Attack02");
    AddAttackStateCandidate("Attack03");
    AddAttackStateCandidate("Attack04");
    AddAttackStateCandidate("Attack03Start");
    AddAttackStateCandidate("JumpAirAttack");
    AddAttackStateCandidate("JumpUpAttack");
  }

  void AddAttackStateCandidate(string stateName)
  {
    if (string.IsNullOrEmpty(stateName) || resolvedAttackStates.Contains(stateName)) return;
    if (animator != null && animator.HasState(0, Animator.StringToHash(stateName)))
    {
      resolvedAttackStates.Add(stateName);
    }
  }

  void AutoConfigureAnimationProfile()
  {
    if (animator == null) return;

    if (string.IsNullOrEmpty(patrolState))
    {
      patrolState = FirstValidState("WalkForwardBattle", "BattleWalkForward", "Walk", "WalkForward", "RunForwardBattle");
    }

    if (string.IsNullOrEmpty(runState))
    {
      runState = FirstValidState("RunForwardBattle", "BattleRunForward", "Run", "WalkForwardBattle", "BattleWalkForward", "Walk");
    }

    if (string.IsNullOrEmpty(searchState))
    {
      searchState = FirstValidState(walkState, patrolState, idleState, "Idle");
    }

    if (string.IsNullOrEmpty(returnState))
    {
      returnState = FirstValidState(runState, patrolState, walkState, idleState);
    }

    if (string.IsNullOrEmpty(alertState))
    {
      alertState = FirstValidState(idleState, "Idle", "Idle01");
    }

    if (string.IsNullOrEmpty(idleState))
    {
      idleState = FirstValidState("Idle_Battle", "Idle", "Idle01", "Idle02");
    }

    if (string.IsNullOrEmpty(walkState))
    {
      walkState = FirstValidState("WalkForwardBattle", "BattleWalkForward", "Walk", "WalkForward");
    }

    if (string.IsNullOrEmpty(attackState))
    {
      attackState = FirstValidState("Attack01", "Attack1", "Attack04", "Attack02", "JumpAirAttack");
    }
  }

  void AutoConfigureArchetype()
  {
    string animatorName = animator != null && animator.runtimeAnimatorController != null
      ? animator.runtimeAnimatorController.name.ToLowerInvariant()
      : string.Empty;

    if (enemyArchetype == EnemyArchetype.Auto)
    {
      if (animatorName.Contains("wizard"))
      {
        enemyArchetype = EnemyArchetype.Wizard;
      }
      else if (animatorName.Contains("lich"))
      {
        enemyArchetype = EnemyArchetype.Lich;
      }
      else
      {
        enemyArchetype = EnemyArchetype.Dog;
      }
    }

    switch (enemyArchetype)
    {
      case EnemyArchetype.Dog:
        alertDuration = Mathf.Min(alertDuration, 0.3f);
        searchDuration = Mathf.Max(2.1f, searchDuration);
        runSpeed = Mathf.Max(runSpeed, walkSpeed + 1.35f);
        preferredCombatDistance = 0f;
        retreatDistance = 0f;
        alertState = FirstValidState(idleState, "Idle_Battle");
        searchState = FirstValidState(walkState, patrolState, idleState);
        break;

      case EnemyArchetype.Lich:
        alertDuration = Mathf.Max(alertDuration, 0.5f);
        searchDuration = Mathf.Max(searchDuration, 4f);
        runSpeed = Mathf.Max(runSpeed, walkSpeed + 0.45f);
        preferredCombatDistance = 0f;
        retreatDistance = 0f;
        patrolEnabled = true;
        useFixedPatrolRoute = true;
        fixedPatrolRouteRadius = Mathf.Max(fixedPatrolRouteRadius, 2.1f);
        fixedPatrolPointCount = Mathf.Max(3, fixedPatrolPointCount);
        patrolRadius = Mathf.Max(patrolRadius, 5f);
        patrolPauseDuration = Mathf.Max(patrolPauseDuration, 1.5f);
        idleState = FirstValidState("idle", "Idle");
        walkState = FirstValidState("walk", "Walk");
        patrolState = walkState;
        runState = FirstValidState("run", walkState, "Run");
        alertState = idleState;
        searchState = walkState;
        returnState = walkState;
        attackState = FirstValidState("attack01", "Attack01", "attack02", "Attack02");
        attackStates = new[] { attackState };
        hitDelay = Mathf.Max(hitDelay, 0.42f);
        attackAnimationDuration = Mathf.Max(attackAnimationDuration, 0.9f);
        break;

      case EnemyArchetype.Wizard:
        detectionRange = Mathf.Max(detectionRange, 13f);
        alertDuration = Mathf.Max(alertDuration, 0.35f);
        searchDuration = Mathf.Max(searchDuration, 4.5f);
        runSpeed = Mathf.Max(runSpeed, walkSpeed + 1f);
        patrolRadius = Mathf.Min(patrolRadius, 4.5f);
        useFixedPatrolRoute = true;
        fixedPatrolRouteRadius = Mathf.Max(1.8f, Mathf.Min(fixedPatrolRouteRadius, 2.8f));
        fixedPatrolPointCount = 3;
        preferredCombatDistance = preferredCombatDistance <= 0f ? Mathf.Max(attackRange - 0.5f, minSeparation + 0.4f) : preferredCombatDistance;
        retreatDistance = 0f;
        attackCooldown = Mathf.Max(attackCooldown, 1.55f);
        hitDelay = Mathf.Max(hitDelay, 0.4f);
        attackAnimationDuration = Mathf.Max(attackAnimationDuration, 0.7f);
        attackState = FirstValidState("Attack02Start", "Attack04", "Attack01", attackState);
        attackStates = new[] { attackState };
        alertState = FirstValidState(idleState, "Idle01");
        searchState = FirstValidState(walkState, patrolState, idleState);
        break;
    }
  }

  public void NotifyDamaged(Transform attacker)
  {
    if (attacker != null)
    {
      player = attacker;
      playerHealth = attacker.GetComponent<PlayerHealth>();
      lastSeenPosition = attacker.position;
      hasLastSeenPosition = true;
      lastSeenTime = Time.time;
    }

    if (!isAttacking)
    {
      TransitionToState(EnemyState.Alert, true);
    }
  }

  public void OnPlayerDefeated()
  {
    playerDefeated = true;
    hasLastSeenPosition = false;
    wizardPhaseTwoRetreating = false;
    StopAllCoroutines();
    CancelAttack();
    TransitionToState(EnemyState.Idle, true);
    ForceIdleAnimation();
    enabled = false;
  }

  void ForceIdleAnimation()
  {
    if (animator == null) return;

    string forcedIdleState = FirstValidState(idleState, patrolState, walkState, "Idle", "Idle01");
    if (string.IsNullOrEmpty(forcedIdleState)) return;

    int stateHash = Animator.StringToHash(forcedIdleState);
    if (!animator.HasState(0, stateHash)) return;

    animator.Play(forcedIdleState, 0, 0f);
    animator.Update(0f);

    if (playerDefeated)
    {
      animator.speed = 0f;
    }
  }

  string FirstValidState(params string[] candidates)
  {
    foreach (string candidate in candidates)
    {
      if (string.IsNullOrEmpty(candidate)) continue;
      if (animator.HasState(0, Animator.StringToHash(candidate)))
      {
        return candidate;
      }
    }

    return string.Empty;
  }

#if UNITY_EDITOR
  void OnValidate()
  {
    detectionRange = Mathf.Max(attackRange + 0.1f, detectionRange);
    walkSpeed = Mathf.Max(0.1f, walkSpeed);
    runSpeed = Mathf.Max(walkSpeed, runSpeed);
    patrolRadius = Mathf.Max(0f, patrolRadius);
    patrolPointTolerance = Mathf.Max(0.1f, patrolPointTolerance);
    alertDuration = Mathf.Max(0f, alertDuration);
    searchDuration = Mathf.Max(0.5f, searchDuration);
    attackCooldown = Mathf.Max(0.1f, attackCooldown);
    hitDelay = Mathf.Max(0f, hitDelay);
    attackAnimationDuration = Mathf.Max(hitDelay, attackAnimationDuration);
    minSeparation = Mathf.Max(0f, minSeparation);
  }
#endif
}

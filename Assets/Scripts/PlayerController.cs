using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
  [Header("Move")]
  public float walkSpeed = 6f;
  public float runSpeed = 12f;
  public float backwardSpeedMultiplier = 0.75f;
  public float gravity = -20f;
  public float jumpHeight = 3.5f;
  public float turnSpeed = 220f;
  public float forwardTurnSmoothSpeed = 12f;

  [Header("Look")]
  public float mouseSensitivity = 70f;
  public float minPitch = -35f;
  public float maxPitch = 60f;

  [Header("Attack Unlocks")]
  public int currentAttackType = 1;
  public bool attack2Unlocked = false;
  public bool attack3Unlocked = false;

  [Header("Refs")]
  public Transform modelRoot;
  public Animator animator;
  public Camera playerCamera;

  private CharacterController controller;
  private Vector3 velocity;
  private float cameraPitch;

  public int CurrentAttackType => currentAttackType;

  void Awake()
  {
    controller = GetComponent<CharacterController>();

    if (animator == null && modelRoot != null)
    {
      animator = modelRoot.GetComponent<Animator>();
    }

    if (playerCamera == null)
    {
      playerCamera = Camera.main;
    }
  }

  void Start()
  {
    if (playerCamera != null)
    {
      cameraPitch = playerCamera.transform.localEulerAngles.x;
      if (cameraPitch > 180f)
      {
        cameraPitch -= 360f;
      }
    }

    Cursor.lockState = CursorLockMode.None;
    Cursor.visible = true;
  }

  void Update()
  {
    if (GameFlowManager.BlocksGameplayInput)
    {
      if (animator != null)
      {
        animator.SetFloat("Speed", 0f);
      }

      return;
    }

    HandleAttackModeSwitch();
    HandleLook();
    HandleMovement();
    HandleJump();
    ApplyGravity();
    UpdateAnimatorValues();
  }

  void HandleAttackModeSwitch()
  {
    if (Input.GetKeyDown(KeyCode.Alpha1))
    {
      currentAttackType = 1;
    }

    if (Input.GetKeyDown(KeyCode.Alpha2))
    {
      if (attack2Unlocked)
    {
      currentAttackType = 2;
    }
    }

    if (Input.GetKeyDown(KeyCode.Alpha3))
    {
      if (attack3Unlocked)
    {
      currentAttackType = 3;
    }
    }
  }

  void HandleLook()
  {
    if (playerCamera == null) return;

    bool isHoldingRightMouse = Input.GetMouseButton(1);

    if (!isHoldingRightMouse)
    {
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;
      return;
    }

    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;

    float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
    float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

    transform.Rotate(0f, mouseX, 0f);

    cameraPitch -= mouseY;
    cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
    playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
  }

  void HandleMovement()
  {
    if (playerCamera == null) return;

    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");

    float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

    Vector3 cameraForward = playerCamera.transform.forward;
    cameraForward.y = 0f;
    cameraForward.Normalize();

    if (v > 0.1f)
    {
      Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
      transform.rotation = Quaternion.Slerp(
          transform.rotation,
          targetRotation,
          forwardTurnSmoothSpeed * Time.deltaTime
      );

      controller.Move(transform.forward * currentSpeed * Time.deltaTime);
    }

    if (v < -0.1f)
    {
      controller.Move(-transform.forward * currentSpeed * backwardSpeedMultiplier * Time.deltaTime);
    }

    if (h < -0.1f)
    {
      transform.Rotate(0f, -turnSpeed * Time.deltaTime, 0f);
    }

    if (h > 0.1f)
    {
      transform.Rotate(0f, turnSpeed * Time.deltaTime, 0f);
    }
  }

  void HandleJump()
  {
    bool isGrounded = controller.isGrounded;

    if (isGrounded && velocity.y < 0f)
    {
      velocity.y = -2f;
    }

    if (Input.GetButtonDown("Jump") && isGrounded)
    {
      velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }
  }

  void ApplyGravity()
  {
    velocity.y += gravity * Time.deltaTime;
    controller.Move(velocity * Time.deltaTime);
  }

  void UpdateAnimatorValues()
  {
    if (animator == null) return;

    float h = Mathf.Abs(Input.GetAxisRaw("Horizontal"));
    float v = Mathf.Abs(Input.GetAxisRaw("Vertical"));

    float speed = 0f;

    if (v > 0.1f || h > 0.1f)
    {
      speed = Input.GetKey(KeyCode.LeftShift) ? 1f : 0.5f;
    }

    animator.SetFloat("Speed", speed);
  }

  public string GetCurrentAttackStateName()
  {
    switch (currentAttackType)
    {
      case 2:
        return attack2Unlocked ? "hit02" : "hit01";
      case 3:
        return attack3Unlocked ? "hit03" : "hit01";
      default:
        return "hit01";
    }
  }
}

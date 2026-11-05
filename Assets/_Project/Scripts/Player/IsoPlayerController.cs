using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(NavMeshAgent))]
public class IsoPlayerController : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 6f;
    public float rotationSpeed = 15f;

    [Header("Ссылка на камеру")]
    public Transform cameraTransform;

    private NavMeshAgent agent;
    private Animator animator;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Выключаем автоматический поворот агента, чтобы крутить его плавно самим
        agent.updateRotation = false; 
        agent.speed = moveSpeed;

        animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // 1. Считываем WASD / Стрелки
        Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1;
        }
#else
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        input = input.normalized;

        // 2. Переводим в изометрический угол камеры
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        // 3. Двигаем по запеченному NavMesh (без единого коллайдера в сцене!)
        if (moveDir.magnitude >= 0.1f)
        {
            agent.Move(moveDir * moveSpeed * Time.deltaTime);

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 4. Анимация
        if (animator != null)
        {
            animator.SetFloat("Speed", moveDir.magnitude, 0.1f, Time.deltaTime);
        }
    }
}
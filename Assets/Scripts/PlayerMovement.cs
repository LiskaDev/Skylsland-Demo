using UnityEngine;

// 这个脚本控制玩家的移动和视角
// 挂载在Player对象上，相机需要是Player的子对象
public class PlayerMovement : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;

    [Header("鼠标视角")]
    [SerializeField] private float mouseSensitivity = 2f;

    // 新增：拖入你的角色模型（带有Animator组件的那个节点）
    [Header("动画")]
    [SerializeField] private Animator animator; 

    private Rigidbody rb;
    private bool isGrounded = true;
    private Vector3 spawnPosition;

    private float cameraYaw = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        // 记录初始朝向
        cameraYaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // 如果没有手动拖入 Animator，尝试在子物体里自动找找
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        cameraYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        cameraYaw += mouseX;
        transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.forward * vertical + transform.right * horizontal;
        moveDirection.y = 0f; 

        // ====== 更新速度动画 ======
        // 判断有没有在移动，获取一个 0(没动) 到 1(移动) 的速度值传递给 Animator 的 Speed 参数
        float currentSpeed = moveDirection.magnitude > 0.1f ? 1f : 0f;
        if (animator != null)
        {
            animator.SetFloat("Speed", currentSpeed);
        }

        transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
            
            // ====== 触发跳跃动画 ======
            if (animator != null)
            {
                animator.SetBool("IsJumping", true); // 根据你设定的参数名触发跳跃
            }
        }

        if (transform.position.y < -10f)
        {
            transform.position = spawnPosition;
            rb.velocity = Vector3.zero;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                
                // ====== 落地关闭跳跃状态 ======
                if (animator != null)
                {
                    animator.SetBool("IsJumping", false);
                }
                break;
            }
        }
    }
}

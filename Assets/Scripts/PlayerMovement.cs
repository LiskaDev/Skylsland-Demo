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

    private Rigidbody rb;
    private bool isGrounded = true;
    private Vector3 spawnPosition;

    // 自己维护yaw角度，避免从eulerAngles读取导致的精度问题
    // （和PaperPlane.cs同样的思路）
    private float cameraYaw = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        spawnPosition = transform.position;
        // 记录初始朝向
        cameraYaw = transform.eulerAngles.y;

        // 锁定鼠标到窗口中心，并隐藏光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // 当脚本被重新启用时（比如从飞机下来），同步yaw角度
    // 这样不会出现视角跳转
    void OnEnable()
    {
        cameraYaw = transform.eulerAngles.y;
    }

    void Update()
    {
        // Escape键释放鼠标（方便退出游戏时操作）
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ====== 鼠标控制视角 ======
        // 鼠标左右移动 → 旋转Player的Y轴
        // 相机是Player的子对象，会自动跟着转
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        cameraYaw += mouseX;
        transform.rotation = Quaternion.Euler(0f, cameraYaw, 0f);

        // ====== WASD移动（相对于朝向） ======
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // transform.forward = 玩家当前面朝的方向
        // transform.right = 玩家的右方
        // 这样W就是"往前走"，A就是"往左走"，符合直觉
        Vector3 moveDirection = transform.forward * vertical + transform.right * horizontal;
        moveDirection.y = 0f; // 保持水平移动，不受旋转俯仰影响

        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // ====== 跳跃 ======
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }

        // ====== 掉落重生 ======
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
                break;
            }
        }
    }
}

using UnityEngine;

// 这个脚本挂在 Player 上，负责根据移动速度和跳跃状态切换动画
public class PlayerAnimationController : MonoBehaviour
{
    // 在Inspector里拖入Y Bot身上的Animator组件
    public Animator animator;

    // 获取Player身上的Rigidbody（物理组件），用来读取速度
    private Rigidbody rb;

    void Start()
    {
        // 游戏开始时找到Rigidbody组件
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 【注意：已注释防冲突】
        // 发现致命冲突：你的 PlayerMovement.cs 里已经在控制 Speed 和 IsJumping 了！
        // 如果这里也同时控制，两个脚本会每帧“互相打架”，导致跳跃落地后状态混乱、滑步卡死。
        // 为了保持架构整洁，我把这里的旧代码注释掉了，动画全权统一交给 PlayerMovement.cs 管理。

        /* 
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        float speed = horizontalVelocity.magnitude;
        animator.SetFloat("Speed", speed);

        bool isJumping = rb.velocity.y > 0.1f;
        animator.SetBool("IsJumping", isJumping);
        */
    }
}
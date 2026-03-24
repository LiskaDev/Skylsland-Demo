using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    private Animator animator;
    private Rigidbody rb;

    void Start()
    {
        // 找到角色身上的Animator组件
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // 根据Rigidbody速度控制动画
        // magnitude = 向量的长度，即速度大小
        float speed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        animator.SetFloat("Speed", speed);
    }
}
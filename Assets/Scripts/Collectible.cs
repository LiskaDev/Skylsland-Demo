using UnityEngine;

public class Collectible : MonoBehaviour
{
    // 是否可以被吸取（建筑方块默认不可吸取）
    [SerializeField] public bool isCollectible = true;

    [Header("吸取设置")]
    // 开始吸取的距离
    [SerializeField] private float attractDistance = 4f;
    // 吸取速度
    [SerializeField] private float attractSpeed = 8f;
    // 完全吸收的距离（消失）
    [SerializeField] private float collectDistance = 0.5f;

    [Header("漂浮动画")]
    // 方块放置后多久开始可以被吸取（防止刚放就被吸走）
    [SerializeField] private float activateDelay = 1f;
    // 漂浮幅度
    [SerializeField] private float floatAmount = 0.15f;
    // 漂浮速度
    [SerializeField] private float floatSpeed = 2f;

    private Transform player;
    private bool isAttracting = false;
    private bool isActive = false;
    private float activateTimer = 0f;
    private Vector3 startPosition;
    private Rigidbody rb;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        // 延迟激活，防止刚放置就被吸走
        activateTimer = activateDelay;

        // 忽略和玩家的碰撞，防止推玩家
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            Collider playerCol = playerObj.GetComponent<Collider>();
            Collider myCol = GetComponent<Collider>();
            if (playerCol != null && myCol != null)
                Physics.IgnoreCollision(myCol, playerCol, true);
        }
    }

    void Update()
    {
        if (!isCollectible) return;

        // 倒计时激活
        if (!isActive)
        {
            activateTimer -= Time.deltaTime;
            if (activateTimer <= 0f)
                isActive = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attractDistance)
        {
            // 进入吸取范围，开始飞向玩家
            isAttracting = true;
        }

        if (isAttracting)
        {
            // 禁用物理，改为脚本控制位置
            if (rb != null) rb.isKinematic = true;

            // 向玩家位置移动
            // Vector3.MoveTowards = 每帧向目标移动固定距离
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                attractSpeed * Time.deltaTime
            );

            // 旋转增加视觉效果
            transform.Rotate(Vector3.up * 180f * Time.deltaTime);

            // 到达玩家身边，消失（被收入背包）
            if (distance <= collectDistance)
            {
                Collect();
            }
        }
        else
        {
            // 落地后才开始漂浮
            // 只有Rigidbody静止后才更新startPosition
            if (rb != null && !rb.isKinematic)
            {
                // 还在受物理控制（还在下落），不漂浮
                // 等它完全落地停止后再激活漂浮
                if (rb.velocity.magnitude < 0.1f)
                {
                    // 速度接近0，说明落地了
                    // 锁定Y轴以外的物理，改为漂浮动画
                    rb.isKinematic = true;
                    startPosition = transform.position; // 更新为落地位置
                }
            }
            else if (rb != null && rb.isKinematic)
            {
                // 已经落地，执行漂浮动画
                float newY = startPosition.y +
                    Mathf.Sin(Time.time * floatSpeed) * floatAmount;
                transform.position = new Vector3(
                    transform.position.x, newY, transform.position.z);
            }
        }
    }

    void Collect()
    {
        if (Inventory.Instance != null)
        {
            string itemName = gameObject.name;
            
            if (itemName.Contains("BlockDrop_Stone"))
                Inventory.Instance.AddItem("Stone");
            else if (itemName.Contains("BlockDrop_Wood"))
                Inventory.Instance.AddItem("Wood");
            else
                Inventory.Instance.AddItem("Brick");
        }
        
        Debug.Log("收集了：" + gameObject.name);
        Destroy(gameObject);
    }
}
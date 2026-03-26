using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.AI;

public class VillagerBuilder : MonoBehaviour
{
    [Header("建造设置")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private float buildInterval = 0.3f;
    [SerializeField] private Transform buildOrigin;

    [Header("交互设置")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private GameObject interactUI;
    [SerializeField] private Transform playerTransform;

    [Header("蓝图UI")]
    [SerializeField] private GameObject blueprintPanel;

    [Header("状态")]
    [SerializeField] private bool isBuilding = false;
    private bool isMovingToBuild = false;
    private bool isBlueprintOpen = false;
    private NavMeshAgent agent;

    // ========== 三种蓝图数据 ==========

    // 蓝图一：小屋
    private Vector3[] blueprintHut = new Vector3[]
    {
        // 地基
        new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(2,0,0),
        new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(2,0,1),
        new Vector3(0,0,2), new Vector3(1,0,2), new Vector3(2,0,2),
        // 墙壁一层
        new Vector3(0,1,0), new Vector3(2,1,0),
        new Vector3(0,1,2), new Vector3(2,1,2),
        new Vector3(1,1,0), new Vector3(1,1,2),
        new Vector3(0,1,1), new Vector3(2,1,1),
        // 墙壁二层
        new Vector3(0,2,0), new Vector3(2,2,0),
        new Vector3(0,2,2), new Vector3(2,2,2),
        new Vector3(1,2,0), new Vector3(1,2,2),
        new Vector3(0,2,1), new Vector3(2,2,1),
        // 屋顶
        new Vector3(0,3,0), new Vector3(1,3,0), new Vector3(2,3,0),
        new Vector3(0,3,1), new Vector3(1,3,1), new Vector3(2,3,1),
        new Vector3(0,3,2), new Vector3(1,3,2), new Vector3(2,3,2),
    };

    // 蓝图二：塔
    private Vector3[] blueprintTower = new Vector3[]
    {
        // 每层只建四个角+中间
        // 第一层
        new Vector3(0,0,0), new Vector3(1,0,0),
        new Vector3(0,0,1), new Vector3(1,0,1),
        // 第二层
        new Vector3(0,1,0), new Vector3(1,1,0),
        new Vector3(0,1,1), new Vector3(1,1,1),
        // 第三层
        new Vector3(0,2,0), new Vector3(1,2,0),
        new Vector3(0,2,1), new Vector3(1,2,1),
        // 第四层
        new Vector3(0,3,0), new Vector3(1,3,0),
        new Vector3(0,3,1), new Vector3(1,3,1),
        // 第五层
        new Vector3(0,4,0), new Vector3(1,4,0),
        new Vector3(0,4,1), new Vector3(1,4,1),
        // 顶部平台
        new Vector3(0,5,0), new Vector3(1,5,0),
        new Vector3(0,5,1), new Vector3(1,5,1),
    };

    // 蓝图三：围墙
    private Vector3[] blueprintWall = new Vector3[]
    {
        // 一排长墙，两层高
        new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(2,0,0),
        new Vector3(3,0,0), new Vector3(4,0,0), new Vector3(5,0,0),
        new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(2,1,0),
        new Vector3(3,1,0), new Vector3(4,1,0), new Vector3(5,1,0),
    };

    // 当前选中的蓝图
    private Vector3[] currentBlueprint;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // 默认选小屋
        currentBlueprint = blueprintHut;
    }

    void Update()
    {
        float distance = Vector3.Distance(
            transform.position, playerTransform.position);

        // 距离够近时显示交互提示
        if (distance <= interactDistance)
        {
            if (interactUI != null)
                interactUI.SetActive(!isBlueprintOpen);

            // E键：打开/关闭蓝图选择面板
            if (Input.GetKeyDown(KeyCode.E) && !isBuilding)
            {
                if (!isBlueprintOpen)
                    OpenBlueprintPanel();
                else
                    CloseBlueprintPanel();
            }
        }
        else
        {
            if (interactUI != null)
                interactUI.SetActive(false);

            // 走远了自动关闭面板
            if (isBlueprintOpen)
                CloseBlueprintPanel();
        }

        // 蓝图面板打开时，数字键选择方案
        if (isBlueprintOpen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                SelectBlueprint(0);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                SelectBlueprint(1);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                SelectBlueprint(2);
        }

        // 右键设置建造位置
        if (Input.GetMouseButtonDown(1) && !isBuilding)
            SetBuildTarget();

        // 村民到达目标位置后开始建造
        if (isMovingToBuild && !isBuilding)
        {
            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance)
            {
                isMovingToBuild = false;
                StartCoroutine(BuildBlueprint());
            }
        }
    }

    void OpenBlueprintPanel()
    {
        isBlueprintOpen = true;
        if (blueprintPanel != null)
            blueprintPanel.SetActive(true);
        Debug.Log("蓝图面板已打开，按1/2/3选择");
    }

    void CloseBlueprintPanel()
    {
        isBlueprintOpen = false;
        if (blueprintPanel != null)
            blueprintPanel.SetActive(false);
    }

    void SelectBlueprint(int index)
    {
        // 根据选择切换蓝图
        switch (index)
        {
            case 0:
                currentBlueprint = blueprintHut;
                Debug.Log("已选择：小屋");
                break;
            case 1:
                currentBlueprint = blueprintTower;
                Debug.Log("已选择：塔");
                break;
            case 2:
                currentBlueprint = blueprintWall;
                Debug.Log("已选择：围墙");
                break;
        }

        // 选完自动关闭面板
        CloseBlueprintPanel();
        Debug.Log("右键点击地面，村民将前往建造！");
    }

    void SetBuildTarget()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            buildOrigin.position = hit.point;
            agent.SetDestination(hit.point);
            isMovingToBuild = true;
        }
    }

    // ==========================================
    // 供外部任务系统调用的专属方法
    // ==========================================
    public void StartBuildingFromQuest()
    {
        if (isBuilding) return;
        
        currentBlueprint = blueprintHut; // 强制盖小屋
        if (buildOrigin != null)
        {
            // 在村民正前方稍微隔开一点的地方生成建筑原点
            buildOrigin.position = transform.position + transform.forward * 3f;
        }
        
        // 启动带有任务标记的建造协程
        StartCoroutine(BuildBlueprintOptions(true));
    }

    IEnumerator BuildBlueprintOptions(bool fromQuest)
    {
        isBuilding = true;
        agent.SetDestination(transform.position);
        Debug.Log("村民开始建造！");

        foreach (Vector3 localPos in currentBlueprint)
        {
            Vector3 worldPos = buildOrigin.position + localPos;
            GameObject newBlock = Instantiate(blockPrefab, worldPos, Quaternion.identity);

            // 建筑方块不可被吸取
            Collectible col = newBlock.GetComponent<Collectible>();
            if (col != null) col.isCollectible = false;
            yield return new WaitForSeconds(buildInterval);
        }

        isBuilding = false;
        Debug.Log("建造完成！");

        // 如果是为任务建造的，完工后呼叫任务管理器发奖励
        if (fromQuest)
        {
            QuestManager qm = Object.FindFirstObjectByType<QuestManager>();
            if (qm != null)
            {
                qm.OnQuestComplete();
            }
        }
    }

    IEnumerator BuildBlueprint()
    {
        // 兼容原版的自主右键建造
        yield return StartCoroutine(BuildBlueprintOptions(false));
    }
}
using UnityEngine;
using TMPro;

public class Inventory : MonoBehaviour
{
    // 单例模式：让其他脚本可以直接用 Inventory.Instance 访问
    // 就像一个全局可访问的背包
    public static Inventory Instance;

    [Header("物品数量")]
    public int brickCount = 0;
    public int stoneCount = 0;
    public int woodCount = 0;

    [Header("UI文字")]
    [SerializeField] private TextMeshProUGUI brickText;
    [SerializeField] private TextMeshProUGUI stoneText;
    [SerializeField] private TextMeshProUGUI woodText;

    void Awake()
    {
        // 单例初始化：确保全场景只有一个背包
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // 添加物品，可以指定数量（默认1个）
    public void AddItem(string type, int amount = 1)
    {
        switch (type)
        {
            case "Brick":
                brickCount += amount;
                break;
            case "Stone":
                stoneCount += amount;
                break;
            case "Wood":
                woodCount += amount;
                break;
        }
        // 更新UI显示
        UpdateUI();
    }

    void UpdateUI()
    {
        if (brickText != null)
            brickText.text = "Brick: " + brickCount;
        if (stoneText != null)
            stoneText.text = "Stone: " + stoneCount;
        if (woodText != null)
            woodText.text = "Wood: " + woodCount;
    }
}

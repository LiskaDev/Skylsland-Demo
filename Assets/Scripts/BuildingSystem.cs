using UnityEngine;
using UnityEngine.UI;  // 用于显示UI文字
using TMPro;  // TextMeshPro的命名空间

public class BuildingSystem : MonoBehaviour
{
    [Header("掉落设置")]
    [SerializeField] private GameObject dropPrefab; // 掉落物预制体
    [SerializeField] private int dropCount = 1;     // 掉落数量

    // 方块类型数组：把三种预制体都放进来
    // 数组 = 一排格子，每个格子放一个东西
    [SerializeField] private GameObject[] blockPrefabs;

    // 方块名称数组：对应显示的名字
    [SerializeField] private string[] blockNames;

    [SerializeField] private float blockSize = 1f;

    // 当前选中的方块索引（0=第一个，1=第二个，2=第三个）
    private int currentBlockIndex = 0;

    // 屏幕上显示方块名称的文字组件
    [SerializeField] private TextMeshProUGUI blockNameText;

    // 幽灵预览方块
    private GameObject previewBlock;
    private Material previewMaterial;

    void Start()
    {
        // 用当前选中的方块创建预览
        CreatePreview();

        if (blockNameText != null)
            blockNameText.text = "Block: " + blockNames[0];
    }

    void Update()
    {
        // 数字键切换方块类型
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchBlock(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchBlock(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchBlock(2);

        // 也可以用滚轮切换
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) SwitchBlock((currentBlockIndex + 1) % blockPrefabs.Length);
        if (scroll < 0f) SwitchBlock((currentBlockIndex - 1 + blockPrefabs.Length) % blockPrefabs.Length);

        UpdatePreview();

        if (Input.GetMouseButtonDown(0) && !Input.GetKey(KeyCode.LeftControl))
        {
            PlaceBlock();
        }

        if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl))
        {
            RemoveBlock();
        }
    }

    // 切换方块类型
    void SwitchBlock(int index)
    {
        // 防止索引超出数组范围
        if (index < 0 || index >= blockPrefabs.Length) return;

        currentBlockIndex = index;

        // 删掉旧的预览，用新方块重新创建
        if (previewBlock != null) Destroy(previewBlock);
        CreatePreview();

        // 在Console窗口打印当前选中的方块（调试用）
        Debug.Log("Switched to: " + blockNames[currentBlockIndex]);

        // 更新屏幕上的文字
        if (blockNameText != null)
            blockNameText.text = "Block: " + blockNames[currentBlockIndex];
    }

    // 创建预览幽灵方块
    void CreatePreview()
    {
        previewBlock = Instantiate(blockPrefabs[currentBlockIndex]);

        previewMaterial = new Material(blockPrefabs[currentBlockIndex]
            .GetComponent<Renderer>().sharedMaterial);

        previewMaterial.SetFloat("_Mode", 3);
        previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.SetInt("_ZWrite", 0);
        previewMaterial.DisableKeyword("_ALPHATEST_ON");
        previewMaterial.EnableKeyword("_ALPHABLEND_ON");
        previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        previewMaterial.renderQueue = 3000;

        Color c = previewMaterial.color;
        c.a = 0.4f;
        previewMaterial.color = c;

        previewBlock.GetComponent<Renderer>().material = previewMaterial;

        if (previewBlock.GetComponent<Collider>() != null)
            Destroy(previewBlock.GetComponent<Collider>());

        previewBlock.SetActive(false);
    }

    void UpdatePreview()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = ~(1 << 6);

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            previewBlock.SetActive(true);
            Vector3 previewPosition = hit.point + hit.normal * (blockSize * 0.5f);
            previewPosition = SnapToGrid(previewPosition);
            previewBlock.transform.position = previewPosition;
        }
        else
        {
            previewBlock.SetActive(false);
        }
    }

    void PlaceBlock()
    {
        if (previewBlock.activeSelf)
        {
            Instantiate(blockPrefabs[currentBlockIndex],
                previewBlock.transform.position, Quaternion.identity);
        }
    }

    void RemoveBlock()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = ~(1 << 6);

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            if (hit.collider.gameObject.name.Contains("Block"))
            {
                // 在方块位置生成掉落物
                if (dropPrefab != null)
                {
                    for (int i = 0; i < dropCount; i++)
                    {
                        // 稍微随机偏移，不要全堆在一起
                        Vector3 dropPos = hit.collider.transform.position + 
                            new Vector3(
                                Random.Range(-0.3f, 0.3f),
                                0.5f,
                                Random.Range(-0.3f, 0.3f)
                            );
                        
                        GameObject drop = Instantiate(
                            dropPrefab, dropPos, Quaternion.identity);
                        
                        // 给掉落物一个随机弹出力，像被打碎一样
                        Rigidbody dropRb = drop.GetComponent<Rigidbody>();
                        if (dropRb != null)
                        {
                            dropRb.AddForce(new Vector3(
                                Random.Range(-2f, 2f),
                                Random.Range(3f, 5f),
                                Random.Range(-2f, 2f)
                            ), ForceMode.Impulse);
                        }
                    }
                }
                
                Destroy(hit.collider.gameObject);
            }
        }
    }

    Vector3 SnapToGrid(Vector3 position)
    {
        float x = Mathf.Round(position.x / blockSize) * blockSize;
        float y = Mathf.Round(position.y / blockSize) * blockSize;
        float z = Mathf.Round(position.z / blockSize) * blockSize;
        return new Vector3(x, y, z);
    }
}
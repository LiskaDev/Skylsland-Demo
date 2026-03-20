# 项目架构说明 (Architecture Design)

## 1. 核心设计原则
- **组件驱动**：每个功能模块独立为 Script，通过 Inspector 进行参数配置。
- **射线检测 (Raycast)**：主要的交互方式。玩家建造、村民目标指派均基于从相机发出的物理射线。
- **网格系统**：世界坐标强制对齐 (Snap)，基准单位为 1.0f。

## 2. 核心模块详解

### A. 玩家控制层 (`PlayerMovement.cs`)
- **状态维护**：手动维护 `cameraYaw` 避免欧拉角旋转误差。
- **物理处理**：使用 Rigidbody 处理位移，避免穿模。
- **图层约定**：玩家层级应避开射线检测层（Layer 6 为忽略层）。
- **地面检测**：通过 `OnCollisionEnter` 检查碰撞法线 `normal.y > 0.5f` 判定着地。
- **掉落重生**：玩家 Y < -10 时回到出生点。

### B. 建造逻辑层 (`BuildingSystem.cs`)
- **预览机制**：放置前实时生成透明材质的 Ghost Block。
- **材质动态修改**：通过代码将 SharedMaterial 转换为透明模式（Alpha 0.4）。
- **坐标计算**：`Mathf.Round(pos / size) * size` 实现精准对齐。
- **多方块支持**：`blockPrefabs[]` 数组存储多种方块，数字键/滚轮切换。
- **删除功能**：Ctrl+左键通过射线检测删除名称包含 "Block" 的物体。

### C. 纸飞机飞行系统 (`PaperPlane.cs`)
- **上下机**：F 键距离触发。上机时禁用 `PlayerMovement`，玩家绑定到 SeatPoint。
- **飞行控制**：WASD 控制方向/速度，Q/Z 控制升降，滚轮微调高度。
- **速度系统**：有 `minSpeed`/`maxSpeed` 限制，无输入时自动回归中速。
- **起飞保护**：上机后 `takeoffProtectTime` 秒内不判定降落，自动上抬避免贴地。
- **降落机制**：碰撞 `Ground` 标签物体时触发降落，滑行减速后停止。
- **物理管理**：上机解除飞机 `isKinematic`，下机重新锁定。玩家上机时设为 `isKinematic`。
- **旋转处理**：与 PlayerMovement 相同思路，手动维护 `currentYaw`，不从 `eulerAngles` 读取。

### D. 传送系统 (`TeleportPoint.cs`)
- **水晶动画**：旋转 + Sin 函数驱动上下浮动。
- **交互流程**：靠近水晶 → F 键打开面板 → 数字键选目标岛屿 → 传送。
- **安全机制**：不允许传送到当前岛屿，传送前清零玩家速度。
- **每个岛一个实例**：`islandIndex` 标记所在岛，`teleportDestinations[]` 存储所有目标点。

### E. 自动化层 (`VillagerBuilder.cs`)
- **数据结构**：采用 `Vector3[]` 局部坐标数组存储蓝图数据。目前为硬编码方式。
- **三种蓝图**：小屋 (3×3×4)、塔 (2×2×6)、围墙 (6×1×2)。
- **流程控制**：`NavMesh` 处理移动 -> `Coroutine` (协程) 处理异步建造流程。
- **交互范围**：基于 `Vector3.Distance` 的简单距离触发。
- **操作流程**：E 键开蓝图面板 → 数字键选蓝图 → 右键点击地面指定位置 → 村民走过去建造。

## 3. 脚本交互关系

```
PlayerMovement ←──禁用/启用──→ PaperPlane
       │                            │
       │  (共享 Player 的 Rigidbody)  │
       │                            │
       └── 都通过 F 键交互 ──→ TeleportPoint
                                     
BuildingSystem ←── 按键冲突风险 ──→ VillagerBuilder
 (1/2/3 切换方块)                (1/2/3 选蓝图)
```

**关键依赖**：
- `PaperPlane` 在上机时会 **禁用** `PlayerMovement`，下机时 **重新启用**
- `PaperPlane` 会直接操控 Player 的 `Rigidbody.isKinematic`
- `TeleportPoint` 和 `PaperPlane` **都使用 F 键**，靠距离判定区分（不在同一位置时不冲突）

## 4. 按键分配表 (Input Map)

| 按键 | 功能 | 脚本 | 备注 |
|------|------|------|------|
| WASD | 移动 / 飞行方向+速度 | PlayerMovement / PaperPlane | 飞行时 W=加速 S=减速 |
| Space | 跳跃 | PlayerMovement | 需 isGrounded |
| F | 上下飞机 / 传送交互 | PaperPlane / TeleportPoint | 靠距离区分 |
| E | 村民蓝图面板 | VillagerBuilder | |
| Q / Z | 飞机上升 / 下降 | PaperPlane | 仅飞行中 |
| 1 / 2 / 3 | 切换方块 / 选蓝图 / 选传送目标 | BuildingSystem / VillagerBuilder / TeleportPoint | ⚠️ 潜在冲突 |
| 滚轮 | 切换方块 / 微调飞行高度 | BuildingSystem / PaperPlane | |
| 左键 | 放置方块 | BuildingSystem | 非 Ctrl 时 |
| Ctrl+左键 | 删除方块 | BuildingSystem | |
| 右键 | 指定村民建造位置 | VillagerBuilder | |
| Escape | 释放鼠标 | PlayerMovement | |

## 5. 开发规范 (AI 请遵守)
- **变量命名**：私有变量使用 `camelCase`，公共变量使用 `PascalCase`。
- **Inspector 友好**：所有可调参数必须使用 `[SerializeField]`。
- **错误处理**：在涉及 `Raycast` 和 `GetComponent` 时，务必进行空值/碰撞检查。
- **旋转处理**：需要持续旋转的地方，手动维护角度变量，不要从 `eulerAngles` 读取（会有精度漂移）。
- **注释规范**：关键逻辑写中文注释，说明"为什么这样做"而不只是"做了什么"。

## 6. 当前进度
- [x] 玩家移动 + 视角控制
- [x] 方块建造系统（放置/删除/多种类型）
- [x] 纸飞机飞行系统（起飞/飞行/降落）
- [x] 传送水晶系统（跨岛传送）
- [x] 村民 NPC 自动建造（NavMesh寻路 + 蓝图建造）
- [ ] 战斗系统
- [ ] 更多岛屿内容
- [ ] 音效/粒子特效
- [ ] 主菜单/暂停菜单

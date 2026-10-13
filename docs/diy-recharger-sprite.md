# 充能器贴图钳制（Recharger Sprite Clamp）——反汇编取证与决策记录

> 本文件内容由 `ContainerSuite.DIYUpgrade/DiyUpgradeMod.RechargerSprites.cs` 文件头注释逐字搬迁（2026-10-13，T-B4 / T-C4）。
> 源码头部保留摘要与指针；代码里的就地判据注释（`为什么 x < 64`、`必须无条件钳制`）仍留在源码中。

2026-10-07：充能器（recharger）贴图钳制 —— 升级后只显示前 4 颗电池。

═══════════════════════════════════════════════════════════════════════════
问题
═══════════════════════════════════════════════════════════════════════════
用升级容器的功能把充能器网格改大之后，塞进超过原版数量（4 颗）的电池，
机器图标上的电池贴图会一路排到图标外面去。

原版：充能器窗口是 2×2 = 4 格，所以最多 4 颗电池，贴图正好铺满机器图标。

═══════════════════════════════════════════════════════════════════════════
原版贴图是怎么画的（IL2CPP 原生反汇编取证，非伪代码）
═══════════════════════════════════════════════════════════════════════════
MachineRecharger.Recharger() @ RVA 0x564CD0 创建机器时会挂一批回调：
  · 物品 +0x88 = Func<GameItem, List<ItemSpriteModifier>>
      = <>c__DisplayClass1_0.<Recharger>b__4 @ RVA 0x577AB0
  · 物品 +0x168 / 窗口 +0x48/+0x50/+0x60/+0x68 = 充放电与刷新回调

<Recharger>b__4 的循环体（0x577C40..0x577DB9）逐条读出来是：
    var list = new List<ItemSpriteModifier>();
    int i = 0;
    foreach (var item in this.itemInventory.Items) {
        Sprite s = RenderHandler.LoadFromAtlas("Items/items_tool", <item 自己的图标>);
        list.Add(new ItemSpriteModifier(s, new Vector2(i * 16, 0), ...));   // ← mov eax,r14d; shl eax,4
        i++;
    }
    list.Add(顶部覆盖层 "recharger_top", (0,0));                            // 固定 1 条，0x577E09
    if (PlayerStore.Instance.<+0xA4>)                                        // 条件：指示灯
        foreach (...) list.Add("recharger_o"/"recharger_g", (i*16 + 7, 0)); // 0x577F39 起

  ⇒ **每颗电池一条贴图，横向位置 = i × 16**。机器图标宽 4 格 = 64px，
    所以第 5 颗落在 x=64 —— 正好出界。这就是"贴图超过四个"的根因。

消费端（数据流已逐层核实）：
  GameItemElement.HandleInnateModSprite() @ 0x78AFD0
    → 取 this.<+0x88>() 拿到上面那份 list
    → SortedByPriority(list)
    → foreach: UpdateInnateModSprite(i, mod.sprite, mod.position, ...) @ 0x78CE30

  ★ 为什么挂 UpdateInnateModSprite 而不是别的：
    · 它是**非编译器生成**的普通私有方法（名字可直接从 dump.cs 抄，不用猜闭包名）；
    · 字节扫确认**全库只有 1 个调用点**（0x78B189，就在 HandleInnateModSprite 里）；
    · `this` 就是那个 GameItemElement（本身是 GameItem），能判容器 id；
    · 电池的**位置参数直接传进来**，可以按 x 过滤，不必知道电池总数。
    · 反汇编确认 `i` 只被用作 List.get_Item(i) 的下标，不做命名/身份用途
      ⇒ Prefix 返回 false 跳过绘制是安全的（后续下标照常各自建节点）。

  ✗ 另一条 UpdateModSprite（ValidatePartialSprite 内，0x78F14C/0x78F349）走的是
    **+0x98** parentItemSpriteModifierForItemFunc，与充能器无关，**不碰**。

═══════════════════════════════════════════════════════════════════════════
本补丁做什么
═══════════════════════════════════════════════════════════════════════════
Prefix 返回 false = 跳过这一次贴图绘制。判据（顺序即成本顺序）：
  ① pos.x < 64  → 放行（纯托管比较，绝大多数调用走这条，零额外开销）
  ② 不是充能器  → 放行（只有出界贴图才会触发 id 判定）
  ③ 其余        → 跳过（= 只保留 x<64 的：前 4 颗电池 x=0/16/32/48、
                         顶部覆盖层 x=0、前 4 个指示灯 x=7/23/39/55）

★ 只作用于充能器：别的机器（酒架 / 液体容器 / 备菜台）也走这条 innate 通道，
  它们各自的贴图布局我们没核过，不能顺手一起改。
  要扩展到别的容器，往 ClampIds 里加 id 即可（但必须先核那个容器的布局）。

★ 原版（≤4 颗电池）不受影响：所有贴图 x ≤ 55 < 64，一条都不会被跳过。
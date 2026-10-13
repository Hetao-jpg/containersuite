"""静态核查 KeyRenames 迁移表与 CfgKeys 注册表的一致性（不跑游戏也能查出静默丢配置）。"""
import io, re, json, collections

# ── 1. 从 CfgKeys.cs 抽出「当前注册的全部键名」 ─────────────────────────
cfg = io.open('ContainerSuite.Shared/CfgKeys.cs', encoding='utf-8').read()
flat = re.findall(r'public const string (\w+)\s*=\s*"([^"]+)"', cfg)
grid = re.findall(r'public static readonly GridKeys (\w+) = new GridKeys\("([^"]+)", "([^"]+)"\)', cfg)
registered = set(v for _, v in flat)
for _, sortpre, name in grid:
    registered.add(sortpre + name + 'Enable')
    registered.add(sortpre + name + 'MaxW')
    registered.add(sortpre + name + 'MaxH')
    registered.add(name + 'Ids')
# AlignSlots 只在 slotCount>1 时创建（多槽 / 模组槽）
for _, sortpre, name in grid:
    if name in ('MultiSlot', 'MultiSlotModule'):
        registered.add(sortpre + name + 'AlignSlots')

# ── 2. 从 Rename.cs 抽出迁移表 ──────────────────────────────────────────
ren = io.open('ContainerSuite.DIYUpgrade/DiyUpgrade.CfgMigration.Rename.cs', encoding='utf-8').read()
m = re.search(r'KeyRenames = new Dictionary<string, string>\(StringComparer\.Ordinal\)\s*\{(.*?)\n    \};', ren, re.S)
table = dict((a, b) for a, b in re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]+)"\s*\}', m.group(1)))

removed = set(re.findall(r'"([\w]+)"', re.search(r'RemovedKeys = new HashSet<string>\(StringComparer\.Ordinal\)\s*\{(.*?)\n    \};', ren, re.S).group(1)))

print('注册键名数 =', len(registered))
print('迁移表条数 =', len(table))
print('已删键     =', sorted(removed))

err = []

# ① 每个迁移目标必须真实存在（否则 FindEntry 返回 null → 该项「未搬动」）
bad_dst = sorted({d for d in table.values() if d not in registered})
print('\n[检查1] 迁移目标不在注册表里（= 该旧键的值会被静默丢弃）:', bad_dst or '无')
if bad_dst: err.append('bad_dst')

# ② 不能有 A→B 且 B→C 的链（B 既是源又是目标，会被 IsDroppedKey 当成旧键删掉）
both = sorted(set(table) & set(table.values()))
print('[检查2] 既做源又做目标（链式，会导致值丢失）:', both or '无')
if both: err.append('chain')

# ③ 同一目标被多个旧键映射 → 后者覆盖前者（语义 OK，但要看得见）
dup = [k for k, v in collections.Counter(table.values()).items() if v > 1]
print('[检查3] 多个旧键指向同一个新键（策略：后者覆盖）:')
for d in sorted(dup):
    print('     ', d, '<-', sorted(k for k, v in table.items() if v == d))

# ④ 自映射不允许
print('[检查4] 自映射:', [k for k, v in table.items() if k == v] or '无')
if [k for k, v in table.items() if k == v]: err.append('self')

# ⑤ 玩家 cfg 里现存的键 ── 用真实 cfg 模拟一次迁移
real = io.open(r'D:\SteamLibrary\steamapps\common\Probably Stolen Demo\UserData\MelonPreferences.cfg',
               encoding='utf-8', errors='replace').read()
sec, keys = None, []
for line in real.split('\n'):
    t = line.strip()
    if t.startswith('#') or t.startswith(';') or not t:
        continue
    if t.startswith('[') and t.endswith(']'):
        sec = t[1:-1]; continue
    if sec != 'CustomInfrastructure': continue
    eq = t.find('=')
    if eq > 0: keys.append(t[:eq].strip().strip('"'))
print('\n[检查5] 真实 cfg [CustomInfrastructure] 现有 %d 个键' % len(keys))

moved, unknown, dropped = {}, [], []
for k in keys:
    if k in table:
        moved[k] = table[k]
    elif k in removed:
        dropped.append(k)
    elif k in registered:
        pass          # 已经是新键名
    else:
        unknown.append(k)
print('   可迁移（旧名→新名）: %d 个' % len(moved))
print('   已是新键名        : %d 个' % (len(keys) - len(moved) - len(dropped) - len(unknown)))
print('   将被清理（已删键）:', dropped)
print('   ⚠ 既不在迁移表、也不在注册表（孤儿键，会被留下）:', unknown)

# 迁移后的文件名重复检查
after = [table.get(k, k) for k in keys if k not in removed]
dups = [k for k, v in collections.Counter(after).items() if v > 1]
print('   ⚠ 迁移后出现同名重复:', dups or '无')
if dups: err.append('dup-after')

# ⑥ 注册表里有哪些键「cfg 里没有」→ 说明是本次新增或玩家从未触发过
missing = sorted(k for k in registered if k not in after)
print('\n[检查6] 迁移后仍未写入 cfg 的注册键（新增项 / 按需创建项）:')
print('   ', missing)

print('\n==== 结论:', '❌ 有问题 ' + str(err) if err else '✅ 一致性检查全部通过')

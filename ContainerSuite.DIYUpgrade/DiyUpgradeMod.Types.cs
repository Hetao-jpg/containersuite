// 职责：小型数据类型（Off / Cat / TargetSlot / GridDef / PrepareCacheEntry）

using ContainerSuite.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    private static class Off
    {
        // S0 数据校对结果：state 字段在 0x1B0（十进制 432），
        // 原值 440（0x1B8）指向的是 modifiedState。
        // 这是 S1 决策文档 §2.3 标记的 🔴 问题。
        public const int TagSystem = 432;
        public const int Id = 448;
        public const int DragSource = 80;
        public const int DragTarget = 136;
        public const int DragTargetInv = 144;
    }

    internal enum Cat { None, Box, Backpack, Cage, MultiSlot, MultiSlotModule, ModContainer, WageBox, WageBackpack }

    internal sealed class TargetSlot
    {
        public MelonPreferences_Entry<bool> Enable;
        public MelonPreferences_Entry<int> IncW;
        public MelonPreferences_Entry<int> IncH;
    }

    internal sealed class GridDef
    {
        public MelonPreferences_Entry<bool> Enable;
        public MelonPreferences_Entry<string> Ids;
        public MelonPreferences_Entry<int> MaxW;
        public MelonPreferences_Entry<int> MaxH;
        /// <summary>多槽容器专用：所有槽位尺寸跟随第一个槽（仅 slotCount &gt; 1 时创建）。</summary>
        public MelonPreferences_Entry<bool> AlignSlots;
        public TargetSlot[] Slots = Array.Empty<TargetSlot>();
        public string[] SlotNames;
        public string[] IdList = Array.Empty<string>();
    }

    private sealed class PrepareCacheEntry
    {
        /// <summary>T1（2026-10-12）：单调毫秒时间戳（原 DateTime）——见 Shared/PollThrottle.cs。</summary>
        public long Time;
        public bool Can;
        public string Reason;
    }
}
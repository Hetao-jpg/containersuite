using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WaterOptimizer.Core;
using WaterOptimizer.Mod;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Entry), "Auto Module Layout Optimizer (formerly WaterOptimizer)", "2.2.0", "Codex", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = "")]
[assembly: AssemblyCompany("WaterOptimizer.Mod")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyDescription("Automatic module layout optimization for Probably Stolen.")]
[assembly: AssemblyFileVersion("2.2.0.0")]
[assembly: AssemblyInformationalVersion("2.2.0")]
[assembly: AssemblyProduct("Auto Module Layout Optimizer (formerly WaterOptimizer)")]
[assembly: AssemblyTitle("WaterOptimizer.Mod")]
[assembly: AssemblyVersion("2.2.0.0")]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableAttribute : Attribute
	{
		public readonly byte[] NullableFlags;

		public NullableAttribute(byte P_0)
		{
			NullableFlags = new byte[1] { P_0 };
		}

		public NullableAttribute(byte[] P_0)
		{
			NullableFlags = P_0;
		}
	}
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableContextAttribute : Attribute
	{
		public readonly byte Flag;

		public NullableContextAttribute(byte P_0)
		{
			Flag = P_0;
		}
	}
}
namespace WaterOptimizer.Core
{
	public sealed record BatteryNeed(int MachineId, int OldBatteryId, int Demand, int Priority);
	public sealed record SpareBattery(int Id, int Energy, bool Charging);
	public sealed record BatteryAssignment(int MachineId, int BatteryId);
	public static class BatteryMatching
	{
		public static BatteryAssignment[] Match(IEnumerable<BatteryNeed> needs, IEnumerable<SpareBattery> batteries, Func<int, int, bool> fits)
		{
			BatteryNeed[] array = (from n in needs
				where n.Demand > 0
				orderby n.Priority, n.Demand descending, n.MachineId
				select n).ToArray();
			HashSet<int> installed = array.Select((BatteryNeed n) => n.OldBatteryId).ToHashSet();
			SpareBattery[] pool = (from b in batteries
				where !installed.Contains(b.Id)
				orderby b.Charging descending, b.Energy, b.Id
				select b).ToArray();
			Dictionary<int, BatteryNeed> byMachine = array.ToDictionary((BatteryNeed n) => n.MachineId);
			Dictionary<int, int> owners = new Dictionary<int, int>();
			Dictionary<int, int[]> edges = array.ToDictionary((BatteryNeed n) => n.MachineId, (BatteryNeed n) => (from b in pool
				where b.Id != n.OldBatteryId && b.Energy >= n.Demand && fits(n.MachineId, b.Id)
				select b.Id).ToArray());
			BatteryNeed[] array2 = array;
			foreach (BatteryNeed batteryNeed in array2)
			{
				Assign(batteryNeed.MachineId, new HashSet<int>());
			}
			return (from kv in owners
				select new BatteryAssignment(kv.Value, kv.Key) into a
				orderby byMachine[a.MachineId].Priority, a.MachineId
				select a).ToArray();
			bool Assign(int machine, HashSet<int> seen)
			{
				int[] array3 = edges[machine];
				foreach (int num2 in array3)
				{
					if (seen.Add(num2) && (!owners.TryGetValue(num2, out var value) || Assign(value, seen)))
					{
						owners[num2] = machine;
						return true;
					}
				}
				return false;
			}
		}
	}
	public static class Cultivation
	{
		public static Stats Remaining(Module m)
		{
			Stats learningMax = m.LearningMax;
			if ((object)learningMax == null)
			{
				return Stats.Zero;
			}
			return new Stats(Math.Max(0, learningMax.P - m.Base.P), Math.Max(0, learningMax.E - m.Base.E), Math.Max(0, learningMax.Q - m.Base.Q));
		}

		public static bool Candidate(Module m)
		{
			if (m.IsLearning && !m.Unstable)
			{
				Stats learningMax = m.LearningMax;
				if ((object)learningMax != null && Remaining(m) != Stats.Zero)
				{
					if (Math.Max(learningMax.P, m.Base.P) <= 0 && Math.Max(learningMax.E, m.Base.E) <= 0)
					{
						return Math.Max(learningMax.Q, m.Base.Q) > 0;
					}
					return true;
				}
			}
			return false;
		}

		public static int Cycles(Module m)
		{
			Stats stats = Remaining(m);
			return Math.Max(stats.P, Math.Max(stats.E, stats.Q));
		}

		public static long Value(Module m)
		{
			if (!Candidate(m))
			{
				return 0L;
			}
			Stats learningMax = m.LearningMax;
			int num = Math.Max(1, m.Variants.FirstOrDefault()?.Cells.Length ?? 1);
			return (4L * (long)Math.Max(0, learningMax.Q) + 2L * (long)Math.Max(0, learningMax.P) + Math.Max(0, learningMax.E) + Cycles(m)) * 100 / num;
		}

		public static (int Count, long Value) Score(Snapshot s, IEnumerable<Placement> ps)
		{
			HashSet<int> ids = ps.Select((Placement p) => p.ModuleId).ToHashSet();
			Module[] array = s.Modules.Where((Module m) => Candidate(m) && ids.Contains(m.Id)).ToArray();
			return (Count: array.Length, Value: ((IEnumerable<Module>)array).Sum((Func<Module, long>)Value));
		}

		public static IEnumerable<Placement> Options(Snapshot s, Module m)
		{
			Machine[] machines = s.Machines;
			foreach (Machine b in machines)
			{
				if (b.Locked)
				{
					continue;
				}
				HashSet<Cell> available = b.Available.ToHashSet();
				HashSet<string> seen = new HashSet<string>();
				Pose[] variants = m.Variants;
				foreach (Pose v in variants)
				{
					for (int y = 0; y < b.Height; y++)
					{
						int x;
						for (x = 0; x < b.Width; x++)
						{
							Cell[] array = v.Cells.Select((Cell c) => new Cell(c.X + x, c.Y + y)).ToArray();
							if (array.All(available.Contains))
							{
								string item = $"{x},{y}:" + string.Join(";", from c in array
									orderby c.Y, c.X
									select $"{c.X},{c.Y}");
								if (seen.Add(item))
								{
									yield return new Placement(m.Id, b.Id, new Pose(x, y, v.Rotation, v.Flipped, array));
								}
							}
						}
					}
				}
			}
		}

		public static bool Overlap(Placement a, Placement b)
		{
			if (a.MachineId == b.MachineId)
			{
				return a.Pose.Cells.Any(((IEnumerable<Cell>)b.Pose.Cells).Contains<Cell>);
			}
			return false;
		}

		public static bool Better(Snapshot s, Placement[] a, Placement[] b)
		{
			return LayoutObjective.Value(s, a).CompareTo(LayoutObjective.Value(s, b)) > 0;
		}

		public static Placement[] Improve(Snapshot s, Placement[] original, bool stable, IReadOnlyDictionary<int, List<Placement>> options, TimeSpan budget, CancellationToken cancel)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			Random rng = new Random(9921);
			Dictionary<int, Module> byId = s.Modules.ToDictionary((Module module) => module.Id);
			Placement[] array = original;
			HashSet<int> productive = (from r in Rules.Evaluate(s, original)
				where r.QualityVolume > 0
				select r.Id).ToHashSet();
			int num = Rules.Evaluate(s, original).Sum((MachineResult r) => r.QualityVolume);
			Module[] source = s.Modules.Where((Module module) => Candidate(module) && module.Movable && (!stable || !module.Unstable)).ToArray();
			int pass;
			for (pass = 0; pass < 16; pass++)
			{
				if (cancel.IsCancellationRequested)
				{
					break;
				}
				if (!(stopwatch.Elapsed < budget))
				{
					break;
				}
				List<Placement> state = original.Where((Placement placement2) => productive.Contains(placement2.MachineId) || !byId[placement2.ModuleId].Movable || (pass == 0 && Candidate(byId[placement2.ModuleId]))).ToList();
				Module[] array2 = ((pass < 2) ? source.OrderByDescending((Module module) => module.Variants[0].Cells.Length).ThenByDescending(Value).ToArray() : source.OrderBy((Module _) => rng.Next()).ToArray());
				foreach (Module m in array2)
				{
					if (state.Any((Placement placement2) => placement2.ModuleId == m.Id))
					{
						continue;
					}
					Placement placement = null;
					double num3 = double.NegativeInfinity;
					foreach (Placement p in options[m.Id])
					{
						if (cancel.IsCancellationRequested || stopwatch.Elapsed >= budget)
						{
							break;
						}
						if (state.Any((Placement q) => Overlap(p, q)))
						{
							continue;
						}
						Placement[] placements = state.Append(p).ToArray();
						MachineResult[] source2 = Rules.Evaluate(s, placements);
						int num4 = source2.Sum((MachineResult r) => r.QualityVolume);
						if (num4 >= num)
						{
							double num5 = (double)num4 * 1000.0 + (double)((!productive.Contains(p.MachineId)) ? 100 : 0) - (double)p.Pose.Y * 0.1 - (double)p.Pose.X * 0.01 - (double)source2.Sum((MachineResult r) => r.Power) * 0.001;
							if (num5 > num3)
							{
								num3 = num5;
								placement = p;
							}
						}
					}
					if (placement != null)
					{
						state.Add(placement);
					}
					if (stopwatch.Elapsed >= budget)
					{
						break;
					}
				}
				Placement[] array3 = state.ToArray();
				if (Better(s, array3, array))
				{
					array = array3;
				}
				foreach (Placement p2 in original.Where((Placement placement2) => !state.Any((Placement q) => q.ModuleId == placement2.ModuleId)))
				{
					if (!state.Any((Placement q) => Overlap(p2, q)))
					{
						Placement[] a = state.Append(p2).ToArray();
						if (Better(s, a, state.ToArray()))
						{
							state.Add(p2);
						}
					}
				}
				array3 = state.ToArray();
				if (Better(s, array3, array))
				{
					array = array3;
				}
				if (Score(s, array).Count == s.Modules.Count(Candidate))
				{
					break;
				}
			}
			Rules.Validate(s, array, stable);
			return array;
		}
	}
	public sealed record ExternalBonusEntry(string Identity, int MachineId, string Kind, int Performance, int Quality, int Efficiency = 0);
	public sealed class ExternalBonusSettings
	{
		public ExternalBonusEntry[] Entries { get; set; } = Array.Empty<ExternalBonusEntry>();

		public Stats Get(string identity, int id, string kind)
		{
			ExternalBonusEntry externalBonusEntry = Entries.LastOrDefault((ExternalBonusEntry e) => e.Identity == identity && e.MachineId == id && MachineProfiles.CanonicalKind(e.Kind) == MachineProfiles.CanonicalKind(kind));
			if (!(externalBonusEntry == null))
			{
				return new Stats(externalBonusEntry.Performance, externalBonusEntry.Efficiency, externalBonusEntry.Quality);
			}
			return Stats.Zero;
		}

		public void Set(string identity, int id, string kind, int performance, int quality)
		{
			Set(identity, id, kind, performance, 0, quality);
		}

		public void Set(string identity, int id, string kind, int performance, int efficiency, int quality)
		{
			if (Math.Abs((long)performance) > 10000 || Math.Abs((long)efficiency) > 10000 || Math.Abs((long)quality) > 10000)
			{
				throw new InvalidOperationException("额外加成请填写 -10000 到 10000 的整数百分比。");
			}
			Entries = Entries.Where((ExternalBonusEntry e) => !(e.Identity == identity) || e.MachineId != id).ToArray();
			if (performance != 0 || efficiency != 0 || quality != 0)
			{
				Entries = Entries.Append(new ExternalBonusEntry(identity, id, MachineProfiles.CanonicalKind(kind), performance, quality, efficiency)).ToArray();
			}
		}
	}
	public static class ExternalBonuses
	{
		public static Stats Remove(Stats total, Stats bonus)
		{
			return new Stats(total.P - bonus.P, total.E - bonus.E, total.Q - bonus.Q);
		}

		public static bool Matches(Stats observed, Stats modules, Stats bonus)
		{
			if (Match(observed.P, modules.P, bonus.P) && Match(observed.E, modules.E, bonus.E))
			{
				return Match(observed.Q, modules.Q, bonus.Q);
			}
			return false;
		}

		private static bool Match(int observed, int modules, int bonus)
		{
			if (observed != modules)
			{
				return observed == modules + bonus;
			}
			return true;
		}

		public static Stats Effective(Stats observed, Stats modules, Stats bonus)
		{
			return new Stats(AddOnce(observed.P, modules.P, bonus.P), AddOnce(observed.E, modules.E, bonus.E), AddOnce(observed.Q, modules.Q, bonus.Q));
		}

		private static int AddOnce(int observed, int modules, int bonus)
		{
			if (observed != modules + bonus)
			{
				return observed + bonus;
			}
			return observed;
		}
	}
	public sealed class GlobalSolver
	{
		public Plan Solve(Snapshot snapshot, bool stable, TimeSpan budget, CancellationToken cancel, Action<Plan>? progress = null, int seed = 711)
		{
			Stopwatch watch = Stopwatch.StartNew();
			Random random = new Random(seed);
			Module[] mods = snapshot.Modules;
			if (snapshot.Machines.Length == 0)
			{
				throw new InvalidOperationException("未发现可优化的机器");
			}
			List<Placement>[] options = new List<Placement>[mods.Length];
			for (int i = 0; i < mods.Length; i++)
			{
				options[i] = new List<Placement>();
				Module m = mods[i];
				if (stable && m.Unstable)
				{
					continue;
				}
				if (!m.Movable)
				{
					Placement placement = snapshot.Current.FirstOrDefault((Placement p) => p.ModuleId == m.Id);
					if (placement != null)
					{
						options[i].Add(placement);
					}
				}
				else
				{
					options[i].AddRange(Cultivation.Options(snapshot, m));
				}
			}
			Placement[] array = mods.Select((Module module) => snapshot.Current.FirstOrDefault((Placement p) => p.ModuleId == module.Id && (!stable || !module.Unstable))).ToArray();
			Placement[] array2 = array.ToArray();
			Placement[] best = array.ToArray();
			Rules.Validate(snapshot, best.OfType<Placement>(), stable);
			MachineResult[] bestResults = Rules.Evaluate(snapshot, best.OfType<Placement>());
			int bestOutput = bestResults.Sum((MachineResult x) => x.QualityVolume);
			bool[] trainable = mods.Select(Cultivation.Candidate).ToArray();
			long[] values = mods.Select(Cultivation.Value).ToArray();
			(int Count, long Value) bestTraining = Training(best);
			LayoutValue bestValue = LayoutObjective.Value(snapshot, bestResults, bestTraining, best.Count((Placement p) => p != null));
			long iteration = 0L;
			double num = 0.0;
			double num2 = 0.0;
			double num3 = Score(array, 0);
			double num4 = 1.0;
			List<Placement>[] array3 = options;
			foreach (List<Placement> list in array3)
			{
				num4 *= (double)(list.Count + 1);
			}
			bool complete;
			Placement?[] arr;
			if (num4 <= 250000.0 && mods.All((Module module) => module.Movable))
			{
				complete = true;
				arr = new Placement[mods.Length];
				Visit(0);
				Prune();
				return Make(complete);
			}
			Prune();
			Dictionary<int, List<Placement>> optionMap = mods.Select((Module module, int num14) => (Id: module.Id, Options: options[num14])).ToDictionary(((int Id, List<Placement> Options) x) => x.Id, ((int Id, List<Placement> Options) x) => x.Options);
			double num6 = Math.Min(3.0, budget.TotalSeconds * 0.2);
			ImproveTraining(TimeSpan.FromSeconds(num6 * 0.3));
			FillUnused(TimeSpan.FromSeconds(num6 * 0.7));
			array = best.ToArray();
			array2 = best.ToArray();
			num3 = Score(array, 0);
			while (watch.Elapsed.TotalSeconds < budget.TotalSeconds - num6 && !cancel.IsCancellationRequested)
			{
				iteration++;
				double totalSeconds = watch.Elapsed.TotalSeconds;
				if (totalSeconds - num2 > 4.0)
				{
					array = ((random.NextDouble() < 0.8) ? best : array2).ToArray();
					if (random.NextDouble() < 0.3)
					{
						for (int num7 = 0; num7 < array.Length; num7++)
						{
							if (mods[num7].Movable && random.NextDouble() < 0.2)
							{
								array[num7] = null;
							}
						}
					}
					num2 = totalSeconds;
					num3 = Score(array, (int)(totalSeconds / 4.0) % 3);
				}
				int mode = (int)(num2 / 4.0) % 3;
				int num8 = random.Next(mods.Length);
				if (!mods[num8].Movable || options[num8].Count == 0)
				{
					continue;
				}
				Placement[] trial = array.ToArray();
				if (random.NextDouble() < 0.08)
				{
					trial[num8] = null;
				}
				else
				{
					Placement chosen = options[num8][random.Next(options[num8].Count)];
					Placement old = trial[num8];
					trial[num8] = null;
					int[] array4 = (from j in Enumerable.Range(0, trial.Length)
						where trial[j] != null && Overlap(chosen, trial[j])
						select j).ToArray();
					if (array4.Any((int j) => !mods[j].Movable))
					{
						continue;
					}
					int[] array5 = array4;
					foreach (int num9 in array5)
					{
						trial[num9] = null;
					}
					trial[num8] = chosen;
					if (old != null && array4.Length == 1 && random.NextDouble() < 0.8)
					{
						int num10 = array4[0];
						Placement[] array6 = options[num10].Where((Placement p) => p.MachineId == old.MachineId).ToArray();
						for (int num11 = 0; num11 < 12; num11++)
						{
							if (array6.Length == 0)
							{
								break;
							}
							Placement q = array6[random.Next(array6.Length)];
							if (trial.OfType<Placement>().All((Placement p) => !Overlap(p, q)))
							{
								trial[num10] = q;
								break;
							}
						}
					}
				}
				double num12 = Score(trial, mode);
				double num13 = 0.025 + 0.6 * Math.Pow(1.0 - Math.Clamp((totalSeconds - num2) / 4.0, 0.0, 1.0), 2.0);
				if (num12 >= num3 || random.NextDouble() < Math.Exp((num12 - num3) / num13))
				{
					array = trial;
					num3 = num12;
				}
				Consider(trial);
				if (totalSeconds - num > 1.0)
				{
					progress?.Invoke(Make(proven: false));
					num = totalSeconds;
				}
			}
			ImproveTraining(TimeSpan.FromSeconds(Math.Max(0.0, (budget - watch.Elapsed).TotalSeconds * 0.3)));
			Prune();
			FillUnused(budget - watch.Elapsed);
			Prune();
			return Make(proven: false);
			void Consider(Placement?[] state)
			{
				MachineResult[] array7 = Rules.Evaluate(snapshot, state.OfType<Placement>());
				int num14 = array7.Sum((MachineResult x) => x.QualityVolume);
				(int, long) tuple = Training(state);
				LayoutValue layoutValue = LayoutObjective.Value(snapshot, array7, tuple, state.Count((Placement p) => p != null));
				if (layoutValue.CompareTo(bestValue) > 0)
				{
					best = state.ToArray();
					bestOutput = num14;
					bestResults = array7;
					bestTraining = tuple;
					bestValue = layoutValue;
				}
			}
			void FillUnused(TimeSpan remaining)
			{
				if (!(remaining <= TimeSpan.Zero) && !cancel.IsCancellationRequested)
				{
					Dictionary<int, Placement> ps = WaterAllocation.Fill(snapshot, best.OfType<Placement>().ToArray(), stable, optionMap, remaining, cancel).ToDictionary((Placement p) => p.ModuleId);
					Consider(mods.Select((Module module) => ps.GetValueOrDefault(module.Id)).ToArray());
				}
			}
			void ImproveTraining(TimeSpan remaining)
			{
				if (!(snapshot.Supply != null) && !(remaining <= TimeSpan.Zero) && !cancel.IsCancellationRequested && trainable.Any((bool x) => x))
				{
					Dictionary<int, Placement> ps = Cultivation.Improve(snapshot, best.OfType<Placement>().ToArray(), stable, optionMap, remaining, cancel).ToDictionary((Placement p) => p.ModuleId);
					Consider(mods.Select((Module module) => ps.GetValueOrDefault(module.Id)).ToArray());
				}
			}
			Plan Make(bool proven)
			{
				Placement[] placements = best.OfType<Placement>().ToArray();
				Rules.Validate(snapshot, placements, stable);
				return new Plan(snapshot.Fingerprint, stable, placements, bestResults, bestOutput, iteration, proven, watch.Elapsed.TotalSeconds);
			}
			void Prune()
			{
				bool flag = true;
				while (flag)
				{
					flag = false;
					for (int j = 0; j < mods.Length; j++)
					{
						if (mods[j].Movable && !(best[j] == null))
						{
							Placement[] array7 = best.ToArray();
							array7[j] = null;
							LayoutValue b = bestValue;
							Consider(array7);
							if (bestValue.CompareTo(b) > 0)
							{
								flag = true;
							}
						}
					}
				}
			}
			double Score(Placement?[] state, int num14)
			{
				MachineResult[] array7 = Rules.Evaluate(snapshot, state.OfType<Placement>());
				if (snapshot.Supply != null)
				{
					return LayoutObjective.Smooth(snapshot, array7) + (double)Training(state).Count * 0.0001;
				}
				return array7.Sum((MachineResult r) => (r.Stats.Q < 100) ? (((num14 == 0) ? 0.92 : ((num14 == 1) ? 0.75 : 1.05)) * (double)r.Volume / 1000000.0 * Math.Pow(Math.Clamp((double)r.Stats.Q / 100.0, 0.0, 1.0), (num14 == 2) ? 4 : 2)) : ((double)r.Volume / 1000000.0)) + (double)array7.Sum((MachineResult r) => Math.Clamp(r.Stats.Q, -100, 150)) * 0.0005 + (double)Training(state).Count * 0.04;
			}
			(int Count, long Value) Training(Placement?[] state)
			{
				int num14 = 0;
				long num15 = 0L;
				for (int j = 0; j < state.Length; j++)
				{
					if (state[j] != null && trainable[j])
					{
						num14++;
						num15 += values[j];
					}
				}
				return (Count: num14, Value: num15);
			}
			void Visit(int at)
			{
				if (cancel.IsCancellationRequested || watch.Elapsed > budget)
				{
					complete = false;
				}
				else if (at == mods.Length)
				{
					iteration++;
					Consider(arr);
				}
				else
				{
					arr[at] = null;
					Visit(at + 1);
					foreach (Placement p in options[at])
					{
						if (!complete)
						{
							break;
						}
						if (!arr.Take(at).OfType<Placement>().Any((Placement b) => Overlap(p, b)))
						{
							arr[at] = p;
							Visit(at + 1);
						}
					}
					arr[at] = null;
				}
			}
		}

		private static bool Overlap(Placement a, Placement b)
		{
			if (a.MachineId == b.MachineId)
			{
				return a.Pose.Cells.Any(((IEnumerable<Cell>)b.Pose.Cells).Contains<Cell>);
			}
			return false;
		}
	}
	public sealed record NumberedItem(string Save, string Group, int Id, int Number);
	public sealed record NumberCandidate(int Id, string Kind, bool Machine);
	public sealed class ItemNumbers
	{
		public List<NumberedItem> Entries { get; set; } = new List<NumberedItem>();

		public bool Register(string save, IEnumerable<NumberCandidate> items)
		{
			bool result = false;
			foreach (NumberCandidate item in items.OrderBy((NumberCandidate x) => x.Id))
			{
				string group = (item.Machine ? MachineProfiles.CanonicalKind(item.Kind) : "$modules");
				if (!Entries.Any((NumberedItem e) => e.Save == save && e.Group == group && e.Id == item.Id))
				{
					int number = (from e in Entries
						where e.Save == save && e.Group == @group
						select e.Number).DefaultIfEmpty(0).Max() + 1;
					Entries.Add(new NumberedItem(save, group, item.Id, number));
					result = true;
				}
			}
			return result;
		}

		public string? Label(string save, int id, string kind, bool machine)
		{
			string group = (machine ? MachineProfiles.CanonicalKind(kind) : "$modules");
			NumberedItem numberedItem = Entries.FirstOrDefault((NumberedItem e) => e.Save == save && e.Group == group && e.Id == id);
			if (!(numberedItem == null))
			{
				if (machine)
				{
					return $"{numberedItem.Number:00}号";
				}
				return $"M{numberedItem.Number:000}";
			}
			return null;
		}
	}
	public static class ModuleKinds
	{
		public static bool NeuralCore(string kind)
		{
			if (!(kind == "system_capped_neural_core"))
			{
				return kind == "system_uncapped_neural_core";
			}
			return true;
		}

		public static bool General(string kind, bool tagged, string type)
		{
			if (!NeuralCore(kind) && !kind.StartsWith("node_"))
			{
				if (tagged)
				{
					return type == "MODULE_TYPE_UNIVERSAL";
				}
				return false;
			}
			return true;
		}
	}
	public sealed record MachineProfile(string Kind, string Name, string Attributes, string Explanation, int? QualityLimit = null, int? PerformanceLimit = null);
	public sealed record SupplyPreference(string Kind, string Attributes);
	public sealed record SupplySettings
	{
		public SupplyPreference[] Order { get; init; } = Array.Empty<SupplyPreference>();

		public SupplyPreference[] Resolve()
		{
			SupplyPreference[] saved = (from x in Order ?? Array.Empty<SupplyPreference>()
				where MachineProfiles.Find(x.Kind) != null
				select new SupplyPreference(MachineProfiles.CanonicalKind(x.Kind), MachineProfiles.Normalize(x.Kind, x.Attributes))).DistinctBy((SupplyPreference x) => x.Kind).ToArray();
			return saved.Concat(from p in MachineProfiles.All
				where !saved.Any((SupplyPreference x) => x.Kind == p.Kind)
				select new SupplyPreference(p.Kind, p.Attributes)).ToArray();
		}
	}
	public static class MachineProfiles
	{
		public static readonly MachineProfile[] All = new MachineProfile[10]
		{
			new MachineProfile("moisture_farm", "水分收集器", "QPE", "排第一时：优质水量 → 基础水量 → 所选属性顺序。性能决定水量；能效降低功耗。", 150),
			new MachineProfile("purifier", "净水器", "QPE", "质量改善净化下限（最高 Q 200）；性能提高每晚去除量；能效降低功耗。", 200),
			new MachineProfile("mirage_projector", "幻影投影仪", "QPE", "性能与质量均增加吸引力；能效降低功耗。"),
			new MachineProfile("furnace", "熔炉", "QE", "质量提高金属纯度；能效降低功耗；性能无效。原料、助熔剂及专用模组影响实际结果。"),
			new MachineProfile("wine_rack", "催陈器", "QPE", "每 125 质量增加 1 天催陈；性能和能效均用于节电。"),
			new MachineProfile("alarm_system", "警报器", "PE", "性能提高防盗率（100% 后不再奖励）；能效降低功耗；质量无效。", null, 50),
			new MachineProfile("desequencer", "解序器", "PE", "性能提高处理速度；能效降低功耗；质量无效。"),
			new MachineProfile("printer", "3D 打印机", "QPE", "质量影响支持品质的打印品；性能提高速度；能效降低功耗。专用芯片保留。"),
			new MachineProfile("hydroponic", "水培系统", "QPE", "质量提高最大产出；性能提高生长速度；能效降低功耗。种子及专用模组保留。"),
			new MachineProfile("evaporator", "蒸发器", "QPE", "质量影响捕获蒸汽量；性能影响蒸发量；能效降低功耗。")
		};

		public static string CanonicalKind(string kind)
		{
			if (!(kind == "projector"))
			{
				return kind;
			}
			return "mirage_projector";
		}

		public static MachineProfile? Find(string kind)
		{
			return All.FirstOrDefault((MachineProfile p) => p.Kind == CanonicalKind(kind));
		}

		public static string Normalize(string kind, string attributes)
		{
			string text = Find(kind)?.Attributes ?? "";
			return new string((attributes + text).Where(text.Contains).Distinct().ToArray());
		}

		public static string StatName(char c)
		{
			return c switch
			{
				'P' => "性能", 
				'Q' => "质量", 
				_ => "能效", 
			};
		}

		public static string OrderName(string order)
		{
			return string.Join(" → ", order.Select(StatName));
		}

		public static double PurifierResidual(int q)
		{
			if (q < 200)
			{
				if (q < 100)
				{
					if (q < 75)
					{
						if (q < 50)
						{
							if (q < 25)
							{
								if (q < 15)
								{
									return 1.0;
								}
								return 0.85;
							}
							return 0.65;
						}
						return 0.35;
					}
					return 0.2;
				}
				return 0.1;
			}
			return 0.0;
		}

		public static int Power(Machine m, Stats s)
		{
			int? efficiencyRatio = m.EfficiencyRatio;
			int num = ((efficiencyRatio.HasValue && efficiencyRatio.GetValueOrDefault() > 0) ? ((int)((float)m.BasePower - (float)(m.BasePower * s.E / m.EfficiencyRatio.Value) / 100f)) : (m.BasePower - m.BasePower * s.E / 100));
			if (m.PerformanceReducesPower)
			{
				efficiencyRatio = m.PerformanceRatio;
				if (efficiencyRatio.HasValue && efficiencyRatio.GetValueOrDefault() > 0)
				{
					num += (int)((float)(m.BasePower * s.P / m.PerformanceRatio.Value) / -100f);
				}
			}
			if (!m.ClampPower)
			{
				return num;
			}
			return Math.Max(1, num);
		}

		public static int Speed(Machine m, Stats s)
		{
			int? performanceRatio = m.PerformanceRatio;
			if (!performanceRatio.HasValue || performanceRatio.GetValueOrDefault() <= 0)
			{
				return m.BaseSpeed + m.BaseSpeed * s.P / 100;
			}
			return (int)((float)m.BaseSpeed + (float)(m.BaseSpeed * s.P / m.PerformanceRatio.Value) / 100f);
		}

		public static double Attribute(Machine m, Stats s, char c)
		{
			MachineProfile machineProfile = Find(m.Kind);
			if (machineProfile == null || !machineProfile.Attributes.Contains(c))
			{
				return 0.0;
			}
			switch (c)
			{
			case 'E':
				return -Power(m, s);
			case 'P':
				if (m.PerformanceReducesPower)
				{
					return -Power(m, s);
				}
				if (m.Kind == "moisture_farm")
				{
					return Math.Max(0, 100 + s.P);
				}
				if (m.Kind == "alarm_system")
				{
					return Math.Clamp(50 + s.P, 0, 100);
				}
				if (m.BaseSpeed > 0)
				{
					return Math.Max(0, Speed(m, s));
				}
				return (double)Math.Max(-100, s.P) / (double)Math.Max(1, m.PerformanceRatio ?? 1);
			default:
			{
				int? num = m.QualityLimit ?? machineProfile.QualityLimit;
				int num2;
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					num2 = Math.Min(valueOrDefault, s.Q);
				}
				else
				{
					num2 = s.Q;
				}
				int num3 = num2;
				if (m.Kind == "wine_rack")
				{
					return (double)Math.Max(0, num3) / 125.0;
				}
				return (double)num3 / (double)Math.Max(1, m.QualityRatio ?? 1);
			}
			}
		}

		public static string Describe(Machine m, MachineResult r)
		{
			return CanonicalKind(m.Kind) switch
			{
				"moisture_farm" => $"日产 {(double)r.Volume / 1000000.0:0.00} L · {WaterAllocation.Grade(r.Stats.Q)} · 优质水 {(double)r.QualityVolume / 1000000.0:0.00} L", 
				"mirage_projector" => $"吸引力加成 {Math.Max(0, 100 + r.Stats.P + r.Stats.Q)}（当前版本原生公式）", 
				"wine_rack" => $"每次额外催陈 {1 + Math.Max(0, r.Stats.Q) / 125} 天", 
				"purifier" => $"净化下限系数 {PurifierResidual(r.Stats.Q):0.##}（越低越好）；实际去除量随容器内容变化", 
				"alarm_system" => $"防盗率 {Math.Clamp(50 + r.Stats.P, 0, 100)}%", 
				"furnace" => "金属品质随原料、质量、助熔剂和专用模组变化；性能不参与分配", 
				_ => (m.BaseSpeed > 0) ? $"处理速度 {Speed(m, r.Stats)}；产出还取决于原料及配方" : "按有效属性分配；实际产出取决于原料、配方及运行条件", 
			};
		}
	}
	public sealed class LayoutValue : IComparable<LayoutValue>
	{
		public readonly double[] Parts;

		public LayoutValue(IEnumerable<double> parts)
		{
			Parts = parts.ToArray();
		}

		public int CompareTo(LayoutValue? b)
		{
			if (b == null)
			{
				return 1;
			}
			for (int i = 0; i < Math.Min(Parts.Length, b.Parts.Length); i++)
			{
				int num = Parts[i].CompareTo(b.Parts[i]);
				if (num != 0)
				{
					return num;
				}
			}
			return Parts.Length.CompareTo(b.Parts.Length);
		}
	}
	public static class LayoutObjective
	{
		public static bool WaterFirst(Snapshot s)
		{
			if (!(s.Supply == null))
			{
				return s.Supply.Resolve()[0].Kind == "moisture_farm";
			}
			return true;
		}

		public static LayoutValue Value(Snapshot s, Placement[] ps)
		{
			return Value(s, Rules.Evaluate(s, ps), Cultivation.Score(s, ps), ps.Length);
		}

		public static LayoutValue Value(Snapshot s, MachineResult[] results, (int Count, long Value) training, int count)
		{
			if (s.Supply == null)
			{
				AllocationValue allocationValue = WaterAllocation.Value(results, training, count);
				return new LayoutValue(new double[9]
				{
					allocationValue.Premium,
					MachineSummary.BaseWater(results),
					allocationValue.Training,
					allocationValue.Quality,
					allocationValue.Volume,
					allocationValue.Efficiency,
					allocationValue.Potential,
					-allocationValue.Power,
					-allocationValue.Count
				});
			}
			List<double> list = new List<double>();
			if (WaterFirst(s))
			{
				HashSet<int> waterIds = (from m in s.Machines
					where m.Kind == "moisture_farm"
					select m.Id).ToHashSet();
				MachineResult[] array = results.Where((MachineResult r) => waterIds.Contains(r.Id)).ToArray();
				list.Add(((IEnumerable<MachineResult>)array).Sum((Func<MachineResult, double>)((MachineResult r) => r.QualityVolume)));
				list.Add(MachineSummary.BaseWater(array));
			}
			SupplyPreference[] array2 = s.Supply.Resolve();
			foreach (SupplyPreference preference in array2)
			{
				Machine[] array3 = s.Machines.Where((Machine m) => MachineProfiles.CanonicalKind(m.Kind) == preference.Kind).ToArray();
				if (array3.Length == 0)
				{
					continue;
				}
				string attributes = preference.Attributes;
				foreach (char attr in attributes)
				{
					if (preference.Kind == "moisture_farm" && attr == 'Q')
					{
						list.Add(((IEnumerable<Machine>)array3).Sum((Func<Machine, double>)((Machine m) => results.Single((MachineResult r) => r.Id == m.Id).QualityVolume)));
					}
					list.Add(array3.Sum((Machine m) => MachineProfiles.Attribute(m, results.Single((MachineResult r) => r.Id == m.Id).Stats, attr)));
				}
			}
			list.Add(training.Count);
			list.Add(training.Value);
			list.Add(0.0 - ((IEnumerable<MachineResult>)results).Sum((Func<MachineResult, double>)((MachineResult r) => Math.Max(0, r.Power))));
			list.Add(-count);
			return new LayoutValue(list);
		}

		public static double Smooth(Snapshot s, MachineResult[] results)
		{
			double num = 0.0;
			double num2 = 1.0;
			if (WaterFirst(s))
			{
				foreach (Machine m in s.Machines.Where((Machine machine) => machine.Kind == "moisture_farm"))
				{
					MachineResult machineResult = results.Single((MachineResult r) => r.Id == m.Id);
					num += (double)machineResult.Volume / 1000000.0 * ((machineResult.Stats.Q >= 100) ? 10.0 : (8.0 * Math.Pow(Math.Clamp((double)machineResult.Stats.Q / 100.0, 0.0, 1.0), 2.0)));
					if (machineResult.Stats.Q < 100)
					{
						num += (double)machineResult.Volume / 1000000.0 * ((machineResult.Stats.Q >= 50) ? 1.0 : (0.8 * Math.Pow(Math.Clamp((double)machineResult.Stats.Q / 50.0, 0.0, 1.0), 2.0)));
					}
				}
				num2 = 0.01;
			}
			SupplyPreference[] array = s.Supply.Resolve();
			foreach (SupplyPreference preference in array)
			{
				Machine[] array2 = s.Machines.Where((Machine machine) => MachineProfiles.CanonicalKind(machine.Kind) == preference.Kind).ToArray();
				if (array2.Length == 0)
				{
					continue;
				}
				double num4 = 1.0;
				string attributes = preference.Attributes;
				foreach (char attr in attributes)
				{
					num = ((!(preference.Kind == "moisture_farm") || attr != 'Q') ? (num + num2 * num4 * array2.Sum((Machine machine) => MachineProfiles.Attribute(machine, results.Single((MachineResult r) => r.Id == machine.Id).Stats, attr)) / 100.0) : (num + num2 * num4 * array2.Sum((Machine machine) =>
					{
						MachineResult machineResult2 = results.Single((MachineResult r) => r.Id == machine.Id);
						return (double)machineResult2.Volume / 1000000.0 * ((machineResult2.Stats.Q >= 100) ? 1.0 : (0.9 * Math.Pow(Math.Clamp((double)machineResult2.Stats.Q / 100.0, 0.0, 1.0), 2.0))) + (double)Math.Clamp(machineResult2.Stats.Q, -100, 150) * 0.0005;
					})));
					num4 *= 0.12;
				}
				num2 *= 0.2;
			}
			return num;
		}
	}
	public sealed record OperatingState(bool Ready, string Reason, int MaxVolume = int.MaxValue);
	public sealed record MachineSummaryRow(string Kind, string Title, string Detail);
	public static class MachineSummary
	{
		public static MachineSummaryRow[] Build(Snapshot s, MachineResult[] current, MachineResult[] preview, IReadOnlyDictionary<int, OperatingState> currentState, IReadOnlyDictionary<int, OperatingState> previewState)
		{
			List<MachineSummaryRow> list = new List<MachineSummaryRow>();
			SupplyPreference[] array = (s.Supply ?? new SupplySettings()).Resolve();
			foreach (SupplyPreference preference in array)
			{
				Machine[] array2 = s.Machines.Where((Machine m) => MachineProfiles.CanonicalKind(m.Kind) == preference.Kind).ToArray();
				if (array2.Length == 0)
				{
					continue;
				}
				Machine[] array3 = array2.Where((Machine m) => currentState.TryGetValue(m.Id, out OperatingState value) && value.Ready).ToArray();
				Machine[] array4 = array2.Where((Machine m) => previewState.TryGetValue(m.Id, out OperatingState value) && value.Ready).ToArray();
				string title = $"{MachineProfiles.Find(preference.Kind).Name} · 可工作 {array3.Length} → {array4.Length} / {array2.Length} 台";
				string detail = Effect(preference.Kind, array3, current) + " → " + Effect(preference.Kind, array4, preview);
				if (array3.Length == 0 && array4.Length == 0)
				{
					detail = "未计入效果：" + string.Join("；", array2.Select((Machine m) => (!currentState.TryGetValue(m.Id, out OperatingState value)) ? "运行条件未核验" : value.Reason).Distinct());
				}
				if (preference.Kind == "moisture_farm")
				{
					HashSet<int> ids = array2.Select((Machine m) => m.Id).ToHashSet();
					MachineResult[] array5 = current.Where((MachineResult r) => ids.Contains(r.Id)).ToArray();
					MachineResult[] array6 = preview.Where((MachineResult r) => ids.Contains(r.Id)).ToArray();
					title = $"水分收集器 · 拥有 {array2.Length} 台（配装额定产能）";
					MachineResult[] array7 = Collectable(array5, currentState);
					MachineResult[] array8 = Collectable(array6, previewState);
					detail = $"额定日产：优质 {(double)((IEnumerable<MachineResult>)array5).Sum((Func<MachineResult, long>)((MachineResult r) => r.QualityVolume)) / 1000000.0:0.00} → {(double)((IEnumerable<MachineResult>)array6).Sum((Func<MachineResult, long>)((MachineResult r) => r.QualityVolume)) / 1000000.0:0.00} L；基础 {(double)BaseWater(array5) / 1000000.0:0.00} → {(double)BaseWater(array6) / 1000000.0:0.00} L\n本次可收集：优质 {(double)((IEnumerable<MachineResult>)array7).Sum((Func<MachineResult, long>)((MachineResult r) => r.QualityVolume)) / 1000000.0:0.00} → {(double)((IEnumerable<MachineResult>)array8).Sum((Func<MachineResult, long>)((MachineResult r) => r.QualityVolume)) / 1000000.0:0.00} L；基础 {(double)BaseWater(array7) / 1000000.0:0.00} → {(double)BaseWater(array8) / 1000000.0:0.00} L\n可工作 {array3.Length} → {array4.Length} / {array2.Length} 台 · 包含水塔接收；按新产水原始等级分别统计，不代表蓄水箱混后等级。";
				}
				list.Add(new MachineSummaryRow(preference.Kind, title, detail));
			}
			return list.ToArray();
		}

		public static long BaseWater(IEnumerable<MachineResult> results)
		{
			return results.Where((MachineResult r) => r.Stats.Q >= 50 && r.Stats.Q < 100).Sum((Func<MachineResult, long>)((MachineResult r) => r.Volume));
		}

		public static MachineResult[] Collectable(MachineResult[] results, IReadOnlyDictionary<int, OperatingState> states)
		{
			return (from r in results
				where states.TryGetValue(r.Id, out OperatingState value) && value.Ready
				select r with
				{
					Volume = Math.Min(r.Volume, Math.Max(0, states[r.Id].MaxVolume)),
					QualityVolume = Math.Min(r.QualityVolume, Math.Max(0, states[r.Id].MaxVolume))
				}).ToArray();
		}

		private static string Range(IEnumerable<double> values, string format = "0.##")
		{
			double[] source = values.ToArray();
			double num = source.Min();
			double num2 = source.Max();
			if (num != num2)
			{
				return num.ToString(format) + "～" + num2.ToString(format);
			}
			return num.ToString(format);
		}

		private static string Effect(string kind, Machine[] machines, MachineResult[] results)
		{
			if (machines.Length == 0)
			{
				return "无工作效果";
			}
			HashSet<int> ids = machines.Select((Machine m) => m.Id).ToHashSet();
			MachineResult[] rs = results.Where((MachineResult r) => ids.Contains(r.Id)).ToArray();
			return kind switch
			{
				"moisture_farm" => $"优质 {(double)((IEnumerable<MachineResult>)rs).Sum((Func<MachineResult, long>)((MachineResult r) => r.QualityVolume)) / 1000000.0:0.00} / 全水 {(double)((IEnumerable<MachineResult>)rs).Sum((Func<MachineResult, long>)((MachineResult r) => r.Volume)) / 1000000.0:0.00} L/日", 
				"mirage_projector" => $"总吸引力 +{((IEnumerable<MachineResult>)rs).Sum((Func<MachineResult, long>)((MachineResult r) => Math.Max(0, 100 + r.Stats.P + r.Stats.Q)))}", 
				"furnace" => "质量加成 " + Range(((IEnumerable<MachineResult>)rs).Select((Func<MachineResult, double>)((MachineResult r) => r.Stats.Q))) + "%（纯度依原料）", 
				"purifier" => $"净化下限系数 {Range(rs.Select((MachineResult r) => MachineProfiles.PurifierResidual(r.Stats.Q)))}；性能 {Range(((IEnumerable<MachineResult>)rs).Select((Func<MachineResult, double>)((MachineResult r) => r.Stats.P)))}%", 
				"wine_rack" => "每台额外催陈 " + Range(((IEnumerable<MachineResult>)rs).Select((Func<MachineResult, double>)((MachineResult r) => 1 + Math.Max(0, r.Stats.Q) / 125))) + " 天/次", 
				"alarm_system" => "每台防盗率 " + Range(((IEnumerable<MachineResult>)rs).Select((Func<MachineResult, double>)((MachineResult r) => Math.Clamp(50 + r.Stats.P, 0, 100)))) + "%", 
				_ => (machines.All((Machine m) => m.BaseSpeed > 0) ? ("处理速度 " + Range(((IEnumerable<Machine>)machines).Select((Func<Machine, double>)((Machine m) => MachineProfiles.Speed(m, rs.Single((MachineResult r) => r.Id == m.Id).Stats))))) : ("性能加成 " + Range(((IEnumerable<MachineResult>)rs).Select((Func<MachineResult, double>)((MachineResult r) => r.Stats.P))) + "%")) + (MachineProfiles.Find(kind).Attributes.Contains('Q') ? ("；质量 " + Range(((IEnumerable<MachineResult>)rs).Select((Func<MachineResult, double>)((MachineResult r) => r.Stats.Q))) + "%") : ""), 
			} + $" · 功耗 {((IEnumerable<MachineResult>)rs).Sum((Func<MachineResult, long>)((MachineResult r) => r.Power))}";
		}
	}
	public static class ShoppingAdviser
	{
		private static PurchaseAdvice[] BuildMixed(Snapshot s, Placement[] layout, bool stable, TimeSpan budget, CancellationToken cancel)
		{
			Rules.Validate(s, layout, stable: false);
			Stopwatch stopwatch = Stopwatch.StartNew();
			Dictionary<int, Module> modules = s.Modules.ToDictionary((Module m) => m.Id);
			MachineResult[] array = Rules.Evaluate(s, layout);
			(int, long) training = Cultivation.Score(s, layout);
			LayoutValue b = LayoutObjective.Value(s, array, training, layout.Length);
			Dictionary<string, (LayoutValue, Module, Machine, MachineResult, MachineResult, int)> dictionary = new Dictionary<string, (LayoutValue, Module, Machine, MachineResult, MachineResult, int)>();
			foreach (Module item2 in from m in s.Modules
				where !m.FixedSpecial && (!stable || !m.Unstable)
				orderby m.Base.Q descending
				select m)
			{
				if (cancel.IsCancellationRequested || stopwatch.Elapsed >= budget)
				{
					break;
				}
				foreach (Placement placement in Cultivation.Options(s, item2))
				{
					if (cancel.IsCancellationRequested || stopwatch.Elapsed >= budget)
					{
						break;
					}
					Placement[] array2 = layout.Where((Placement p) => p.MachineId == placement.MachineId).ToArray();
					Placement[] array3 = array2.Where((Placement p) => Cultivation.Overlap(p, placement)).ToArray();
					if (array3.Length > 1 || array3.Any((Placement p) => !modules[p.ModuleId].Movable || Cultivation.Candidate(modules[p.ModuleId])))
					{
						continue;
					}
					Machine machine = s.Machines.Single((Machine m) => m.Id == placement.MachineId);
					MachineResult item = array.Single((MachineResult r) => r.Id == machine.Id);
					MachineResult after = Rules.Evaluate(machine, Rules.Calculate((from p in array2.Except(array3)
						select (modules[p.ModuleId], Pose: p.Pose)).Append((item2, placement.Pose)).ToArray()) + machine.ExternalBonus);
					MachineResult[] results = array.Select((MachineResult r) => (r.Id != machine.Id) ? r : after).ToArray();
					LayoutValue layoutValue = LayoutObjective.Value(s, results, training, layout.Length + 1 - array3.Length);
					if (layoutValue.CompareTo(b) > 0)
					{
						string key = item2.Kind + ":" + string.Join(",", item2.Effects.OrderBy((string e) => e));
						if (!dictionary.TryGetValue(key, out var value) || layoutValue.CompareTo(value.Item1) > 0)
						{
							dictionary[key] = (layoutValue, item2, machine, item, after, array3.Length);
						}
					}
				}
			}
			List<PurchaseAdvice> list = dictionary.Values.OrderByDescending(((LayoutValue Score, Module Module, Machine Machine, MachineResult Before, MachineResult After, int Removed) w) => w.Score).Take(4).Select(((LayoutValue Score, Module Module, Machine Machine, MachineResult Before, MachineResult After, int Removed) w) => new PurchaseAdvice($"寻找：{w.Module.Name} ×1 → {w.Machine.Name} #{w.Machine.Id}", $"参考 #{w.Module.Id}，P/E/Q {w.Module.Base}，{w.Module.Variants[0].Cells.Length} 格；{string.Join("、", w.Module.Effects.Select(EffectName))}。\n试放{((w.Removed == 0) ? "空位" : "替换一件非培养件")}：P/E/Q {w.Before.Stats} → {w.After.Stats}；功耗 {w.Before.Power} → {w.After.Power}。\n" + MachineProfiles.Describe(w.Machine, w.After)))
				.ToList();
			if (list.Count == 0)
			{
				list.Add(new PurchaseAdvice("暂未找到符合优先级的购买项", "限时试放已有真实模组模板，没有确认额外收益；这不代表商店没有更好的形状或词条。"));
			}
			list.Add(new PurchaseAdvice("购买前核对实物", "建议只代表同形状、同当前属性的一件新模组；未读取商店库存或价格。各项独立试算，不能叠加。改动优先级或库存后请重新优化。"));
			return list.ToArray();
		}

		public static PurchaseAdvice[] Build(Snapshot s, Placement[] layout, bool stable, TimeSpan budget, CancellationToken cancel = default(CancellationToken))
		{
			if (s.Supply != null)
			{
				return BuildMixed(s, layout, stable, budget, cancel);
			}
			Rules.Validate(s, layout, stable: false);
			Stopwatch stopwatch = Stopwatch.StartNew();
			Dictionary<int, Module> modules = s.Modules.ToDictionary((Module m) => m.Id);
			MachineResult[] source = Rules.Evaluate(s, layout);
			int num = source.Sum((MachineResult r) => r.QualityVolume);
			List<PurchaseAdvice> list = new List<PurchaseAdvice>();
			Dictionary<string, (int, Module, int, int)> dictionary = new Dictionary<string, (int, Module, int, int)>();
			Module[] array = (from m in s.Modules
				where !stable || !m.Unstable
				orderby m.Base.Q descending, m.Base.P descending
				select m).ToArray();
			foreach (Module module in array)
			{
				if (stopwatch.Elapsed >= budget || cancel.IsCancellationRequested)
				{
					break;
				}
				foreach (Placement p in Cultivation.Options(s, module))
				{
					if (stopwatch.Elapsed >= budget || cancel.IsCancellationRequested)
					{
						break;
					}
					Placement[] array2 = layout.Where((Placement q) => q.MachineId == p.MachineId).ToArray();
					Placement[] array3 = array2.Where((Placement q) => Cultivation.Overlap(p, q)).ToArray();
					if (array3.Length <= 1 && !array3.Any((Placement q) => !modules[q.ModuleId].Movable || Cultivation.Candidate(modules[q.ModuleId])))
					{
						(Module, Pose)[] items = (from q in array2.Except(array3)
							select (modules[q.ModuleId], Pose: q.Pose)).Append((module, p.Pose)).ToArray();
						Machine machine = s.Machines.Single((Machine b) => b.Id == p.MachineId);
						int num3 = Rules.Evaluate(machine, Rules.Calculate(items) + machine.ExternalBonus).QualityVolume - source.Single((MachineResult r) => r.Id == machine.Id).QualityVolume;
						string key = module.Kind + ":" + string.Join(",", module.Effects.OrderBy((string x) => x));
						if (num3 > 0 && (!dictionary.TryGetValue(key, out var value) || num3 > value.Item1))
						{
							dictionary[key] = (num3, module, machine.Id, array3.Length);
						}
					}
				}
			}
			foreach (var item3 in dictionary.Values.OrderByDescending(((int Gain, Module Module, int Machine, int Replaced) w) => w.Gain).Take(3))
			{
				Module item = item3.Item2;
				int value2 = item.Variants.FirstOrDefault()?.Cells.Length ?? 0;
				list.Add(new PurchaseAdvice($"优先寻找：{item.Name} ×1（试算 +{(double)item3.Item1 / 1000000.0:0.00} L/日）", $"参考现有 #{item.Id}：P/E/Q {item.Base}，{value2} 格，词条 {string.Join("、", item.Effects.Select(EffectName))}。\n相同形状和当前数值，可在机器 #{item3.Item3} {((item3.Item4 == 0) ? "空位安装" : "替换一件非培养模组")}；总量 {(double)num / 1000000.0:0.00} → {(double)(num + item3.Item1) / 1000000.0:0.00} L/日。各建议独立试算，收益不能相加。"));
			}
			if (dictionary.Count == 0)
			{
				list.Add(new PurchaseAdvice("暂无试算确认的直接增产购买项", "在本次限时试放范围内，新增一件现有属性模板未找到增产位置；这不代表所有商店模组都无用。"));
			}
			MachineResult machineResult = (from r in source
				where r.Stats.Q < 100 && r.Volume > 0
				orderby r.Stats.Q descending
				select r).FirstOrDefault();
			if (machineResult != null)
			{
				list.Add(new PurchaseAdvice("补质量方向：质量 / 精炼模组", $"机器 #{machineResult.Id} 当前 Q={machineResult.Stats.Q}，还需净增加 {100 - machineResult.Stats.Q} 点才能达标。优先比较小占格、高 Q、优质/接收器等词条；加入后的邻接、性能变化和形状仍需重新优化确认。"));
			}
			int num4 = s.Modules.Count(Cultivation.Candidate);
			int item2 = Cultivation.Score(s, layout).Count;
			list.Add((num4 > item2) ? new PurchaseAdvice("培养方面：先利用现有库存", $"当前还有 {num4 - item2} 件有成长空间的自学习模组未安排到水机。先运行优化、检查培养页和机器运行条件，再决定是否购买更多。") : new PurchaseAdvice("长期补货：有成长空间的自学习模组", "留意质量、精炼、超频或性能模组中的自学习件，比较真实成长上限、占格和搭配词条；优先能放进培养机的形状。" + (stable ? "长期稳定策略排除退化/过充组合。" : "退化/过充组合不列为长期培养目标。")));
			return list.ToArray();
		}

		public static string EffectName(string x)
		{
			return x.Replace("MODULE_EFFECT_", "").Replace("LEARNING_ALGORITHM", "自学习").Replace("NEGATIVE_FEEDBACK", "负反馈")
				.Replace("RECEIVER", "接收器")
				.Replace("TOP_MOUNT", "顶部挂载")
				.Replace("SIDE_MOUNT", "侧边挂载")
				.Replace("PREMIUM", "优质")
				.Replace("INFERIOR", "劣质")
				.Replace("DEGRADING", "退化")
				.Replace("OVERVOLTED", "过充")
				.Replace("NODE_EFFECT_SUPPORT", "中节点");
		}
	}
	public record Cell(int X, int Y);
	public record Stats(int P, int E, int Q)
	{
		public static Stats Zero => new Stats(0, 0, 0);

		[JsonIgnore]
		public Stats Negative => new Stats(Math.Min(P, 0), Math.Min(E, 0), Math.Min(Q, 0));

		public static Stats operator +(Stats a, Stats b)
		{
			return new Stats(a.P + b.P, a.E + b.E, a.Q + b.Q);
		}

		public Stats Percent(int pct)
		{
			return new Stats(P * pct / 100, E * pct / 100, Q * pct / 100);
		}

		public override string ToString()
		{
			return $"{P}/{E}/{Q}";
		}
	}
	public record Pose(int X, int Y, int Rotation, bool Flipped, Cell[] Cells);
	public sealed record Module
	{
		public int Id { get; init; }

		public string Kind { get; init; } = "";

		public string Name { get; init; } = "";

		public Stats Base { get; init; } = Stats.Zero;

		public string[] Effects { get; init; } = Array.Empty<string>();

		public Pose[] Variants { get; init; } = Array.Empty<Pose>();

		public bool Movable { get; init; } = true;

		public bool FixedSpecial { get; init; }

		public Stats? LearningMax { get; init; }

		public bool IsLearning => Enumerable.Contains(Effects, "MODULE_EFFECT_LEARNING_ALGORITHM");

		public bool NeuralCore => ModuleKinds.NeuralCore(Kind);

		public bool Node => Kind.StartsWith("node_");

		public bool Unstable => Effects.Any((string e) => e.Contains("DEGRADING") || e.Contains("OVERVOLTED") || e.Contains("OVERCHARG"));
	}
	public sealed record Machine
	{
		public Stats ExternalBonus { get; init; } = Stats.Zero;

		public string Kind { get; init; } = "moisture_farm";

		public string Name { get; init; } = "水分收集器";

		public int? PerformanceRatio { get; init; }

		public int? EfficiencyRatio { get; init; }

		public int? QualityRatio { get; init; }

		public int? QualityLimit { get; init; }

		public bool PerformanceReducesPower { get; init; }

		public bool ClampPower { get; init; }

		public bool Locked { get; init; }

		public int BaseSpeed { get; init; }

		public int ObservedSpeed { get; init; }

		public int Id { get; init; }

		public int Width { get; init; }

		public int Height { get; init; }

		public Cell[] Available { get; init; } = Array.Empty<Cell>();

		public int Expansion { get; init; }

		public int BasePower { get; init; }

		public Stats Observed { get; init; } = Stats.Zero;

		public int ObservedVolume { get; init; }

		public int ObservedPower { get; init; }
	}
	public record Placement(int ModuleId, int MachineId, Pose Pose);
	public sealed record Snapshot
	{
		public SupplySettings? Supply { get; init; }

		public string Identity { get; init; } = "";

		public string Fingerprint { get; set; } = "";

		public Machine[] Machines { get; init; } = Array.Empty<Machine>();

		public Module[] Modules { get; init; } = Array.Empty<Module>();

		public Placement[] Current { get; init; } = Array.Empty<Placement>();
	}
	public record MachineResult(int Id, Stats Stats, int Power, int Volume, int QualityVolume);
	public record Plan(string Fingerprint, bool Stable, Placement[] Placements, MachineResult[] Machines, int QualityVolume, long Iterations, bool ProvenOptimal, double Seconds);
	public static class Rules
	{
		public static bool Adjacent(Pose a, Pose b)
		{
			return a.Cells.Any((Cell x) => b.Cells.Any((Cell y) => Math.Abs(x.X - y.X) + Math.Abs(x.Y - y.Y) == 1));
		}

		public static Stats Calculate(IReadOnlyList<(Module Module, Pose Pose)> items)
		{
			Stats zero = Stats.Zero;
			for (int i = 0; i < items.Count; i++)
			{
				(Module Module, Pose Pose) tuple = items[i];
				Module item = tuple.Module;
				Pose item2 = tuple.Pose;
				List<Module> list = new List<Module>();
				for (int j = 0; j < items.Count; j++)
				{
					if (i != j && Adjacent(item2, items[j].Pose))
					{
						list.Add(items[j].Module);
					}
				}
				Stats stats = item.Base;
				if (item.Node)
				{
					stats = list.Where((Module x) => !x.Node).Aggregate(Stats.Zero, (Stats s, Module x) => s + x.Base).Percent(20);
				}
				else
				{
					string[] effects = item.Effects;
					foreach (string obj in effects)
					{
						if (obj == "MODULE_EFFECT_RECEIVER")
						{
							stats += item.Base.Percent(10 * list.Count((Module x) => x.Node));
						}
						if (obj == "MODULE_EFFECT_TOP_MOUNT" && item2.Y <= 0)
						{
							stats += item.Base.Percent(20);
						}
						if (obj == "MODULE_EFFECT_SIDE_MOUNT" && item2.X <= 0)
						{
							stats += item.Base.Percent(20);
						}
						if (obj == "MODULE_EFFECT_NEGATIVE_FEEDBACK")
						{
							stats += list.Where((Module x) => !x.Node).Aggregate(Stats.Zero, (Stats s, Module x) => s + x.Base.Negative).Percent(25);
						}
					}
				}
				zero += stats;
			}
			return zero;
		}

		public static MachineResult Evaluate(Machine m, Stats s)
		{
			return new MachineResult(m.Id, s, MachineProfiles.Power(m, s), (m.Kind == "moisture_farm") ? Math.Max(0, (100 + s.P) * 10000) : 0, (m.Kind == "moisture_farm" && s.Q >= 100) ? Math.Max(0, (100 + s.P) * 10000) : 0);
		}

		public static MachineResult[] Evaluate(Snapshot s, IEnumerable<Placement> placements)
		{
			Dictionary<int, Module> modules = s.Modules.ToDictionary((Module x) => x.Id);
			Placement[] ps = placements.ToArray();
			return s.Machines.Select((Machine m) => Evaluate(m, Calculate((from p in ps
				where p.MachineId == m.Id
				select (modules[p.ModuleId], Pose: p.Pose)).ToArray()) + m.ExternalBonus)).ToArray();
		}

		public static void Validate(Snapshot s, IEnumerable<Placement> placements, bool stable)
		{
			Placement[] array = placements.ToArray();
			Dictionary<int, Module> modules = s.Modules.ToDictionary((Module x) => x.Id);
			Dictionary<int, Machine> dictionary = s.Machines.ToDictionary((Machine x) => x.Id);
			if (array.Select((Placement placement) => placement.ModuleId).Distinct().Count() != array.Length)
			{
				throw new InvalidOperationException("重复使用模组实例");
			}
			Placement[] array2 = array;
			foreach (Placement p in array2)
			{
				if (!modules.TryGetValue(p.ModuleId, out var value) || !dictionary.TryGetValue(p.MachineId, out var machine))
				{
					throw new InvalidOperationException("未知实例");
				}
				if (machine.Locked && !s.Current.Any((Placement q) => q.ModuleId == p.ModuleId && q.MachineId == p.MachineId && q.Pose.Cells.ToHashSet().SetEquals(p.Pose.Cells)))
				{
					throw new InvalidOperationException("目标机器格盘已锁定");
				}
				if (stable && value.Unstable)
				{
					throw new InvalidOperationException("稳定策略包含短期件");
				}
				if (!p.Pose.Cells.All(((IEnumerable<Cell>)machine.Available).Contains<Cell>))
				{
					throw new InvalidOperationException($"机器 {machine.Id} 模组 {value.Id} 占用锁定格或越界：{string.Join(",", p.Pose.Cells.Where((Cell c) => !Enumerable.Contains(machine.Available, c)))}");
				}
				Pose pose = value.Variants.FirstOrDefault((Pose v) => v.Rotation == p.Pose.Rotation && v.Flipped == p.Pose.Flipped);
				if (pose == null || !pose.Cells.Select((Cell c) => new Cell(c.X + p.Pose.X, c.Y + p.Pose.Y)).ToHashSet().SetEquals(p.Pose.Cells))
				{
					throw new InvalidOperationException("形状与真实实例不符");
				}
			}
			foreach (IGrouping<int, Placement> item in from placement in array
				group placement by placement.MachineId)
			{
				if (item.SelectMany((Placement placement) => placement.Pose.Cells).Distinct().Count() != item.Sum((Placement placement) => placement.Pose.Cells.Length))
				{
					throw new InvalidOperationException("布局重叠");
				}
			}
			foreach (Placement p2 in s.Current.Where((Placement placement) => !modules[placement.ModuleId].Movable))
			{
				if (!array.Any((Placement x) => x.ModuleId == p2.ModuleId && x.MachineId == p2.MachineId && x.Pose.Cells.ToHashSet().SetEquals(p2.Pose.Cells)))
				{
					throw new InvalidOperationException("无法移动的模组被改变");
				}
			}
		}
	}
	public record PurchaseAdvice(string Title, string Detail);
	public static class StoragePacking
	{
		public static Dictionary<int, Pose>? Pack(Machine box, Module[] items, TimeSpan budget)
		{
			if (items.Length == 0)
			{
				return new Dictionary<int, Pose>();
			}
			if (items.Sum((Module m) => m.Variants.First().Cells.Length) > box.Available.Length)
			{
				return null;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			Random rng = new Random(823);
			Snapshot s = new Snapshot
			{
				Machines = new Machine[1] { box },
				Modules = items
			};
			Dictionary<int, Placement[]> options = items.ToDictionary((Module m) => m.Id, (Module m) => Cultivation.Options(s, m).ToArray());
			if (options.Values.Any((Placement[] p) => p.Length == 0))
			{
				return null;
			}
			int attempt;
			for (attempt = 0; attempt < 128; attempt++)
			{
				if (!(stopwatch.Elapsed < budget))
				{
					break;
				}
				HashSet<Cell> occupied = new HashSet<Cell>();
				Dictionary<int, Pose> dictionary = new Dictionary<int, Pose>();
				Module[] array = ((attempt == 0) ? (from m in items
					orderby m.Variants[0].Cells.Length descending, options[m.Id].Length
					select m).ToArray() : items.OrderByDescending((Module m) => (double)m.Variants[0].Cells.Length + rng.NextDouble() * 6.0).ToArray());
				foreach (Module module in array)
				{
					if (stopwatch.Elapsed >= budget)
					{
						break;
					}
					Placement placement = (from p in options[module.Id]
						where p.Pose.Cells.All((Cell c) => !occupied.Contains(c))
						orderby (double)(p.Pose.Cells.Max((Cell c) => c.Y) * 100 + p.Pose.Cells.Max((Cell c) => c.X)) + ((attempt == 0) ? 0.0 : (rng.NextDouble() * 80.0))
						select p).FirstOrDefault();
					if (placement == null)
					{
						break;
					}
					dictionary.Add(module.Id, placement.Pose);
					occupied.UnionWith(placement.Pose.Cells);
				}
				if (dictionary.Count == items.Length)
				{
					return dictionary;
				}
			}
			return null;
		}
	}
	public readonly record struct AllocationValue(int Premium, int Training, long Quality, long Volume, long Efficiency, long Potential, int Power, int Count) : IComparable<AllocationValue>
	{
		public int CompareTo(AllocationValue b)
		{
			return (Premium, Training, Quality, Volume, Efficiency, Potential, -Power, -Count).CompareTo((b.Premium, b.Training, b.Quality, b.Volume, b.Efficiency, b.Potential, -b.Power, -b.Count));
		}
	}
	public static class WaterAllocation
	{
		public static string Grade(int q)
		{
			if (q >= -50)
			{
				if (q >= 0)
				{
					if (q >= 50)
					{
						if (q >= 100)
						{
							if (q >= 150)
							{
								return "纯水";
							}
							return "优质水";
						}
						return "Basewater";
					}
					return "幽灵水";
				}
				return "Rustwater";
			}
			return "Gutterflow";
		}

		public static AllocationValue Value(MachineResult[] results, (int Count, long Value) training, int count)
		{
			return new AllocationValue(results.Sum((MachineResult r) => r.QualityVolume), training.Count, ((IEnumerable<MachineResult>)results).Sum((Func<MachineResult, long>)((MachineResult r) => Math.Clamp(r.Stats.Q, -100, 150))), ((IEnumerable<MachineResult>)results).Sum((Func<MachineResult, long>)((MachineResult r) => r.Volume)), ((IEnumerable<MachineResult>)results).Sum((Func<MachineResult, long>)((MachineResult r) => Math.Clamp(r.Stats.E, -1000, 100))), training.Value, results.Sum((MachineResult r) => Math.Max(0, r.Power)), count);
		}

		public static AllocationValue Value(Snapshot s, Placement[] ps)
		{
			return Value(Rules.Evaluate(s, ps), Cultivation.Score(s, ps), ps.Length);
		}

		public static Placement[] Fill(Snapshot s, Placement[] incumbent, bool stable, IReadOnlyDictionary<int, List<Placement>> options, TimeSpan budget, CancellationToken cancel)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			List<Placement> list = incumbent.ToList();
			LayoutValue layoutValue = LayoutObjective.Value(s, incumbent);
			Module[] array = s.Modules.Where((Module module) => module.Movable && (!stable || !module.Unstable)).OrderByDescending(Cultivation.Candidate).ThenByDescending((Module module) => module.Base.Q)
				.ThenByDescending((Module module) => module.Base.P)
				.ToArray();
			bool flag = true;
			while (flag && !cancel.IsCancellationRequested && stopwatch.Elapsed < budget)
			{
				flag = false;
				Module[] array2 = array;
				foreach (Module m in array2)
				{
					if (list.Any((Placement placement2) => placement2.ModuleId == m.Id))
					{
						continue;
					}
					Placement placement = null;
					LayoutValue layoutValue2 = layoutValue;
					foreach (Placement p in options[m.Id])
					{
						if (cancel.IsCancellationRequested || stopwatch.Elapsed >= budget)
						{
							break;
						}
						if (!list.Any((Placement q) => Cultivation.Overlap(p, q)))
						{
							Placement[] ps = list.Append(p).ToArray();
							LayoutValue layoutValue3 = LayoutObjective.Value(s, ps);
							if (layoutValue3.CompareTo(layoutValue2) > 0)
							{
								placement = p;
								layoutValue2 = layoutValue3;
							}
						}
					}
					if (placement != null)
					{
						list.Add(placement);
						layoutValue = layoutValue2;
						flag = true;
					}
					if (stopwatch.Elapsed >= budget)
					{
						break;
					}
				}
			}
			Placement[] array3 = list.ToArray();
			Rules.Validate(s, array3, stable);
			return array3;
		}
	}
	public sealed record WaterReceiver(int Id, int FreeVolume, int[] MachineOrder);
	public static class WaterReceivers
	{
		public const string MissingContainer = "未装独立容器";

		public static void IncludeTowers(MachineResult[] results, Dictionary<int, OperatingState> states, IEnumerable<WaterReceiver> towers)
		{
			Dictionary<int, MachineResult> dictionary = results.ToDictionary((MachineResult r) => r.Id);
			HashSet<int> hashSet = new HashSet<int>();
			HashSet<int> hashSet2 = new HashSet<int>();
			foreach (WaterReceiver tower in towers)
			{
				if (!hashSet2.Add(tower.Id))
				{
					continue;
				}
				int num = Math.Max(0, tower.FreeVolume);
				int[] machineOrder = tower.MachineOrder;
				foreach (int num3 in machineOrder)
				{
					if (dictionary.TryGetValue(num3, out var value) && states.TryGetValue(num3, out OperatingState value2) && !(value2.Reason != "未装独立容器") && hashSet.Add(num3))
					{
						int num4 = Math.Min(Math.Max(0, value.Volume), num);
						num -= num4;
						states[num3] = new OperatingState(num4 > 0, (num4 > 0) ? $"产水进入水塔 #{tower.Id}" : $"水塔 #{tower.Id} 余量不足或本机无产水", num4);
					}
				}
			}
		}
	}
}
namespace WaterOptimizer.Mod
{
	public sealed record BatterySwap(GameItem Machine, GameItem Old, GameItem Fresh, GameInventory MachineSlot, GameInventory SpareSlot, GridShape OldOrigin, GridShape SpareOrigin, GridShape InstalledShape, GridShape ReturnedShape, int Demand, int OldEnergy, int FreshEnergy, string Source);
	public sealed record BatteryReview(string Identity, int Checked, int Insufficient, int Spares, BatterySwap[] Swaps, string[] Notes);
	public sealed class GameAdapter
	{
		public sealed record StorageBox(int Id, string Name, GameGridInventory Inventory);

		public sealed record Origin(GameItem Item, GameInventory Inventory, GridShape Shape, Pose Pose);

		public bool BatteryRecovery;

		public string LastBatteryResult = "尚未检查电池。";

		public static readonly JsonSerializerOptions Json = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		public static readonly HashSet<string> SupportedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "8F45E54BC03EDA985409D888E8E0B76150D9EF4F1DC637B22A0B2560BEF378F8", "3BEA17EEEC77ADAB6418918C28A8AA9A06F290582A2972639A48BD7E5A01AB44" };

		public readonly Dictionary<int, GameItem> Items = new Dictionary<int, GameItem>();

		public readonly Dictionary<int, GameItem> Machines = new Dictionary<int, GameItem>();

		public readonly Dictionary<int, GameGridInventory> Bays = new Dictionary<int, GameGridInventory>();

		public string DataDir = Path.Combine(AppContext.BaseDirectory, "UserData", "WaterOptimizer");

		public bool Compatible;

		public SupplySettings Supply = new SupplySettings();

		public ExternalBonusSettings BonusSettings = new ExternalBonusSettings();

		private readonly HashSet<int> fixedSpecial = new HashSet<int>();

		public List<string> Validation = new List<string>();

		public List<StorageBox> StorageBoxes = new List<StorageBox>();

		public int? SelectedStorageId;

		public string LastStorageSummary = "";

		public GameGridInventory? RecoveryStaging;

		public List<Origin>? RecoveryOrigins;

		public string? RecoveryDirectory;

		private static readonly HashSet<string> KnownEffects = new HashSet<string>
		{
			"", "MODULE_EFFECT_PREMIUM", "MODULE_EFFECT_INFERIOR", "MODULE_EFFECT_LEARNING_ALGORITHM", "MODULE_EFFECT_DEGRADING", "MODULE_EFFECT_OVERVOLTED", "MODULE_EFFECT_NEGATIVE_FEEDBACK", "MODULE_EFFECT_RECEIVER", "MODULE_EFFECT_TOP_MOUNT", "MODULE_EFFECT_SIDE_MOUNT",
			"NODE_EFFECT_SUPPORT"
		};

		public string StorageLabel
		{
			get
			{
				int? selectedStorageId = SelectedStorageId;
				if (selectedStorageId.HasValue)
				{
					int id = selectedStorageId.GetValueOrDefault();
					StorageBox storageBox = StorageBoxes.FirstOrDefault((StorageBox b) => b.Id == id);
					if ((object)storageBox != null)
					{
						return $"收纳箱：{storageBox.Name} #{id}";
					}
					return $"收纳箱 #{id} 不可用";
				}
				return "收纳箱：自动选择（点击切换）";
			}
		}

		public static int BatteryEnergy(GameItem item)
		{
			return Int(item, "power_source_item_energy");
		}

		private static bool BatteryMovable(GameItem item)
		{
			if (item.unitCount == 1 && item.parentInventory != null && item.MaxNumRemove() >= 1 && !item.parentInventory.IsRemoveLocked())
			{
				return !item.parentInventory.IsInsertLocked();
			}
			return false;
		}

		private static bool SpareLocation(GameItem battery)
		{
			GameInventory parentInventory = battery.parentInventory;
			GameItem val = ((parentInventory != null) ? parentInventory.GetParentItem() : null);
			if (val == null)
			{
				return true;
			}
			string identifier = val.identifier;
			if (identifier == "recharger" || identifier == "watertower")
			{
				return ((Il2CppObjectBase)battery.parentInventory).TryCast<GameGridInventory>() != null;
			}
			if (val.identifier == "powerblock" || val.IsTag("STANDARD_MACHINE_TAG") || val.IsTag("MACHINE_RECHARGABLE_TAG"))
			{
				return false;
			}
			if (!val.identifier.StartsWith("storage_bay") && !val.identifier.StartsWith("backpack"))
			{
				return val.IsGameItemType("STORAGE");
			}
			return true;
		}

		private static bool FitsOriginalSpare(GameItem old, GameItem spare, out GridShape returned)
		{
			Pose pose = ReadPose(spare.modifiedShape);
			returned = Shape(old, pose);
			if (!ReadPose(returned).Cells.All(((IEnumerable<Cell>)pose.Cells).Contains<Cell>))
			{
				return false;
			}
			List<GameItem> val = new List<GameItem>();
			val.Add(spare);
			return spare.parentInventory.CheckShapePlacement(old, returned, val);
		}

		public BatteryReview ReviewBatteries()
		{
			if (RecoveryStaging != null)
			{
				throw new InvalidOperationException("请先完成上次布局或电池事务的恢复。");
			}
			GameItem[] source = (from i in Owned()
				group i by i.uniqueId into g
				select g.First()).ToArray();
			string identity = PlayerStore.Instance.runID + ":" + PlayerStore.Instance.saveSlotId;
			List<string> list = new List<string>();
			List<BatteryNeed> list2 = new List<BatteryNeed>();
			Dictionary<int, (GameItem Machine, GameSlotInventory Slot, GameItem Old)> targets = new Dictionary<int, (GameItem, GameSlotInventory, GameItem)>();
			HashSet<IntPtr> protectedSlots = new HashSet<IntPtr>();
			int num = 0;
			SupplyPreference[] array = Supply.Resolve();
			foreach (GameItem machine in source.Where((GameItem i) => i.IsTag("STANDARD_MACHINE_TAG") || i.IsTag("MACHINE_RECHARGABLE_TAG") || i.IsTag("machinery_drawn_power") || MachineProfiles.Find(i.identifier) != null))
			{
				GameSlotInventory batterySlot;
				try
				{
					batterySlot = MachineHelper.GetBatterySlot(machine);
				}
				catch
				{
					continue;
				}
				if (batterySlot == null)
				{
					continue;
				}
				protectedSlots.Add(((Il2CppObjectBase)batterySlot).Pointer);
				if (!machine.IsTag("machinery_drawn_power"))
				{
					continue;
				}
				num++;
				int num2 = Int(machine, "machinery_drawn_power");
				if (num2 <= 0)
				{
					continue;
				}
				if (((GameInventory)batterySlot).childItems.Count != 1)
				{
					list.Add($"{machine.name} #{machine.uniqueId}：未装单个电池，无法对调。");
					continue;
				}
				GameItem val = ((GameInventory)batterySlot).childItems[0];
				if (!val.IsTag("power_source_item"))
				{
					list.Add($"{machine.name} #{machine.uniqueId}：电源类型未支持。");
				}
				else
				{
					if (PowerHelper.CanDrawPowerSource(val, num2))
					{
						continue;
					}
					if (!BatteryMovable(val))
					{
						list.Add($"{machine.name} #{machine.uniqueId}：电量不足，但电池或槽位被锁定。");
					}
					else
					{
						int num3 = Array.FindIndex(array, (SupplyPreference p) => p.Kind == MachineProfiles.CanonicalKind(machine.identifier));
						list2.Add(new BatteryNeed(machine.uniqueId, val.uniqueId, num2, (num3 < 0) ? int.MaxValue : num3));
						targets[machine.uniqueId] = (machine, batterySlot, val);
					}
				}
			}
			Dictionary<int, GameItem> spares = source.Where((GameItem i) => i.IsTag("power_source_item") && BatteryMovable(i) && BatteryEnergy(i) > 0 && !protectedSlots.Contains(((Il2CppObjectBase)i.parentInventory).Pointer) && SpareLocation(i)).ToDictionary((GameItem i) => i.uniqueId);
			Dictionary<(int, int), BatterySwap> pairs = new Dictionary<(int, int), BatterySwap>();
			BatteryAssignment[] assigned = BatteryMatching.Match(list2, spares.Values.Select((GameItem i) =>
			{
				int uniqueId = i.uniqueId;
				int energy = BatteryEnergy(i);
				GameItem parentItem = i.parentInventory.GetParentItem();
				return new SpareBattery(uniqueId, energy, ((parentItem != null) ? parentItem.identifier : null) == "recharger");
			}), Fits);
			foreach (BatteryNeed item in list2.Where((BatteryNeed n) => !assigned.Any((BatteryAssignment a) => a.MachineId == n.MachineId)))
			{
				list.Add($"{targets[item.MachineId].Machine.name} #{item.MachineId}：电量 {BatteryEnergy(targets[item.MachineId].Old)} / 今晚需 {item.Demand}；没有够电且原位置兼容的闲置电池。");
			}
			BatteryReview batteryReview = new BatteryReview(identity, num, list2.Count, spares.Count, assigned.Select((BatteryAssignment a) => pairs[(a.MachineId, a.BatteryId)]).ToArray(), list.ToArray());
			Directory.CreateDirectory(DataDir);
			File.WriteAllText(Path.Combine(DataDir, "battery-review.json"), JsonSerializer.Serialize(new
			{
				identity = identity,
				count = num,
				Insufficient = list2.Count,
				Spares = spares.Count,
				Swaps = batteryReview.Swaps.Select((BatterySwap p) => new
				{
					Machine = p.Machine.uniqueId,
					Old = p.Old.uniqueId,
					Fresh = p.Fresh.uniqueId,
					Demand = p.Demand,
					OldEnergy = p.OldEnergy,
					FreshEnergy = p.FreshEnergy,
					Source = p.Source
				}),
				Notes = list
			}, Json));
			return batteryReview;
			bool Fits(int machineId, int spareId)
			{
				(GameItem, GameSlotInventory, GameItem) tuple = targets[machineId];
				GameItem val2 = spares[spareId];
				int num4 = Int(tuple.Item1, "machinery_drawn_power");
				if (!PowerHelper.CanDrawPowerSource(val2, num4) || !FitsOriginalSpare(tuple.Item3, val2, out GridShape returned))
				{
					return false;
				}
				List<GameItem> val3 = new List<GameItem>();
				val3.Add(tuple.Item3);
				GridShape val4 = Shape(val2, ReadPose(tuple.Item3.modifiedShape));
				if (!((GameInventory)tuple.Item2).CheckShapePlacement(val2, val4, val3))
				{
					return false;
				}
				GameItem parentItem = val2.parentInventory.GetParentItem();
				Pose pose = ReadPose(val2.modifiedShape);
				string source2 = ((parentItem == null) ? "库存" : $"{parentItem.name} #{parentItem.uniqueId}") + $"（格 {pose.X + 1},{pose.Y + 1}）";
				pairs[(machineId, spareId)] = new BatterySwap(tuple.Item1, tuple.Item3, val2, (GameInventory)(object)tuple.Item2, val2.parentInventory, tuple.Item3.modifiedShape.Clone(), val2.modifiedShape.Clone(), val4, returned, num4, BatteryEnergy(tuple.Item3), BatteryEnergy(val2), source2);
				return true;
			}
		}

		public string ExchangeBatteries(bool verifyRestore = false)
		{
			//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c4: Expected Obj, but got Unknown
			if (!Compatible)
			{
				throw new InvalidOperationException("游戏版本未核验，不能自动换电。");
			}
			BatteryReview batteryReview = ReviewBatteries();
			if (batteryReview.Swaps.Length == 0)
			{
				return LastBatteryResult = $"电池检查：{batteryReview.Checked} 台；无需或没有可执行的对调。" + ((batteryReview.Notes.Length != 0) ? (" " + string.Join("；", batteryReview.Notes)) : "");
			}
			BatterySwap[] swaps = batteryReview.Swaps;
			List<Origin> list = swaps.SelectMany((BatterySwap p) => new Origin[2]
			{
				new Origin(p.Old, p.MachineSlot, p.OldOrigin, ReadPose(p.OldOrigin)),
				new Origin(p.Fresh, p.SpareSlot, p.SpareOrigin, ReadPose(p.SpareOrigin))
			}).ToList();
			if (list.Select((Origin o) => o.Item.uniqueId).Distinct().Count() != list.Count)
			{
				throw new InvalidOperationException("电池实例重复，取消对调。");
			}
			Dictionary<int, int> energies = list.ToDictionary((Origin o) => o.Item.uniqueId, (Origin o) => BatteryEnergy(o.Item));
			PlayerStore.Instance.SaveGame();
			string text = Path.Combine(DataDir, "battery-backups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
			Directory.CreateDirectory(text);
			string[] files = Directory.GetFiles(Application.persistentDataPath, "*.es3");
			foreach (string text2 in files)
			{
				File.Copy(text2, Path.Combine(text, Path.GetFileName(text2)));
			}
			File.WriteAllText(Path.Combine(text, "origins.json"), JsonSerializer.Serialize(list.Select((Origin o) =>
			{
				int uniqueId = o.Item.uniqueId;
				GameItem parentItem = o.Inventory.GetParentItem();
				return new
				{
					uniqueId = uniqueId,
					Parent = ((parentItem != null) ? new int?(parentItem.uniqueId) : ((int?)null)),
					Inventory = o.Inventory.identifier,
					Pose = o.Pose,
					Energy = energies[o.Item.uniqueId]
				};
			}), Json));
			File.WriteAllText(Path.Combine(text, "status.txt"), "prepared");
			if (batteryReview.Identity != PlayerStore.Instance.runID + ":" + PlayerStore.Instance.saveSlotId || list.Any((Origin o) =>
			{
				GameInventory parentInventory3 = o.Item.parentInventory;
				IntPtr? intPtr2 = ((parentInventory3 != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory3).Pointer) : ((IntPtr?)null));
				IntPtr pointer2 = ((Il2CppObjectBase)o.Inventory).Pointer;
				return !intPtr2.HasValue || (intPtr2.HasValue && intPtr2.GetValueOrDefault() != pointer2) || o.Item.unitCount != 1 || BatteryEnergy(o.Item) != energies[o.Item.uniqueId] || !ReadPose(o.Item.modifiedShape).Cells.ToHashSet().SetEquals(o.Pose.Cells);
			}))
			{
				throw new InvalidOperationException("备份期间电池状态发生变化，已取消，未移动电池。");
			}
			GameGridInventory val = (RecoveryStaging = new GameGridInventory(Math.Max(16, list.Sum((Origin o) => o.Shape.globalWidth + 2)), Math.Max(16, list.Max((Origin o) => o.Shape.globalHeight + 2))));
			RecoveryOrigins = list;
			RecoveryDirectory = text;
			BatteryRecovery = true;
			try
			{
				StageAll(list, val);
				BatterySwap[] array = swaps;
				foreach (BatterySwap batterySwap in array)
				{
					Move(batterySwap.Fresh, batterySwap.MachineSlot, batterySwap.InstalledShape);
					Move(batterySwap.Old, batterySwap.SpareSlot, batterySwap.ReturnedShape);
				}
				array = swaps;
				int num = 0;
				while (num < array.Length)
				{
					BatterySwap batterySwap2 = array[num];
					GameInventory parentInventory = batterySwap2.Fresh.parentInventory;
					if (!(((parentInventory != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory).Pointer) : ((IntPtr?)null)) != ((Il2CppObjectBase)batterySwap2.MachineSlot).Pointer))
					{
						GameInventory parentInventory2 = batterySwap2.Old.parentInventory;
						IntPtr? intPtr = ((parentInventory2 != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory2).Pointer) : ((IntPtr?)null));
						IntPtr pointer = ((Il2CppObjectBase)batterySwap2.SpareSlot).Pointer;
						if (intPtr.HasValue && (!intPtr.HasValue || !(intPtr.GetValueOrDefault() != pointer)) && ReadPose(batterySwap2.Old.modifiedShape).Cells.ToHashSet().SetEquals(ReadPose(batterySwap2.ReturnedShape).Cells) && PowerHelper.CanDrawPowerSource(batterySwap2.Fresh, batterySwap2.Demand))
						{
							num++;
							continue;
						}
					}
					throw new InvalidOperationException("对调后位置或电量校验失败。");
				}
				if (((GameInventory)val).childItems.Count != 0 || list.Any((Origin o) => o.Item.unitCount != 1 || BatteryEnergy(o.Item) != energies[o.Item.uniqueId]))
				{
					throw new InvalidOperationException("电池数量或电量发生了意外变化。");
				}
				if (verifyRestore)
				{
					Restore();
					if (list.Any((Origin o) => BatteryEnergy(o.Item) != energies[o.Item.uniqueId]))
					{
						throw new InvalidOperationException("恢复后电量不一致。");
					}
					File.WriteAllText(Path.Combine(text, "status.txt"), "battery swap and restore verified");
					return LastBatteryResult = $"电池对调与恢复验证通过：{swaps.Length} 对，原位置已恢复并保存。";
				}
				foreach (GameInventory item in list.Select((Origin o) => o.Inventory).DistinctBy((GameInventory i) => ((Il2CppObjectBase)i).Pointer))
				{
					item.Validate();
				}
				PlayerStore.Instance.SaveGame();
				File.WriteAllText(Path.Combine(text, "status.txt"), "committed");
				RecoveryStaging = null;
				RecoveryOrigins = null;
				RecoveryDirectory = null;
				BatteryRecovery = false;
				return LastBatteryResult = $"已对调 {swaps.Length} 对电池；换下的电池已回到备用电池原位置。" + ((batteryReview.Notes.Length != 0) ? $"另有 {batteryReview.Notes.Length} 项提示，请查看电池页。" : "") + " 备份：" + text;
			}
			catch (Exception ex)
			{
				RecoveryStaging = val;
				RecoveryOrigins = list;
				RecoveryDirectory = text;
				BatteryRecovery = true;
				try
				{
					Restore();
				}
				catch (Exception ex2)
				{
					File.WriteAllText(Path.Combine(text, "status.txt"), "ROLLBACK INCOMPLETE\n" + ex?.ToString() + "\n" + ex2);
					throw new InvalidOperationException("电池回滚未完成，暂存实例仍保留，请先点击恢复原布局。备份：" + text, ex2);
				}
				throw new InvalidOperationException("电池对调失败，已恢复原位置：" + ex.Message, ex);
			}
		}

		public void CycleStorage()
		{
			int num = StorageBoxes.FindIndex((StorageBox b) => b.Id == SelectedStorageId);
			SelectedStorageId = ((num + 1 < StorageBoxes.Count) ? new int?(StorageBoxes[num + 1].Id) : ((int?)null));
		}

		public static int Int(GameItem item, string key)
		{
			TagState tagReadonly = item.GetTagReadonly(key);
			if (tagReadonly == null)
			{
				return 0;
			}
			return tagReadonly.valueInt;
		}

		public static string Str(GameItem item, string key)
		{
			TagState tagReadonly = item.GetTagReadonly(key);
			return ((tagReadonly != null) ? tagReadonly.valueString : null) ?? "";
		}

		public static Stats ReadStats(GameItem item, string prefix, string middle = "")
		{
			return new Stats(Int(item, prefix + "PERFORMANCE" + middle + "_INT"), Int(item, prefix + "EFFICIENCY" + middle + "_INT"), Int(item, prefix + "QUALITY" + middle + "_INT"));
		}

		public static Stats Totals(GameItem item)
		{
			return new Stats(Int(item, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT"), Int(item, "TOTAL_PERCENTAGE_POWER_BONUS_INT"), Int(item, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT"));
		}

		public static Pose ReadPose(GridShape shape)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected Obj, but got Unknown
			GridShapeBuilder val = new GridShapeBuilder(shape);
			List<Cell> list = new List<Cell>();
			for (int i = 0; i < val.height; i++)
			{
				for (int j = 0; j < val.width; j++)
				{
					if (val.GetLocal(j, i) == 1)
					{
						list.Add(Transform(val, j, i));
					}
				}
			}
			return new Pose(val.minX, val.minY, val.orientation, val.flipped, list.ToArray());
		}

		private static Cell Transform(GridShapeBuilder b, int x, int y)
		{
			int num = (b.flipped ? (b.width - 1 - x) : x);
			int num2 = y;
			(num, num2) = (b.orientation % 4) switch
			{
				1 => (num2, b.width - 1 - num), 
				2 => (b.width - 1 - num, b.height - 1 - num2), 
				3 => (b.height - 1 - num2, num), 
				_ => (num, num2), 
			};
			return new Cell(num + b.minX, num2 + b.minY);
		}

		public static GridShape Shape(GameItem item, Pose pose)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			return new GridShapeBuilder(item.shape).SetTransform(pose.X, pose.Y, pose.Flipped, pose.Rotation).Build();
		}

		private static IEnumerable<GameItem> Owned()
		{
			if (!PlayerStore.IsInstanceExist())
			{
				throw new InvalidOperationException("请先进入存档");
			}
			List<GameItem> list = PlayerStore.Instance.FindAllItem(true);
			for (int i = 0; i < list.Count; i++)
			{
				yield return list[i];
			}
		}

		public Snapshot Capture()
		{
			//IL_067a: Unknown result type (might be due to invalid IL or missing references)
			if (RecoveryStaging != null)
			{
				throw new InvalidOperationException("上次回滚未完成，请先恢复原布局");
			}
			Validation.Clear();
			Items.Clear();
			Machines.Clear();
			Bays.Clear();
			fixedSpecial.Clear();
			GameItem[] array = (from val4 in Owned()
				group val4 by val4.uniqueId into g
				select g.First()).ToArray();
			Directory.CreateDirectory(DataDir);
			File.WriteAllText(Path.Combine(DataDir, "machine-catalog.json"), JsonSerializer.Serialize(from val4 in array
				where val4.IsTag("STANDARD_MACHINE_TAG") || MachineProfiles.Find(val4.identifier) != null
				select new
				{
					uniqueId = val4.uniqueId,
					identifier = val4.identifier,
					name = val4.name,
					Recognized = (MachineProfiles.Find(val4.identifier) != null)
				}, Json));
			foreach (GameItem item2 in array.Where((GameItem val4) => MachineProfiles.Find(val4.identifier) != null))
			{
				Machines.Add(item2.uniqueId, item2);
				GameGridInventory moduleInv = MachineHelper.GetModuleInv(item2);
				if (moduleInv == null)
				{
					throw new InvalidOperationException($"机器 {item2.uniqueId} 无法读取格盘");
				}
				Bays.Add(item2.uniqueId, moduleInv);
			}
			if (Machines.Count == 0)
			{
				throw new InvalidOperationException("当前存档没有已支持的带模组格盘机器");
			}
			GameItem[] array2 = array;
			foreach (GameItem i in array2)
			{
				if (ModuleKinds.General(i.identifier, i.IsTag("MODULE_TAG"), Str(i, "MODULE_TYPE")))
				{
					GameInventory parentInventory = i.parentInventory;
					GameItem val = ((parentInventory != null) ? parentInventory.GetParentItem() : null);
					if (val == null || (!val.IsTag("STANDARD_MACHINE_TAG") && !Machines.ContainsKey(val.uniqueId)) || Bays.Values.Any((GameGridInventory b) =>
					{
						IntPtr pointer = ((Il2CppObjectBase)b).Pointer;
						GameInventory parentInventory3 = i.parentInventory;
						IntPtr? intPtr = ((parentInventory3 != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory3).Pointer) : ((IntPtr?)null));
						return pointer == intPtr;
					}))
					{
						Items.Add(i.uniqueId, i);
					}
				}
			}
			foreach (GameGridInventory value in Bays.Values)
			{
				Enumerator<GameItem> enumerator3 = ((GameInventory)value).childItems.GetEnumerator();
				while (enumerator3.MoveNext())
				{
					GameItem current2 = enumerator3.Current;
					if (!Items.ContainsKey(current2.uniqueId))
					{
						if (!current2.IsTag("MODULE_TAG"))
						{
							throw new InvalidOperationException("格盘含非模组物品 " + current2.identifier + "，停止以免遗漏");
						}
						Items.Add(current2.uniqueId, current2);
						fixedSpecial.Add(current2.uniqueId);
					}
				}
			}
			StorageBoxes.Clear();
			foreach (GameItem box in from val4 in array
				where val4.identifier.StartsWith("storage_bay")
				orderby val4.uniqueId
				select val4)
			{
				GameInventory? val2 = array.Select((GameItem val4) => val4.parentInventory).FirstOrDefault((GameInventory inv) =>
				{
					int? num4;
					if (inv == null)
					{
						num4 = null;
					}
					else
					{
						GameItem parentItem = inv.GetParentItem();
						num4 = ((parentItem != null) ? new int?(parentItem.uniqueId) : ((int?)null));
					}
					return num4 == box.uniqueId;
				});
				GameGridInventory val3 = ((val2 != null) ? ((Il2CppObjectBase)val2).TryCast<GameGridInventory>() : null);
				if (val3 == null && box.contentWindow != null)
				{
					val3 = FindStorageGrid(box.contentWindow.child, 0);
				}
				if (val3 != null && !((GameInventory)val3).IsInsertLocked())
				{
					StorageBoxes.Add(new StorageBox(box.uniqueId, box.name, val3));
				}
			}
			List<WaterOptimizer.Core.Module> list = new List<WaterOptimizer.Core.Module>();
			StringBuilder stringBuilder = new StringBuilder();
			foreach (GameItem item in Items.Values.OrderBy((GameItem val4) => val4.uniqueId))
			{
				if (item.unitCount != 1)
				{
					throw new InvalidOperationException($"模组 {item.uniqueId} 是堆叠，请在游戏中拆为单件后扫描");
				}
				string[] array3 = new string[2]
				{
					Str(item, "MODULE_EFFECT1_TAG"),
					Str(item, "MODULE_EFFECT2_TAG")
				}.Where((string x) => x.Length > 0).ToArray();
				if (array3.Any((string x) => !KnownEffects.Contains(x)))
				{
					throw new InvalidOperationException("未支持的词条：" + string.Join(",", array3));
				}
				if (item.identifier.StartsWith("node_") && !Enumerable.Contains(array3, "NODE_EFFECT_SUPPORT"))
				{
					throw new InvalidOperationException("未支持的节点类型：" + item.identifier);
				}
				List<Pose> list2 = new List<Pose>();
				for (int num2 = 0; num2 < 2; num2++)
				{
					for (int num3 = 0; num3 < 4; num3++)
					{
						list2.Add(ReadPose(new GridShapeBuilder(item.shape).SetTransform(0, 0, num2 == 1, num3).Build()));
					}
				}
				list.Add(new WaterOptimizer.Core.Module
				{
					Id = item.uniqueId,
					Kind = item.identifier,
					Name = item.name,
					Base = ReadStats(item, "BONUS_PERCENTAGE_"),
					Effects = array3,
					Variants = list2.ToArray(),
					FixedSpecial = fixedSpecial.Contains(item.uniqueId),
					Movable = (!fixedSpecial.Contains(item.uniqueId) && item.MaxNumRemove() >= 1 && !Bays.Values.Any((GameGridInventory b) =>
					{
						IntPtr pointer = ((Il2CppObjectBase)b).Pointer;
						GameInventory parentInventory3 = item.parentInventory;
						IntPtr? intPtr = ((parentInventory3 != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory3).Pointer) : ((IntPtr?)null));
						return pointer == intPtr && ((GameInventory)b).IsInsertLocked();
					})),
					LearningMax = (Enumerable.Contains(array3, "MODULE_EFFECT_LEARNING_ALGORITHM") ? ReadStats(item, "LEARNING_ALGO_MAX_") : null)
				});
				StringBuilder stringBuilder2 = stringBuilder.Append(item.uniqueId).Append(':');
				GameInventory parentInventory2 = item.parentInventory;
				stringBuilder2.Append((parentInventory2 != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory2).Pointer) : ((IntPtr?)null)).Append(':').Append(JsonSerializer.Serialize(ReadPose(item.modifiedShape)))
					.Append(':');
				List<KeyValuePair<string, TagState>> list3 = new List<KeyValuePair<string, TagState>>();
				Enumerator<string, TagState> enumerator4 = item.state.dict.GetEnumerator();
				while (enumerator4.MoveNext())
				{
					KeyValuePair<string, TagState> current3 = enumerator4.Current;
					list3.Add(current3);
				}
				foreach (KeyValuePair<string, TagState> item3 in list3.OrderBy((KeyValuePair<string, TagState> x) => x.Key))
				{
					if (!item3.Key.StartsWith("TEMP_"))
					{
						stringBuilder.Append(item3.Key).Append('=').Append(item3.Value.valueInt)
							.Append('/')
							.Append(item3.Value.valueBool)
							.Append('/')
							.Append(item3.Value.valueString)
							.Append(';');
					}
				}
			}
			string identity = PlayerStore.Instance.runID + ":" + PlayerStore.Instance.saveSlotId;
			Machine[] array4 = Machines.Values.OrderBy((GameItem m) => m.uniqueId).Select((GameItem m) =>
			{
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0021: Expected Obj, but got Unknown
				GridShapeBuilder val4 = new GridShapeBuilder(Bays[m.uniqueId].inventoryShape);
				List<Cell> list4 = new List<Cell>();
				for (int j = 0; j < val4.height; j++)
				{
					for (int k = 0; k < val4.width; k++)
					{
						if (val4.GetLocal(k, j) == 0)
						{
							list4.Add(Transform(val4, k, j));
						}
					}
				}
				return new Machine
				{
					Id = m.uniqueId,
					Kind = MachineProfiles.CanonicalKind(m.identifier),
					Name = m.name,
					ExternalBonus = BonusSettings.Get(identity, m.uniqueId, m.identifier),
					PerformanceRatio = (m.IsTag("MACHINE_PERFORMANCE_RATIO_INT") ? new int?(Int(m, "MACHINE_PERFORMANCE_RATIO_INT")) : ((int?)null)),
					EfficiencyRatio = (m.IsTag("MACHINE_EFFICIENCY_RATIO_INT") ? new int?(Int(m, "MACHINE_EFFICIENCY_RATIO_INT")) : ((int?)null)),
					QualityRatio = (m.IsTag("MACHINE_QUALITY_RATIO_INT") ? new int?(Int(m, "MACHINE_QUALITY_RATIO_INT")) : ((int?)null)),
					QualityLimit = ReadQualityLimit(m),
					PerformanceReducesPower = m.IsTag("MACHINE_PERFORMANCE_REDUCES_POWER_TAG"),
					ClampPower = true,
					Locked = ((GameInventory)Bays[m.uniqueId]).IsInsertLocked(),
					BaseSpeed = Int(m, "BASE_PROCESSING_SPEED_TAG"),
					ObservedSpeed = Int(m, "CURRENT_PROCESSING_SPEED_TAG"),
					Width = val4.maxX + 1,
					Height = val4.maxY + 1,
					Available = list4.ToArray(),
					Expansion = Int(m, "MODULE_UPGRADE_STAGE_INT"),
					BasePower = Int(m, "MACHINERY_DRAWN_POWER_BASE"),
					Observed = Totals(m),
					ObservedVolume = ((m.identifier == "moisture_farm") ? MachineMoistureFarm.GetOutputVolume(m) : 0),
					ObservedPower = Int(m, "machinery_drawn_power")
				};
			}).ToArray();
			Placement[] current5 = Items.Values.Select((GameItem val4) =>
			{
				KeyValuePair<int, GameGridInventory> keyValuePair = Bays.FirstOrDefault((KeyValuePair<int, GameGridInventory> kv) =>
				{
					GameInventory parentInventory3 = val4.parentInventory;
					IntPtr? intPtr = ((parentInventory3 != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory3).Pointer) : ((IntPtr?)null));
					IntPtr pointer = ((Il2CppObjectBase)kv.Value).Pointer;
					if (!intPtr.HasValue)
					{
						return false;
					}
					return !intPtr.HasValue || intPtr.GetValueOrDefault() == pointer;
				});
				return (keyValuePair.Value != null) ? new Placement(val4.uniqueId, keyValuePair.Key, ReadPose(val4.modifiedShape)) : null;
			}).OfType<Placement>().ToArray();
			Snapshot snapshot = new Snapshot
			{
				Supply = Supply,
				Identity = identity,
				Machines = array4,
				Modules = list.ToArray(),
				Current = current5
			};
			stringBuilder.Append(identity).Append(JsonSerializer.Serialize(array4.Select((Machine m) => new
			{
				m.Id, m.Kind, m.Available, m.Expansion, m.BasePower, m.BaseSpeed, m.PerformanceRatio, m.EfficiencyRatio, m.QualityRatio, m.QualityLimit,
				m.Locked, m.PerformanceReducesPower, m.ExternalBonus
			})));
			stringBuilder.Append(JsonSerializer.Serialize(Supply));
			snapshot.Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
			Directory.CreateDirectory(DataDir);
			File.WriteAllText(Path.Combine(DataDir, "latest-snapshot.json"), JsonSerializer.Serialize(snapshot, Json));
			File.WriteAllText(Path.Combine(DataDir, "raw-boards.json"), JsonSerializer.Serialize(Bays.Select((KeyValuePair<int, GameGridInventory> kv) =>
			{
				//IL_0024: Unknown result type (might be due to invalid IL or missing references)
				return new
				{
					Id = kv.Key,
					Grid = ReadPose(kv.Value.inventoryShape),
					Data = ((IEnumerable<byte>)new GridShapeBuilder(kv.Value.inventoryShape).data).ToArray()
				};
			}), Json));
			Rules.Validate(snapshot, snapshot.Current, stable: false);
			MachineResult[] array5 = Rules.Evaluate(snapshot, snapshot.Current);
			foreach (MachineResult result in array5)
			{
				Machine machine = array4.Single((Machine m) => m.Id == result.Id);
				if (machine.PerformanceRatio == 0)
				{
					Validation.Add($"机器 {machine.Id} 的性能系数为零，当前不支持");
				}
				if (!MatchesNative(machine, result))
				{
					Validation.Add($"机器 {machine.Id}: 复算 {result.Stats}/{result.Power}/{result.Volume}，游戏 {machine.Observed}/{machine.ObservedPower}/{machine.ObservedVolume}");
				}
			}
			Directory.CreateDirectory(DataDir);
			File.WriteAllText(Path.Combine(DataDir, "latest-snapshot.json"), JsonSerializer.Serialize(snapshot, Json));
			File.WriteAllText(Path.Combine(DataDir, "validation.txt"), (Validation.Count == 0) ? $"PASS {array4.Length} machines; {list.Count} modules" : string.Join("\n", Validation));
			return snapshot;
		}

		private static GameGridInventory? FindStorageGrid(GraphNodeStorage? node, int depth)
		{
			if (node == null || depth > 8)
			{
				return null;
			}
			GameGridInventory val = ((Il2CppObjectBase)node).TryCast<GameGridInventory>();
			if (val != null)
			{
				return val;
			}
			PixelWindow val2 = ((Il2CppObjectBase)node).TryCast<PixelWindow>();
			if (val2 != null)
			{
				return FindStorageGrid(val2.child, depth + 1);
			}
			GridPixelElement val3 = ((Il2CppObjectBase)node).TryCast<GridPixelElement>();
			if (val3 != null)
			{
				Enumerator<GraphNodeStorage> enumerator = val3.children.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameGridInventory val4 = FindStorageGrid(enumerator.Current, depth + 1);
					if (val4 != null)
					{
						return val4;
					}
				}
			}
			return null;
		}

		private (StorageBox? Box, Dictionary<int, Pose> Poses) PlanStorage(Snapshot s, Plan plan, HashSet<int> moving)
		{
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Expected Obj, but got Unknown
			HashSet<int> used = plan.Placements.Select((Placement p) => p.ModuleId).ToHashSet();
			WaterOptimizer.Core.Module[] array = s.Modules.Where((WaterOptimizer.Core.Module m) => !used.Contains(m.Id)).ToArray();
			if (array.Length == 0)
			{
				return (Box: null, Poses: new Dictionary<int, Pose>());
			}
			StorageBox[] array2 = StorageBoxes.Where((StorageBox b) => !SelectedStorageId.HasValue || SelectedStorageId == b.Id).ToArray();
			if (array2.Length == 0)
			{
				throw new InvalidOperationException("没有可用的目标储藏箱。请准备一个储藏箱并重新扫描；尚未移动任何物品。");
			}
			StorageBox[] array3 = array2;
			foreach (StorageBox box in array3)
			{
				if (array.Any((WaterOptimizer.Core.Module m) =>
				{
					if (!m.Movable)
					{
						GameInventory parentInventory = Items[m.Id].parentInventory;
						IntPtr? intPtr = ((parentInventory != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory).Pointer) : ((IntPtr?)null));
						IntPtr pointer = ((Il2CppObjectBase)box.Inventory).Pointer;
						if (!intPtr.HasValue)
						{
							return true;
						}
						if (!intPtr.HasValue)
						{
							return false;
						}
						return intPtr.GetValueOrDefault() != pointer;
					}
					return false;
				}))
				{
					continue;
				}
				GridShapeBuilder val = new GridShapeBuilder(box.Inventory.inventoryShape);
				HashSet<Cell> hashSet = new HashSet<Cell>();
				for (int num2 = 0; num2 < val.height; num2++)
				{
					for (int num3 = 0; num3 < val.width; num3++)
					{
						if (val.GetLocal(num3, num2) == 0)
						{
							hashSet.Add(Transform(val, num3, num2));
						}
					}
				}
				Enumerator<GameItem> enumerator = ((GameInventory)box.Inventory).childItems.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					if (!moving.Contains(current.uniqueId))
					{
						hashSet.ExceptWith(ReadPose(current.modifiedShape).Cells);
					}
				}
				Dictionary<int, Pose> dictionary = StoragePacking.Pack(new Machine
				{
					Id = box.Id,
					Width = val.maxX + 1,
					Height = val.maxY + 1,
					Available = hashSet.ToArray()
				}, array.Where((WaterOptimizer.Core.Module m) => m.Movable).ToArray(), TimeSpan.FromSeconds(2.0));
				if (dictionary != null)
				{
					return (Box: box, Poses: dictionary);
				}
			}
			throw new InvalidOperationException("未能把全部闲置模组放入同一个储藏箱。请腾空、换箱或解除模组锁定后重试；尚未移动任何物品。");
		}

		public Dictionary<int, string> ReadTrainingStatus(MachineResult[] results)
		{
			return ReadOperatingStates(results).ToDictionary((KeyValuePair<int, OperatingState> x) => x.Key, (KeyValuePair<int, OperatingState> x) => (!x.Value.Ready) ? ("暂不能确认培养：" + x.Value.Reason) : "本次检查可工作；仅在机器成功工作时成长");
		}

		private int? ReadQualityLimit(GameItem machine)
		{
			if (machine.identifier != "furnace")
			{
				return MachineProfiles.Find(machine.identifier)?.QualityLimit;
			}
			string purityConst = IngotPurityHelper.GetPurityConst("INGOT_PURITY_LOW", 100);
			int i;
			for (i = 0; i < 100 && IngotPurityHelper.GetPurityConst("INGOT_PURITY_LOW", i) != purityConst; i++)
			{
			}
			bool flag = false;
			Enumerator<GameItem> enumerator = ((GameInventory)Bays[machine.uniqueId]).childItems.GetEnumerator();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.identifier == "furnace_module_blast")
				{
					flag = true;
				}
			}
			return (i + (flag ? 1 : 0)) * 100;
		}

		private static bool CheckDerived(GameItem item, MachineResult r)
		{
			return MachineProfiles.CanonicalKind(item.identifier) switch
			{
				"mirage_projector" => MachineProjector.GetAttractivenessBoost(item) == Math.Max(0, 100 + r.Stats.P + r.Stats.Q), 
				"wine_rack" => MachineWineRack.GetAgingDays(item) == 1 + Math.Max(0, r.Stats.Q) / 125, 
				"purifier" => Math.Abs(MachinePurifier.GetPurityMult(r.Stats.Q) - MachineProfiles.PurifierResidual(r.Stats.Q)) < 1E-08, 
				"alarm_system" => MachineAlarm.GetAlarmeStopRate(item) == Math.Max(0, 50 + r.Stats.P), 
				_ => true, 
			};
		}

		private bool MatchesNative(Machine model, MachineResult expected)
		{
			GameItem item = Machines[model.Id];
			Stats stats = Totals(item);
			Stats stats2 = ExternalBonuses.Remove(expected.Stats, model.ExternalBonus);
			if (!ExternalBonuses.Matches(stats, stats2, model.ExternalBonus))
			{
				return false;
			}
			MachineResult[] source = new MachineResult[3]
			{
				expected,
				Rules.Evaluate(model, stats2),
				Rules.Evaluate(model, stats)
			};
			if (source.Any((MachineResult r) => r.Power == Int(item, "machinery_drawn_power")) && (model.Kind != "moisture_farm" || source.Any((MachineResult r) => r.Volume == MachineMoistureFarm.GetOutputVolume(item))) && (model.BaseSpeed <= 0 || source.Any((MachineResult r) => MachineProfiles.Speed(model, r.Stats) == Int(item, "CURRENT_PROCESSING_SPEED_TAG"))))
			{
				return source.Any((MachineResult r) => CheckDerived(item, r));
			}
			return false;
		}

		public void Refresh()
		{
			foreach (GameItem value in Machines.Values)
			{
				GameGridInventory val = Bays[value.uniqueId];
				ModuleEffectHelper.ComputeAllModuleTempStat(((GameInventory)val).childItems);
				ModuleHelper.ComputeModuleEffect(((GameInventory)val).childItems, value);
				ModuleHelper.ApplyBasicModuleEffect((GameInventory)(object)val, (GameItem)null, value);
				value.Validate();
			}
		}

		private static void Move(GameItem item, GameInventory target, GridShape shape)
		{
			SlotMarker val = target.TryInventorySlot(item, shape, (TagSystem)null);
			if (val != null && val.IsValid() && val.targetItem == null && val.TryAcceptOnce(1) == 1)
			{
				GameInventory parentInventory = item.parentInventory;
				if (!(((parentInventory != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory).Pointer) : ((IntPtr?)null)) != ((Il2CppObjectBase)target).Pointer))
				{
					return;
				}
			}
			throw new InvalidOperationException($"游戏拒绝搬运实例 {item.uniqueId} 到 {target.identifier}");
		}

		public string Apply(Snapshot original, Plan plan, bool rollbackTest = false)
		{
			//IL_0344: Unknown result type (might be due to invalid IL or missing references)
			//IL_034b: Expected Obj, but got Unknown
			if (!Compatible)
			{
				throw new InvalidOperationException("游戏文件版本不匹配，禁止自动应用");
			}
			Snapshot snapshot = Capture();
			if (snapshot.Fingerprint != original.Fingerprint || snapshot.Fingerprint != plan.Fingerprint)
			{
				throw new InvalidOperationException("库存、成长值或布局已变化，请重新扫描和求解");
			}
			if (Validation.Count > 0)
			{
				throw new InvalidOperationException("当前属性复算不一致，禁止应用");
			}
			Rules.Validate(snapshot, plan.Placements, plan.Stable);
			HashSet<int> ids = (from m in snapshot.Modules
				where m.Movable
				select m.Id).ToHashSet();
			List<Origin> list = ids.Select((int id) =>
			{
				GameItem val2 = Items[id];
				return new Origin(val2, val2.parentInventory, val2.modifiedShape.Clone(), ReadPose(val2.modifiedShape));
			}).ToList();
			if (list.Any((Origin o) => o.Inventory == null || o.Item.MaxNumRemove() < 1))
			{
				throw new InvalidOperationException("存在不能卸载的模组");
			}
			(StorageBox, Dictionary<int, Pose>) tuple = (rollbackTest ? (null, new Dictionary<int, Pose>()) : PlanStorage(snapshot, plan, ids));
			PlayerStore.Instance.SaveGame();
			string text = Path.Combine(DataDir, "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
			Directory.CreateDirectory(text);
			string[] files = Directory.GetFiles(Application.persistentDataPath, "*.es3");
			foreach (string text2 in files)
			{
				File.Copy(text2, Path.Combine(text, Path.GetFileName(text2)));
			}
			File.WriteAllText(Path.Combine(text, "backup-manifest.json"), JsonSerializer.Serialize(from f in Directory.GetFiles(text, "*.es3")
				select new
				{
					File = Path.GetFileName(f),
					Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f)))
				}, Json));
			File.WriteAllText(Path.Combine(text, "original.json"), JsonSerializer.Serialize(snapshot, Json));
			File.WriteAllText(Path.Combine(text, "plan.json"), JsonSerializer.Serialize(plan, Json));
			File.WriteAllText(Path.Combine(text, "storage.json"), JsonSerializer.Serialize(new
			{
				BoxId = tuple.Item1?.Id,
				Poses = tuple.Item2
			}, Json));
			File.WriteAllText(Path.Combine(text, "origins.json"), JsonSerializer.Serialize(list.Select((Origin o) =>
			{
				int uniqueId = o.Item.uniqueId;
				string identifier = o.Inventory.identifier;
				GameItem parentItem = o.Inventory.GetParentItem();
				return new
				{
					uniqueId = uniqueId,
					Inventory = identifier,
					Parent = ((parentItem != null) ? new int?(parentItem.uniqueId) : ((int?)null)),
					Pose = o.Pose
				};
			}), Json));
			File.WriteAllText(Path.Combine(text, "status.txt"), "prepared");
			GameGridInventory val = (RecoveryStaging = new GameGridInventory(Math.Max(16, list.Sum((Origin o) => o.Shape.globalWidth + 2)), Math.Max(16, list.Select((Origin o) => o.Shape.globalHeight + 2).DefaultIfEmpty(16).Max())));
			RecoveryOrigins = list;
			RecoveryDirectory = text;
			BatteryRecovery = false;
			try
			{
				StageAll(list, val);
				if (rollbackTest)
				{
					Restore();
					if (Capture().Fingerprint != snapshot.Fingerprint)
					{
						throw new InvalidOperationException("回滚后状态指纹不一致");
					}
					File.WriteAllText(Path.Combine(text, "status.txt"), "rollback-test passed");
					return text;
				}
				foreach (Placement item in plan.Placements.Where((Placement p) => ids.Contains(p.ModuleId)))
				{
					Move(Items[item.ModuleId], (GameInventory)(object)Bays[item.MachineId], Shape(Items[item.ModuleId], item.Pose));
				}
				foreach (KeyValuePair<int, Pose> item2 in tuple.Item2)
				{
					Move(Items[item2.Key], (GameInventory)(object)tuple.Item1.Inventory, Shape(Items[item2.Key], item2.Value));
				}
				HashSet<int> used = plan.Placements.Select((Placement p) => p.ModuleId).ToHashSet();
				foreach (WaterOptimizer.Core.Module item3 in snapshot.Modules.Where((WaterOptimizer.Core.Module m) => !used.Contains(m.Id)))
				{
					GameInventory parentInventory = Items[item3.Id].parentInventory;
					IntPtr? intPtr = ((parentInventory != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory).Pointer) : ((IntPtr?)null));
					var (storageBox, _) = tuple;
					if (intPtr != (((object)storageBox != null) ? new IntPtr?(((Il2CppObjectBase)storageBox.Inventory).Pointer) : ((IntPtr?)null)))
					{
						throw new InvalidOperationException("闲置模组未统一进入目标储藏箱");
					}
				}
				Refresh();
				MachineResult[] machines = plan.Machines;
				foreach (MachineResult expected in machines)
				{
					if (!MatchesNative(snapshot.Machines.Single((Machine m) => m.Id == expected.Id), expected))
					{
						throw new InvalidOperationException($"应用后机器 {expected.Id} 与预览不符");
					}
				}
				if (((GameInventory)val).childItems.Count != 0)
				{
					throw new InvalidOperationException("暂存区仍有物品");
				}
				foreach (Origin item4 in list)
				{
					if (item4.Item.unitCount != 1 || item4.Item.parentInventory == null)
					{
						throw new InvalidOperationException("实例完整性验证失败");
					}
				}
				RecoveryStaging = null;
				Snapshot snapshot2 = Capture();
				if (!snapshot2.Modules.Select((WaterOptimizer.Core.Module m) => m.Id).ToHashSet().SetEquals(snapshot.Modules.Select((WaterOptimizer.Core.Module m) => m.Id)))
				{
					throw new InvalidOperationException("应用后库存实例集合不一致");
				}
				Dictionary<int, Placement> byId = snapshot2.Current.ToDictionary((Placement p) => p.ModuleId);
				if (snapshot2.Current.Length != plan.Placements.Length || plan.Placements.Any((Placement p) => !byId.TryGetValue(p.ModuleId, out var value) || value.MachineId != p.MachineId || !value.Pose.Cells.ToHashSet().SetEquals(p.Pose.Cells)))
				{
					throw new InvalidOperationException("应用后物品位置与预览不一致");
				}
				PlayerStore.Instance.SaveGame();
				if (tuple.Item1 != null)
				{
					SelectedStorageId = tuple.Item1.Id;
				}
				LastStorageSummary = ((tuple.Item1 == null) ? "没有闲置模组" : $"闲置模组已统一收纳至 {tuple.Item1.Name} #{tuple.Item1.Id}");
				File.WriteAllText(Path.Combine(text, "status.txt"), "committed");
				RecoveryOrigins = null;
				RecoveryDirectory = null;
				return text;
			}
			catch (Exception ex)
			{
				RecoveryStaging = val;
				RecoveryOrigins = list;
				RecoveryDirectory = text;
				try
				{
					Restore();
					throw new InvalidOperationException("应用失败，已恢复原始布局。原因：" + ex.Message, ex);
				}
				catch (Exception ex2) when (RecoveryStaging != null)
				{
					File.WriteAllText(Path.Combine(text, "status.txt"), "ROLLBACK INCOMPLETE\n" + ex?.ToString() + "\n" + ex2);
					throw new InvalidOperationException("回滚未完成。暂存实例仍保留，请点击恢复原布局；若无法恢复，退出游戏后从备份恢复。备份：" + text, ex2);
				}
			}
		}

		private static void StageAll(List<Origin> origins, GameGridInventory stage)
		{
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			int num = 0;
			foreach (Origin origin in origins)
			{
				GameInventory parentInventory = origin.Item.parentInventory;
				if (((parentInventory != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory).Pointer) : ((IntPtr?)null)) == ((Il2CppObjectBase)stage).Pointer)
				{
					num += origin.Shape.globalWidth + 2;
					continue;
				}
				Move(origin.Item, (GameInventory)(object)stage, new GridShapeBuilder(origin.Shape).SetPosition(num, 0).Build());
				num += origin.Shape.globalWidth + 2;
			}
		}

		public void Restore()
		{
			if (RecoveryStaging == null || RecoveryOrigins == null)
			{
				throw new InvalidOperationException("没有待恢复事务");
			}
			StageAll(RecoveryOrigins, RecoveryStaging);
			foreach (Origin recoveryOrigin in RecoveryOrigins)
			{
				Move(recoveryOrigin.Item, recoveryOrigin.Inventory, recoveryOrigin.Shape);
			}
			if (BatteryRecovery)
			{
				foreach (GameInventory item in RecoveryOrigins.Select((Origin o) => o.Inventory).DistinctBy((GameInventory i) => ((Il2CppObjectBase)i).Pointer))
				{
					item.Validate();
				}
			}
			else
			{
				Refresh();
			}
			foreach (Origin recoveryOrigin2 in RecoveryOrigins)
			{
				GameInventory parentInventory = recoveryOrigin2.Item.parentInventory;
				IntPtr? intPtr = ((parentInventory != null) ? new IntPtr?(((Il2CppObjectBase)parentInventory).Pointer) : ((IntPtr?)null));
				IntPtr pointer = ((Il2CppObjectBase)recoveryOrigin2.Inventory).Pointer;
				if (!intPtr.HasValue || (intPtr.HasValue && intPtr.GetValueOrDefault() != pointer) || !ReadPose(recoveryOrigin2.Item.modifiedShape).Cells.ToHashSet().SetEquals(recoveryOrigin2.Pose.Cells))
				{
					throw new InvalidOperationException("恢复验证失败");
				}
			}
			PlayerStore.Instance.SaveGame();
			File.WriteAllText(Path.Combine(RecoveryDirectory, "status.txt"), "rolled back");
			RecoveryStaging = null;
			RecoveryOrigins = null;
			RecoveryDirectory = null;
			BatteryRecovery = false;
		}

		public bool MachineRosterChanged(Snapshot snapshot)
		{
			if (!PlayerStore.IsInstanceExist())
			{
				return false;
			}
			if (snapshot.Identity != PlayerStore.Instance.runID + ":" + PlayerStore.Instance.saveSlotId)
			{
				return true;
			}
			return !(from i in Owned()
				where MachineProfiles.Find(i.identifier) != null
				select (uniqueId: i.uniqueId, MachineProfiles.CanonicalKind(i.identifier))).ToHashSet().SetEquals(snapshot.Machines.Select((Machine m) => (Id: m.Id, MachineProfiles.CanonicalKind(m.Kind))));
		}

		public MachineResult[] ReadCurrentResults(Snapshot snapshot)
		{
			Dictionary<int, MachineResult> current = Rules.Evaluate(snapshot, snapshot.Current).ToDictionary((MachineResult r) => r.Id);
			return snapshot.Machines.Select((Machine m) => Rules.Evaluate(m, ExternalBonuses.Effective(Totals(Machines[m.Id]), ExternalBonuses.Remove(current[m.Id].Stats, m.ExternalBonus), m.ExternalBonus))).ToArray();
		}

		public Dictionary<int, OperatingState> ReadOperatingStates(MachineResult[] results)
		{
			Dictionary<int, OperatingState> dictionary = new Dictionary<int, OperatingState>();
			GameItem[] array;
			try
			{
				array = Owned().ToArray();
			}
			catch
			{
				return dictionary;
			}
			HashSet<int> hashSet = array.Select((GameItem i) => i.uniqueId).ToHashSet();
			foreach (MachineResult machineResult in results)
			{
				try
				{
					GameItem val = Machines[machineResult.Id];
					if (!hashSet.Contains(machineResult.Id))
					{
						dictionary[machineResult.Id] = new OperatingState(Ready: false, "机器已不在当前库存，请重新扫描");
						continue;
					}
					GameSlotInventory batterySlot = MachineHelper.GetBatterySlot(val);
					GameItem val2 = ((batterySlot != null && ((GameInventory)batterySlot).childItems.Count > 0) ? ((GameInventory)batterySlot).childItems[0] : null);
					if (val2 == null)
					{
						dictionary[machineResult.Id] = new OperatingState(Ready: false, "未装电源");
					}
					else if (!PowerHelper.CanDrawPowerSource(val2, machineResult.Power))
					{
						dictionary[machineResult.Id] = new OperatingState(Ready: false, "电量不足");
					}
					else
					{
						dictionary[machineResult.Id] = CheckInputs(val, array);
					}
				}
				catch (Exception ex)
				{
					dictionary[machineResult.Id] = new OperatingState(Ready: false, "运行条件未核验（" + ex.GetType().Name + "）");
				}
			}
			WaterReceivers.IncludeTowers(results, dictionary, WaterTowerBridge.Read(array));
			return dictionary;
		}

		private static OperatingState Ready()
		{
			return new OperatingState(Ready: true, "本次周期可工作");
		}

		private static OperatingState Stopped(string why)
		{
			return new OperatingState(Ready: false, why);
		}

		private static GameInventory? Column(GameItem item, int column)
		{
			PixelWindow contentWindow = item.contentWindow;
			if (contentWindow == null)
			{
				return null;
			}
			GraphNodeStorage child = contentWindow.child;
			if (child == null)
			{
				return null;
			}
			GridPixelElement val = ((Il2CppObjectBase)child).TryCast<GridPixelElement>();
			if (val == null)
			{
				return null;
			}
			PixelElement element = val.GetElement(column, 1);
			if (element == null)
			{
				return null;
			}
			return ((Il2CppObjectBase)element).TryCast<GameInventory>();
		}

		private static GameItem[] Children(GameInventory? inventory)
		{
			if (inventory == null)
			{
				return Array.Empty<GameItem>();
			}
			List<GameItem> list = new List<GameItem>();
			Enumerator<GameItem> enumerator = inventory.childItems.GetEnumerator();
			while (enumerator.MoveNext())
			{
				GameItem current = enumerator.Current;
				list.Add(current);
			}
			return list.ToArray();
		}

		private OperatingState CheckInputs(GameItem m, GameItem[] owned)
		{
			//IL_0422: Unknown result type (might be due to invalid IL or missing references)
			//IL_0429: Expected Obj, but got Unknown
			//IL_04d1: Unknown result type (might be due to invalid IL or missing references)
			GameItem[] source = owned.Where((GameItem i) =>
			{
				GameInventory parentInventory = i.parentInventory;
				int? num4;
				if (parentInventory == null)
				{
					num4 = null;
				}
				else
				{
					GameItem parentItem = parentInventory.GetParentItem();
					num4 = ((parentItem != null) ? new int?(parentItem.uniqueId) : ((int?)null));
				}
				return num4 == m.uniqueId;
			}).ToArray();
			switch (MachineProfiles.CanonicalKind(m.identifier))
			{
			case "moisture_farm":
			{
				GameItem val2 = source.FirstOrDefault((GameItem i) => i.IsTag("LIQUID_CONTAINER_TAG"));
				if (val2 != null)
				{
					if (WaterHelper.GetFreeCapacity(val2) > 0)
					{
						return new OperatingState(Ready: true, "原版独立容器可接收", WaterHelper.GetFreeCapacity(val2));
					}
					return Stopped("独立容器已满");
				}
				return Stopped("未装独立容器");
			}
			case "mirage_projector":
			case "alarm_system":
				return Ready();
			case "purifier":
			{
				GameItem val = Children(Column(m, 2)).FirstOrDefault((GameItem i) => i.IsTag("LIQUID_CONTAINER_TAG"));
				if (val != null && !WaterHelper.IsEmpty(val))
				{
					return Ready();
				}
				return Stopped("未装待处理液体");
			}
			case "wine_rack":
				if (!((IEnumerable<GameItem>)source).Any((Func<GameItem, bool>)MachineWineRack.CanAgeWine))
				{
					return Stopped("没有可继续催陈的物品");
				}
				return Ready();
			case "furnace":
			{
				GameItem[] source2 = Children(Column(m, 2));
				bool flag = Children((GameInventory?)(object)Bays[m.uniqueId]).Any((GameItem i) => i.identifier == "furnace_module_blast");
				bool flag2 = Children((GameInventory?)(object)Bays[m.uniqueId]).Any((GameItem i) => i.identifier == "furnace_module_junk");
				bool flag3 = source2.Count((GameItem i) =>
				{
					string identifier = i.identifier;
					return identifier == "scrap_metal" || identifier == "common_ore";
				}) >= (flag ? 3 : 2);
				bool flag4 = flag2 && source2.Count((GameItem i) => i.identifier == "junk") >= 6;
				if (!flag3 && !flag4)
				{
					return Stopped("原料不足一批");
				}
				GameInventory? val3 = Column(m, 3);
				GameGridInventory val4 = ((val3 != null) ? ((Il2CppObjectBase)val3).TryCast<GameGridInventory>() : null);
				if (val4 == null)
				{
					return Stopped("输出空间未核验");
				}
				if (((GameInventory)val4).childItems.Count == 0)
				{
					return Ready();
				}
				GameItem val5 = owned.FirstOrDefault((GameItem i) => i.identifier == "metal_ingot");
				if (val5 == null)
				{
					return Stopped("输出区非空，空间未核验");
				}
				GridShapeBuilder val6 = new GridShapeBuilder(val4.inventoryShape);
				HashSet<Cell> free = new HashSet<Cell>();
				for (int num = 0; num < val6.height; num++)
				{
					for (int num2 = 0; num2 < val6.width; num2++)
					{
						if (val6.GetLocal(num2, num) == 0)
						{
							free.Add(Transform(val6, num2, num));
						}
					}
				}
				Enumerator<GameItem> enumerator = ((GameInventory)val4).childItems.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					free.ExceptWith(ReadPose(current.modifiedShape).Cells);
				}
				for (int num3 = 0; num3 < 4; num3++)
				{
					Cell[] cells = ReadPose(new GridShapeBuilder(val5.shape).SetTransform(0, 0, false, num3).Build()).Cells;
					for (int y = 0; y < val6.height; y++)
					{
						int x;
						for (x = 0; x < val6.width; x++)
						{
							if (cells.All((Cell c) => free.Contains(new Cell(c.X + x, c.Y + y))))
							{
								return Ready();
							}
						}
					}
				}
				return Stopped("输出区没有空位（未计可堆叠空间）");
			}
			case "desequencer":
				if (!(Str(m, "DESEQUENCER_STATE_TAG") == "STATE_DESEQUENCING"))
				{
					return Stopped("尚未开始解序");
				}
				return Ready();
			case "hydroponic":
				if (!MachineHydroponic.CanWater(m))
				{
					return Stopped("灌溉用水不足");
				}
				if (MachineProgressHelper.GetMachineState(m) == "STATE_READY")
				{
					if (Children((GameInventory?)(object)MachineHydroponic.GetInputSlot(m)).Length == 0)
					{
						return Stopped("未放种子");
					}
					return Ready();
				}
				goto case "printer";
			case "printer":
			case "evaporator":
				if (!m.IsTag("MACHINE_STATE_TAG"))
				{
					return Stopped("运行状态接口未核验");
				}
				if (MachineProgressHelper.GetMachineState(m) == "STATE_READY")
				{
					return Stopped("尚未开始加工");
				}
				if (!MachineProgressHelper.IsProgressTypeMachineFinished(m))
				{
					return Ready();
				}
				return Stopped("加工已完成，等待取出");
			default:
				return Stopped("运行条件未核验");
			}
		}
	}
	public sealed record BatterySettings
	{
		public bool BeforeNight { get; set; } = true;
	}
	public sealed class Entry : MelonMod
	{
		private BatterySettings batterySettings = new BatterySettings();

		private static Entry? batteryOwner;

		private bool batteryBusy;

		private DateTime lastBatteryNight;

		private string lastBatteryIdentity = "";

		private string batterySummary = "点击检查，读取当前机器功耗和可对调的电池。";

		private string[] batteryRows = Array.Empty<string>();

		private int bonusMachineId = -1;

		private int bonusFocus = -1;

		private bool replaceBonus;

		private string bonusP = "0";

		private string bonusE = "0";

		private string bonusQ = "0";

		private MachineResult[] waterCurrent = Array.Empty<MachineResult>();

		private MachineResult[] waterPreview = Array.Empty<MachineResult>();

		private MachineResult[] waterCurrentCollectable = Array.Empty<MachineResult>();

		private MachineResult[] waterPreviewCollectable = Array.Empty<MachineResult>();

		private string waterReasons = "";

		private string lastStatusReport = "";

		private GameAdapter adapter;

		private Snapshot? snapshot;

		private Plan? plan;

		private Plan? live;

		private Task<Plan>? worker;

		private CancellationTokenSource? cancel;

		private Task<PurchaseAdvice[]>? adviceWorker;

		private PurchaseAdvice[]? advice;

		private Dictionary<int, string> trainingStatus = new Dictionary<int, string>();

		private int view;

		private int reportPage;

		private MachineSummaryRow[] summaries = Array.Empty<MachineSummaryRow>();

		private string summaryKind = "";

		private DateTime nextSummary = DateTime.MinValue;

		private bool visible;

		private bool stable;

		private string status = "进入存档后按 F8，点击扫描并复算。";

		private int page;

		private GUIStyle? label;

		private GUIStyle? button;

		private GUIStyle? mono;

		private GUIStyle? summaryStyle;

		private GUIStyle? solid;

		private readonly List<(Rect Rect, Action Run, string Text)> clickTargets = new List<(Rect, Action, string)>();

		private readonly List<Behaviour> blockedInput = new List<Behaviour>();

		private int closedFrame = -1;

		private float seconds = 60f;

		private bool autoScanned;

		private DateTime nextScan = DateTime.MinValue;

		private ItemNumbers numbers = new ItemNumbers();

		private NumberDisplaySettings numberSettings = new NumberDisplaySettings();

		private readonly Dictionary<int, string> numberLabels = new Dictionary<int, string>();

		private readonly Dictionary<int, string> numberNames = new Dictionary<int, string>();

		private readonly Dictionary<int, TreeNodeRender> numberBadges = new Dictionary<int, TreeNodeRender>();

		private readonly Dictionary<int, (PixelWindow Window, string Original, string Added)> numberedWindows = new Dictionary<int, (PixelWindow, string, string)>();

		private string numberIdentity = "";

		private bool captureNumberKey;

		private DateTime nextNumberScan;

		private DateTime nextNumberWarning;

		private KeyCode numberKey = (KeyCode)288;

		private static Entry? numberOwner;

		private string NumberPath => Path.Combine(adapter.DataDir, "item-numbers.json");

		private string NumberSettingsPath => Path.Combine(adapter.DataDir, "number-display.json");

		private void InitializeBatteries()
		{
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Expected Obj, but got Unknown
			string path = Path.Combine(adapter.DataDir, "battery-settings.json");
			if (File.Exists(path))
			{
				batterySettings = JsonSerializer.Deserialize<BatterySettings>(File.ReadAllText(path)) ?? new BatterySettings();
			}
			MethodInfo method = typeof(PlayerStore).GetMethod("EndNight");
			if (method == null)
			{
				throw new InvalidOperationException("未找到原生夜间结算入口，未启用自动换电。");
			}
			((MelonBase)this).HarmonyInstance.Patch((MethodBase)method, new HarmonyMethod(typeof(Entry).GetMethod("BatteryNightPrefix", BindingFlags.Static | BindingFlags.NonPublic))
			{
				priority = 800
			}, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			batteryOwner = this;
			((MelonBase)this).LoggerInstance.Msg("2.2 电池对调：睡前自动检查=" + batterySettings.BeforeNight);
		}

		private static bool BatteryNightPrefix()
		{
			Entry entry = batteryOwner;
			if (entry == null || !entry.batterySettings.BeforeNight || !entry.adapter.Compatible || entry.batteryBusy)
			{
				return true;
			}
			try
			{
				if (entry.adapter.RecoveryStaging != null)
				{
					throw new InvalidOperationException("存在未完成的恢复事务，请先点击恢复原布局，再继续睡觉。");
				}
				string text = PlayerStore.Instance.runID + ":" + PlayerStore.Instance.saveSlotId;
				if (text == entry.lastBatteryIdentity && (DateTime.UtcNow - entry.lastBatteryNight).TotalSeconds < 3.0)
				{
					return true;
				}
				entry.lastBatteryIdentity = text;
				entry.lastBatteryNight = DateTime.UtcNow;
				entry.RunBatteryExchange(verifyRestore: false);
			}
			catch (Exception e)
			{
				entry.Fail(e);
				if (entry.adapter.RecoveryStaging != null)
				{
					entry.SetVisible(value: true);
					return false;
				}
			}
			return true;
		}

		private void CheckBatteries()
		{
			BatteryReview batteryReview = adapter.ReviewBatteries();
			batterySummary = $"已检查 {batteryReview.Checked} 台 · 电量不足且可移动 {batteryReview.Insufficient} 台 · 可用备用电池 {batteryReview.Spares} 件 · 可对调 {batteryReview.Swaps.Length} 对";
			batteryRows = batteryReview.Swaps.Select((BatterySwap p) => $"{MachineNumber(p.Machine.uniqueId)}：今晚需 {p.Demand}，原电池 {p.OldEnergy} → 换入 {p.FreshEnergy}\n换入 {p.Fresh.name} #{p.Fresh.uniqueId}；原电池将回到 {NumberedText(p.Source)}。").Concat(batteryReview.Notes.Select(NumberedText)).ToArray();
			reportPage = 0;
		}

		private void RunBatteryExchange(bool verifyRestore)
		{
			if (batteryBusy)
			{
				throw new InvalidOperationException("电池对调正在执行。");
			}
			batteryBusy = true;
			try
			{
				string text = (status = adapter.ExchangeBatteries(verifyRestore));
				((MelonBase)this).LoggerInstance.Msg(text);
				CheckBatteries();
				if (snapshot != null)
				{
					RefreshSummary(refreshRoster: false);
				}
			}
			finally
			{
				batteryBusy = false;
			}
		}

		private void DrawBatteries(float x, float y, float w, float h, bool idle)
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			bool flag = idle && !batteryBusy;
			DrawButton(new Rect(x, y, 235f, 32f), batterySettings.BeforeNight ? "睡前自动换电：开启" : "睡前自动换电：关闭", !batteryBusy, () =>
			{
				batterySettings.BeforeNight = !batterySettings.BeforeNight;
				WriteSettings(Path.Combine(adapter.DataDir, "battery-settings.json"), batterySettings);
			});
			DrawButton(new Rect(x + 245f, y, 160f, 32f), "检查今晚用电", flag, CheckBatteries);
			DrawButton(new Rect(x + 415f, y, 140f, 32f), "立即对调", flag && adapter.Compatible, () =>
			{
				RunBatteryExchange(verifyRestore: false);
			});
			DrawButton(new Rect(x + 565f, y, 190f, 32f), "验证对调并恢复", flag && adapter.Compatible, () =>
			{
				RunBatteryExchange(verifyRestore: true);
			});
			y += 40f;
			GUI.Label(new Rect(x, y, w - 55f, 66f), "睡觉结算前，按当前实际功耗给电量不足的机器换电。优先从充能器取够电的闲置电池，与旧电池原位对调；不抽走其他机器、手动设备或能量阵列正在使用的电池。找不到兼容电池时保留原状并提示。", label);
			y += 72f;
			GUI.Label(new Rect(x, y, w - 55f, 50f), batterySummary, label);
			y += 54f;
			GUI.Label(new Rect(x, y, w - 55f, 54f), adapter.LastBatteryResult, mono);
			y += 58f;
			int num = Math.Max(1, (int)((h - y - 48f) / 68f));
			ReportNavigation(x, ref y, batteryRows.Length, num);
			foreach (string item in batteryRows.Skip(reportPage * num).Take(num))
			{
				GUI.Label(new Rect(x, y, w - 55f, 64f), item, label);
				y += 68f;
			}
		}

		private Machine[] OrderedMachines()
		{
			return (from m in snapshot.Machines
				orderby Array.FindIndex(adapter.Supply.Resolve(), (SupplyPreference p) => p.Kind == m.Kind), m.Id
				select m).ToArray();
		}

		private void ReadBonusFields(Machine m)
		{
			bonusMachineId = m.Id;
			bonusP = m.ExternalBonus.P.ToString();
			bonusE = m.ExternalBonus.E.ToString();
			bonusQ = m.ExternalBonus.Q.ToString();
			bonusFocus = -1;
		}

		private void HandleBonusInput()
		{
			if (view != 4 || bonusFocus < 0 || worker != null || adviceWorker != null)
			{
				return;
			}
			if (Input.GetKeyDown((KeyCode)27))
			{
				bonusMachineId = -1;
				bonusFocus = -1;
				return;
			}
			if ((Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305)) && Input.GetKeyDown((KeyCode)97))
			{
				replaceBonus = true;
				return;
			}
			string inputString = Input.inputString;
			for (int i = 0; i < inputString.Length; i++)
			{
				char c = inputString[i];
				if (c == '\r' || c == '\n')
				{
					try
					{
						SaveBonuses();
						break;
					}
					catch (Exception e)
					{
						Fail(e);
						break;
					}
				}
				string text = bonusFocus switch
				{
					0 => bonusP, 
					1 => bonusE, 
					_ => bonusQ, 
				};
				if (c == '\b')
				{
					string text2;
					if (!replaceBonus)
					{
						if (text.Length <= 0)
						{
							text2 = "";
						}
						else
						{
							string text3 = text;
							text2 = text3.Substring(0, text3.Length - 1);
						}
					}
					else
					{
						text2 = "";
					}
					text = text2;
					replaceBonus = false;
				}
				else if (char.IsAscii(c) && (char.IsDigit(c) || c == '-' || c == '+'))
				{
					if (replaceBonus)
					{
						text = "";
						replaceBonus = false;
					}
					if (text.Length < 7 && ((c != '-' && c != '+') || text.Length == 0))
					{
						text += c;
					}
				}
				if (bonusFocus == 0)
				{
					bonusP = text;
				}
				else if (bonusFocus == 1)
				{
					bonusE = text;
				}
				else
				{
					bonusQ = text;
				}
			}
		}

		private void SaveBonuses()
		{
			if (!(snapshot == null) && bonusMachineId >= 0 && worker == null && adviceWorker == null)
			{
				if (!int.TryParse(bonusP, out var result) || !int.TryParse(bonusE, out var result2) || !int.TryParse(bonusQ, out var result3))
				{
					throw new InvalidOperationException("请填写整数百分比，例如 30、0 或 -10。");
				}
				Machine machine = snapshot.Machines.Single((Machine m) => m.Id == bonusMachineId);
				adapter.BonusSettings.Set(snapshot.Identity, machine.Id, machine.Kind, result, result2, result3);
				Directory.CreateDirectory(adapter.DataDir);
				File.WriteAllText(Path.Combine(adapter.DataDir, "external-bonuses.json"), JsonSerializer.Serialize(adapter.BonusSettings, GameAdapter.Json));
				Scan();
				status = "外部加成已保存，仅用于计算。旧方案已清除，请重新优化。";
				((MelonBase)this).LoggerInstance.Msg(status);
			}
		}

		private void DrawBonuses(float x, float y, float w, bool idle)
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_021f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ed: Unknown result type (might be due to invalid IL or missing references)
			Machine[] array = OrderedMachines();
			if (array.Length == 0)
			{
				return;
			}
			page = Math.Clamp(page, 0, array.Length - 1);
			Machine machine = array[page];
			if (bonusMachineId != machine.Id)
			{
				ReadBonusFields(machine);
			}
			DrawButton(new Rect(x, y, 100f, 30f), "上一台", idle && page > 0, () =>
			{
				page--;
				bonusMachineId = -1;
			});
			GUI.Label(new Rect(x + 110f, y, w - 260f, 30f), $"{page + 1}/{array.Length} · {MachineNumber(machine.Id)}", label);
			DrawButton(new Rect(x + w - 145f, y, 100f, 30f), "下一台", idle && page < array.Length - 1, () =>
			{
				page++;
				bonusMachineId = -1;
			});
			y += 42f;
			GUI.Label(new Rect(x, y, w - 55f, 52f), "填写其他来源在格盘之外提供的 P/E/Q 加成，按机器和存档保存。仅用于计算，不添加属性；已公开在机器总属性中的相同加成不会重复计算。", label);
			y += 58f;
			string[] array2 = new string[3] { "额外性能 P（%）", "额外效率 E（%）", "额外质量 Q（%）" };
			string[] array3 = new string[3] { bonusP, bonusE, bonusQ };
			float num = (w - 55f) / 3f;
			for (int num2 = 0; num2 < 3; num2++)
			{
				int field = num2;
				float num3 = x + (float)num2 * num;
				GUI.Label(new Rect(num3, y, num - 10f, 24f), array2[num2], label);
				DrawButton(new Rect(num3, y + 26f, num - 14f, 34f), ((bonusFocus == num2) ? "? " : "") + array3[num2] + ((bonusFocus == num2) ? " |" : ""), idle, () =>
				{
					bonusFocus = field;
					replaceBonus = true;
				});
			}
			y += 68f;
			GUI.Label(new Rect(x, y, w - 55f, 44f), "点击输入框后键入整数；Enter 保存，Esc 取消。E 正数表示节电效率，不是直接减少的功耗值。只支持相加的百分比点数。", label);
			y += 48f;
			DrawButton(new Rect(x, y, 170f, 34f), "保存并重新扫描", idle, SaveBonuses);
			DrawButton(new Rect(x + 185f, y, 170f, 34f), "清除此台外部加成", idle, () =>
			{
				bonusP = "0";
				bonusE = "0";
				bonusQ = "0";
				SaveBonuses();
			});
		}

		private void DrawWaterDetails(float x, float y, float w, float h)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			if (waterCurrent.Length == 0)
			{
				GUI.Label(new Rect(x, y, w - 50f, 40f), "当前没有水分收集器。", label);
				return;
			}
			GUI.Label(new Rect(x, y, w - 50f, 70f), "优质水：质量 Q ≥ +100% 的新产水（含纯水）。\n基础水：+50% ≤ Q < +100% 的新产水。幽灵水等其他等级不计入这两项。", label);
			y += 78f;
			GUI.Label(new Rect(x, y, w - 50f, 85f), "额定日产量展示配装可以达到的产能；可收集量再考虑每台电源、独立容器和水塔剩余容量。\n按各机新产水的原始等级分别统计；不计算水塔混合后的水质，也不计净水器后续处理。", label);
			y += 92f;
			GUI.Label(new Rect(x, y, w - 50f, 85f), WaterTowerBridge.Status + "\n没有独立容器且位于水塔内的水机直接产入水塔；有容器的水机只装入自己的容器。共享水箱按水塔的顺序分配余量。", label);
			y += 92f;
			GUI.Label(new Rect(x, y, w - 50f, 110f), "未计入可收集量的原因：" + ((waterReasons.Length == 0) ? "无。" : waterReasons), label);
		}

		public override void OnInitializeMelon()
		{
			adapter = new GameAdapter
			{
				DataDir = Path.Combine(MelonEnvironment.GameRootDirectory, "UserData", "WaterOptimizer")
			};
			using (FileStream inputStream = File.OpenRead(Path.Combine(MelonEnvironment.GameRootDirectory, "GameAssembly.dll")))
			{
				adapter.Compatible = GameAdapter.SupportedHashes.Contains(Convert.ToHexString(SHA256.Create().ComputeHash(inputStream)));
			}
			try
			{
				string path = Path.Combine(adapter.DataDir, "supply-settings.json");
				if (File.Exists(path))
				{
					adapter.Supply = JsonSerializer.Deserialize<SupplySettings>(File.ReadAllText(path)) ?? new SupplySettings();
				}
			}
			catch (Exception ex)
			{
				((MelonBase)this).LoggerInstance.Warning("优先级设置无法读取，使用默认值：" + ex.Message);
			}
			try
			{
				string path2 = Path.Combine(adapter.DataDir, "external-bonuses.json");
				if (File.Exists(path2))
				{
					adapter.BonusSettings = JsonSerializer.Deserialize<ExternalBonusSettings>(File.ReadAllText(path2)) ?? new ExternalBonusSettings();
				}
			}
			catch (Exception ex2)
			{
				((MelonBase)this).LoggerInstance.Warning("额外加成设置无法读取：" + ex2.Message);
			}
			try
			{
				InitializeBatteries();
			}
			catch (Exception ex3)
			{
				((MelonBase)this).LoggerInstance.Error("电池自动对调初始化失败：" + ex3);
			}
			try
			{
				InitializeNumbers();
			}
			catch (Exception ex4)
			{
				((MelonBase)this).LoggerInstance.Error("编号初始化失败：" + ex4);
			}
			((MelonBase)this).LoggerInstance.Msg("F8: Auto Module Layout Optimizer 2.2. Native build match: " + adapter.Compatible);
		}

		public override void OnUpdate()
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				NumberInput();
				UpdateNumbers();
			}
			catch (Exception e)
			{
				Fail(e);
			}
			if (Input.GetKeyDown((KeyCode)289))
			{
				SetVisible(!visible);
			}
			if (visible)
			{
				BlockInput((Behaviour?)(object)EventSystem.current);
				BlockInput((Behaviour?)(object)InputActionManager.current);
				if (Input.GetMouseButtonDown(0))
				{
					Vector3 mousePosition = Input.mousePosition;
					Vector2 val = new Vector2(mousePosition.x, (float)Screen.height - mousePosition.y);
					for (int num = clickTargets.Count - 1; num >= 0; num--)
					{
						(Rect, Action, string) tuple = clickTargets[num];
						if (tuple.Item1.Contains(val))
						{
							(Rect, Action, string) tuple2 = clickTargets[num];
							((MelonBase)this).LoggerInstance.Msg("点击：" + tuple2.Item3);
							try
							{
								tuple2.Item2();
							}
							catch (Exception e2)
							{
								Fail(e2);
							}
							break;
						}
					}
				}
				HandleBonusInput();
			}
			else if (Time.frameCount > closedFrame && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
			{
				ReleaseInput();
			}
			if (!autoScanned && DateTime.UtcNow >= nextScan && PlayerStore.IsInstanceExist())
			{
				nextScan = DateTime.UtcNow.AddSeconds(8.0);
				try
				{
					if (PlayerStore.Instance.FindAllItem(true).Count > 0)
					{
						Scan();
						autoScanned = true;
					}
				}
				catch (Exception e3)
				{
					autoScanned = true;
					Fail(e3);
				}
			}
			Task<Plan>? task = worker;
			if (task != null && task.IsCompleted)
			{
				try
				{
					plan = worker.GetAwaiter().GetResult();
					status = $"搜索完成：已装 {plan.Placements.Length} 件；优质水 {L(plan.QualityVolume)} L/日，基础水 {(double)MachineSummary.BaseWater(plan.Machines) / 1000000.0:0.00} L/日；" + (plan.ProvenOptimal ? "已完成穷举证明" : "当前找到的最好方案，未证明全局最优");
					File.WriteAllText(Path.Combine(adapter.DataDir, "latest-plan.json"), JsonSerializer.Serialize(plan, GameAdapter.Json));
				}
				catch (Exception e4)
				{
					Fail(e4);
				}
				worker = null;
				advice = null;
				if (snapshot != null)
				{
					trainingStatus = adapter.ReadTrainingStatus(plan?.Machines ?? Rules.Evaluate(snapshot, snapshot.Current));
				}
			}
			Task<PurchaseAdvice[]>? task2 = adviceWorker;
			if (task2 != null && task2.IsCompleted)
			{
				try
				{
					advice = adviceWorker.GetAwaiter().GetResult();
				}
				catch (Exception e5)
				{
					Fail(e5);
				}
				adviceWorker = null;
			}
			if (visible && snapshot != null && DateTime.UtcNow >= nextSummary && adapter.RecoveryStaging == null)
			{
				RefreshSummary();
			}
		}

		private void RefreshSummary(bool refreshRoster = true)
		{
			nextSummary = DateTime.UtcNow.AddSeconds(3.0);
			if (snapshot == null)
			{
				return;
			}
			try
			{
				if (refreshRoster && worker == null && adviceWorker == null && bonusFocus < 0 && adapter.MachineRosterChanged(snapshot))
				{
					HashSet<int> previous = snapshot.Machines.Select((Machine m) => m.Id).ToHashSet();
					Scan();
					Machine machine = snapshot.Machines.FirstOrDefault((Machine m) => !previous.Contains(m.Id));
					if (machine != null)
					{
						summaryKind = machine.Kind;
					}
					status += " 机器列表已变化，已自动刷新；请重新优化。";
					return;
				}
				MachineResult[] array = adapter.ReadCurrentResults(snapshot);
				MachineResult[] array2 = (plan ?? live)?.Machines ?? array;
				Dictionary<int, OperatingState> dictionary = adapter.ReadOperatingStates(array);
				Dictionary<int, OperatingState> previewState = ((array == array2) ? dictionary : adapter.ReadOperatingStates(array2));
				summaries = MachineSummary.Build(snapshot, array, array2, dictionary, previewState);
				waterCurrent = array.Where((MachineResult r) => snapshot.Machines.Any((Machine m) => m.Id == r.Id && m.Kind == "moisture_farm")).ToArray();
				waterPreview = array2.Where((MachineResult r) => snapshot.Machines.Any((Machine m) => m.Id == r.Id && m.Kind == "moisture_farm")).ToArray();
				waterCurrentCollectable = MachineSummary.Collectable(waterCurrent, dictionary);
				waterPreviewCollectable = MachineSummary.Collectable(waterPreview, previewState);
				waterReasons = string.Join("；", (from r in waterPreview
					select previewState.GetValueOrDefault(r.Id) into s
					where s != null && !s.Ready
					select s.Reason).Distinct());
				string text = JsonSerializer.Serialize(new
				{
					summaries = summaries,
					currentState = dictionary,
					previewState = previewState,
					Towers = WaterTowerBridge.Status
				}, GameAdapter.Json);
				if (text != lastStatusReport)
				{
					File.WriteAllText(Path.Combine(adapter.DataDir, "status-summary.json"), text);
					lastStatusReport = text;
				}
			}
			catch (Exception ex)
			{
				summaries = new MachineSummaryRow[1]
				{
					new MachineSummaryRow("", "效果汇总暂不可用", ex.Message + "；请重新扫描。")
				};
			}
		}

		private void Scan()
		{
			snapshot = adapter.Capture();
			UpdateNumbers(force: true);
			plan = null;
			live = null;
			advice = null;
			reportPage = 0;
			bonusMachineId = -1;
			bonusFocus = -1;
			trainingStatus = adapter.ReadTrainingStatus(Rules.Evaluate(snapshot, snapshot.Current));
			RefreshSummary(refreshRoster: false);
			status = ((adapter.Validation.Count == 0) ? $"核验通过：{snapshot.Machines.Length} 台机器，{snapshot.Modules.Length} 件模组。" : string.Join("\n", adapter.Validation));
			if (snapshot.Machines.Any((Machine m) => m.ExternalBonus != Stats.Zero))
			{
				status += " 已启用手填外部加成（计算假设，不修改游戏属性）。";
			}
			if (snapshot.Modules.Any((WaterOptimizer.Core.Module m) => m.NeuralCore))
			{
				status += $" 神经核心 {snapshot.Modules.Count((WaterOptimizer.Core.Module m) => m.NeuralCore)} 件已纳入。";
			}
			((MelonBase)this).LoggerInstance.Msg(status);
		}

		private void Solve()
		{
			Scan();
			if (adapter.Validation.Count > 0)
			{
				throw new InvalidOperationException("复算不一致，不能求解");
			}
			Snapshot s = snapshot;
			bool mode = stable;
			cancel = new CancellationTokenSource();
			CancellationToken token = cancel.Token;
			worker = Task.Run(() => new GlobalSolver().Solve(s, mode, TimeSpan.FromSeconds(seconds), token, (Plan p) =>
			{
				live = p;
			}));
			status = "全机器联合搜索中，可随时停止并保留最佳结果。";
		}

		private void Fail(Exception e)
		{
			status = e.Message;
			((MelonBase)this).LoggerInstance.Error(e.ToString());
		}

		private void SetVisible(bool value)
		{
			visible = value;
			clickTargets.Clear();
			if (value)
			{
				BlockInput((Behaviour?)(object)EventSystem.current);
				BlockInput((Behaviour?)(object)InputActionManager.current);
				nextSummary = DateTime.MinValue;
			}
			else
			{
				closedFrame = Time.frameCount;
				bonusFocus = -1;
				captureNumberKey = false;
			}
		}

		private void StartAdvice()
		{
			if (!(snapshot == null) && worker == null && adviceWorker == null && adapter.Validation.Count <= 0)
			{
				Snapshot s = snapshot;
				Placement[] ps = (plan?.Placements ?? snapshot.Current).ToArray();
				bool mode = stable;
				cancel = new CancellationTokenSource();
				CancellationToken token = cancel.Token;
				advice = null;
				reportPage = 0;
				adviceWorker = Task.Run(() => ShoppingAdviser.Build(s, ps, mode, TimeSpan.FromSeconds(5.0), token));
			}
		}

		private void SelectView(int next)
		{
			view = next;
			reportPage = 0;
			captureNumberKey = false;
			bonusFocus = -1;
			if (view == 2 && advice == null)
			{
				StartAdvice();
			}
			if (view == 7)
			{
				CheckBatteries();
			}
		}

		private void BlockInput(Behaviour? component)
		{
			if (!((Object)(object)component == (Object)null) && component.enabled)
			{
				if (!blockedInput.Any((Behaviour x) => ((Il2CppObjectBase)x).Pointer == ((Il2CppObjectBase)component).Pointer))
				{
					blockedInput.Add(component);
				}
				component.enabled = false;
			}
		}

		private void ReleaseInput()
		{
			foreach (Behaviour item in blockedInput)
			{
				if ((Object)(object)item != (Object)null)
				{
					item.enabled = true;
				}
			}
			blockedInput.Clear();
		}

		private void DrawButton(Rect rect, string text, bool enabled, Action action)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			GUI.enabled = enabled;
			GUI.Box(rect, text, button);
			GUI.enabled = true;
			if (enabled)
			{
				clickTargets.Add((rect, action, text));
			}
		}

		private void RollbackTest()
		{
			status = "正在备份并验证装卸……";
			((MelonBase)this).LoggerInstance.Msg(status);
			Scan();
			Snapshot snapshot = this.snapshot;
			MachineResult[] array = Rules.Evaluate(snapshot, snapshot.Current);
			adapter.Apply(snapshot, new Plan(snapshot.Fingerprint, Stable: false, snapshot.Current, array, array.Sum((MachineResult r) => r.QualityVolume), 0L, ProvenOptimal: false, 0.0), rollbackTest: true);
			Scan();
			status = "装卸与回滚验证通过，原布局已恢复并保存。";
			((MelonBase)this).LoggerInstance.Msg(status);
		}

		private static string L(int units)
		{
			return ((double)units / 1000000.0).ToString("0.00");
		}

		public override void OnGUI()
		{
			//IL_1271: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Invalid comparison between Unknown and I4
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0128: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected Obj, but got Unknown
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Expected Obj, but got Unknown
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Expected Obj, but got Unknown
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Expected Obj, but got Unknown
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Expected Obj, but got Unknown
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0228: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0302: Unknown result type (might be due to invalid IL or missing references)
			//IL_033b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0393: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0436: Unknown result type (might be due to invalid IL or missing references)
			//IL_049d: Unknown result type (might be due to invalid IL or missing references)
			//IL_04da: Unknown result type (might be due to invalid IL or missing references)
			//IL_0517: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_06b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0926: Unknown result type (might be due to invalid IL or missing references)
			//IL_086f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0967: Unknown result type (might be due to invalid IL or missing references)
			//IL_08b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_09a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_07d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_09fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a42: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a89: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bb4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bec: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c50: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d74: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ea1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ff3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0f31: Unknown result type (might be due to invalid IL or missing references)
			//IL_114b: Unknown result type (might be due to invalid IL or missing references)
			//IL_10cb: Unknown result type (might be due to invalid IL or missing references)
			if (!visible || (int)Event.current.type != 7)
			{
				return;
			}
			clickTargets.Clear();
			int depth = GUI.depth;
			Color color = GUI.color;
			try
			{
				GUI.depth = -10000;
				GUI.color = Color.white;
				if (label == null)
				{
					label = new GUIStyle(GUI.skin.label)
					{
						fontSize = 16,
						wordWrap = true
					};
					button = new GUIStyle(GUI.skin.button)
					{
						fontSize = 16
					};
					mono = new GUIStyle(label)
					{
						fontSize = 14,
						wordWrap = false
					};
					summaryStyle = new GUIStyle(mono)
					{
						wordWrap = true
					};
					solid = new GUIStyle();
					solid.normal.background = Texture2D.whiteTexture;
				}
				float num = Math.Min(1040, Screen.width - 32);
				float num2 = Screen.height - 32;
				Rect val = new Rect(16f, 16f, num, num2);
				GUI.color = new Color(0.08f, 0.1f, 0.13f, 1f);
				GUI.Box(val, "", solid);
				GUI.color = Color.white;
				float num3 = 28f;
				float num4 = 30f;
				GUI.Label(new Rect(num4, num3, num - 80f, 30f), "Auto Module Layout Optimizer 2.2 (formerly WaterOptimizer)  |  F8", label);
				num3 += 36f;
				DrawButton(new Rect(num - 35f, 24f, 42f, 28f), "×", enabled: true, () =>
				{
					SetVisible(value: false);
				});
				bool flag = worker == null && adviceWorker == null && adapter.RecoveryStaging == null && !batteryBusy;
				DrawButton(new Rect(num4, num3, 170f, 34f), "扫描并复算当前状态", flag, Scan);
				DrawButton(new Rect(num4 + 180f, num3, 170f, 34f), stable ? "策略：长期稳定" : "策略：当前极限", flag, () =>
				{
					stable = !stable;
					this.plan = null;
					live = null;
					advice = null;
				});
				DrawButton(new Rect(num4 + 360f, num3, 135f, 34f), $"搜索 {seconds:0} 秒", flag, () =>
				{
					seconds = ((seconds == 60f) ? 180 : ((seconds == 180f) ? 600 : 60));
				});
				DrawButton(new Rect(num4 + 505f, num3, 155f, 34f), "全局联合优化", flag, Solve);
				DrawButton(new Rect(num4 + 670f, num3, 90f, 34f), "停止搜索", worker != null, () =>
				{
					cancel?.Cancel();
				});
				DrawButton(new Rect(num4 + 770f, num3, 200f, 34f), (view == 7) ? "● 电池自动对调" : "电池自动对调", flag, () =>
				{
					SelectView(7);
				});
				num3 += 42f;
				Plan plan = this.plan ?? live;
				DrawButton(new Rect(num4, num3, 170f, 34f), "应用方案（自动备份）", flag && this.plan != null && adapter.Compatible && adapter.Validation.Count == 0, () =>
				{
					string text3 = adapter.Apply(snapshot, this.plan);
					Scan();
					status = "已应用并保存。" + adapter.LastStorageSummary + "。备份：" + text3;
					((MelonBase)this).LoggerInstance.Msg(status);
				});
				DrawButton(new Rect(num4 + 180f, num3, 170f, 34f), "恢复原布局", adapter.RecoveryStaging != null, () =>
				{
					adapter.Restore();
					Scan();
					status = "已恢复并保存原布局。";
					((MelonBase)this).LoggerInstance.Msg(status);
				});
				DrawButton(new Rect(num4 + 360f, num3, 200f, 34f), "验证装卸与回滚", flag && snapshot != null && adapter.Compatible && adapter.Validation.Count == 0, RollbackTest);
				DrawButton(new Rect(num4 + 570f, num3, num - 600f, 34f), adapter.StorageLabel, flag, () =>
				{
					adapter.CycleStorage();
				});
				num3 += 42f;
				GUI.Label(new Rect(num4, num3, num - 50f, 65f), status, label);
				num3 += 65f;
				if (!adapter.Compatible)
				{
					GUI.Label(new Rect(num4, num3, num - 50f, 30f), "当前游戏版本尚未适配，无法应用；请更新 Mod。", label);
					num3 += 32f;
				}
				if (snapshot == null)
				{
					if (view == 7)
					{
						DrawBatteries(num4, num3, num, num2, flag);
					}
					return;
				}
				MachineResult[] source = plan?.Machines ?? Rules.Evaluate(snapshot, snapshot.Current);
				Placement[] array = plan?.Placements ?? snapshot.Current;
				(int, long) tuple = Cultivation.Score(snapshot, array);
				int value = snapshot.Modules.Count(Cultivation.Candidate);
				GUI.Label(new Rect(num4, num3, num - 50f, 26f), $"当前效果 → 方案预览 | {snapshot.Machines.Length} 台机器 · 已装 {array.Length}/{snapshot.Modules.Length} 件 · 培养 {tuple.Item1}/{value} 件", label);
				num3 += 28f;
				GUI.Label(new Rect(num4, num3, num - 50f, 24f), $"点击机器类型查看效果 · 已拥有 {summaries.Length} 类 · 每 3 秒刷新", mono);
				num3 += 26f;
				if (summaries.Length != 0)
				{
					if (!summaries.Any((MachineSummaryRow r) => r.Kind == summaryKind))
					{
						summaryKind = summaries[0].Kind;
					}
					int num5 = Math.Max(1, (int)((num - 50f) / 155f));
					float num6 = (num - 50f) / (float)num5;
					for (int num7 = 0; num7 < summaries.Length; num7++)
					{
						MachineSummaryRow machineSummaryRow = summaries[num7];
						string kind = machineSummaryRow.Kind;
						string text = MachineProfiles.Find(kind)?.Name ?? "状态提示";
						DrawButton(new Rect(num4 + (float)(num7 % num5) * num6, num3 + (float)(num7 / num5 * 32), num6 - 6f, 28f), ((summaryKind == kind) ? "● " : "") + text, enabled: true, () =>
						{
							summaryKind = kind;
						});
					}
					num3 += (float)((summaries.Length + num5 - 1) / num5 * 32);
					MachineSummaryRow machineSummaryRow2 = summaries.First((MachineSummaryRow r) => r.Kind == summaryKind);
					GUI.Label(new Rect(num4, num3, num - 50f, 24f), machineSummaryRow2.Title, label);
					int num8 = ((machineSummaryRow2.Kind == "moisture_farm") ? 68 : 43);
					GUI.Label(new Rect(num4, num3 + 24f, num - 50f, (float)num8), machineSummaryRow2.Detail, summaryStyle);
					num3 += (float)(num8 + 27);
				}
				DrawButton(new Rect(num4, num3, 130f, 30f), (view == 0) ? "● 机器布局" : "机器布局", enabled: true, () =>
				{
					SelectView(0);
				});
				DrawButton(new Rect(num4 + 140f, num3, 150f, 30f), (view == 1) ? "● 自学习培养" : "自学习培养", enabled: true, () =>
				{
					SelectView(1);
				});
				DrawButton(new Rect(num4 + 300f, num3, 130f, 30f), (view == 2) ? "● 购买建议" : "购买建议", enabled: true, () =>
				{
					SelectView(2);
				});
				DrawButton(new Rect(num4 + 440f, num3, 160f, 30f), (view == 3) ? "● 分配优先级" : "分配优先级", flag, () =>
				{
					SelectView(3);
				});
				num3 += 38f;
				DrawButton(new Rect(num4 + 610f, num3 - 38f, 140f, 30f), (view == 4) ? "● 外部加成" : "外部加成", flag, () =>
				{
					SelectView(4);
				});
				DrawButton(new Rect(num4 + 760f, num3 - 38f, 115f, 30f), (view == 5) ? "● 产水说明" : "产水说明", enabled: true, () =>
				{
					SelectView(5);
				});
				DrawButton(new Rect(num4 + 885f, num3 - 38f, 100f, 30f), (view == 6) ? "● 编号" : "编号设置", enabled: true, () =>
				{
					SelectView(6);
				});
				if (view == 7)
				{
					DrawBatteries(num4, num3, num, num2, flag);
				}
				else if (view == 6)
				{
					DrawNumberSettings(num4, num3, num);
				}
				else if (view == 1)
				{
					DrawTraining(num4, num3, num, num2, array);
				}
				else if (view == 2)
				{
					DrawAdvice(num4, num3, num, num2);
				}
				else if (view == 3)
				{
					DrawSupply(num4, num3, num, num2, flag);
				}
				else if (view == 4)
				{
					DrawBonuses(num4, num3, num, flag);
				}
				else if (view == 5)
				{
					DrawWaterDetails(num4, num3, num, num2);
				}
				else
				{
					if (snapshot.Machines.Length == 0)
					{
						return;
					}
					page = Math.Clamp(page, 0, snapshot.Machines.Length - 1);
					DrawButton(new Rect(num4, num3, 100f, 30f), "上一台", page > 0, () =>
					{
						page = Math.Max(0, page - 1);
					});
					GUI.Label(new Rect(num4 + 115f, num3, 120f, 30f), $"{page + 1} / {snapshot.Machines.Length}", label);
					DrawButton(new Rect(num4 + 230f, num3, 100f, 30f), "下一台", page < snapshot.Machines.Length - 1, () =>
					{
						page = Math.Min(snapshot.Machines.Length - 1, page + 1);
					});
					num3 += 38f;
					float num9 = num3;
					Dictionary<int, WaterOptimizer.Core.Module> dictionary = snapshot.Modules.ToDictionary((WaterOptimizer.Core.Module module2) => module2.Id);
					{
						foreach (Machine m in (from machine in snapshot.Machines
							orderby Array.FindIndex(adapter.Supply.Resolve(), (SupplyPreference p) => p.Kind == machine.Kind), machine.Id
							select machine).Skip(page).Take(1))
						{
							MachineResult machineResult = source.Single((MachineResult r) => r.Id == m.Id);
							Placement[] array2 = array.Where((Placement p) => p.MachineId == m.Id).ToArray();
							GUI.Label(new Rect(num4, num9, num - 90f, 30f), $"{MachineNumber(m.Id)}  扩容阶段 {m.Expansion} · 可用 {m.Available.Length} 格{(m.Locked ? "（格盘锁定）" : "")}  |  P {machineResult.Stats.P:+0;-0;0} / E {machineResult.Stats.E:+0;-0;0} / Q {machineResult.Stats.Q:+0;-0;0}", label);
							num9 += 30f;
							GUI.Label(new Rect(num4, num9, num - 90f, 30f), $"功耗 {machineResult.Power} · {MachineProfiles.Describe(m, machineResult)}", label);
							num9 += 30f;
							if (m.ExternalBonus != Stats.Zero)
							{
								GUI.Label(new Rect(num4, num9, num - 90f, 25f), $"含手填外部加成：性能 {m.ExternalBonus.P:+0;-0;0}% / 效率 {m.ExternalBonus.E:+0;-0;0}% / 质量 {m.ExternalBonus.Q:+0;-0;0}%（已纳入求解）", mono);
								num9 += 26f;
							}
							GUI.Label(new Rect(num4, num9, num - 70f, 44f), MachineProfiles.Find(m.Kind)?.Explanation ?? "", mono);
							num9 += 46f;
							for (int num10 = 0; num10 < m.Height; num10++)
							{
								for (int num11 = 0; num11 < m.Width; num11++)
								{
									Cell c = new Cell(num11, num10);
									int num12 = Array.FindIndex(array2, (Placement p) => Enumerable.Contains(p.Pose.Cells, c));
									string text2;
									if (Enumerable.Contains(m.Available, c))
									{
										text2 = ((num12 < 0) ? "·" : ModuleNumber(array2[num12].ModuleId));
									}
									else
									{
										text2 = "■";
									}
									GUI.Box(new Rect(num4 + (float)(num11 * 48), num9 + (float)(num10 * 24), 47f, 23f), text2);
								}
							}
							num9 += (float)(m.Height * 24 + 8);
							for (int num13 = 0; num13 < array2.Length; num13++)
							{
								WaterOptimizer.Core.Module module = dictionary[array2[num13].ModuleId];
								GUI.Label(new Rect(num4, num9, num - 90f, 22f), $"{ModuleNumber(module.Id)}: {module.Name}  基础 {module.Base.P}/{module.Base.E}/{module.Base.Q}  {string.Join(",", module.Effects.Select(EffectName))}", mono);
								num9 += 22f;
							}
							num9 += 25f;
						}
						return;
					}
				}
			}
			finally
			{
				GUI.depth = depth;
				GUI.color = color;
				GUI.enabled = true;
			}
		}

		private void ReportNavigation(float x, ref float y, int count, int pageSize)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			int num = Math.Max(1, (count + pageSize - 1) / pageSize);
			reportPage = Math.Clamp(reportPage, 0, num - 1);
			DrawButton(new Rect(x, y, 100f, 30f), "上一页", reportPage > 0, () =>
			{
				reportPage--;
			});
			GUI.Label(new Rect(x + 115f, y, 120f, 30f), $"{reportPage + 1} / {num}", label);
			DrawButton(new Rect(x + 230f, y, 100f, 30f), "下一页", reportPage < num - 1, () =>
			{
				reportPage++;
			});
			y += 38f;
		}

		private void SaveSupply(SupplyPreference[] order)
		{
			adapter.Supply = new SupplySettings
			{
				Order = order
			};
			Directory.CreateDirectory(adapter.DataDir);
			File.WriteAllText(Path.Combine(adapter.DataDir, "supply-settings.json"), JsonSerializer.Serialize(adapter.Supply, GameAdapter.Json));
			int num = reportPage;
			Scan();
			reportPage = num;
			status = "优先级已保存。请重新优化；从上到下分配，集水器排第一时先比优质水量、再比基础水量，其余按属性顺序比较。";
		}

		private void DrawSupply(float x, float y, float w, float h, bool idle)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_023b: Unknown result type (might be due to invalid IL or missing references)
			SupplyPreference[] array = adapter.Supply.Resolve();
			DrawButton(new Rect(x + 350f, y, 160f, 30f), "恢复默认顺序", idle, () =>
			{
				SaveSupply(new SupplySettings().Resolve());
			});
			int num = Math.Max(1, Math.Min(4, (int)(h - y - 100f) / 120));
			ReportNavigation(x, ref y, array.Length, num);
			GUI.Label(new Rect(x, y, w - 60f, 54f), "越靠前越先供应；集水器排第一时固定先比优质水量、再比基础水量，之后才比属性。无效属性自动跳过，点击属性按钮切换顺序。", label);
			y += 58f;
			for (int num2 = reportPage * num; num2 < Math.Min(array.Length, (reportPage + 1) * num); num2++)
			{
				int at = num2;
				SupplyPreference preference = array[at];
				MachineProfile machineProfile = MachineProfiles.Find(preference.Kind);
				int value = snapshot.Machines.Count((Machine m) => m.Kind == preference.Kind);
				GUI.Label(new Rect(x, y, 290f, 30f), $"{at + 1}. {machineProfile.Name}（拥有 {value} 台）", label);
				DrawButton(new Rect(x + 300f, y, 68f, 30f), "上移", idle && at > 0, () =>
				{
					SupplyPreference[] array2 = adapter.Supply.Resolve();
					ref SupplyPreference reference = ref array2[at - 1];
					ref SupplyPreference reference2 = ref array2[at];
					SupplyPreference supplyPreference = array2[at];
					SupplyPreference supplyPreference2 = array2[at - 1];
					reference = supplyPreference;
					reference2 = supplyPreference2;
					SaveSupply(array2);
				});
				DrawButton(new Rect(x + 375f, y, 68f, 30f), "下移", idle && at < array.Length - 1, () =>
				{
					SupplyPreference[] array2 = adapter.Supply.Resolve();
					ref SupplyPreference reference = ref array2[at + 1];
					ref SupplyPreference reference2 = ref array2[at];
					SupplyPreference supplyPreference = array2[at];
					SupplyPreference supplyPreference2 = array2[at + 1];
					reference = supplyPreference;
					reference2 = supplyPreference2;
					SaveSupply(array2);
				});
				DrawButton(new Rect(x + 455f, y, 270f, 30f), MachineProfiles.OrderName(preference.Attributes), idle, () =>
				{
					string[] array2 = new string[6] { "QPE", "QEP", "PQE", "PEQ", "EQP", "EPQ" }.Select((string v) => MachineProfiles.Normalize(preference.Kind, v)).Distinct().ToArray();
					SupplyPreference[] array3 = adapter.Supply.Resolve();
					int num3 = Array.IndexOf(array2, array3[at].Attributes);
					array3[at] = array3[at]with
					{
						Attributes = array2[(num3 + 1) % array2.Length]
					};
					SaveSupply(array3);
				});
				GUI.Label(new Rect(x, y + 35f, w - 65f, 76f), machineProfile.Explanation, label);
				y += 120f;
			}
		}

		private void DrawTraining(float x, float y, float w, float h, Placement[] placements)
		{
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_01de: Unknown result type (might be due to invalid IL or missing references)
			WaterOptimizer.Core.Module[] array = snapshot.Modules.Where((WaterOptimizer.Core.Module module) => module.IsLearning).OrderByDescending(Cultivation.Candidate).ThenByDescending(Cultivation.Value)
				.ToArray();
			if (array.Length == 0)
			{
				GUI.Label(new Rect(x, y, w - 60f, 60f), "当前未发现自学习模组。", label);
				return;
			}
			DrawButton(new Rect(x + 350f, y, 180f, 30f), "检查运行条件", enabled: true, () =>
			{
				trainingStatus = adapter.ReadTrainingStatus(plan?.Machines ?? Rules.Evaluate(snapshot, snapshot.Current));
			});
			int num = Math.Max(1, Math.Min(4, (int)(h - y - 65f) / 110));
			ReportNavigation(x, ref y, array.Length, num);
			foreach (WaterOptimizer.Core.Module m in array.Skip(reportPage * num).Take(num))
			{
				Placement placement = placements.FirstOrDefault((Placement p) => p.ModuleId == m.Id);
				string value;
				if (m.LearningMax == null)
				{
					value = "成长上限未读取";
				}
				else if (m.Unstable)
				{
					value = "短期件，不列为长期培养目标";
				}
				else if (Cultivation.Remaining(m) == Stats.Zero)
				{
					value = "已满成长";
				}
				else if (Cultivation.Candidate(m))
				{
					value = ((placement == null) ? "未安排：本次搜索未找到符合优先级的培养位置" : ("已安排至 " + MachineNumber(placement.MachineId)));
				}
				else
				{
					value = "成熟属性无正向收益，不优先培养";
				}
				string value2 = ((placement == null) ? "可能仍位于库存或其他机器。" : trainingStatus.GetValueOrDefault(placement.MachineId, "请检查运行条件"));
				GUI.Label(new Rect(x, y, w - 60f, 106f), $"{ModuleNumber(m.Id)} {m.Name}  |  {value}\n当前 P/E/Q {m.Base}；成长上限 {m.LearningMax?.ToString() ?? "未知"}；剩余最多 {Cultivation.Cycles(m)} 次成功工作。\n{value2}", label);
				y += 110f;
			}
		}

		private void DrawAdvice(float x, float y, float w, float h)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			DrawButton(new Rect(x + 350f, y, 150f, 30f), "刷新购买建议", worker == null && adviceWorker == null, StartAdvice);
			if (advice == null)
			{
				GUI.Label(new Rect(x, y + 42f, w - 60f, 65f), (adviceWorker != null) ? "正在按当前方案试放参考模组……" : "请先完成优化，再点击刷新购买建议。", label);
				return;
			}
			int num = Math.Max(1, Math.Min(3, (int)(h - y - 115f) / 130));
			ReportNavigation(x, ref y, advice.Length, num);
			GUI.Label(new Rect(x, y, w - 60f, 58f), "按当前预览布局独立试算一件同形状、同属性模组，最多替换一件非培养件。未读取商店和价格；建议收益不能相加，购买前核对实物。", label);
			y += 62f;
			foreach (PurchaseAdvice item in advice.Skip(reportPage * num).Take(num))
			{
				GUI.Label(new Rect(x, y, w - 60f, 28f), NumberedText(item.Title), label);
				GUI.Label(new Rect(x, y + 30f, w - 60f, 95f), NumberedText(item.Detail), label);
				y += 130f;
			}
		}

		private static string EffectName(string x)
		{
			return x.Replace("MODULE_EFFECT_", "").Replace("LEARNING_ALGORITHM", "自学习").Replace("NEGATIVE_FEEDBACK", "负反馈")
				.Replace("RECEIVER", "接收器")
				.Replace("TOP_MOUNT", "顶部挂载")
				.Replace("SIDE_MOUNT", "侧边挂载")
				.Replace("PREMIUM", "优质")
				.Replace("INFERIOR", "劣质")
				.Replace("DEGRADING", "退化")
				.Replace("OVERVOLTED", "过充")
				.Replace("NODE_EFFECT_SUPPORT", "中节点");
		}

		public override void OnApplicationQuit()
		{
			cancel?.Cancel();
			ReleaseInput();
			ClearNumberViews();
			numberOwner = null;
			batteryOwner = null;
		}

		private static void WriteSettings(string path, object value)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(value, GameAdapter.Json));
			File.Move(path + ".tmp", path, overwrite: true);
		}

		private void InitializeNumbers()
		{
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Expected Obj, but got Unknown
			if (File.Exists(NumberPath))
			{
				numbers = JsonSerializer.Deserialize<ItemNumbers>(File.ReadAllText(NumberPath)) ?? throw new InvalidOperationException("编号文件无法读取，请保留文件并反馈。");
			}
			if (File.Exists(NumberSettingsPath))
			{
				numberSettings = JsonSerializer.Deserialize<NumberDisplaySettings>(File.ReadAllText(NumberSettingsPath)) ?? new NumberDisplaySettings();
			}
			if (!Enum.TryParse<KeyCode>(numberSettings.ToggleKey, out numberKey) || !AllowedNumberKey(numberKey))
			{
				numberKey = (KeyCode)288;
			}
			numberSettings.ToggleKey = ((object)numberKey/*cast due to constrained. prefix*/).ToString();
			numberOwner = this;
			MethodInfo method = typeof(GameItemElement).GetMethod("GetTooltipBasic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				throw new InvalidOperationException("物品编号提示接口不存在。");
			}
			((MelonBase)this).HarmonyInstance.Patch((MethodBase)method, (HarmonyMethod)null, new HarmonyMethod(typeof(Entry).GetMethod("NumberTooltip", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		}

		private static void NumberTooltip(GameItemElement __instance, RichTextBuilder __result)
		{
			try
			{
				if (numberOwner != null && PlayerStore.IsInstanceExist() && !(numberOwner.numberIdentity != SaveIdentity()) && numberOwner.numberLabels.TryGetValue(((GameItem)__instance).uniqueId, out string value) && __result != null)
				{
					__result.AddLine("排列编号：" + value, true, (ColorPalette)(-1), false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
				}
			}
			catch
			{
			}
		}

		private static string SaveIdentity()
		{
			return PlayerStore.Instance.runID + ":" + PlayerStore.Instance.saveSlotId;
		}

		private static bool AllowedNumberKey(KeyCode key)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Invalid comparison between Unknown and I4
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Invalid comparison between Unknown and I4
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Invalid comparison between Unknown and I4
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Invalid comparison between Unknown and I4
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Invalid comparison between Unknown and I4
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Invalid comparison between Unknown and I4
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Invalid comparison between Unknown and I4
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Invalid comparison between Unknown and I4
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Invalid comparison between Unknown and I4
			if ((int)key != 289 && (int)key != 27 && (int)key != 0 && (int)key < 323 && (int)key != 306 && (int)key != 305 && (int)key != 308 && (int)key != 307 && (int)key != 304 && (int)key != 303 && (int)key != 13)
			{
				return (int)key != 8;
			}
			return false;
		}

		private void NumberInput()
		{
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			if (numberOwner != this)
			{
				return;
			}
			if (captureNumberKey)
			{
				if (!Input.GetKeyDown((KeyCode)27))
				{
					foreach (KeyCode item in Enum.GetValues<KeyCode>().Distinct())
					{
						if (AllowedNumberKey(item) && Input.GetKeyDown(item))
						{
							numberKey = item;
							numberSettings.ToggleKey = ((object)item/*cast due to constrained. prefix*/).ToString();
							captureNumberKey = false;
							WriteSettings(NumberSettingsPath, numberSettings);
							status = "编号快捷键已保存：" + ((object)item/*cast due to constrained. prefix*/).ToString();
							break;
						}
					}
					return;
				}
				captureNumberKey = false;
			}
			else if (bonusFocus < 0 && Input.GetKeyDown(numberKey))
			{
				ToggleNumbers();
			}
		}

		private void ToggleNumbers()
		{
			numberSettings.Visible = !numberSettings.Visible;
			WriteSettings(NumberSettingsPath, numberSettings);
			nextNumberScan = DateTime.MinValue;
			UpdateNumbers();
		}

		private void ClearNumberViews()
		{
			foreach (TreeNodeRender value in numberBadges.Values)
			{
				if ((Object)(object)value != (Object)null)
				{
					Object.Destroy((Object)(object)((Component)value).gameObject);
				}
			}
			numberBadges.Clear();
			foreach (var value2 in numberedWindows.Values)
			{
				try
				{
					if (value2.Window.titleString == value2.Added)
					{
						value2.Window.SetTitle(value2.Original);
					}
				}
				catch
				{
				}
			}
			numberedWindows.Clear();
			numberLabels.Clear();
			numberNames.Clear();
		}

		private void UpdateNumbers(bool force = false)
		{
			//IL_0355: Unknown result type (might be due to invalid IL or missing references)
			//IL_036a: Unknown result type (might be due to invalid IL or missing references)
			//IL_037f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0394: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0414: Unknown result type (might be due to invalid IL or missing references)
			//IL_0425: Unknown result type (might be due to invalid IL or missing references)
			//IL_042a: Unknown result type (might be due to invalid IL or missing references)
			if (numberOwner != this || (!force && DateTime.UtcNow < nextNumberScan))
			{
				return;
			}
			nextNumberScan = DateTime.UtcNow.AddSeconds(1.0);
			try
			{
				if (!PlayerStore.IsInstanceExist())
				{
					ClearNumberViews();
					numberIdentity = "";
					return;
				}
				string text = SaveIdentity();
				if (text != numberIdentity)
				{
					ClearNumberViews();
					numberIdentity = text;
				}
				List<GameItem> val = PlayerStore.Instance.FindAllItem(true);
				Dictionary<int, GameItem> owned = new Dictionary<int, GameItem>();
				List<NumberCandidate> list = new List<NumberCandidate>();
				Enumerator<GameItem> enumerator = val.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					bool flag = current.IsTag("STANDARD_MACHINE_TAG") || MachineProfiles.Find(current.identifier) != null;
					if (flag || current.IsTag("MODULE_TAG") || ModuleKinds.NeuralCore(current.identifier) || current.identifier.StartsWith("node_"))
					{
						owned[current.uniqueId] = current;
						list.Add(new NumberCandidate(current.uniqueId, current.identifier, flag));
					}
				}
				if (numbers.Register(text, list))
				{
					WriteSettings(NumberPath, numbers);
				}
				numberLabels.Clear();
				numberNames.Clear();
				foreach (NumberCandidate item in list)
				{
					GameItem val2 = owned[item.Id];
					string text2 = numbers.Label(text, item.Id, item.Kind, item.Machine);
					numberLabels[item.Id] = text2;
					numberNames[item.Id] = (item.Machine ? (val2.name + " " + text2) : text2);
					GameItemElement val3 = ((Il2CppObjectBase)val2).TryCast<GameItemElement>();
					if (val3 != null && !val3.IsDestroyed() && (Object)(object)val3.middle != (Object)null)
					{
						if (numberBadges.TryGetValue(item.Id, out TreeNodeRender value) && (Object)(object)value != (Object)null)
						{
							TreeNodeRender parent = value.parent;
							if (((parent != null) ? new IntPtr?(((Il2CppObjectBase)parent).Pointer) : ((IntPtr?)null)) != ((Il2CppObjectBase)val3.middle).Pointer)
							{
								Object.Destroy((Object)(object)((Component)value).gameObject);
								numberBadges.Remove(item.Id);
							}
						}
						if (numberSettings.Visible && (!numberBadges.TryGetValue(item.Id, out TreeNodeRender value2) || (Object)(object)value2 == (Object)null))
						{
							TreeNodeRender val4 = TreeNodeRender.InstantiateText();
							numberBadges[item.Id] = val4;
							((Object)((Component)val4).gameObject).name = "AMLO-Number";
							val4.SetParent(val3.middle, false);
							RectTransform rectTransform = val4.rectTransform;
							rectTransform.anchorMin = new Vector2(0f, 1f);
							rectTransform.anchorMax = new Vector2(0f, 1f);
							rectTransform.pivot = new Vector2(0f, 1f);
							rectTransform.anchoredPosition = new Vector2(1f, -1f);
							rectTransform.sizeDelta = new Vector2(38f, 10f);
							((Transform)rectTransform).localScale = Vector3.one;
							((TMP_Text)val4.text).fontSize = 6f;
							((TMP_Text)val4.text).alignment = (TextAlignmentOptions)257;
							((TMP_Text)val4.text).enableWordWrapping = false;
							((Graphic)val4.text).raycastTarget = false;
							((Graphic)val4.text).color = new Color(1f, 1f, 0.65f, 1f);
							((TMP_Text)val4.text).outlineColor = Color32.op_Implicit(Color.black);
							((TMP_Text)val4.text).outlineWidth = 0.25f;
							if ((Object)(object)val4.image != (Object)null)
							{
								((Graphic)val4.image).raycastTarget = false;
							}
							((Behaviour)val4).enabled = false;
						}
						if (numberBadges.TryGetValue(item.Id, out TreeNodeRender value3) && (Object)(object)value3 != (Object)null)
						{
							((TMP_Text)value3.text).text = text2;
							((Component)value3).gameObject.SetActive(numberSettings.Visible);
							((Component)value3).transform.SetAsLastSibling();
						}
					}
					if (!item.Machine || val2.contentWindow == null)
					{
						continue;
					}
					PixelWindow contentWindow = val2.contentWindow;
					if (numberedWindows.TryGetValue(item.Id, out (PixelWindow, string, string) value4) && ((Il2CppObjectBase)value4.Item1).Pointer != ((Il2CppObjectBase)contentWindow).Pointer)
					{
						try
						{
							if (value4.Item1.titleString == value4.Item3)
							{
								value4.Item1.SetTitle(value4.Item2);
							}
						}
						catch
						{
						}
						numberedWindows.Remove(item.Id);
					}
					(PixelWindow, string, string) value6;
					if (numberSettings.Visible)
					{
						if (!numberedWindows.TryGetValue(item.Id, out (PixelWindow, string, string) value5) || contentWindow.titleString != value5.Item3)
						{
							string titleString = contentWindow.titleString;
							string text3 = titleString + " · " + text2;
							numberedWindows[item.Id] = (contentWindow, titleString, text3);
							contentWindow.SetTitle(text3);
						}
					}
					else if (numberedWindows.Remove(item.Id, out value6) && contentWindow.titleString == value6.Item3)
					{
						contentWindow.SetTitle(value6.Item2);
					}
				}
				int[] array = numberBadges.Keys.Where((int id) => !owned.ContainsKey(id)).ToArray();
				foreach (int key in array)
				{
					TreeNodeRender val5 = numberBadges[key];
					if ((Object)(object)val5 != (Object)null)
					{
						Object.Destroy((Object)(object)((Component)val5).gameObject);
					}
					numberBadges.Remove(key);
				}
				array = numberedWindows.Keys.Where((int id) => !owned.ContainsKey(id)).ToArray();
				foreach (int key2 in array)
				{
					(PixelWindow, string, string) tuple = numberedWindows[key2];
					try
					{
						if (tuple.Item1.titleString == tuple.Item3)
						{
							tuple.Item1.SetTitle(tuple.Item2);
						}
					}
					catch
					{
					}
					numberedWindows.Remove(key2);
				}
			}
			catch (Exception ex)
			{
				if (DateTime.UtcNow >= nextNumberWarning)
				{
					((MelonBase)this).LoggerInstance.Warning("编号显示更新失败：" + ex.Message);
					nextNumberWarning = DateTime.UtcNow.AddSeconds(30.0);
				}
			}
		}

		private string MachineNumber(int id)
		{
			return numberNames.GetValueOrDefault(id, $"机器（内部 ID {id}）");
		}

		private string ModuleNumber(int id)
		{
			return numberLabels.GetValueOrDefault(id, $"内部 ID {id}");
		}

		private string NumberedText(string text)
		{
			return Regex.Replace(text, "#(\\d+)", (Match m) => (!int.TryParse(m.Groups[1].Value, out var result)) ? m.Value : numberNames.GetValueOrDefault(result, m.Value));
		}

		private void DrawNumberSettings(float x, float y, float w)
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			DrawButton(new Rect(x, y, 220f, 34f), numberSettings.Visible ? "长期显示编号：开启" : "长期显示编号：关闭", enabled: true, ToggleNumbers);
			y += 45f;
			DrawButton(new Rect(x, y, 260f, 34f), captureNumberKey ? "请按新键（Esc 取消）" : $"切换快捷键：{numberKey}（点击修改）", enabled: true, () =>
			{
				captureNumberKey = true;
			});
			y += 45f;
			DrawButton(new Rect(x, y, 220f, 34f), "快捷键恢复为 F7", enabled: true, () =>
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				numberKey = (KeyCode)288;
				numberSettings.ToggleKey = "F7";
				captureNumberKey = false;
				WriteSettings(NumberSettingsPath, numberSettings);
			});
			y += 48f;
			GUI.Label(new Rect(x, y, w - 55f, 180f), "机器按类型分别编号：水分收集器 01号、熔炉 01号等。模组统一编号 M001、M002……，与物品提示、格盘图及外部加成页面一致。\n已有物品按游戏内部 ID 近似获得顺序编号；之后新获得的物品继续顺延，不因重排、出售或读档重新编号。\n关闭长期显示后，鼠标悬停的物品提示仍显示编号。F8 保留用于开关优化面板。", label);
		}

		public Entry()
		{
			//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	public sealed record NumberDisplaySettings
	{
		public bool Visible { get; set; } = true;

		public string ToggleKey { get; set; } = "F7";
	}
	internal static class WaterTowerBridge
	{
		private static Assembly? assembly;

		private static bool checkedAssembly;

		public static string Status { get; private set; } = "未检测到水塔 Mod";

		public static WaterReceiver[] Read(GameItem[] owned)
		{
			if (!checkedAssembly)
			{
				checkedAssembly = true;
				assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly a) => a.GetName().Name == "WaterTower");
				if (assembly != null)
				{
					try
					{
						string text = assembly.Location;
						if (string.IsNullOrWhiteSpace(text))
						{
							text = Path.Combine(MelonEnvironment.GameRootDirectory, "Mods", "WaterTower.dll");
						}
						using FileStream inputStream = File.OpenRead(text);
						using SHA256 sHA = SHA256.Create();
						if (Convert.ToHexString(sHA.ComputeHash(inputStream)) != "AEB206606EDD15C2129A6049B8DD426E22424676342039E5215B059F3A74350D")
						{
							assembly = null;
							Status = "水塔版本未核验；额定产能仍完整显示";
						}
					}
					catch (Exception ex)
					{
						assembly = null;
						Status = "水塔版本未核验：" + ex.GetType().Name + "；额定产能仍完整显示";
					}
				}
			}
			if (assembly == null)
			{
				return Array.Empty<WaterReceiver>();
			}
			try
			{
				Type type = assembly.GetType("WaterTower.TowerRegistry");
				Type type2 = assembly.GetType("WaterTower.NightSettlement");
				IEnumerable enumerable = (IEnumerable)type.GetProperty("All").GetValue(null);
				HashSet<int> hashSet = owned.Select((GameItem i) => i.uniqueId).ToHashSet();
				HashSet<int> hashSet2 = new HashSet<int>();
				List<WaterReceiver> list = new List<WaterReceiver>();
				foreach (object item in enumerable)
				{
					GameItem val = Game(item.GetType().GetField("Item").GetValue(item));
					if (val == null || !hashSet.Contains(val.uniqueId) || !(bool)type.GetMethod("IsAlive").Invoke(null, new object[1] { item }))
					{
						continue;
					}
					GameItem val2 = Game(type.GetMethod("ResolveTank").Invoke(null, new object[1] { item }));
					if (val2 == null || !hashSet.Contains(val2.uniqueId) || !hashSet2.Add(val2.uniqueId))
					{
						continue;
					}
					IEnumerable enumerable2 = (IEnumerable)type2.GetMethod("MachinesOf", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[2] { item, "moisture_farm" });
					List<int> list2 = new List<int>();
					foreach (object item2 in enumerable2)
					{
						GameItem val3 = Game(item2);
						if (val3 != null && hashSet.Contains(val3.uniqueId))
						{
							list2.Add(val3.uniqueId);
						}
					}
					list.Add(new WaterReceiver(val2.uniqueId, WaterHelper.GetFreeCapacity(val2), list2.Distinct().ToArray()));
				}
				Status = $"已适配 WaterTower 1.0.2 · {list.Count} 座水塔";
				return list.ToArray();
			}
			catch (Exception ex2)
			{
				Status = "水塔接口读取失败：" + ex2.GetType().Name + "；额定产能仍完整显示";
				return Array.Empty<WaterReceiver>();
			}
		}

		private static GameItem? Game(object? value)
		{
			GameItem val = (GameItem)((value is GameItem) ? value : null);
			if (val == null)
			{
				object? obj = ((value is Il2CppObjectBase) ? value : null);
				if (obj == null)
				{
					return null;
				}
				return ((Il2CppObjectBase)obj).TryCast<GameItem>();
			}
			return val;
		}
	}
}

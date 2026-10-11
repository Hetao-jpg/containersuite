using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Better_trading;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(BetterTradingMod), "Enhanced Trade Display", "1.2.0", "luochen", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: MelonColor(255, 220, 150, 40)]
[assembly: MelonAuthorColor(255, 120, 220, 120)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("EnhancedTradeDisplay")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("EnhancedTradeDisplay")]
[assembly: AssemblyTitle("EnhancedTradeDisplay")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
[module: UnverifiableCode]
[module: Safe(11)]
[module: SuppressIldasm]
[module: Code("Confuser.Core 1.6.0+447341964f")]
[module: SuppressIldasm]
[module: ConfusedBy("Confuser.Core 1.6.0+447341964f")]
internal class <Module>
{
	internal static byte[] \u200b\u206f\u200d\u202c\u200e\u202e\u206b\u202e\u202d\u206b\u206a\u202c\u206d\u206d\u200c\u202d\u200d\u206e\u206a\u202d\u202d\u206f\u206f\u202b\u202a\u206a\u200f\u206a\u206f\u206e\u206b\u206f\u202e\u202a\u206b\u206a\u206b\u202e\u206a\u206a\u202e;

	internal static Main \u200f\u202c\u200b\u200d\u206c\u202b\u206c\u200b\u202e\u202c\u202e\u202a\u200c\u206e\u202d\u206d\u202d\u202b\u202a\u202c\u206f\u200d\u202d\u200d\u206b\u202c\u200b\u202a\u206b\u200d\u200d\u200e\u206c\u200b\u200f\u206c\u206d\u202c\u200b\u202a\u202e/* Not supported: data(B6 66 87 8E C2 DE 8E 12 80 B1 16 A4 88 46 0A 0C BB DD 10 D2 F4 AA 02 3D 75 6C 9B 64 6B 74 88 7D EC 6C C7 41 E1 C0 A0 E5 5F B3 EB 4A 95 7D F3 62 4E F3 00 56 90 B3 AF 97 1E E3 61 FF E6 04 A2 B7 81 70 7D 3E 36 12 33 17 91 91 8E 4C 86 F7 55 4A 3B 51 60 CE 4A 83 A4 97 DE 77 EA F1 CF 60 5E B3 C2 0C 11 BC 74 AF 7D B3 24 25 26 5A A1 64 59 7E EC 1A 1F 08 BC 2A 47 8D 4C E3 70 3E 2C 2A FD 88 DE 11 01 87 EA 16 3F AA 9C FF F3 69 43 98 14 E4 86 9E AF 72 2C 2D 6F C8 68 6B 08 6E 23 1F 58 1A DC 3A 25 EE 1D 3E B3 88 7D D4 EB DA 25 F0 FF 13 CC 98 92 AE 72 8F 4C 6B 69 F1 E0 F2 35 97 2C 13 91 9C 15 30 92 9B 89 2C A2 97 0F 75 31 A6 C1 F5 CF 4C 1D C0 A0 91 D2 58 F9 96 31 53 24 AF 32 0E 38 38 A4 2D 78 96 52 71 9E 93 E0 7B 27 23 18 0E 7B CE 3D B6 0E 09 3B CA AD 02 A1 18 70 D4 95 C1 78 7A 88 57 5D 5F 23 81 5A 44 68 18 A8 E6 05 79 21 6B B2 C7 7F 0A 53 AD 3B DF 2F 41 DE E5 AB B2 B0 46 1D A5 78 28 FD B3 35 13 14 4E 0E 15 44 09 7B E5 53 08 84 EE 92 A1 10 CA 74 2C 85 A2 47 B8 92 E2 5B CC 75 82 A0 72 02 D0 1E 37 9C 0C D7 37 C2 EC 97 56 40 AA 7E E8 67 83 E4 10 EB E6 A7 A7 43 F0 C4 CF 59 B0 DF 7A A1 41 06 96 82 0F 74 DE 29 BB 49 35 6A DA BD 5C 10 5F 9A E9 AE C0 0E 72 DD E0 11 6D 06 8B 08 4A 2D 6E 66 14 40 7C 5E 7C 68 D9 AE E6 49 20 63 83 28 4F 01 25 B8 E0 BE 70 22 49 51 B0 8D 80 72 85 F8 C9 4D B5 9B 10 13 08 AC 15 B2 A3 6A F8 52 2E 1C 37 C7 41 B3 95 8A A2 DA 69 F3 B9 B0 00 31 5B B6 14 19 5F DD 78 18 0E 44 43 EF 23 E5 87 81 4A AA 21 30 21 A6 3C E8 42 8B FE B4 0F CF F8 72 CB 79 52 D9 9E 47 39 19 9C 87 A2 C5 74 0F E7 E5 95 F0 2E C8 68 78 54 EF 82 1F 45 D5 A5 6D 2B ED CE 2C DB 5D 60 F1 C4 75 B8 37 4D 2D 6F 38 E7 CF 8A C3 A4 B4 71 D4 32 A2 68 26 0B 11 6F D8 70 3D D6 E1 62 A4 79 13 47 6A F2 75 5E C7 8F 87 C4 46 BA B2 23 F3 20 6A 2C BD 89 6F A1 5E 98 E0 9E 3D 4C 18 6E 2D 76 16 69 92 71 33 17 FE E6 47 5B 22 7D A7 E8 94 35 8F 57 76 7A 75 B6 00 D4 C0 F7 3E 71 5B 57 63 05 BF F2 A6 1E F8 AA 7B 5A E8 60 B5 EF 8A EA 44 05 30 24 C5 C8 9C 13 74 77 E8 74 C0 92 7B 30 69 90 C5 2F 59 88 70 A7 30 7D A4 C2 DF 29 94 F6 EB 6A 46 C0 25 13 4D EE 53 A0 3B 4E ED 40 A8 AD 35 E6 96 C8 3B CD B6 81 00 23 0E D7 BA CC 2F A2 6F 02 5A E2 41 D6 7F 74 41 A6 6C AB 84 85 07 28 B9 56 C2 46 EB C7 05 65 D5 F7 DE 21 61 CA F6 EB E0 9C 89 8D 98 7B 03 CD BA BF E6 22 05 F0 5C 8A 74 D0 9D 18 1F A3 4C 0D F7 35 24 37 DD 3D EE AA 5A E0 04 0B 77 B3 9B 14 44 B6 DA 8F D9 61 A6 37 E1 1C 27 8F 8A FF 96 F8 75 AC 15 1A 5F DB 8D 41 3D 92 42 D2 BB C8 08 BE E9 44 2F 5F 93 B1 58 6F 59 3D 4C 46 39 71 21 AD 11 EA C9 BD E5 ED C4 0F 12 D9 F6 38 54 DA 32 22 93 B4 6A 99 B9 ED FF A1 CD 11 4A A9 DA E7 FE 3E FF E5 0A C4 F8 3C 9A F7 50 CA 0F 8C E4 FF 9A 2D 18 89 85 B0 38 51 B9 19 C8 53 D2 C7 E3 D6 A5 F8 31 4E 4F 3C BC 18 F6 4D 39 54 D8 78 EC C0 CE F0 4D 5E 19 B5 DC E9 30 A6 4A FB 7D DB 36 EA 76 BF F3 7B 5B F1 6E 35 52 9B 84 B9 7C 84 D7 6C EE D9 F2 92 27 31 AF 2A FE 1F 43 27 F6 8C 4B 3D B4 FF D8 81 90 0F 1F 56 E9 03 22 10 D4 A7 11 7F DC 09 B9 81 86 22 86 4A B0 C0 E2 EA 09 0F 0A 67 20 0C F1 C1 98 67 86 57 59 2D 6E 8B 3D 91 DA B4 B8 FF DB 34 3A 4A B1 51 B9 6B 1F B9 C6 99 E1 65 BE AB A7 17 F3 5F D6 09 0A CF 68 7B BD 08 20 C2 82 B4 EC E3 FC 97 2C 28 1C BD 51 6C DE 21 8D F9 31 45 EC 2D 9D 27 5F 05 80 AA C8 A4 36 6B AC F8 C9 75 4D BD 4F A4 D7 59 2A 02 94 1A 18 15 6D 25 09 26 00 D4 49 E2 30 3F 27 D6 F5 D2 E8 BC 97 9C 4A 8C CF 4F 17 4F D9 C6 20 72 04 7C FD D2 C7 59 B1 42 12 08 12 ED 91 D5 EB B8 AD B6 9F FD 88 99 DB B5 B4 04 14 23 12 5F 2A 72 B2 25 30 87 4D 91 D3 C8 E1 2F 57 22 C7 8B 52 DB 6A A6 83 34 09 88 2B EF A4 4F 8F A9 6F 5A 3A 4A A4 66 01 C9 86 CC FF E4 B6 C7 1D 7E 74 B3 B8 A1 14 1C 88 60 E9 65 3B 82 BC A8 79 38 0D 69 8B 22 ED DB 40 A0 39 9C C9 0C 72 54 5E 7E A9 3E 79 26 FC 52 1E 8D 9F 97) */;

	static <Module>()
	{
		\u206d\u206d\u206b\u206f\u206a\u206f\u206c\u202a\u206c\u206b\u200f\u202b\u206b\u200d\u206a\u206f\u200c\u206f\u200f\u206c\u206e\u200c\u202a\u200b\u206b\u202a\u200b\u202d\u200d\u206a\u200c\u206e\u206e\u200c\u202d\u202c\u202a\u202c\u202c\u202d\u202e();
		\u206f\u202c\u206a\u200c\u206e\u206c\u200f\u206e\u206b\u200c\u206b\u206e\u206f\u206c\u206b\u200e\u200f\u206a\u202b\u206f\u206c\u206d\u202b\u206a\u206b\u206f\u202a\u202c\u202b\u200d\u202b\u200f\u200d\u202b\u200c\u202a\u206e\u206d\u200d\u202c\u202e();
		\u206c\u206d\u200d\u206b\u206d\u206f\u206c\u206f\u202a\u200d\u206b\u206b\u206c\u200c\u200e\u206f\u206e\u200b\u206d\u200c\u200f\u200d\u202d\u200c\u200e\u206f\u202a\u200f\u206c\u206b\u206b\u202a\u202d\u206e\u200c\u206b\u202d\u206c\u202a\u206e\u202e();
		\u206d\u202e\u202e\u200e\u200f\u202c\u202e\u206e\u200c\u200b\u202b\u206c\u206b\u200d\u202e\u200e\u206a\u206e\u202d\u200c\u206b\u206f\u202d\u206a\u206d\u206c\u200c\u206f\u200c\u206a\u206e\u206a\u200d\u206e\u200f\u200d\u202b\u206f\u202b\u206c\u202e();
	}

	internal static byte[] \u206f\u200f\u202e\u202d\u200c\u200d\u202e\u202b\u202c\u200c\u200e\u202c\u202c\u200c\u206f\u202c\u200e\u202b\u200c\u206e\u206d\u200f\u206a\u202b\u200f\u202c\u202c\u206f\u200f\u202a\u200f\u206a\u202e\u200c\u206c\u202c\u206a\u206e\u206b\u200d\u202e(byte[] data)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x40
	}

	internal static void \u206d\u202e\u202e\u200e\u200f\u202c\u202e\u206e\u200c\u200b\u202b\u206c\u206b\u200d\u202e\u200e\u206a\u206e\u202d\u200c\u206b\u206f\u202d\u206a\u206d\u206c\u200c\u206f\u200c\u206a\u206e\u206a\u200d\u206e\u200f\u200d\u202b\u206f\u202b\u206c\u202e()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x2B 0xCD
	}

	internal static - \u200d\u206c\u206d\u206b\u206e\u202e\u202d\u202a\u202b\u206c\u206a\u202a\u206e\u202c\u202a\u200e\u206c\u200f\u200b\u206a\u200f\u200c\u206a\u206e\u202b\u206e\u206c\u200e\u200e\u202b\u206e\u200d\u202a\u200f\u206c\u206d\u202b\u200c\u200c\u206e\u202e<->(int id)
	{
		_ = ((ushort[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/];
		/*Error near IL_0001: Unknown opcode: 0xE5*/;
	}

	internal static - \u206b\u202d\u202d\u200e\u200e\u206e\u202b\u206b\u200e\u206f\u200c\u200c\u202c\u206e\u202d\u206d\u202a\u200f\u202e\u200c\u206e\u206e\u206d\u202c\u200f\u200f\u206c\u206c\u202d\u206f\u206a\u202b\u202c\u206e\u200d\u200b\u200d\u202e\u206a\u202e<->(int id)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x60
	}

	internal static - \u202c\u200e\u202c\u202d\u206e\u202d\u206b\u202c\u206e\u206f\u206a\u202c\u200c\u200b\u202a\u206f\u206f\u206a\u202c\u200f\u206e\u206b\u206f\u206c\u206e\u202e\u206d\u206c\u206c\u202b\u200b\u206e\u200e\u206a\u206d\u206e\u206a\u200d\u200e\u206a\u202e<->(int id)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xDB 0xEC
	}

	internal static - \u202c\u206d\u206a\u202b\u200c\u202e\u200d\u200e\u206b\u202d\u202a\u206b\u200c\u206a\u202a\u206e\u206d\u200e\u206c\u200d\u200c\u202a\u200f\u202d\u206b\u206c\u200e\u202b\u202e\u202d\u206a\u206c\u206f\u206d\u200f\u202a\u202c\u206d\u202e\u200d\u202e<->(int id)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x95
	}

	internal static - \u202e\u206d\u206d\u202d\u206b\u206b\u202e\u206c\u200b\u200e\u206d\u202a\u202e\u206d\u206e\u200e\u200f\u206d\u202c\u200c\u202c\u202d\u200d\u206a\u200c\u200b\u200f\u202b\u206f\u206a\u206d\u206c\u206c\u200d\u206b\u206a\u206a\u200c\u200f\u202e<->(int id)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x1B 0x1D
	}

	private static void \u206c\u206d\u200d\u206b\u206d\u206f\u206c\u206f\u202a\u200d\u206b\u206b\u206c\u200c\u200e\u206f\u206e\u200b\u206d\u200c\u200f\u200d\u202d\u200c\u200e\u206f\u202a\u200f\u206c\u206b\u206b\u202a\u202d\u206e\u200c\u206b\u202d\u206c\u202a\u206e\u202e()
	{
		/*Error: Unknown opcode: 0xCA*/;
	}

	private static void \u206b\u206b\u200b\u206a\u200d\u206d\u202e\u200e\u206c\u202d\u206f\u202d\u200f\u206e\u200c\u206d\u200b\u200d\u206b\u200e\u200b\u202d\u206e\u200f\u206c\u206b\u200f\u202b\u200b\u206c\u206a\u206a\u200f\u202c\u206b\u202b\u206f\u202e\u202b\u202e(object thread)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x51
	}

	[DllImport("kernel32.dll", EntryPoint = "VirtualProtect")]
	internal unsafe static extern bool \u206f\u200b\u206b\u206c\u202a\u206a\u200b\u206a\u200b\u202c\u206d\u202d\u206b\u200c\u206a\u206c\u206b\u206d\u202c\u200c\u206d\u206f\u202b\u200e\u206c\u200d\u206f\u200d\u200c\u202a\u206b\u202d\u200d\u202b\u200b\u200e\u206e\u206b\u200e\u202a\u202e(byte* lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

	internal static void \u206f\u202c\u206a\u200c\u206e\u206c\u200f\u206e\u206b\u200c\u206b\u206e\u206f\u206c\u206b\u200e\u200f\u206a\u202b\u206f\u206c\u206d\u202b\u206a\u206b\u206f\u202a\u202c\u202b\u200d\u202b\u200f\u200d\u202b\u200c\u202a\u206e\u206d\u200d\u202c\u202e()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x37 0x56
	}

	[DllImport("kernel32.dll", EntryPoint = "VirtualProtect")]
	internal static extern bool \u206c\u200f\u206d\u200b\u202b\u202e\u202c\u202d\u206c\u200d\u200f\u200b\u200f\u202c\u202c\u202e\u206e\u202a\u206b\u206e\u200d\u206d\u206c\u200b\u200d\u200d\u202d\u206f\u202e\u206f\u206c\u206c\u206d\u200f\u206d\u200b\u202b\u200c\u200e\u202e\u202e(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

	internal unsafe static void \u206d\u206d\u206b\u206f\u206a\u206f\u206c\u202a\u206c\u206b\u200f\u202b\u206b\u200d\u206a\u206f\u200c\u206f\u200f\u206c\u206e\u200c\u202a\u200b\u206b\u202a\u200b\u202d\u200d\u206a\u200c\u206e\u206e\u200c\u202d\u202c\u202a\u202c\u202c\u202d\u202e()
	{
		Module module = typeof(<Module>).Module;
		string fullyQualifiedName = module.FullyQualifiedName;
		bool flag = fullyQualifiedName.Length > 0 && fullyQualifiedName[0] == '<';
		byte* ptr = (byte*)(void*)Marshal.GetHINSTANCE(module);
		byte* num = ptr + (uint)((int*)ptr)[15];
		ushort num2 = ((ushort*)num)[3];
		ushort num3 = ((ushort*)num)[10];
		uint* ptr2 = null;
		uint num4 = 0u;
		uint* ptr3 = (uint*)(num + 24 + (int)num3);
		uint num5 = 3535373487u;
		uint num6 = 2077516225u;
		uint num7 = 150479118u;
		uint num8 = 2375633146u;
		for (int i = 0; i < num2; i++)
		{
			switch (*(ptr3++) * *(ptr3++))
			{
			case 2493601453u:
				ptr2 = (uint*)(ptr + (flag ? ptr3[3] : ptr3[1]));
				num4 = (flag ? ptr3[2] : (*ptr3)) >> 2;
				break;
			default:
			{
				uint* ptr4 = (uint*)(ptr + (flag ? ptr3[3] : ptr3[1]));
				uint num9 = ptr3[2] >> 2;
				for (uint num10 = 0u; num10 < num9; num10++)
				{
					uint num11 = (num5 ^ *(ptr4++)) + num6 + num7 * num8;
					num5 = num6;
					num6 = num7;
					num6 = num8;
					num8 = num11;
				}
				break;
			}
			case 0u:
				break;
			}
			ptr3 += 8;
		}
		uint[] array = new uint[16];
		uint[] array2 = new uint[16];
		for (int j = 0; j < 16; j++)
		{
			array[j] = num8;
			array2[j] = num6;
			num5 = (num6 >> 5) | (num6 << 27);
			num6 = (num7 >> 3) | (num7 << 29);
			num7 = (num8 >> 7) | (num8 << 25);
			num8 = (num5 >> 11) | (num5 << 21);
		}
		array[0] = array[0] ^ array2[0];
		array[1] = array[1] * array2[1];
		array[2] = array[2] + array2[2];
		array[3] = array[3] ^ array2[3];
		array[4] = array[4] * array2[4];
		array[5] = array[5] + array2[5];
		array[6] = array[6] ^ array2[6];
		array[7] = array[7] * array2[7];
		array[8] = array[8] + array2[8];
		array[9] = array[9] ^ array2[9];
		array[10] = array[10] * array2[10];
		array[11] = array[11] + array2[11];
		array[12] = array[12] ^ array2[12];
		array[13] = array[13] * array2[13];
		array[14] = array[14] + array2[14];
		array[15] = array[15] ^ array2[15];
		uint lpflOldProtect = 64u;
		\u206c\u200f\u206d\u200b\u202b\u202e\u202c\u202d\u206c\u200d\u200f\u200b\u200f\u202c\u202c\u202e\u206e\u202a\u206b\u206e\u200d\u206d\u206c\u200b\u200d\u200d\u202d\u206f\u202e\u206f\u206c\u206c\u206d\u200f\u206d\u200b\u202b\u200c\u200e\u202e\u202e((IntPtr)ptr2, num4 << 2, lpflOldProtect, out lpflOldProtect);
		if (lpflOldProtect != 64)
		{
			uint num12 = 0u;
			for (uint num13 = 0u; num13 < num4; num13++)
			{
				*ptr2 ^= array[num12 & 0xF];
				array[num12 & 0xF] = (array[num12 & 0xF] ^ *(ptr2++)) + 1035675673;
				num12++;
			}
		}
	}
}
[CompilerGenerated]
[Util]
internal sealed class Util : Attribute
{
	public Util()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x13 0x68
	}
}
[CompilerGenerated]
[Util]
[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
internal sealed class Safe : Attribute
{
	public readonly int Version;

	public Safe(int P_0)
	{
		/*Error: Unknown opcode: 0xC9*/;
	}
}
namespace Better_trading;

public class BetterTradingMod : MelonMod
{
	private const string Tag = "[抄价]";

	private const string BoughtPriceTag = "BOUGHT_PRICE_TAG";

	private const string RowTag = " 赚";

	private const string RowTagBuy = " 预计赚";

	private const string TotalTag = " · 合计";

	private const string BarterBlockTag = "合计进货价格：";

	private const string BarterLabelTagProfit = "标签价预计赚：";

	private const string BarterLabelMarketProfit = "市场价预计赚：";

	internal static BetterTradingMod Instance;

	private static string _cachedBodyBase;

	private static string _cachedEnriched;

	private static int _cachedMode;

	private static float _lastBarterCalc;

	private static int _cachedSetIndex;

	private static long _cachedClientKey;

	private static bool _cacheDirty;

	private static string _cachedNameBase;

	private static string _cachedNameEnriched;

	private static long _cachedMultiCost;

	private static long _cachedWholesaleCost;

	private static MelonPreferences_Category _cfg;

	private static MelonPreferences_Entry<bool> _autoClose;

	private static MelonPreferences_Entry<bool> _nightReport;

	private static MelonPreferences_Entry<bool> _bargainHelper;

	private const string SettingsWindowId = "enhanced_trade_display_settings";

	private static Action<bool> _toggleCallback;

	private static Action<bool> _nightToggleCallback;

	private const string ColorGreen = "#00E676";

	private const string ColorRed = "#FF5252";

	private const string ColorWhite = "#FFFFFF";

	internal static bool AutoCloseEnabled
	{
		get
		{
			//Invalid MethodBodyBlock: Invalid method header: 0x28
		}
	}

	internal static bool NightReportEnabled
	{
		get
		{
			//Invalid MethodBodyBlock: Invalid method header: 0xF4
		}
	}

	internal static bool BargainHelperEnabled
	{
		get
		{
			//Invalid MethodBodyBlock: Invalid method header: 0xEC
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnInitializeMelon()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xA9
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnUpdate()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x5C
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OpenSettingsWindow()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xBB 0x06
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnAutoCloseToggled(bool v)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xBF 0xD4
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void OnNightReportToggled(bool v)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x33 0xA5
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public override void OnLateUpdate()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xB5
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RenderTrade()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xD0
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string BuildEnriched(PlayerStore store, NegociationUIManager ui, string baseText)
	{
		/*Error: Unknown opcode: 0xF0*/;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string BuildNameEnriched(PlayerStore store, NegociationUIManager ui, string baseText)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xDF 0xF7
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static List<GameItem> CollectItems(bool selling)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x53 0x7C
	}

	private static bool BelongsToTradeSide(GameItem item, bool playerSells)
	{
		checked
		{
			_ = (byte)/*Error near IL_0001: Stack underflow*/;
			/*Error near IL_0001: Invalid field token*/;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static long BuyPrice(GameItem item)
	{
		checked
		{
			_ = (short)unchecked((int)/*Error near IL_0001: Stack underflow*/);
			/*Error near IL_0002: Unknown opcode: 0xBE*/;
		}
	}

	internal static long OfferValue(GameItem item)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x74
	}

	private static long RetailValue(GameItem item)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x77 0x62
	}

	private static long BaseValue(GameItem item)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xF8
	}

	internal static bool IsOwnedByPlayer(GameItem item)
	{
		/*Error: Empty body found. Decompiled assembly might be a reference assembly.*/;
	}

	private static StoreClient GetStoreClient(PlayerStore store)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x28
	}

	private static bool ClientWantsItem(NegociationUIManager ui, StoreClient client, GameItem item)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x27 0xEB
	}

	private static bool DeterminePlayerSells(PlayerStore store, NegociationUIManager ui, ClientIntent intent)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x83 0xBC
	}

	private static void GetCostValue(NegociationUIManager ui, GameItem item, bool playerSells, out long cost, out long value)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xE3 0x9C
	}

	private static long PaidPrice(NegociationUIManager ui, GameItem item)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xF3 0x0E
	}

	private static long GetActualPaidTotal(NegociationUIManager ui, StoreClient client, long fallbackSum)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x9D
	}

	private static long ReadActiveTmpNumber(TextMeshProUGUI t)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		/*Error: stloc 2 (out-of-bounds)*/;
		if (checked(/*Error near IL_0003: Stack underflow*/ * /*Error near IL_0001: Stack underflow*/) == 0)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error: End of method reached without returning.*/;
	}

	private static long ExtractFirstNumber(string text)
	{
		/*Error: Unknown opcode: 0xB2*/;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string BuildBarter(PlayerStore store, NegociationUIManager ui, string baseText)
	{
		/*Error: Invalid metadata token*/;
	}

	private static void CollectInto(List<GameItem> src, List<GameItem> dst)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x9F 0x99
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string StripMarks(string text)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x3D
	}

	private static long ClientKey(StoreClient client)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x61
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string Enrich(string baseText, List<long?> profits, long totalCost, long totalValue, bool playerSells)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x48
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static string Credits(long value)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xFC
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static string Signed(long value)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xA7 0x0E
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string ColoredSigned(long value)
	{
		((double[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/] = (double)/*Error near IL_0001: Stack underflow*/;
		/*Error near IL_0002: Not a type handle*/;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private unsafe static string ColoredCredits(long value)
	{
		//IL_0001: Unsupported input type for neg.
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		((sbyte[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/] = (sbyte)/*Error near IL_0001: Stack underflow*/;
		_ = *(float*)(nint)(0 - /*Error near IL_0002: Stack underflow*/);
		/*Error near IL_0003: Unknown opcode: 0xC7*/;
	}

	public BetterTradingMod()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x4F 0x51
	}

	static BetterTradingMod()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xC5
	}
}
[HarmonyPatch(typeof(PlayerStore), "BuyItemFromTable")]
internal static class P_AutoBuyFromTable
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x99
	}

	private static void Postfix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x18
	}
}
[HarmonyPatch(typeof(PlayerStore), "SellItemFromTable")]
internal static class P_AutoSellFromTable
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x53 0xA0
	}

	private static void Postfix()
	{
		/*Error: Invalid metadata token*/;
	}
}
[HarmonyPatch(typeof(PlayerStore), "AcceptBarter")]
internal static class P_AutoAcceptBarter
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xA8
	}

	private static void Postfix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x87 0xAC
	}
}
[HarmonyPatch(typeof(NegociationUIManager), "OnOfferAcceptClick")]
internal static class P_AutoOnOfferAcceptClick
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x53 0xA0
	}

	private static void Postfix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x50
	}
}
[HarmonyPatch(typeof(NegociationUIManager), "OnAcceptMultipleButtonClick")]
internal static class P_AutoOnAcceptMultClick
{
	private static void Prefix()
	{
		//IL_0003: Invalid comparison between Unknown and I8
		((short[])/*Error near IL_0002: Stack underflow*/)[/*Error near IL_0002: Stack underflow*/] = (short)((ushort[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/];
		if ((long)/*Error near IL_0008: Stack underflow*/ > (long)/*Error near IL_0003: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0008: Unknown opcode: 0xEF*/;
	}

	private static void Postfix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x50
	}
}
[HarmonyPatch(typeof(NegociationUIManager), "OnWholesaleClick")]
internal static class P_AutoOnWholesaleClick
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x40
	}

	private static void Postfix()
	{
		if (/*Error near IL_0005: Stack underflow*/ > /*Error near IL_0005: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0005: Unknown opcode: 0xFE42*/;
	}
}
[HarmonyPatch(typeof(NegociationUIManager), "OnConfirmPanelYes")]
internal static class P_AutoOnConfirmYes
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x1F 0x2F
	}

	private unsafe static void Postfix()
	{
		//IL_0004: Expected native int or pointer, but got F8
		checked
		{
			_ = (ushort)unchecked((long)/*Error near IL_0001: Stack underflow*/);
			/*Error near IL_0002: stloc 3 (out-of-bounds)*/;
			_ = (sbyte)unchecked((uint)((sbyte[])/*Error near IL_0006: Stack underflow*/)[*(ushort*)(nint)(double)/*Error near IL_0004: Stack underflow*/]);
			/*Error near IL_0007: Unknown opcode: 0xF5*/;
		}
	}
}
[HarmonyPatch(typeof(ConfirmPanelUIManager), "OnYesClick")]
internal static class P_AutoConfirmYes
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x08
	}

	private static void Postfix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x47 0x56
	}
}
[HarmonyPatch(typeof(PlayerStore), "BuyItem")]
internal static class P_StatBuyItem
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x15
	}

	private static void Postfix()
	{
		if (/*Error near IL_0002: Stack underflow*/ < /*Error near IL_0002: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0002: Invalid branch target*/;
	}
}
[HarmonyPatch(typeof(PlayerStore), "SellItem")]
internal static class P_StatSellItem
{
	private static void Prefix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x53 0xA0
	}

	private static void Postfix()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x60
	}
}
[HarmonyPatch(typeof(StoreEventManager), "OnDayStart")]
internal static class P_NightReportDayStats
{
	private static void Postfix()
	{
		/*Error: Empty body found. Decompiled assembly might be a reference assembly.*/;
	}
}
internal static class Type
{
	private const string ReportBaseColor = "B26A00";

	private const string ReportExpenseHex = "#2E9E4F";

	private const string ReportNetUpHex = "#D93025";

	private const string ReportNetDownHex = "#2E9E4F";

	private static int _depth;

	private static int _depthFrame;

	private static int _cashBefore;

	private static int _day;

	private static long _income;

	private static long _expense;

	private static int _salesCount;

	private static long _soldCost;

	private static long _snapSoldCost;

	private static bool _skipTrade;

	internal static void get(bool barter = false)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xE5
	}

	internal static void set()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x23 0x2A
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void run()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x61
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void add()
	{
		//Invalid MethodBodyBlock: Invalid local signature token: 0x80CA0C15
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string @new(string text, string hex)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xA0
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string has(long value)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		_ = /*Error near IL_0002: Stack underflow*/- ((long[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/];
		_ = 6;
		/*Error near IL_0003: Not a type handle*/;
	}

	private static long to()
	{
		if (/*Error near IL_0005: Stack underflow*/ >= /*Error near IL_0005: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0005: Unknown opcode: 0xCA*/;
	}

	private static bool of()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x00
	}

	private static void @do()
	{
		((sbyte[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/] = (sbyte)/*Error near IL_0001: Stack underflow*/;
		if (/*Error near IL_0002: Stack underflow*/ > /*Error near IL_0002: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0002: starg 226 (out-of-bounds)*/;
		/*Error: End of method reached without returning.*/;
	}

	private static void @is()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xFB 0xA0
	}

	private static void @as()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xD9
	}

	private static int on()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x40
	}

	static Type()
	{
		((sbyte[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/] = (sbyte)/*Error near IL_0001: Stack underflow*/;
		if (/*Error near IL_0003: Stack underflow*/ <= /*Error near IL_0003: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error: End of method reached without returning.*/;
	}
}
internal static class List
{
	private const string Mark = "\n<size=90%><color=#8A5A00>";

	private const string MarkEnd = "</color></size>";

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void get()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xA9
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string set(BargainType type, int percent, string faction)
	{
		/*Error near IL_0001: Unknown opcode: 0xBC*/;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string run(BargainType type, string faction)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x89
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string add(int penalty, double mult)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x65
	}

	private static bool @new()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xAF 0x5D
	}

	private static double has(string factionId)
	{
		_ = 7;
		/*Error near IL_0001: Invalid field token*/;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool to(BargainType type, out double per1000)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x38
	}

	private static double of(BargainType type, int percent)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xC1
	}

	private static double @do(bool selling)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x3C
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string @is(string factionId)
	{
		if ((int)((double[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/] != 0)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0006: Read out of bounds.*/;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string @as(double v)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x23 0x5C
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string on(string text)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x77 0xB8
	}
}
internal static class Map
{
	private static readonly HashSet<string> Seen;

	internal static void get(string message)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x88
	}

	internal static void set(string message)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x47 0xAB
	}

	private static void run(string message, bool warning)
	{
		/*Error: Unknown opcode: 0xF4*/;
	}

	static Map()
	{
		_ = 3;
		/*Error near IL_0001: Invalid metadata token*/;
	}
}
internal static class Form
{
	private static bool _closingGuard;

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal static void get()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xF5
	}
}
internal class Code : Attribute
{
	public unsafe Code(string P_0)
	{
		_ = *(short*)(-1);
		/*Error near IL_0002: Unknown opcode: 0xCD*/;
	}
}
internal struct Init
{
	internal uint \u206f\u206c\u202a\u200d\u206a\u206c\u206f\u202c\u206e\u202a\u206d\u202b\u202c\u206f\u200f\u200d\u200d\u206b\u202a\u202c\u200d\u206e\u206b\u202d\u202c\u202a\u200f\u202e\u202c\u200f\u200b\u202d\u206b\u206b\u202e\u200d\u200d\u200e\u202e\u202b\u202e;

	internal void get()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x69
	}

	internal uint set(Data rangeDecoder)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xCF 0xA2
	}
}
internal struct Core
{
	internal readonly Init[] \u200c\u200d\u202c\u202e\u200e\u206c\u200c\u206c\u202b\u206f\u206f\u200f\u200f\u202a\u206a\u206e\u206e\u200e\u202e\u206c\u206d\u200b\u206d\u206c\u202c\u206a\u206e\u200b\u206b\u202c\u206b\u200b\u206e\u200b\u206b\u206e\u206a\u202b\u200e\u202d\u202e;

	internal readonly int \u206c\u200b\u206d\u202b\u206e\u200b\u206e\u200c\u200d\u200c\u202e\u206f\u200e\u200c\u200b\u200e\u200f\u202e\u206b\u206b\u202c\u202d\u202d\u202e\u202a\u206e\u200c\u206a\u200e\u202b\u202e\u202a\u206e\u200e\u200f\u200c\u202b\u200b\u206d\u206c\u202e;

	internal Core(int numBitLevels)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xA8
	}

	internal void get()
	{
		_ = (int)/*Error near IL_0001: Stack underflow*/;
		/*Error near IL_0001: Invalid field token*/;
	}

	internal uint set(Data rangeDecoder)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xBC
	}

	internal uint run(Data rangeDecoder)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xC7 0xE5
	}

	internal static uint add(Init[] Models, uint startIndex, Data rangeDecoder, int NumBitLevels)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x51
	}
}
internal class Data
{
	internal uint \u206d\u202a\u200b\u200f\u206e\u206f\u202c\u200c\u202e\u200e\u202e\u200e\u202e\u200d\u206e\u200f\u206a\u206a\u206e\u202b\u206c\u200d\u202e\u206e\u206c\u200d\u202b\u202b\u200b\u206e\u200e\u202c\u200c\u202c\u206f\u206f\u206f\u206c\u206f\u206b\u202e;

	internal uint \u200e\u206c\u200f\u200e\u206b\u206c\u202b\u206a\u200e\u206b\u206d\u200d\u206e\u202c\u202a\u202c\u202e\u200c\u206c\u202e\u202a\u200c\u206a\u202d\u206d\u202b\u200b\u200c\u206d\u206f\u200f\u200f\u200c\u206f\u206e\u200b\u206c\u206c\u200b\u200f\u202e;

	internal Stream \u200f\u206a\u202a\u200c\u200d\u202a\u206e\u206e\u206e\u202d\u206f\u200e\u202b\u206c\u202e\u200c\u206d\u200b\u200c\u200e\u202a\u206f\u206e\u206e\u202a\u202e\u202e\u200f\u202a\u200c\u200c\u202d\u206d\u206c\u202a\u206b\u202e\u206f\u206a\u200e\u202e;

	internal void get(Stream stream)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x55
	}

	internal void set()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x45
	}

	internal void run()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x03 0x66
	}

	internal uint add(int numTotalBits)
	{
		/*Error: Unknown opcode: 0xE3*/;
	}

	internal Data()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xDC
	}
}
internal class Item
{
	internal readonly Init[] \u200d\u200c\u206b\u200f\u206a\u200f\u206d\u200f\u202b\u206e\u206d\u200c\u206c\u202d\u206a\u202d\u200b\u202e\u200c\u206d\u202d\u200b\u200b\u202d\u200e\u206f\u206d\u206a\u206e\u200d\u200b\u206f\u200f\u200e\u202d\u206d\u206c\u200c\u206c\u202c\u202e;

	internal readonly Init[] \u206a\u206c\u206f\u206c\u202a\u202c\u200c\u206e\u206c\u200c\u200b\u200e\u202b\u200e\u206e\u206a\u206f\u202a\u200f\u206b\u200d\u202c\u200d\u202c\u200e\u202e\u202b\u202d\u202c\u206d\u202d\u202a\u206b\u206e\u206f\u206b\u200d\u206b\u202c\u202c\u202e;

	internal readonly Init[] \u206b\u202b\u206b\u206e\u206e\u206e\u200b\u200d\u206d\u202a\u202e\u200d\u206c\u202e\u206d\u206a\u202e\u200f\u202b\u202b\u202d\u202e\u202c\u200d\u206f\u200c\u206b\u202c\u206f\u200f\u206e\u206a\u206f\u202b\u200b\u206d\u200c\u206c\u202c\u202e\u202e;

	internal readonly Init[] \u202b\u202c\u206e\u200d\u202c\u200d\u200f\u206e\u200b\u202c\u206e\u206e\u206c\u200d\u200f\u200b\u200d\u206d\u202d\u206c\u202b\u200c\u200c\u202e\u202a\u202e\u200b\u202c\u200f\u202b\u206c\u202c\u200f\u202a\u202a\u206d\u200b\u202b\u202e\u200e\u202e;

	internal readonly Init[] \u202e\u202b\u206b\u206f\u202b\u200e\u200b\u206b\u206b\u200d\u206b\u202e\u200c\u200d\u206d\u200b\u202e\u200f\u206b\u202c\u206c\u206c\u202e\u200e\u200e\u202d\u206a\u202c\u202c\u202a\u200e\u206a\u206e\u200d\u206a\u200f\u202d\u206d\u202e;

	internal readonly Init[] \u202c\u202e\u200e\u202b\u200c\u200d\u206d\u206a\u202d\u200f\u206e\u202e\u200b\u206f\u200e\u206e\u202c\u202b\u206b\u200f\u206f\u200b\u202b\u200e\u202e\u206f\u202a\u202a\u200e\u206d\u200d\u206f\u206d\u200f\u200b\u200b\u200f\u206c\u200c\u206f\u202e;

	internal readonly Node \u206f\u200b\u200e\u200e\u200b\u200e\u202c\u200d\u202c\u200d\u200c\u206d\u206c\u202b\u200f\u206c\u200b\u202e\u202c\u200d\u200b\u206b\u202b\u206e\u206f\u200f\u206a\u206e\u202e\u202d\u202d\u202d\u202e\u206b\u202c\u200d\u206f\u202e\u202d\u200f\u202e;

	internal readonly Text \u202c\u206b\u202e\u202c\u200f\u202c\u200d\u206b\u206b\u206c\u206e\u200c\u202e\u200c\u206e\u200e\u206d\u206f\u202c\u206d\u202d\u200f\u206c\u206d\u202d\u200f\u206b\u200d\u206e\u202e\u206e\u200d\u206c\u206c\u202c\u206c\u206f\u202a\u200b\u206c\u202e;

	internal readonly Info \u200e\u202e\u202d\u202a\u202c\u202a\u202c\u200f\u202c\u200c\u200f\u200b\u200f\u202d\u200f\u202b\u202d\u206f\u206e\u206f\u202d\u206f\u200b\u200f\u206e\u202b\u202a\u200c\u206f\u200e\u206c\u200c\u206c\u202a\u206a\u206f\u200f\u206a\u202a\u206a\u202e;

	internal readonly Init[] \u206a\u206c\u202e\u200b\u206b\u206d\u200e\u200b\u206e\u200c\u202c\u202b\u206f\u206a\u200b\u200b\u206c\u206e\u206a\u206f\u202c\u202d\u206e\u200f\u200d\u202a\u202e\u206e\u206b\u200d\u202a\u200c\u202a\u206a\u200b\u206c\u200b\u200b\u200b\u200c\u202e;

	internal readonly Core[] \u202d\u206a\u202e\u206b\u200c\u202d\u202d\u202d\u200f\u200c\u206d\u200e\u200f\u200e\u202c\u206b\u200c\u200f\u206b\u200d\u206a\u202b\u200e\u202b\u206a\u200f\u202c\u200f\u200c\u200d\u202a\u206b\u202a\u202d\u206b\u200e\u202a\u206d\u206c\u202b\u202e;

	internal readonly Data \u206c\u206b\u202b\u200f\u206e\u206e\u206d\u206f\u206c\u200c\u202e\u206d\u202b\u202a\u200f\u206b\u202e\u206e\u206f\u206e\u200d\u202e\u202d\u206b\u200c\u206f\u200f\u202e\u202a\u202c\u200f\u206d\u206b\u202d\u206d\u200f\u206e\u202d\u202d\u200d\u202e;

	internal readonly Node \u202a\u206b\u200f\u202d\u202c\u202a\u202d\u206b\u202b\u200e\u202c\u202e\u206f\u200f\u200d\u202a\u202c\u202e\u202a\u206e\u206f\u200c\u202d\u206f\u202a\u202d\u202e\u202e\u200e\u202d\u206f\u200b\u206b\u202a\u202b\u206b\u202e\u200b\u202c\u202d\u202e;

	internal bool \u206c\u206b\u202a\u200f\u202d\u206c\u206b\u206b\u202c\u200d\u206b\u202b\u200c\u200d\u206a\u206b\u200b\u206c\u200e\u200f\u200b\u202a\u206e\u200c\u200c\u202c\u202c\u202b\u206b\u202e\u202a\u202e\u202c\u202a\u200b\u206d\u202b\u206a\u202e\u200e\u202e;

	internal uint \u206f\u206a\u202b\u206a\u202a\u202e\u202c\u202d\u200f\u206f\u206f\u206b\u206a\u202c\u202c\u200c\u200c\u206d\u202b\u200b\u200b\u206b\u200b\u202a\u206d\u202b\u200d\u200d\u200e\u206f\u200f\u200e\u200c\u206d\u202e\u206c\u202e\u202c\u206f\u202d\u202e;

	internal uint \u206c\u200e\u200d\u202e\u202c\u200e\u202a\u202e\u200e\u202c\u202d\u202b\u200e\u202d\u200b\u202b\u200d\u206c\u206c\u200b\u200d\u202c\u206a\u200d\u202c\u200e\u206d\u200b\u202d\u202e\u200e\u206a\u200b\u200f\u200d\u202e\u206c\u202d\u200b\u202c\u202e;

	internal Core \u202c\u206c\u202a\u206e\u206b\u202d\u200d\u200b\u206d\u206c\u206e\u206b\u200e\u206e\u202d\u200e\u206f\u206c\u206a\u206b\u206c\u202a\u206b\u206a\u200f\u200b\u200b\u206a\u200d\u200b\u202a\u202e\u206d\u206d\u206f\u202c\u202c\u200e\u200c\u206c\u202e;

	internal uint \u200d\u206e\u202c\u200c\u200f\u200b\u200c\u200d\u206d\u200d\u202c\u206f\u206d\u202e\u202a\u206f\u200c\u200c\u202c\u202c\u206c\u200b\u200e\u200d\u200c\u206f\u202a\u206b\u202c\u206d\u200f\u206c\u202d\u206e\u202c\u200c\u206a\u200c\u202d\u202d\u202e;

	internal Item()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xAB 0x56
	}

	internal void get(uint dictionarySize)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x28
	}

	internal void set(int lp, int lc)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x61
	}

	internal void run(int pb)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x3F 0xDF
	}

	internal void add(Stream inStream, Stream outStream)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xB0
	}

	internal void @new(Stream inStream, Stream outStream, long inSize, long outSize)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x20
	}

	internal void has(byte[] properties)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x2C
	}

	internal static uint to(uint len)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Expected Obj, but got Unknown
		_ = ((Array)(/*Error near IL_0001: Stack underflow*/ / /*Error near IL_0001: Stack underflow*/)).LongLength;
		/*Error near IL_0002: stloc 1 (out-of-bounds)*/;
		/*Error near IL_0003: Invalid metadata token*/;
	}
}
internal class Node
{
	internal readonly Core[] \u200b\u200b\u202d\u202d\u200e\u200e\u200f\u200f\u206d\u200e\u206f\u206d\u200d\u200d\u202c\u202e\u202b\u200b\u202c\u202e\u206b\u206a\u206d\u200d\u206b\u206d\u202b\u206a\u202d\u200f\u206b\u206b\u202b\u202d\u200b\u200d\u202e\u200b\u202c\u202b\u202e;

	internal readonly Core[] \u206e\u206d\u202d\u206d\u200d\u200f\u206a\u200e\u206c\u202c\u206c\u200c\u202c\u200b\u206a\u206c\u200f\u206a\u206e\u200b\u206a\u202e\u206f\u200e\u206d\u206b\u202a\u206f\u202c\u206f\u200b\u206e\u202e\u206e\u200b\u206a\u200e\u206e\u206a\u202a\u202e;

	internal Init \u200d\u206f\u200c\u200f\u200c\u206e\u206d\u206a\u200c\u200c\u202a\u202c\u202c\u202b\u202e\u202e\u200b\u200d\u202a\u206e\u200d\u200f\u200c\u200d\u202b\u206e\u200b\u202b\u200c\u202a\u206c\u200b\u200b\u200f\u206c\u200d\u206d\u202b\u202e\u206a\u202e;

	internal Init \u206f\u202a\u200c\u206d\u200e\u200f\u200f\u202d\u206b\u206a\u206b\u202e\u206e\u200e\u200f\u202e\u206d\u206d\u200e\u200f\u202e\u202d\u200d\u202d\u200c\u206f\u202a\u206f\u202e\u206f\u202a\u206f\u202d\u202b\u202b\u206e\u200d\u202c\u202c\u200e\u202e;

	internal Core \u200b\u200e\u202a\u202a\u200e\u200e\u202c\u206e\u200e\u206b\u200e\u202c\u206d\u200d\u202c\u200f\u202a\u200d\u200c\u200d\u202e\u202a\u206a\u200f\u200b\u202c\u200b\u202e\u200b\u202d\u206b\u200f\u206a\u202b\u206d\u202c\u206d\u200f\u206a\u202d\u202e;

	internal uint \u200b\u200c\u206c\u202b\u200e\u202c\u202a\u206b\u200c\u206c\u200e\u200b\u206d\u206e\u202d\u202e\u200c\u202b\u206a\u206f\u206d\u202d\u200e\u206f\u206a\u206a\u206a\u200f\u200d\u202d\u200d\u206c\u202e\u200c\u206c\u202e\u202c\u202a\u200d\u202a\u202e;

	internal void get(uint numPosStates)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xB5
	}

	internal void set()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x24
	}

	internal uint run(Data rangeDecoder, uint posState)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xCF 0x40
	}

	internal Node()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x11
	}
}
internal class Text
{
	internal File[] \u206c\u200f\u206e\u206d\u202e\u200e\u202e\u202b\u206a\u202a\u200f\u206b\u206d\u206e\u202e\u202d\u206d\u200c\u202e\u202e\u206c\u202c\u202e\u202c\u206f\u202b\u206b\u200f\u206f\u200c\u206d\u206f\u200e\u202b\u202a\u206d\u202a\u206a\u206e\u200c\u202e;

	internal int \u200e\u202e\u206a\u206e\u202b\u206b\u206e\u202e\u206f\u202b\u200f\u200d\u206f\u202d\u206c\u202c\u200e\u202a\u200c\u206b\u200f\u202e\u202a\u202d\u200d\u202d\u200c\u202e\u206f\u202b\u206b\u202b\u206e\u200b\u206b\u202e\u206d\u202e\u200b\u206b\u202e;

	internal int \u202a\u200b\u206d\u202e\u200e\u202e\u200e\u200f\u202b\u200e\u206a\u206e\u202c\u206e\u200c\u200e\u206f\u206f\u200e\u202a\u200d\u206c\u200d\u206f\u206a\u206b\u202d\u202b\u202d\u200f\u200e\u202c\u200d\u200d\u206c\u202b\u206f\u202a\u206f\u202e\u202e;

	internal uint \u206b\u206f\u206b\u202a\u202d\u206d\u200f\u200f\u202e\u206d\u200b\u200d\u200d\u206d\u202a\u202c\u202e\u202a\u206f\u202d\u200b\u202c\u200e\u200e\u200f\u202b\u206d\u200b\u206d\u206c\u202e\u200c\u206b\u202a\u202e\u206c\u200b\u202b\u200c\u200c\u202e;

	internal void get(int numPosBits, int numPrevBits)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xE3 0x6D
	}

	internal void set()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xED
	}

	internal uint run(uint pos, byte prevByte)
	{
		/*Error: Invalid branch target*/;
	}

	internal byte add(Data rangeDecoder, uint pos, byte prevByte)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x8F 0xA8
	}

	internal byte @new(Data rangeDecoder, uint pos, byte prevByte, byte matchByte)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xE7 0x98
	}

	internal Text()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xDC
	}
}
internal struct File
{
	internal Init[] \u200c\u200b\u200f\u202b\u206f\u202c\u206b\u202c\u202e\u200f\u206c\u202b\u200f\u206e\u206c\u206d\u200b\u206b\u202a\u200b\u206d\u202a\u200b\u202d\u202d\u200f\u200e\u202b\u202a\u200c\u206a\u206b\u200b\u200e\u206b\u200b\u202d\u206a\u200e\u202d\u202e;

	internal void get()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x9B 0x4F
	}

	internal unsafe void set()
	{
		//IL_0005: Expected I8, but got I4
		sbyte b = *(sbyte*)(nint)/*Error near IL_0001: Stack underflow*/;
		/*Error near IL_0001: ldloc 0 (out-of-bounds)*/;
		/*Error near IL_0002: stloc 40 (out-of-bounds)*/;
		((long[])/*Error near IL_0005: Stack underflow*/)[/*Error near IL_0005: Stack underflow*/] = b;
		*(?*)(nint)/*Error near IL_0006: Stack underflow*/ = /*Error near IL_0006: Stack underflow*/;
		/*Error near IL_0006: Unknown opcode: 0xE5*/;
	}

	internal byte run(Data rangeDecoder)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x2C
	}

	internal byte add(Data rangeDecoder, byte matchByte)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x7D
	}
}
internal class Info
{
	internal byte[] \u206b\u200e\u206f\u200c\u206f\u200e\u206a\u202a\u202d\u206a\u202b\u206a\u202b\u202d\u200c\u200d\u200b\u206d\u206a\u202d\u206e\u206e\u202c\u200e\u206f\u202d\u200f\u200d\u200d\u200c\u200d\u206a\u202d\u200c\u200c\u206b\u206f\u206b\u206f\u200e\u202e;

	internal uint \u206c\u202e\u200c\u202a\u206b\u206c\u202c\u206d\u206e\u200c\u200c\u206b\u200d\u206f\u206a\u200b\u202b\u200c\u206d\u206d\u206a\u206e\u206a\u200e\u206e\u206d\u202b\u206c\u206c\u206b\u206c\u206b\u202d\u202b\u206e\u202c\u200b\u206e\u206f\u202e;

	internal Stream \u206c\u202b\u206b\u202d\u200c\u200f\u200d\u202b\u200d\u200d\u202a\u206d\u202d\u200f\u206e\u200c\u200f\u200f\u202e\u206e\u202c\u206f\u206a\u206f\u202e\u206f\u206d\u200c\u206b\u206a\u206a\u206e\u206a\u202b\u202a\u202d\u202d\u202e\u200d\u202e\u202e;

	internal uint \u206e\u200f\u206c\u202d\u206d\u202a\u206d\u200f\u202b\u202b\u202c\u200d\u206a\u206a\u200f\u206f\u200c\u200c\u202b\u206b\u200b\u202a\u200f\u200c\u206e\u206c\u200f\u202a\u202a\u202a\u202d\u202d\u206f\u200d\u202e\u200d\u206f\u206c\u206a\u206c\u202e;

	internal uint \u200d\u200b\u206f\u206b\u200c\u202d\u200d\u200e\u200c\u206a\u200b\u200c\u206c\u200c\u202a\u202a\u206f\u202c\u202a\u200f\u202e\u202a\u202b\u200c\u206b\u202d\u206d\u202a\u206f\u206e\u200f\u202b\u206a\u200d\u200f\u206c\u202e\u206e\u206a\u206f\u202e;

	internal void get(uint windowSize)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xBD
	}

	internal void set(Stream stream, bool solid)
	{
		/*Error: Invalid metadata token*/;
	}

	internal void run()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x6D
	}

	internal void add()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x90
	}

	internal void @new(uint distance, uint len)
	{
		((int[])/*Error near IL_0001: Stack underflow*/)[/*Error near IL_0001: Stack underflow*/] = (int)/*Error near IL_0001: Stack underflow*/;
		checked
		{
			_ = (sbyte)/*Error near IL_0002: Stack underflow*/;
			/*Error: End of method reached without returning.*/;
		}
	}

	internal void has(byte b)
	{
		/*Error: Metadata token must be either a methoddef, memberref or methodspec*/;
	}

	internal byte to(uint distance)
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xB5
	}

	internal Info()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xDC
	}
}
internal struct Base
{
	internal uint \u202a\u200e\u200e\u200b\u200b\u202d\u200c\u202b\u206b\u202b\u202c\u202b\u200e\u206c\u200d\u200f\u206a\u206d\u200d\u202a\u206f\u200f\u202a\u200e\u206a\u206c\u206e\u202b\u206c\u200b\u206f\u200f\u206d\u206f\u206b\u202c\u202e\u200e\u206b\u202b\u202e;

	internal void get()
	{
		/*Error: Unknown opcode: 0xBC*/;
	}

	internal void set()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x9F 0x88
	}

	internal void run()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0x94
	}

	internal unsafe void add()
	{
		//IL_0002: Invalid comparison between Unknown and I4
		*(?*)(nint)/*Error near IL_0001: Stack underflow*/ = /*Error near IL_0001: Stack underflow*/;
		if ((int)/*Error near IL_0007: Stack underflow*/ > 8)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0007: Unknown opcode: 0xCA*/;
	}

	internal void @new()
	{
		if (/*Error near IL_0002: Stack underflow*/ >= /*Error near IL_0002: Stack underflow*/)
		{
			/*Error: Invalid branch target*/;
		}
		/*Error near IL_0002: Unknown opcode: 0xF7*/;
	}

	internal bool has()
	{
		//Invalid MethodBodyBlock: Invalid method header: 0xF0
	}
}
[StructLayout(LayoutKind.Explicit, Size = 1216)]
internal struct Main
{
}
internal class ConfusedByAttribute : Attribute
{
	public ConfusedByAttribute(string P_0)
	{
	}
}

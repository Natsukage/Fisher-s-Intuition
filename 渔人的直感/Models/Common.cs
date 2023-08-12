﻿using System;
using System.Collections.Generic;

namespace 渔人的直感.Models
{
    /// <summary>
    /// 数据内容相关
    /// </summary>
    public static class Data
    {

        public const int FishEyesBuffId = 762; // 50; //      鱼眼的Buff ID

        public static List<SpecialWeather> SpecialWeathers = new List<SpecialWeather>();

        private static SigScanner _scanner;
        public static IntPtr StatusPtr;
        public static IntPtr ActorTable;
        //public static IntPtr BuffTablePtr;
        private static IntPtr weatherPtr;

        private static IntPtr eventFrameworkPtr;
        private static short contentDirectorOffset;
        private static short oceanFishingTimeOffsetOffset;
        private static short contentTimeLeftOffset;
        private static short contentDirectorTypeOffset;

        public static IntPtr WeatherPtr
        {
            get => _scanner.ReadIntPtr(weatherPtr) + 0x20;
        }

        public static IntPtr OceanFishingContentDirector => GetInstanceContentDirector();

        public const int UiStatusEffects = 6488; //UIStatusEffects相对于ActorTable的偏移。此值随着版本更新随时可能发生改变。

        public static void Initialize(SigScanner scanner)
        {
            _scanner = scanner;
            //Status用于获取EventPlay时玩家动作，判断抛竿、咬钩、脱钩动作。
            StatusPtr = scanner.GetStaticAddressFromSig("48 8D 0D ? ? ? ? 48 8B AC 24 ? ? ? ?");

            //ActorTable用于获取UIStatusEffects地址，追踪玩家身上的buff
            ActorTable = scanner.GetStaticAddressFromSig("88 91 ?? ?? ?? ?? 48 8D 3D ?? ?? ?? ??");
            //玩家退回标题界面重新登录后，ActorTable地址不变，但是UIStatusEffects对应地址会变，因此不能在初始化时直接解析，只能在每次访问时解析。
            //当处于登录界面时，ActorTable第一位指向的Actor地址为0。可以据此判断当前处于登录界面。
            //Data.BuffTablePtr = scanner.ReadIntPtr(Data.ActorTable) + 6488;

            //Weather用于获取当前天气，判断幻海流、空岛特殊天气触发等。
            weatherPtr = scanner.GetStaticAddressFromSig("48 8D 0D ? ? ? ? 0F 28 DE") + 8;

            //获取EventFrameworkPtr
            eventFrameworkPtr = scanner.GetStaticAddressFromSig("48 8B 35 ?? ?? ?? ?? 0F B6 EA 4C 8B F1");

            //获取Offset相关
            contentDirectorOffset = scanner.ReadInt16(scanner.ScanText("48 83 B9 ?? ?? ?? ?? ?? 74 ?? B0 ?? C3 48 8B 81"), 3);
            oceanFishingTimeOffsetOffset = scanner.ReadInt16(scanner.ScanText("89 83 ? ? ? ? 48 89 83 ? ? ? ? 48 89 83 ? ? ? ? 88 83 ? ? ? ? 89 83 ? ? ? ? 88 83"), 2);
            contentTimeLeftOffset = scanner.ReadInt16(scanner.ScanText("F3 0F 10 81 ?? ?? ?? ?? 0F 2F C4"), 4);
            contentDirectorTypeOffset = scanner.ReadInt16(scanner.ScanText("80 B9 ?? ?? ?? ?? ?? 48 8B D9 75 ?? 48 8B 49 ?? BA"), 2);

            Console.WriteLine($@"contentDirectorTypeOffset: 0x{contentDirectorTypeOffset:X}, contentTimeLeftOffset: 0x{contentTimeLeftOffset:X}, oceanFishingTimeOffsetOffset: 0x{oceanFishingTimeOffsetOffset:X}, contentDirectorOffset: 0x{contentDirectorOffset:X}");

            SpecialWeathers.Add(new SpecialWeather { Id = 145, Name = "幻海流", Duration = 120f });
            if (Properties.Settings.Default.CheckDiademWeather)
            {
                SpecialWeathers.Add(new SpecialWeather { Id = 133, Name = "灵烈火", Duration = 600f });
                SpecialWeathers.Add(new SpecialWeather { Id = 134, Name = "灵飘尘", Duration = 600f });
                SpecialWeathers.Add(new SpecialWeather { Id = 135, Name = "灵飞电", Duration = 600f });
                SpecialWeathers.Add(new SpecialWeather { Id = 136, Name = "灵罡风", Duration = 600f });
            }
            //SpecialWeathers.Add(new SpecialWeather { Id = 1, Name = "碧空（测试）", Duration = 60f });
            //SpecialWeathers.Add(new SpecialWeather { Id = 2, Name = "晴朗（测试）", Duration = 60f });
        }

        //基本上是rebuild了这个函数 E8 ?? ?? ?? ?? 0F B6 98
        private static IntPtr GetInstanceContentDirector()
        {
            //找不到EventFrameworkPtr
            if (eventFrameworkPtr == IntPtr.Zero)
                return IntPtr.Zero;

            var directorPtr = _scanner.ReadIntPtr(eventFrameworkPtr, contentDirectorOffset);
            //找不到ContentDirector
            if (directorPtr == IntPtr.Zero)
                return IntPtr.Zero;

            //检查Director类型是否为InstanceContent
            var type = BitConverter.ToUInt16(_scanner.ReadBytes(_scanner.ReadIntPtr(directorPtr, 8) + 2, 2), 0);
            if (type != 0x8003)
                return IntPtr.Zero;

            //检查InstanceContent的类型是否为OceanFishing
            if (_scanner.ReadByte(directorPtr, contentDirectorTypeOffset) != 16)
            {

                return IntPtr.Zero;
            }

            return directorPtr;
        }

    }

    public struct SpecialWeather
    {
        public byte Id;
        public string Name;
        public float Duration;
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using ImGuiNET;

namespace Divination.Debugger.Window
{
    public class DataViewer
    {
        private readonly bool isEnableValueFilter;
        private readonly long filterValue;
        private readonly List<DataRow>? rows;

        public DataViewer(DataType dataType, byte[] data, bool isEnableValueFilter, long filterValue)
        {
            this.isEnableValueFilter = isEnableValueFilter;
            this.filterValue = filterValue;

            rows = dataType switch
            {
                DataType.UInt8 => PrepareData(data, 1),
                DataType.Int8 => PrepareData(data.TransformIntoInt8(), 1),
                DataType.UInt16 => PrepareData(data.TransformIntoUInt16(), 2),
                DataType.Int16 => PrepareData(data.TransformIntoInt16(), 2),
                DataType.UInt32 => PrepareData(data.TransformIntoUInt32(), 4),
                DataType.Int32 => PrepareData(data.TransformIntoInt32(), 4),
                DataType.UInt64 => PrepareData(data.TransformIntoUInt64(), 8),
                DataType.Int64 => PrepareData(data.TransformIntoInt64(), 8),
                DataType.HexView => PrepareData(data, 16),
                _ => throw new NotImplementedException(),
            };
        }

        public void Draw()
        {
            if (rows == null)
            {
                return;
            }

            ImGui.Columns(2);

            ImGui.Text("Index"); ImGui.NextColumn();
            ImGui.Text("Value"); ImGui.NextColumn();
            ImGui.Separator();

            foreach (var (index, value) in rows)
            {
                ImGui.Text($"0x{index:X4}  ({index})"); ImGui.NextColumn();
                ImGui.PushFont(Dalamud.Interface.UiBuilder.MonoFont);
                ImGui.Text(value); ImGui.NextColumn();
                ImGui.PopFont();
            }

            ImGui.Columns(1);
        }

        public bool Any() => rows is { Count: > 0 };

        private List<DataRow>? PrepareData<T>(T[] source, int byteCount) where T: struct, IFormattable
        {
            if (!IsMatchedPre(source))
            {
                return null;
            }
            int l = Marshal.SizeOf(typeof(T)) * 2;
            int n = (l == (byteCount * 2)) ? 1 : byteCount;
            string fmt = (n == 1) ? "d" : $"x{l}";
            System.Globalization.NumberFormatInfo nFmt = Thread.CurrentThread.CurrentCulture.NumberFormat;
            return source.Where((row, index) => (index * byteCount) < source.Length)
                .Select((value, index) => (index: (index * byteCount),
                                           value: new ArraySegment<T>(source, (index * byteCount),
                                                                      SafeUbound((index * byteCount), n, source.Length))))
                .Where((row) => IsMatchedPost(row.value))
                .Select(row => new DataRow(row.index, String.Join(" ", row.value.Select(v => v.ToString(fmt, nFmt)!))))
                .ToList();
        }

        private int SafeUbound(int pos, int slice, int total)
        {
            return Math.Max(0, Math.Min((total - pos), slice));
        }

        private bool IsMatchedPre<T>(T[] source) where T : struct
        {
            if (!isEnableValueFilter)
            {
                return true;
            }

            switch (source)
            {
                case byte[]:
                    return filterValue is >= byte.MinValue and <= byte.MaxValue;
                case sbyte[]:
                    return filterValue is >= sbyte.MinValue and <= sbyte.MaxValue;
                case ushort[]:
                    return filterValue is >= ushort.MinValue and <= ushort.MaxValue;
                case short[]:
                    return filterValue is >= short.MinValue and <= short.MaxValue;
                case uint[]:
                    return filterValue is >= uint.MinValue and <= uint.MaxValue;
                case int[]:
                    return filterValue is >= int.MinValue and <= int.MaxValue;
                case ulong[]:
                    return filterValue >= 0;
                case long[]:
                    return true;
                default:
                    throw new NotImplementedException();
            }
        }

        private bool IsMatchedPost<T>(ArraySegment<T> value) where T: struct
        {
            if (!isEnableValueFilter)
            {
                return true;
            }

            switch (value[0])
            {
                case byte u8:
                    return u8 == filterValue;
                case sbyte i8:
                    return i8 == filterValue;
                case ushort u16:
                    return u16 == filterValue;
                case short i16:
                    return i16 == filterValue;
                case uint u32:
                    return u32 == filterValue;
                case int i32:
                    return i32 == filterValue;
                case ulong u64:
                    return u64 == (ulong)filterValue;
                case long i64:
                    return i64 == filterValue;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}

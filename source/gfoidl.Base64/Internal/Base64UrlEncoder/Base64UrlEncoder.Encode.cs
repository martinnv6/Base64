﻿using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Scalar based on https://github.com/dotnet/corefx/tree/ec34e99b876ea1119f37986ead894f4eded1a19a/src/System.Memory/src/System/Buffers/Text
// SSE2 based on https://github.com/aklomp/base64/tree/a27c565d1b6c676beaf297fe503c4518185666f7/lib/arch/ssse3
// AVX2 based on https://github.com/aklomp/base64/tree/a27c565d1b6c676beaf297fe503c4518185666f7/lib/arch/avx2

namespace gfoidl.Base64.Internal
{
    partial class Base64UrlEncoder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetEncodedLength(int sourceLength)
        {
            // Shortcut for Guid and other 16 byte data
            if (sourceLength == 16)
                return 22;

            int numPaddingChars  = GetNumBase64PaddingCharsAddedByEncode(sourceLength);
            int base64EncodedLen = GetBase64EncodedLength(sourceLength);

            return base64EncodedLen - numPaddingChars;
        }
        //---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBufferSizeRequiredToBase64Encode(int sourceLength, out int numPaddingChars)
        {
            // Shortcut for Guid and other 16 byte data
            if (sourceLength == 16)
            {
                numPaddingChars = 2;
                return 24;
            }

            numPaddingChars = GetNumBase64PaddingCharsAddedByEncode(sourceLength);
            return GetBase64EncodedLength(sourceLength);
        }
        //---------------------------------------------------------------------
        // PERF: can't be in base class due to inlining (generic virtual)
        protected override OperationStatus EncodeCore(
            ReadOnlySpan<byte> data,
            Span<byte> encoded,
            out int consumed,
            out int written,
            int encodedLength = -1,
            bool isFinalBlock = true)
            => this.EncodeImpl(data, encoded, out consumed, out written, encodedLength, isFinalBlock);
        //---------------------------------------------------------------------
        // PERF: can't be in base class due to inlining (generic virtual)
        protected override OperationStatus EncodeCore(
            ReadOnlySpan<byte> data,
            Span<char> encoded,
            out int consumed,
            out int written,
            int encodedLength = -1,
            bool isFinalBlock = true)
            => this.EncodeImpl(data, encoded, out consumed, out written, encodedLength, isFinalBlock);
        //---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private OperationStatus EncodeImpl<T>(
            ReadOnlySpan<byte> data,
            Span<T> encoded,
            out int consumed,
            out int written,
            int encodedLength = -1,
            bool isFinalBlock = true)
            where T : unmanaged
        {
            if (data.IsEmpty)
            {
                consumed = 0;
                written = 0;
                return OperationStatus.Done;
            }

            int srcLength     = data.Length;
            ref byte srcBytes = ref MemoryMarshal.GetReference(data);
            ref T dest        = ref MemoryMarshal.GetReference(encoded);

            if (encodedLength == -1)
                encodedLength = this.GetEncodedLength(srcLength);

            return this.EncodeImpl(ref srcBytes, srcLength, ref dest, encoded.Length, encodedLength, out consumed, out written, isFinalBlock);
        }
        //---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeTwoBytes<T>(ref byte twoBytes, ref T encoded, ref byte encodingMap)
        {
            uint i = (uint)twoBytes << 16
                | (uint)Unsafe.Add(ref twoBytes, 1) << 8;

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 18));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 12) & 0x3F));
            uint i2 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 6) & 0x3F));

            if (typeof(T) == typeof(byte))
            {
                ref byte enc = ref Unsafe.As<T, byte>(ref encoded);
                Unsafe.Add(ref enc, 0) = (byte)i0;
                Unsafe.Add(ref enc, 1) = (byte)i1;
                Unsafe.Add(ref enc, 2) = (byte)i2;
            }
            else if (typeof(T) == typeof(char))
            {
                ref char enc = ref Unsafe.As<T, char>(ref encoded);
                Unsafe.Add(ref enc, 0) = (char)i0;
                Unsafe.Add(ref enc, 1) = (char)i1;
                Unsafe.Add(ref enc, 2) = (char)i2;
            }
            else
            {
                throw new NotSupportedException();  // just in case new types are introduced in the future
            }
        }
        //---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncodeOneByte<T>(ref byte oneByte, ref T encoded, ref byte encodingMap)
        {
            uint i = (uint)oneByte << 8;

            uint i0 = Unsafe.Add(ref encodingMap, (IntPtr)(i >> 10));
            uint i1 = Unsafe.Add(ref encodingMap, (IntPtr)((i >> 4) & 0x3F));

            if (typeof(T) == typeof(byte))
            {
                ref byte enc = ref Unsafe.As<T, byte>(ref encoded);
                Unsafe.Add(ref enc, 0) = (byte)i0;
                Unsafe.Add(ref enc, 1) = (byte)i1;
            }
            else if (typeof(T) == typeof(char))
            {
                ref char enc = ref Unsafe.As<T, char>(ref encoded);
                Unsafe.Add(ref enc, 0) = (char)i0;
                Unsafe.Add(ref enc, 1) = (char)i1;
            }
            else
            {
                throw new NotSupportedException();  // just in case new types are introduced in the future
            }
        }
        //---------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumBase64PaddingCharsAddedByEncode(int dataLength)
        {
            // Calculation is:
            // switch (dataLength % 3)
            // 0 -> 0
            // 1 -> 2
            // 2 -> 1

            return dataLength % 3 == 0 ? 0 : 3 - dataLength % 3;
        }
        //---------------------------------------------------------------------
        // internal because tests use this map too
        internal static ReadOnlySpan<byte> s_encodingMap => new byte[] {
            0,      // https://github.com/dotnet/coreclr/issues/23194
            65, 66, 67, 68, 69, 70, 71, 72,         //A..H
            73, 74, 75, 76, 77, 78, 79, 80,         //I..P
            81, 82, 83, 84, 85, 86, 87, 88,         //Q..X
            89, 90, 97, 98, 99, 100, 101, 102,      //Y..Z, a..f
            103, 104, 105, 106, 107, 108, 109, 110, //g..n
            111, 112, 113, 114, 115, 116, 117, 118, //o..v
            119, 120, 121, 122, 48, 49, 50, 51,     //w..z, 0..3
            52, 53, 54, 55, 56, 57, 45, 95          //4..9, -, _
        };
    }
}

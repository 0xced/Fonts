// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace SixLabors.Fonts.Native
{
    /// <summary>
    /// An enumerator that enumerates over available macOS system fonts.
    /// The enumerated strings are the absolute paths to the font files.
    /// </summary>
    /// <remarks>
    /// Internally, it uses a native library that calls CoreText's <c>CTFontManagerCopyAvailableFontURLs</c>
    /// method to retrieve the list of fonts so using this class must be guarded by <c>RuntimeInformation.IsOSPlatform(OSPlatform.OSX)</c>.
    /// </remarks>
    internal class MacSystemFontsEnumerator : IEnumerable<string>, IEnumerator<string>
    {
        private static int iteratorId;

        private readonly ArrayPool<byte> bytePool;
        private readonly nuint initialBufferLength;
        private readonly int id;
        private readonly bool nativeEndIterating;

        public MacSystemFontsEnumerator(ArrayPool<byte>? bytePool = null, nuint initialBufferLength = 1024)
            : this(bytePool ?? ArrayPool<byte>.Shared, initialBufferLength, null)
        {
        }

        private MacSystemFontsEnumerator(ArrayPool<byte> bytePool, nuint initialBufferLength, int? id = null)
        {
            this.bytePool = bytePool;
            this.initialBufferLength = initialBufferLength;

            this.Current = null!;

            if (id.HasValue)
            {
                this.id = id.Value;
            }
            else
            {
                Interlocked.Increment(ref iteratorId);
                this.id = iteratorId;
                NativeMethods.StartIterating(this.id);
            }

            this.nativeEndIterating = !id.HasValue;
        }

        private enum MoveNextStatusCode
        {
            Success = 0,
            BufferTooSmall = 1,
        }

        public string Current { get; private set; }

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            var bufferLength = this.initialBufferLength;
            var fontPathBuffer = this.bytePool.Rent((int)bufferLength);
            MoveNextStatusCode status = NativeMethods.MoveNext(this.id, fontPathBuffer, ref bufferLength);
            if (status == MoveNextStatusCode.BufferTooSmall)
            {
                this.bytePool.Return(fontPathBuffer);
                fontPathBuffer = this.bytePool.Rent((int)bufferLength);
                status = NativeMethods.MoveNext(this.id, fontPathBuffer, ref bufferLength);
            }

            Trace.Assert(status == MoveNextStatusCode.Success, $"The native MoveNext method returned a non success status code ({status})");

            this.Current = Encoding.UTF8.GetString(fontPathBuffer, 0, (int)bufferLength);

            this.bytePool.Return(fontPathBuffer);

            return this.Current.Length > 0;
        }

        public void Reset() => NativeMethods.Reset(this.id);

        public void Dispose()
        {
            if (this.nativeEndIterating)
            {
                NativeMethods.EndIterating(this.id);
            }
        }

        public IEnumerator<string> GetEnumerator() => new MacSystemFontsEnumerator(this.bytePool, this.initialBufferLength, this.id);

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private static class NativeMethods
        {
            private const string SixLaborsFontsNativeLib = "SixLabors.Fonts.Native";

            [DllImport(SixLaborsFontsNativeLib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void StartIterating(int iteratorId);

            [DllImport(SixLaborsFontsNativeLib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern MoveNextStatusCode MoveNext(int iteratorId, byte[] fontPath, ref nuint fontPathLength);

            [DllImport(SixLaborsFontsNativeLib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void Reset(int iteratorId);

            [DllImport(SixLaborsFontsNativeLib, CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
            internal static extern void EndIterating(int iteratorId);
        }
    }
}

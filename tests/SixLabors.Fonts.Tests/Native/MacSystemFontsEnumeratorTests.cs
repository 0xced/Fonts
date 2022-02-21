// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

#if !NET472
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using SixLabors.Fonts.Native;
using Xunit;

namespace SixLabors.Fonts.Tests.Native
{
    public class MacSystemFontsEnumeratorTests
    {
        [Fact]
        public void TestDefaultEnumeration()
        {
            var counter = new CounterByteArrayPool();

            using var enumerator = new MacSystemFontsEnumerator(counter, initialBufferLength: 1024);
            _ = enumerator.Count();

            // Ensure that 1024 was enough and that a larger buffer was never rented
            var minimumLength = Assert.Single(counter.RentCount.Keys);
            Assert.Equal(1024, minimumLength);

            // Ensure everything rented from the pool was returned
            var totalRentCount = counter.RentCount.Values.Sum();
            Assert.Equal(totalRentCount, counter.ReturnCount);
        }

        [Fact]
        public void TestSmallBufferEnumeration()
        {
            var counter = new CounterByteArrayPool();

            using var enumerator = new MacSystemFontsEnumerator(counter, initialBufferLength: 5);
            var numberOfFonts = enumerator.Count();

            // Ensure that larger buffers were rented
            Assert.True(counter.RentCount.Keys.Count > 1);

            // Ensure everything rented from the pool was returned
            var totalRentCount = counter.RentCount.Values.Sum();
            Assert.Equal(totalRentCount, counter.ReturnCount);

            // Ensure rent with minimumLength of 5 was called the number of fonts plus one (for the terminating condition)
            Assert.Equal(numberOfFonts + 1, counter.RentCount[5]);

            // Ensure that buffers were rented at the second try for the exact number of fonts
            counter.RentCount.Remove(5);
            Assert.Equal(numberOfFonts, counter.RentCount.Values.Sum());
        }

        [Fact]
        public void TestReset()
        {
            using var enumerator = new MacSystemFontsEnumerator();
            var fonts1 = new HashSet<string>(enumerator);
            Assert.NotEmpty(fonts1);

            enumerator.Reset();
            var fonts2 = new HashSet<string>(enumerator);
            Assert.Empty(fonts1.Except(fonts2));
        }

        private class CounterByteArrayPool : ArrayPool<byte>
        {
            public readonly Dictionary<int, int> RentCount = new();
            public int ReturnCount;

            public override byte[] Rent(int minimumLength)
            {
                if (!this.RentCount.TryAdd(minimumLength, 1))
                {
                    this.RentCount[minimumLength] += 1;
                }

                return Shared.Rent(minimumLength);
            }

            public override void Return(byte[] array, bool clearArray = false)
            {
                this.ReturnCount += 1;
                Shared.Return(array, clearArray);
            }
        }
    }
}
#endif

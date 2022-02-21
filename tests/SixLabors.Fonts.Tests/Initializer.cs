// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

#if NETCOREAPP3_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using SixLabors.Fonts.Native;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("SixLabors.Fonts.Tests.Initializer", "SixLabors.Fonts.Tests")]

namespace SixLabors.Fonts.Tests
{
    // Adapted from https://stackoverflow.com/questions/13829737/run-code-once-before-and-after-all-tests-in-xunit-net/53143426#53143426
    public class Initializer : XunitTestFramework
    {
        private readonly ConcurrentDictionary<string, IntPtr> nativeLibraries = new();

        public Initializer(IMessageSink messageSink)
            : base(messageSink)
        {
            NativeLibrary.SetDllImportResolver(typeof(MacSystemFontsEnumerator).Assembly, this.ResolveNativeLibrary);
        }

        /// <summary>
        /// Resolving libSixLabors.Fonts.Native.dylib is required in unit tests because the test runner
        /// doesn't know where to find the dylib. When distributed as a NuGet package, with the dylib inside the
        /// <c>runtimes/osx/native</c> directory, the runtime knows how to automatically resolve the dylib.
        /// </summary>
        private IntPtr ResolveNativeLibrary(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            return this.nativeLibraries.GetOrAdd(libraryName, name =>
            {
                var configuration = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
                var frameworkName = NuGetFramework.Parse(assembly.GetCustomAttribute<TargetFrameworkAttribute>()!.FrameworkName).GetShortFolderName();
                var initialDirectory = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!);
                for (DirectoryInfo directory = initialDirectory; directory != null; directory = directory.Parent)
                {
                    var artifacts = Directory.EnumerateDirectories(directory.FullName, "artifacts").FirstOrDefault();
                    if (artifacts is not null)
                    {
                        return NativeLibrary.Load(Path.Combine(artifacts, "obj", "src", "SixLabors.Fonts", configuration, frameworkName, $"lib{name}.dylib"));
                    }
                }

                return IntPtr.Zero;
            });
        }
    }
}
#endif

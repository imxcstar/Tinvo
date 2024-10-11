using LLama.Abstractions;
using LLama.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TrustInnova.Abstractions;

namespace TrustInnova.Provider.LLama
{
    public static class LLamaProviderRegistererExtensions
    {
        public static ProviderRegisterer RegistererLLamaProvider(this ProviderRegisterer registerer)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                NativeLibraryConfig.All.WithLibrary("./", "./").WithAutoFallback(true);
            else
                NativeLibraryConfig.All.WithAutoFallback(false);

            var avx = AvxLevel.None;
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86 || RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                if (
                    System.Runtime.Intrinsics.X86.Avx512BW.IsSupported ||
                    System.Runtime.Intrinsics.X86.Avx512CD.IsSupported ||
                    System.Runtime.Intrinsics.X86.Avx512DQ.IsSupported ||
                    System.Runtime.Intrinsics.X86.Avx512F.IsSupported ||
                    System.Runtime.Intrinsics.X86.Avx512Vbmi.IsSupported)
                {
                    avx = AvxLevel.Avx512;
                }
                else if (System.Runtime.Intrinsics.X86.Avx2.IsSupported)
                {
                    avx = AvxLevel.Avx2;
                }
                else if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
                {
                    avx = AvxLevel.Avx;
                }
            }

            NativeLibraryConfig.All.WithAvx(avx)
                                    .WithCuda(false)
                                    .WithVulkan(true);

            var types = Assembly.GetExecutingAssembly().GetTypes();
            registerer.AddProviderInfo(new ProviderInfo()
            {
                ID = "LLama",
                Name = "LLama",
                AllTaskType = types.Where(x => x.GetCustomAttribute<ProviderTaskAttribute>() != null).ToList(),
                AllTaskConfigType = types.Where(x => x.GetCustomAttribute<TypeMetadataDisplayNameAttribute>() != null).ToList(),
            });
            return registerer;
        }
    }
}

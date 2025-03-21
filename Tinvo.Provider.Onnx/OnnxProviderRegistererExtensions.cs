﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tinvo.Abstractions;

namespace Tinvo.Provider.Onnx
{
    public static class OnnxProviderRegistererExtensions
    {
        public static ProviderRegisterer RegistererOnnxProvider(this ProviderRegisterer registerer)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            registerer.Services.AddSingleton<OnnxChatProviderLoader>();
            registerer.AddProviderInfo(new ProviderInfo()
            {
                ID = "Onnx",
                Name = "Onnx",
                AllTaskType = types.Where(x => x.GetCustomAttribute<ProviderTaskAttribute>() != null).ToList(),
                AllTaskConfigType = types.Where(x => x.GetCustomAttribute<TypeMetadataDisplayNameAttribute>() != null).ToList(),
            });
            return registerer;
        }
    }
}

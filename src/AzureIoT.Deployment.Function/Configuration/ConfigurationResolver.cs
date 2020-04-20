﻿using System.Collections.Generic;
using VirtualRtu.Configuration.Deployment;

namespace AzureIoT.Deployment.Function.Configuration
{
    public abstract class ConfigurationResolver
    {
        private static List<MetadataHandler> metadataHandlers;

        public static DeviceConfig Configure(DeviceConfig edgeConfig, LocalConfig localConfig)
        {
            metadataHandlers = new List<MetadataHandler>
            {
                ConfigureTemplate, ConfigureIoTHubConnectionString, ConfigureStorageConnectionString
            };
            foreach (var handler in metadataHandlers) edgeConfig = handler(edgeConfig, localConfig);

            return edgeConfig;
        }

        private static DeviceConfig ConfigureTemplate(DeviceConfig edgeConfig, LocalConfig localConfig)
        {
            edgeConfig.Base64EncodedTemplate ??= localConfig.DefaultTemplate;
            return edgeConfig;
        }

        private static DeviceConfig ConfigureIoTHubConnectionString(DeviceConfig edgeConfig, LocalConfig localConfig)
        {
            edgeConfig.IoTHubConnectionString ??= localConfig.DefaultIoTHubConnectionString;
            return edgeConfig;
        }

        private static DeviceConfig ConfigureStorageConnectionString(DeviceConfig edgeConfig, LocalConfig localConfig)
        {
            edgeConfig.StorageConnectionString ??= localConfig.StorageConnectionString;
            return edgeConfig;
        }

        private delegate DeviceConfig MetadataHandler(DeviceConfig edgeConfig, LocalConfig localConfig);
    }
}
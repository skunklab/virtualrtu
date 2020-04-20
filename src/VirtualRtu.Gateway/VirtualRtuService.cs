﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VirtualRtu.Communications.Logging;
using VirtualRtu.Communications.Tcp;
using VirtualRtu.Configuration;

namespace VirtualRtu.Gateway
{
    public class VirtualRtuService : IHostedService
    {
        private readonly VrtuConfig config;
        private ScadaClientListener listener;
        private readonly ILogger logger;

        public VirtualRtuService(VrtuConfig config, Logger logger = null)
        {
            this.config = config;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //start a TCP listener
            try
            {
                listener = new ScadaClientListener(config, logger);
                await listener.RunAsync();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fault on startup.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                await listener.Shutdown();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Fault shutting down.");
            }
        }
    }
}
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace VirtualRtu.Communications.Logging
{
    public interface ILog : ILogger
    {
        Task LogInformationAsync(string message, params object[] args);

        Task LogTraceAsync(string message, params object[] args);

        Task LogDebugAsync(string message, params object[] args);

        Task LogWarningAsync(string message, params object[] args);

        Task LogErrorAsync(string message, params object[] args);

        Task LogErrorAsync(Exception error, string message, params object[] args);

        Task LogCriticalAsync(string message, params object[] args);
    }
}

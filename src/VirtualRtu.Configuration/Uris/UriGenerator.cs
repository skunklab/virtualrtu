using Microsoft.WindowsAzure.Storage.Blob.Protocol;
using System;

namespace VirtualRtu.Configuration.Uris
{
    public class UriGenerator
    {        
        public static string GetRtuPiSystem(string hostname, string virtualRtuId, string deviceId, byte unitId, bool inbound)
        {
            string direction = inbound ? "in" : "out";
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{unitId.ToString()}/{direction}";
        }
        
        public static string GetDeviceDiagnosticsPiSystem(string hostname, string virtualRtuId, string deviceId)
        {
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/diagnostics";
        }

        public static string GetDeviceTelemetryPiSystem(string hostname, string virtualRtuId, string deviceId)
        {
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/telemetry";
        }

        public static string GetDevicePolicyId(string hostname, string virtualRtuId, string deviceId, bool publish)
        {
            string direction = publish ? "in" : "out";
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{direction}/policy";
        }

        public static string GetVirtualRtuDiagnosticsPiSystem(string hostname, string virtualRtuId)
        {
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/diagnostics";
        }

        public static string GetVirtualRtuTelemetryPiSystem(string hostname, string virtualRtuId)
        {
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/telemetry";
        }

        public static string GetVirtualRtuPolicyId(string hostname, string virtualRtuId, bool publish)
        {
            string direction = publish ? "in" : "out";
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{direction}/policy";
        }

       public static string GetDiagnosticsRequestPolicyId(string hostname, string virtualRtuId)
        {
            return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/diagnotics/policy";
        }



        //public static string Get-RtuPiSystem(ContainerEntity entity, byte unitId, bool request)
        //{
        //    return Get-RtuPiSystem(entity.Hostname, entity.VirtualRtuId, entity.DeviceId, entity.-Id, unitId, request);
        //}

        //public static string Get-PiSystem(string hostname, string virtualRtuId, string deviceId, byte unitId, bool request)
        //{
        //    string direction = request ? "in" : "out";
        //    return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{unitId.ToString()}/{direction}";
        //}

        //public static string Get-RtuPiSystem(string hostname, string virtualRtuId, string deviceId, string moduleId, byte unitId, bool request)
        //{
        //    string direction = request ? "in" : "out";
        //    return $"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{moduleId.ToLowerInvariant()}/{unitId.ToString()}/{direction}";
        //}

        //public static string Get-MonitorPiSystem(string hostname, string virtualRtuId, string deviceId, string moduleId)
        //{
        //    return $"http://{hostname.ToLowerInvariant()}/monitor/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{moduleId.ToLowerInvariant()}";
        //}

        //public static string GetVirtualRtuMonitorPiSystem(string hostname, string virtualRtuId)
        //{
        //    return $"http://{hostname.ToLowerInvariant()}/monitor/{virtualRtuId.ToLowerInvariant()}";
        //}

        //public static string Get-LogPiSystem(string hostname, string virtualRtuId, string deviceId, string moduleId)
        //{
        //    return $"http://{hostname.ToLowerInvariant()}/log/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{moduleId.ToLowerInvariant()}";
        //}

        //public static string GetVirtualRtuLogPiSystem(string hostname, string virtualRtuId)
        //{
        //    return $"http://{hostname.ToLowerInvariant()}/log/{virtualRtuId.ToLowerInvariant()}";
        //}


        //public static string Get-PolicyId(string hostname, string virtualRtuId, string deviceId, string moduleId, bool publish)
        //{
        //    string direction = publish ? "in" : "out";
        //    return $"http://{hostname.ToLowerInvariant()}/policy/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/{moduleId.ToLowerInvariant()}/{direction}";
        //}

        //public static string GetVirtualRtuPolicyId(string hostname, string virtualRtuId, bool publish)
        //{
        //    string direction = publish ? "in" : "out";
        //    return $"http://{hostname.ToLowerInvariant()}/policy/{virtualRtuId.ToLowerInvariant()}/{direction}";
        //}

        public static string GetDevicePublishPolicyId(string hostname, string virtualRtuId, string deviceId)
        {
            return String.Format($"http:/{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/publish");
        }

        public static string GetDeviceSubscribePolicyId(string hostname, string virtualRtuId, string deviceId)
        {
            return String.Format($"http:/{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/subscribe");
        }
        public static string GetDevicePublishPiSystem(string hostname, string virtualRtuId, string deviceId)
        {
            return String.Format($"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/publish");
        }

        public static string GetDeviceSubscribePiSystem(string hostname, string virtualRtuId, string deviceId)
        {
            return String.Format($"http://{hostname.ToLowerInvariant()}/{virtualRtuId.ToLowerInvariant()}/{deviceId.ToLowerInvariant()}/subscribe");
        }
    }
}

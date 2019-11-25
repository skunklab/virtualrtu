using Capl.Authorization;
using Capl.Authorization.Matching;
using Capl.Authorization.Operations;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using VirtualRtu.Configuration.Uris;

namespace VirtualRtu.Configuration.Deployment
{
    public class PiraeusApi
    {
        public PiraeusApi()
        {
        }

        public static AuthorizationPolicy CreateDeviceCaplPolicy(string hostname, string virtualRtuId, string deviceId, bool publish)
        {
            Uri policyId = new Uri(UriGenerator.GetDevicePolicyId(hostname, virtualRtuId, deviceId, publish));

            Match nameMatch = new Match(LiteralMatchExpression.MatchUri, $"http://{hostname.ToLowerInvariant()}/name");
            EvaluationOperation nameOperation = new EvaluationOperation() { Type = EqualOperation.OperationUri, ClaimValue = deviceId.ToLowerInvariant() };
            Rule nameRule = new Rule(nameMatch, nameOperation, true);

            //Match roleMatch = new Match(LiteralMatchExpression.MatchUri, $"http://{hostname.ToLowerInvariant()}/role");
            //EvaluationOperation roleOperation = new EvaluationOperation() { Type = EqualOperation.OperationUri, ClaimValue = moduleId.ToLowerInvariant() };
            //Rule roleRule = new Rule(roleMatch, roleOperation, true);

            //LogicalAndCollection logicalAnd = new LogicalAndCollection();
            //logicalAnd.Add(nameRule);
            //logicalAnd.Add(roleRule);

            //return new AuthorizationPolicy(logicalAnd, policyId);
            return new AuthorizationPolicy(nameRule, policyId);
        }

        public static AuthorizationPolicy CreateVirtualRtuCaplPolicy(string hostname, string virtualRtuId, bool publish)
        {
            Uri policyId = new Uri(UriGenerator.GetVirtualRtuPolicyId(hostname, virtualRtuId, publish));
            Match match = new Match(LiteralMatchExpression.MatchUri, $"http://{hostname.ToLowerInvariant()}/name");
            EvaluationOperation operation = new EvaluationOperation() { Type = EqualOperation.OperationUri, ClaimValue = virtualRtuId.ToLowerInvariant() };
            Rule rule = new Rule(match, operation);
            return new AuthorizationPolicy(rule, policyId);
        }

        public static EventMetadata CreateRtuPiSystem(string description, string hostname, string virtualRtuId, string deviceId, byte unitId, AuthorizationPolicy publishPolicy, AuthorizationPolicy subscribePolicy, bool request)
        {
            string resourceUriString = UriGenerator.GetRtuPiSystem(hostname, virtualRtuId, deviceId, unitId, request);
            return new EventMetadata()
            {
                Description = description,
                Enabled = true,
                RequireEncryptedChannel = false,
                ResourceUriString = resourceUriString,
                PublishPolicyUriString = publishPolicy.PolicyId.ToString(),
                SubscribePolicyUriString = subscribePolicy.PolicyId.ToString()
            };
        }

        public static void CreateVrtuDiagnosticsPiSystem(string description, string hostname, string virtualRtuId, AuthorizationPolicy publishPolicy, AuthorizationPolicy subscribePolicy, string accessToken)
        {
            string resourceUriString = UriGenerator.GetVirtualRtuDiagnosticsPiSystem(hostname, virtualRtuId);
            EventMetadata metadata = GetEventMetadata(resourceUriString, hostname, accessToken);
            if (metadata == null || string.IsNullOrEmpty(metadata.ResourceUriString))
            {
                //create the pi-system
                metadata = new EventMetadata()
                {
                    Description = description,
                    Enabled = true,
                    RequireEncryptedChannel = false,
                    ResourceUriString = resourceUriString,
                    PublishPolicyUriString = publishPolicy.PolicyId.ToString(),
                    SubscribePolicyUriString = subscribePolicy.PolicyId.ToString()
                };

                AddEventMetadata(metadata, hostname, accessToken);
            }

        }

        public static void CreateVrtuTelemetryPiSystem(string description, string hostname, string virtualRtuId, AuthorizationPolicy publishPolicy, AuthorizationPolicy subscribePolicy, string accessToken)
        {
            string resourceUriString = UriGenerator.GetVirtualRtuTelemetryPiSystem(hostname, virtualRtuId);
            EventMetadata metadata = GetEventMetadata(resourceUriString, hostname, accessToken);
            if (metadata == null)
            {
                //create the pi-system
                metadata = new EventMetadata()
                {
                    Description = description,
                    Enabled = true,
                    RequireEncryptedChannel = false,
                    ResourceUriString = resourceUriString,
                    PublishPolicyUriString = publishPolicy.PolicyId.ToString(),
                    SubscribePolicyUriString = subscribePolicy.PolicyId.ToString()
                };

                AddEventMetadata(metadata, hostname, accessToken);
            }

        }

        public static EventMetadata CreateDeviceDiagnosticsPiSystem(string description, string hostname, string virtualRtuId, string deviceId, AuthorizationPolicy publishPolicy, AuthorizationPolicy subscribePolicy)
        {
            string resourceUriString = UriGenerator.GetDeviceDiagnosticsPiSystem(hostname, virtualRtuId, deviceId);
            return new EventMetadata()
            {
                Description = description,
                Enabled = true,
                RequireEncryptedChannel = false,
                ResourceUriString = resourceUriString,
                PublishPolicyUriString = publishPolicy.PolicyId.ToString(),
                SubscribePolicyUriString = subscribePolicy.PolicyId.ToString()
            };
        }

        public static EventMetadata CreateDeviceTelemetryPiSystem(string description, string hostname, string virtualRtuId, string deviceId, AuthorizationPolicy publishPolicy, AuthorizationPolicy subscribePolicy)
        {
            string resourceUriString = UriGenerator.GetDeviceTelemetryPiSystem(hostname, virtualRtuId, deviceId);
            return new EventMetadata()
            {
                Description = description,
                Enabled = true,
                RequireEncryptedChannel = false,
                ResourceUriString = resourceUriString,
                PublishPolicyUriString = publishPolicy.PolicyId.ToString(),
                SubscribePolicyUriString = subscribePolicy.PolicyId.ToString()
            };
        }
        public static AuthorizationPolicy CreateDiagnosticsRequestPolicy(string hostname, string virtualRtuId)
        {
            Uri policyId = new Uri(UriGenerator.GetDiagnosticsRequestPolicyId(hostname, virtualRtuId));

            Match match = new Match(LiteralMatchExpression.MatchUri, $"http://{hostname.ToLowerInvariant()}/role");
            EvaluationOperation operation = new EvaluationOperation() { Type = EqualOperation.OperationUri, ClaimValue = "diagnostics" };
            Rule rule = new Rule(match, operation);
            return new AuthorizationPolicy(rule, policyId);
        }

       

        public static string GetAccessToken(string hostname, string apiToken)
        {
            HttpClient client = new HttpClient();

            HttpResponseMessage response = client.GetAsync(String.Format($"https://{hostname}/api/manage?code={apiToken}")).GetAwaiter().GetResult();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                byte[] result = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<string>(Encoding.UTF8.GetString(result));
            }
            else
            {
                return null;
            }

        }

        public static EventMetadata GetEventMetadata(string resourceUriString, string hostname, string accessToken)
        {
            EventMetadata metadata = null;

            string url = $"https://{hostname}/api/resource/GetPISystemMetadata?ResourceUriString={resourceUriString}";
            RestRequestBuilder builder = new RestRequestBuilder("GET", url, RestConstants.ContentType.Json, true, accessToken);
            RestRequest request = new RestRequest(builder);
            try
            {
                metadata = request.Get<EventMetadata>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to get event metadata");
                Console.WriteLine(ex.Message);
            }

            if (metadata == null || string.IsNullOrEmpty(metadata.ResourceUriString))
            {
                return null;
            }
            else
            {
                return metadata;
            }
        }

        public static bool HasCaplPolicy(string policyId, string hostname, string accessToken)
        {
            string url = String.Format($"https://{hostname}/api/accesscontrol/getaccesscontrolpolicy?policyuristring={policyId.ToLowerInvariant()}");
            RestRequestBuilder builder = new RestRequestBuilder("GET", url, RestConstants.ContentType.Xml, true, accessToken);
            RestRequest request = new RestRequest(builder);

            try
            {
                AuthorizationPolicy policy = request.Get<AuthorizationPolicy>();
                return policy != null;
            }
            catch
            {
                return false;
            }
        }

        public static void AddCaplPolicy(AuthorizationPolicy policy, string hostname, string accessToken)
        {
            if (HasCaplPolicy(policy.PolicyId.ToString(), hostname, accessToken))
            {
                return;
            }

            string url = String.Format($"https://{hostname}/api/accesscontrol/upsertaccesscontrolpolicy");
            RestRequestBuilder builder = new RestRequestBuilder("PUT", url, RestConstants.ContentType.Xml, false, accessToken);
            RestRequest request = new RestRequest(builder);

            request.Put<AuthorizationPolicy>(policy);
        }

        public static void AddEventMetadata(EventMetadata metadata, string hostname, string accessToken)
        {
            string url = String.Format($"https://{hostname}/api/resource/UpsertPiSystemMetadata");
            RestRequestBuilder builder = new RestRequestBuilder("PUT", url, RestConstants.ContentType.Json, false, accessToken);
            RestRequest request = new RestRequest(builder);
            request.Put<EventMetadata>(metadata);
        }
    }
}

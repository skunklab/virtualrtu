using System.Net;

namespace VirtualRtu.Configuration.Deployment
{
    internal class RestRequestBuilder
    {
        public RestRequestBuilder(string method, string url, string contentType, bool zeroLength,
            string securityToken = null)
        {
            Method = method;
            ContentType = contentType;
            BaseUrl = url;
            IsZeroContentLength = zeroLength;
            SecurityToken = securityToken;
        }

        public RestRequestBuilder(string method, string url, string contentType, string securityKey)
        {
            Method = method;
            ContentType = contentType;
            BaseUrl = url;
            IsZeroContentLength = true;
            SecurityKey = securityKey;
        }

        public string ContentType { get; internal set; }
        public string Method { get; internal set; }
        public string BaseUrl { get; internal set; }
        public bool IsZeroContentLength { get; internal set; }

        public string SecurityToken { get; internal set; }

        public string SecurityKey { get; internal set; }

        public HttpWebRequest BuildRequest()
        {
            HttpWebRequest request = null;

            if (!string.IsNullOrEmpty(SecurityKey))
            {
                string url = string.Format("{0}?key={1}", BaseUrl, SecurityKey);
                request = (HttpWebRequest) WebRequest.Create(url);
            }
            else
            {
                request = (HttpWebRequest) WebRequest.Create(BaseUrl);
            }

            request.ContentType = ContentType;
            request.Method = Method;

            if (IsZeroContentLength)
            {
                request.ContentLength = 0;
            }

            if (!string.IsNullOrEmpty(SecurityToken))
            {
                request.Headers.Add("Authorization", string.Format("Bearer {0}", SecurityToken));
            }

            return request;
        }
    }
}
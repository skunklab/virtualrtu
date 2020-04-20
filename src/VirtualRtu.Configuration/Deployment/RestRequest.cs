﻿using System;
using System.IO;
using System.Net;

namespace VirtualRtu.Configuration.Deployment
{
    internal class RestRequest : RestRequestBase
    {
        private readonly RestRequestBuilder requestBuilder;

        public RestRequest(RestRequestBuilder builder)
        {
            requestBuilder = builder;
        }

        public override T Get<T>()
        {
            byte[] buffer = null;
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            HttpWebRequest request = requestBuilder.BuildRequest();
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST GET operation return status code {0}",
                    response.StatusCode.ToString()));
            }

            using (Stream stream = response.GetResponseStream())
            {
                buffer = new byte[response.ContentLength];
                stream.Read(buffer, 0, buffer.Length);
            }

            return Serializer.Deserialize<T>(contentType, buffer);
        }

        public override void Post()
        {
            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }

        public override T Post<T>()
        {
            byte[] buffer = null;
            string contentType = requestBuilder.ContentType.ToLowerInvariant();

            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = 0;
            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                buffer = new byte[response.ContentLength];
                responseStream.Read(buffer, 0, buffer.Length);
            }

            return Serializer.Deserialize<T>(contentType, buffer);
        }

        public override U Post<T, U>(T body)
        {
            byte[] buffer = null;
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            byte[] payload = Serializer.Serialize(contentType, body);

            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = payload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(payload, 0, payload.Length);

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }

            using (Stream responseStream = response.GetResponseStream())
            {
                buffer = new byte[response.ContentLength];
                responseStream.Read(buffer, 0, buffer.Length);
            }

            return Serializer.Deserialize<U>(contentType, buffer);
        }

        public override void Post<T>(T body)
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            byte[] payload = Serializer.Serialize(contentType, body);
            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = payload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(payload, 0, payload.Length);

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }

        public override void Delete()
        {
            HttpWebRequest request = requestBuilder.BuildRequest();

            HttpWebResponse response = (HttpWebResponse) request.GetResponse();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new WebException(string.Format("REST POST operation return status code {0}",
                    response.StatusCode.ToString()));
            }
        }

        public override void Put<T>(T body)
        {
            string contentType = requestBuilder.ContentType.ToLowerInvariant();
            byte[] payload = Serializer.Serialize(contentType, body);
            HttpWebRequest request = requestBuilder.BuildRequest();
            request.ContentLength = payload.Length;

            Stream stream = request.GetRequestStream();
            stream.Write(payload, 0, payload.Length);

            try
            {
                HttpWebResponse response = (HttpWebResponse) request.GetResponse();

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException(string.Format("REST PUT operation return status code {0}",
                        response.StatusCode.ToString()));
                }
            }
            catch (WebException we)
            {
                throw we;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
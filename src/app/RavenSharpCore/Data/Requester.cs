#region License

// Copyright (c) 2014 The Sentry Team and individual contributors.
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted
// provided that the following conditions are met:
// 
//     1. Redistributions of source code must retain the above copyright notice, this list of
//        conditions and the following disclaimer.
// 
//     2. Redistributions in binary form must reproduce the above copyright notice, this list of
//        conditions and the following disclaimer in the documentation and/or other materials
//        provided with the distribution.
// 
//     3. Neither the name of the Sentry nor the names of its contributors may be used to
//        endorse or promote products derived from this software without specific prior written
//        permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
// WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
// ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;

using RavenSharpCore.Utilities;

namespace RavenSharpCore.Data
{
    /// <summary>
    /// The class responsible for performing the HTTP request to Sentry.
    /// </summary>
    public partial class Requester
    {
        private readonly RequestData data;
        private readonly JsonPacket packet;
        private readonly RavenClient ravenClient;
        private readonly HttpClient webRequest;


        /// <summary>
        /// Initializes a new instance of the <see cref="Requester"/> class.
        /// </summary>
        /// <param name="packet">The <see cref="JsonPacket"/> to initialize with.</param>
        /// <param name="ravenClient">The <see cref="RavenClient"/> to initialize with.</param>
        internal Requester(JsonPacket packet, RavenClient ravenClient)
        {
            if (packet == null)
                throw new ArgumentNullException("packet");

            if (ravenClient == null)
                throw new ArgumentNullException("ravenClient");

            this.ravenClient = ravenClient;
            this.packet = ravenClient.PreparePacket(packet);
            this.data = new RequestData(this);

            this.webRequest.BaseAddress = ravenClient.CurrentDsn.SentryUri;
            this.webRequest.Timeout = ravenClient.Timeout;
            this.webRequest.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            this.webRequest.DefaultRequestHeaders.Add("X-Sentry-Auth", PacketBuilder.CreateAuthenticationHeader(ravenClient.CurrentDsn));
            this.webRequest.DefaultRequestHeaders.UserAgent.ParseAdd(PacketBuilder.UserAgent);
        }

        /// <summary>
        /// Gets the <see cref="IRavenClient"/>.
        /// </summary> 
        public IRavenClient Client
        {
            get { return this.ravenClient; }
        }

        /// <summary>
        /// 
        /// </summary>
        public RequestData Data
        {
            get { return this.data; }
        }

        /// <summary>
        /// Gets the <see cref="JsonPacket"/> being sent to Sentry.
        /// </summary>
        public JsonPacket Packet
        {
            get { return this.packet; }
        }

        /// <summary>
        /// Gets the <see cref="HttpWebRequest"/> instance being used to perform the HTTP request to Sentry.
        /// </summary>
        public HttpClient WebRequest
        {
            get { return this.webRequest; }
        }

        /// <summary>
        /// Gets or sets the HTTPContent being sent to Sentry
        /// </summary>
        public HttpContent RequestContent
        {
            get; set;
        }

        /// <summary>
        /// Executes the HTTP request to Sentry.
        /// </summary>
        /// <returns>
        /// The <see cref="JsonPacket.EventID" /> of the successfully captured JSON packet, or <c>null</c> if it fails.
        /// </returns>
        public async Task<string> RequestAsync()
        {
            using (var stream = new MemoryStream())
            {
                if (this.ravenClient.Compression)
                {
                    GzipUtil.Write(this.data.Scrubbed, stream);
                }
                else
                {
                    using (var sw = new StreamWriter(stream))
                    {
                        sw.Write(this.data.Scrubbed);
                    }
                }

                var content = new StreamContent(stream);
                this.RequestContent = content;
            }

            var request = await this.WebRequest.PostAsync("", this.RequestContent);
            return await request.Content.ReadAsStringAsync();
        }
    }
}
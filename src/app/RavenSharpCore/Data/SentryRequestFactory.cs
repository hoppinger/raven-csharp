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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Http;

using Newtonsoft.Json;

using RavenSharpCore.Utilities;

namespace RavenSharpCore.Data
{
    /// <summary>
    /// A default implementation of <see cref="ISentryRequestFactory"/>. Override the <see cref="OnCreate"/>
    /// method to adjust the values of the <see cref="SentryRequest"/> before it is sent to Sentry.
    /// </summary>
    public class SentryRequestFactory : ISentryRequestFactory
    {
        private static bool checkedForHttpContextProperty;

        /// <summary>
        /// Gets or sets the CurrentHttpContextProperty
        /// </summary>
        /// <value>
        /// The current httpcontext property
        /// </value>
        internal static dynamic CurrentHttpContextProperty { get; set; }

        [JsonIgnore]
        internal static bool HasCurrentHttpContextProperty
        {
            get { return CurrentHttpContextProperty != null; }
        }

        [JsonIgnore]
        internal static bool HasHttpContext
        {
            get { return HttpContext != null; }
        }

        /// <summary>
        /// Gets or sets the HTTP context.
        /// </summary>
        /// <value>
        /// The HTTP context.
        /// </value>
        internal static HttpContext HttpContext
        {
            get; set;
        }

        public SentryRequestFactory(IHttpContextAccessor accessor)
        {
            HttpContext = accessor.HttpContext;
        }

        /// <summary>
        /// Creates a new instance of <see cref="SentryRequest"/>
        /// for the current packet.
        /// </summary>
        /// <returns>A new instance of <see cref="SentryRequest"/> with information relating to the current HTTP request</returns>
        public ISentryRequest Create()
        {
            if (!HasHttpContext || HttpContext.Request == null)
                return OnCreate(null);

            var request = new SentryRequest
            {
                Url = HttpContext.Request.Path.ToString(),
                Method = HttpContext.Request.Method,
                Environment = new Dictionary<string, string>(), //how do we get server variables in .net core?
                Headers = Convert(HttpContext.Request.Headers),
                Cookies = HttpContext.Request.Cookies.ToDictionary(x => x.Key, x => x.Value),
                Data = BodyConvert(),
                QueryString = HttpContext.Request.QueryString.ToString()
            };

            return OnCreate(request);
        }


        /// <summary>
        /// Called when the <see cref="SentryRequest"/> has been created. Can be overridden to
        /// adjust the values of the <paramref name="request"/> before it is sent to Sentry.
        /// </summary>
        /// <param name="request">The HTTP request information.</param>
        /// <returns>
        /// The <see cref="SentryRequest"/>.
        /// </returns>
        public virtual SentryRequest OnCreate(SentryRequest request)
        {
            return request;
        }


        private static object BodyConvert()
        {
            if (!HasHttpContext)
                return null;

            try
            {
                return HttpRequestBodyConverter.Convert(HttpContext);
            }
            catch (Exception exception)
            {
                SystemUtil.WriteError(exception);
            }

            return null;
        }

        private static IDictionary<string, string> Convert(IEnumerable<KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> collection)
        {
            return collection.ToDictionary(x => x.Key, x => String.Join(" ", x.Value.ToArray()));
        }
    }
}
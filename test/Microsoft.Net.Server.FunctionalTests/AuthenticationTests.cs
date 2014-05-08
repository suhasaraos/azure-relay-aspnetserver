﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
{
    public class AuthenticationTests
    {
        private const string Address = "http://localhost:8080/";

        [Theory]
        [InlineData(AuthenticationType.Kerberos)]
        [InlineData(AuthenticationType.Negotiate)]
        [InlineData(AuthenticationType.Ntlm)]
        [InlineData(AuthenticationType.Digest)]
        [InlineData(AuthenticationType.Basic)]
        [InlineData(AuthenticationType.Kerberos | AuthenticationType.Negotiate | AuthenticationType.Ntlm | AuthenticationType.Digest | AuthenticationType.Basic)]
        public async Task AuthTypes_EnabledButNotChalleneged_PassThrough(AuthenticationType authType)
        {
            using (var server = Utilities.CreateAuthServer(authType))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Dispose();

                var response = await responseTask;
                response.EnsureSuccessStatusCode();
            }
        }

        [Theory]
        [InlineData(AuthenticationType.Kerberos)]
        [InlineData(AuthenticationType.Negotiate)]
        [InlineData(AuthenticationType.Ntlm)]
        // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationType.Basic)]
        public async Task AuthType_Specify401_ChallengesAdded(AuthenticationType authType)
        {
            using (var server = Utilities.CreateAuthServer(authType))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task MultipleAuthTypes_Specify401_ChallengesAdded()
        {
            using (var server = Utilities.CreateAuthServer(
                AuthenticationType.Kerberos
                | AuthenticationType.Negotiate
                | AuthenticationType.Ntlm
                /* | AuthenticationType.Digest TODO: Not implemented */
                | AuthenticationType.Basic))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Kerberos, Negotiate, NTLM, basic", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }
        /* TODO: User
        [Theory]
        [InlineData(AuthenticationType.Kerberos)]
        [InlineData(AuthenticationType.Negotiate)]
        [InlineData(AuthenticationType.Ntlm)]
        // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationType.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationType.Kerberos | AuthenticationType.Negotiate | AuthenticationType.Ntlm | / *AuthenticationType.Digest |* / AuthenticationType.Basic)]
        public async Task AuthTypes_Login_Success(AuthenticationType authType)
        {
            int requestCount = 0;
            using (Utilities.CreateAuthServer(authType, env =>
            {
                requestCount++;
                / * // TODO: Expose user as feature.
                object obj;
                if (env.TryGetValue("server.User", out obj) && obj != null)
                {
                    return Task.FromResult(0);
                }* /
                new DefaultHttpContext((IFeatureCollection)env).Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address, useDefaultCredentials: true);
                response.EnsureSuccessStatusCode();
            }
        }
        */

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool useDefaultCredentials = false)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = useDefaultCredentials;
            using (HttpClient client = new HttpClient(handler))
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
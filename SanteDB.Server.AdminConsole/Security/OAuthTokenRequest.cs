/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2021-8-27
 */
using Newtonsoft.Json;
using SanteDB.Core.Http;
using System;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Server.AdminConsole.Security
{
    /// <summary>
    /// OAuth token response.
    /// </summary>
    [JsonObject, Serializable]
    [ExcludeFromCodeCoverage]
    public class OAuthTokenResponse
    {

        /// <summary>
        /// Gets or sets the error
        /// </summary>
        [JsonProperty("error")]
        public String Error { get; set; }

        /// <summary>
        /// Description of the error
        /// </summary>
        [JsonProperty("error_description")]
        public String ErrorDescription { get; set; }

        /// <summary>
        /// Access token
        /// </summary>
        [JsonProperty("access_token")]
        public String AccessToken { get; set; }

        /// <summary>
        /// Access token
        /// </summary>
        [JsonProperty("id_token")]
        public String IdToken { get; set; }

        /// <summary>
        /// Token type
        /// </summary>
        [JsonProperty("token_type")]
        public String TokenType { get; set; }

        /// <summary>
        /// Expires in
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Refresh token
        /// </summary>
        [JsonProperty("refresh_token")]
        public String RefreshToken { get; set; }

        /// <summary>
        /// Represent the object as a string
        /// </summary>
        public override string ToString()
        {
            return string.Format("[OAuthTokenResponse: Error={0}, ErrorDescription={1}, AccessToken={2}, TokenType={3}, ExpiresIn={4}, RefreshToken={5}]", Error, ErrorDescription, AccessToken, TokenType, ExpiresIn, RefreshToken);
        }
    }

    /// <summary>
    /// OAuth token request.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class OAuthTokenRequest
    {

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="SanteDB.Server.AdminConsole.Security.OAuthTokenServiceCredentials+OAuthTokenRequest"/> class.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <param name="scope">Scope.</param>
        public OAuthTokenRequest(String username, String password, String scope)
        {
            this.Username = username;
            this.Password = password;
            this.Scope = scope;
            this.GrantType = "password";
        }

        /// <summary>
        /// Token request for refresh
        /// </summary>
        public OAuthTokenRequest(TokenClaimsPrincipal current, String scope)
        {
            this.GrantType = "refresh_token";
            this.RefreshToken = current.RefreshToken;
            this.Scope = scope;
        }

        /// <summary>
        /// Gets the type of the grant.
        /// </summary>
        /// <value>The type of the grant.</value>
        [FormElement("grant_type")]
        public String GrantType
        {
            get; set;
        }

        /// <summary>
        /// Gets the refresh token
        /// </summary>
        [FormElement("refresh_token")]
        public String RefreshToken { get; private set; }

        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>The username.</value>
        [FormElement("username")]
        public String Username
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the password.
        /// </summary>
        /// <value>The password.</value>
        [FormElement("password")]
        public String Password
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the scope.
        /// </summary>
        /// <value>The scope.</value>
        [FormElement("scope")]
        public String Scope
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the client id
        /// </summary>
        [FormElement("client_id")]
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret
        /// </summary>
        [FormElement("client_secret")]
        public string ClientSecret { get; set; }
    }
}
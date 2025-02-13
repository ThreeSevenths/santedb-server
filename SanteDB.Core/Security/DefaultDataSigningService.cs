﻿/*
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
using SanteDB.Core;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Server.Core.Security
{
    /// <summary>
    /// Default data signature service
    /// </summary>
    /// <remarks>
    /// <para>This digital signature service uses the keys configured in the <see cref="SecurityConfigurationSection"/>
    /// to sign data based on the type of signature algorithm in the <see cref="SecurityConfigurationSection"/>. Supported signature 
    /// algorithms are:</para>
    /// <list type="bullet">
    ///     <item>HMAC256 (HMAC + SHA256) using shared secrets</item>
    ///     <item>RS256 (RSA+SHA256) using X.509 certificates (generation of a signature requires private key)</item>
    ///     <item>RS512 (RSA+SHA512)</item>
    /// </list>
    /// </remarks>
    public class DefaultDataSigningService : IDataSigningService
    {

        
        /// <summary>
        /// Gets the name of the service
        /// </summary>
        public string ServiceName => "Digital Signature Service";

        // Master configuration
        private SecurityConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<SecurityConfigurationSection>();

        /// <summary>
        /// Sign the specfiied data
        /// </summary>
        public byte[] SignData(byte[] data, string keyId = null)
        {
            var credentials = SecurityUtils.CreateSigningCredentials(keyId);
            if (credentials == null)
                throw new InvalidOperationException($"Couldn't create signature for key {keyId}");
            using (var signatureProvider = new SignatureProviderFactory().CreateForSigning(credentials.SigningKey, credentials.SignatureAlgorithm))
                return signatureProvider.Sign(data);
        }

        /// <summary>
        /// Verify the signature
        /// </summary>
        public bool Verify(byte[] data, byte[] signature, string keyId = null)
        {
            var credentials = SecurityUtils.CreateSigningCredentials(keyId);
            using (var signatureProvider = new SignatureProviderFactory().CreateForVerifying(credentials.SigningKey, credentials.SignatureAlgorithm))
                return signatureProvider.Verify(data, signature);
        }

        /// <summary>
        /// Get the specified signature algorithm
        /// </summary>
        public string GetSignatureAlgorithm(string keyId = null)
        {
            return SecurityUtils.CreateSigningCredentials(keyId)?.SignatureAlgorithm;
        }

        /// <summary>
        /// Get all available keys
        /// </summary>
        public IEnumerable<string> GetKeys()
        {
            return SecurityUtils.GetKeyIdentifiers();
        }

        /// <summary>
        /// Add signing key to the service
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="keyData"></param>
        /// <param name="signatureAlgorithm"></param>
        public void AddSigningKey(string keyId, byte[] keyData, string signatureAlgorithm)
        {
            SecurityUtils.AddSigningCredentials(keyId, keyData, signatureAlgorithm);
        }
    }
}

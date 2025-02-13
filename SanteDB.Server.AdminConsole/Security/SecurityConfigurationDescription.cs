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
using SanteDB.Core.Http;
using SanteDB.Core.Http.Description;
using SanteDB.Server.AdminConsole.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Server.AdminConsole.Security
{
    /// <summary>
    /// Rest Client Security Description
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SecurityConfigurationDescription : IRestClientSecurityDescription
    {

        // Cert validator
        private ICertificateValidator m_certificateValidator = new ConsoleCertificateValidator();

        /// <summary>
        /// Authentication realm
        /// </summary>
        public string AuthRealm
        {
            get
            {
                return ApplicationContext.Current.RealmId;
            }
        }

        /// <summary>
        /// Certificate validator
        /// </summary>
        public ICertificateValidator CertificateValidator
        {
            get
            {
                return this.m_certificateValidator;
            }
        }

        /// <summary>
        /// Gets the client certificate
        /// </summary>
        public IRestClientCertificateDescription ClientCertificate
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the credential provider
        /// </summary>
        public ICredentialProvider CredentialProvider
        {
            get; set;
        }

        /// <summary>
        /// Security scheme
        /// </summary>
        public SecurityScheme Mode
        {
            get; set;
        }

        /// <summary>
        /// Preemtive authentication?
        /// </summary>
        public bool PreemptiveAuthentication
        {
            get; set;
        }
    }

    /// <summary>
    /// Certificate validator
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class ConsoleCertificateValidator : ICertificateValidator
    {

        private static HashSet<object> m_trustedCerts = new HashSet<object>();

        /// <summary>
        /// Validate certificate
        /// </summary>
        public bool ValidateCertificate(object certificate, object chain)
        {
            if (m_trustedCerts.Contains(certificate.ToString())) return true;
            String response = String.Empty;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                while (response != "y" && response != "n" && response != "s")
                {
                    Console.WriteLine("Certificate {0} presented by server is invalid.", certificate.ToString());
                    Console.Write("Trust this certificate? ([y]es/[n]o/[s]ession):");
                    response = Console.ReadLine();
                }
            }
            finally
            {
                Console.ResetColor();
            }

            if (response == "s")
            {
                m_trustedCerts.Add(certificate.ToString());
                return true;
            }
            else
                return response == "y";
        }
    }
}

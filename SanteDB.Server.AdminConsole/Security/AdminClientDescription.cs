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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SanteDB.Server.AdminConsole.Security
{
    /// <summary>
    /// Administrative client description
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AdminClientDescription : IRestClientDescription
    {
        // Endpoints
        private List<IRestClientEndpointDescription> m_endpoints = new List<IRestClientEndpointDescription>();

        /// <summary>
        /// Represents client description
        /// </summary>
        public AdminClientDescription()
        {
        }

        /// <summary>
        /// Gets or sets the accept
        /// </summary>
        public string Accept { get; set; }

        /// <summary>
        /// Bidning description
        /// </summary>
        public ServiceClientBindingDescription Binding
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the binding description
        /// </summary>
        IRestClientBindingDescription IRestClientDescription.Binding
        {
            get
            {
                return this.Binding;
            }
        }

        /// <summary>
        /// Gets the list of endpoints
        /// </summary>
        public List<IRestClientEndpointDescription> Endpoint
        {
            get
            {
                return this.m_endpoints;
            }
        }

        /// <summary>
        /// Trace?
        /// </summary>
        public bool Trace
        {
            get
            {
                return false;
            }
        }
    }
}
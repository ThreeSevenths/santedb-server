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
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a local security role service
    /// </summary>
    public class LocalSecurityRoleRepositoryService : GenericLocalSecurityRepository<SecurityRole>
    {
        /// <summary>
        /// Local security DI constructor
        /// </summary>
        public LocalSecurityRoleRepositoryService(IPolicyEnforcementService policyService, ILocalizationService localizationService, IPrivacyEnforcementService privacyService = null) : base(policyService, localizationService, privacyService)
        {
        }

        /// <inheritdoc/>
        protected override string WritePolicy => PermissionPolicyIdentifiers.CreateRoles;
        /// <inheritdoc/>
        protected override string DeletePolicy => PermissionPolicyIdentifiers.AlterRoles;
        /// <inheritdoc/>
        protected override string AlterPolicy => PermissionPolicyIdentifiers.AlterRoles;


    }
}

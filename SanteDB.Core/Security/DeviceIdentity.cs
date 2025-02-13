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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using System;


namespace SanteDB.Server.Core.Security
{
    /// <summary>
    /// Represents a device identity.
    /// </summary>
    /// <seealso cref="System.Security.Claims.ClaimsIdentity" />
    public class DeviceIdentity : SanteDBClaimsIdentity, IDeviceIdentity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DeviceIdentity"/> class.
		/// </summary>
		/// <param name="sid">The sid.</param>
		/// <param name="name">The name.</param>
		/// <param name="isAuthenticated">if set to <c>true</c> [is authenticated].</param>
		public DeviceIdentity(Guid sid, string name, bool isAuthenticated) :
            base(name, isAuthenticated, "SYSTEM")
		{
			this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Sid, sid.ToString()));
			this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.SanteDBDeviceIdentifierClaim, sid.ToString()));
			this.AddClaim(new SanteDBClaim(SanteDBClaimTypes.Actor, ActorTypeKeys.DeviceUser.ToString()));

		}


	}
}

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
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;

namespace SanteDB.Persistence.Data.ADO.Security
{
    /// <summary>
    /// Represents an ADO Session
    /// </summary>
    public class AdoSecuritySession : GenericSession, ISession
    {
        
        /// <summary>
        /// Represents the session key
        /// </summary>
        internal Guid Key { get; private set; }

        /// <summary>
        /// For serialization
        /// </summary>
        public AdoSecuritySession()
        {

        }

        /// <summary>
        /// Creates a new ADO Session
        /// </summary>
        internal AdoSecuritySession(Guid key, byte[] id, byte[] refreshToken, DateTimeOffset notBefore, DateTimeOffset notAfter, IClaim[] claims) : base(id, refreshToken, notBefore, notAfter, claims)
        {
            this.Key = key;
        }

        /// <summary>
        /// Create security session
        /// </summary>
        internal AdoSecuritySession(DbSession dbSession) : this(dbSession.Key, null, null, dbSession.NotBefore, dbSession.NotAfter, null)
        {
        }
    }
}

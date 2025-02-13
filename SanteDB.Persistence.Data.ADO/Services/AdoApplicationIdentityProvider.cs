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
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Configuration;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security;
using System.Security.Authentication;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Text;
using SanteDB.Server.Core.Security.Attribute;
using SanteDB.Server.Core.Security;
using SanteDB.Core.Exceptions;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Sql Application IdP
    /// </summary>
#pragma warning disable CS0067
    [ServiceProvider("ADO.NET Application Identity Provider")]
    public class AdoApplicationIdentityProvider : IApplicationIdentityProviderService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "ADO.NET Application Identity Provider";

        // Trace source
        private Tracer m_traceSource = new Tracer(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoPersistenceConfigurationSection m_configuration;

        // Policy enforcements
        private IPolicyEnforcementService m_policyEnforcementService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AdoApplicationIdentityProvider(IConfigurationManager configurationManager, IPolicyEnforcementService pepService)
        {
            this.m_policyEnforcementService = pepService;
            this.m_configuration = configurationManager.GetSection<AdoPersistenceConfigurationSection>();
        }
        /// <summary>
        /// Fired prior to an authentication request being made
        /// </summary>
        public event EventHandler<AuthenticatingEventArgs> Authenticating;

        /// <summary>
        /// Fired after an authentication request has been made
        /// </summary>
        public event EventHandler<AuthenticatedEventArgs> Authenticated;

        /// <summary>
        /// Authenticate the application identity to an application principal
        /// </summary>
        public IPrincipal Authenticate(string applicationId, string applicationSecret)
        {
            // Data context
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {
                    dataContext.Open();
                    IPasswordHashingService hashService = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();

                    var client = dataContext.ExecuteProcedure<DbSecurityApplication>("auth_app", applicationId, hashService.ComputeHash(applicationSecret) , 5);
                    if (client == null)
                        throw new SecurityException("Invalid application credentials");
                    else if (client.Key == Guid.Empty)
                        throw new AuthenticationException(client.PublicId);

                    IPrincipal applicationPrincipal = new ApplicationPrincipal(new Server.Core.Security.ApplicationIdentity(client.Key, client.PublicId, true));
                    this.m_policyEnforcementService.Demand(PermissionPolicyIdentifiers.LoginAsService, applicationPrincipal);
                    return applicationPrincipal;
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(EventLevel.Error,  "Error authenticating {0} : {1}", applicationId, e);
                    throw new AuthenticationException("Error authenticating application", e);
                }
            }


        }

        /// <summary>
        /// Gets the specified identity
        /// </summary>
        public IIdentity GetIdentity(string name)
        {
            // Data context
            using (DataContext dataContext = this.m_configuration.Provider.GetReadonlyConnection())
                try
                {
                    dataContext.Open();
                    var client = dataContext.FirstOrDefault<DbSecurityApplication>(o => o.PublicId == name);
                    if (client == null)
                        return null;
                    else 
                        return new SanteDB.Server.Core.Security.ApplicationIdentity(client.Key, client.PublicId, false);

                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(EventLevel.Error,  "Error getting identity data for {0} : {1}", name, e);
                    throw new Exception($"Error fetching {name}", e);
                }

        }

        /// <summary>
        /// Set the lockout for the specified application
        /// </summary>
        public void SetLockout(string name, bool lockoutState, IPrincipal principal)
        {
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    dataContext.Open();
                    var provId = dataContext.EstablishProvenance(principal, null);

                    var app = dataContext.FirstOrDefault<DbSecurityApplication>(o => o.PublicId == name);
                    if (app == null)
                        throw new KeyNotFoundException($"Application {name} not found");
                    app.Lockout = lockoutState ? (DateTimeOffset?)DateTimeOffset.MaxValue.AddDays(-10) : null;
                    app.LockoutSpecified = true;

                    app.UpdatedByKey = provId;
                    app.UpdatedTime = DateTimeOffset.Now;
                    dataContext.Update(app);
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(EventLevel.Error, "Error getting identity data for {0} : {1}", name, e);
                    throw new DataPersistenceException($"Error setting lockout on {name}", e);
                }
        }

        /// <summary>
        /// Changes the application secret
        /// </summary>
        public void ChangeSecret(string name, string secret, IPrincipal principal)
        {
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    dataContext.Open();
                    var provId = dataContext.EstablishProvenance(principal, null);

                    var app = dataContext.FirstOrDefault<DbSecurityApplication>(o => o.PublicId == name);
                    if (app == null)
                        throw new KeyNotFoundException($"Application {name} not found");

                    var phash = ApplicationServiceContext.Current.GetService<IPasswordHashingService>();
                    if (phash == null)
                        throw new InvalidOperationException("Cannot find password hashing service");

                    app.UpdatedByKey = provId;
                    app.UpdatedTime = DateTimeOffset.Now;
                    app.Secret = phash.ComputeHash(secret);
                    dataContext.Update(app);
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(EventLevel.Error, "Error setting secret for {0} : {1}", name, e);
                    throw new DataPersistenceException($"Error changing secret for {name}", e);
                }
        }

        /// <summary>
        /// Get secure key for the unknown application name
        /// </summary>
        public byte[] GetSecureKey(string name)
        {
            using (DataContext dataContext = this.m_configuration.Provider.GetWriteConnection())
                try
                {
                    dataContext.Open();

                    var dbType = TableMapping.Get(typeof(DbSecurityApplication));
                    var stmt = dataContext.CreateSqlStatement().SelectFrom(dbType.OrmType, dbType.Columns.First(o => o.SourceProperty.Name == nameof(DbSecurityApplication.Secret)))
                        .Where<DbSecurityApplication>(o => o.PublicId == name);

                    var secret = dataContext.FirstOrDefault<String>(stmt.Build());

                    // Secret is the key
                    return secret.ParseHexString();
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceEvent(EventLevel.Error, "Error setting secret for {0} : {1}", name, e);
                    throw new DataPersistenceException($"Error getting secure key for {name}", e);
                }
        }
    }
}
#pragma warning restore CS0067

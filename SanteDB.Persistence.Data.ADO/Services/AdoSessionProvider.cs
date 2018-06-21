﻿using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SanteDB.Core.Security;
using System.Security.Claims;
using System.Security.Principal;
using SanteDB.Persistence.Data.ADO.Configuration;
using System.Diagnostics;
using MARC.HI.EHRS.SVC.Core;
using MARC.HI.EHRS.SVC.Core.Services;
using System.Security;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Configuration;
using System.Security.Cryptography;
using System.IdentityModel.Tokens;
using System.IO;
using System.Data;

namespace SanteDB.Persistence.Data.ADO.Services
{
    /// <summary>
    /// Represents a session provider for ADO based sessions
    /// </summary>
    public class AdoSessionProvider : ISessionProviderService
    {

        // Sync lock
        private Object m_syncLock = new object();

        // Trace source
        private TraceSource m_traceSource = new TraceSource(AdoDataConstants.IdentityTraceSourceName);

        // Configuration
        private AdoConfiguration m_configuration = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection(AdoDataConstants.ConfigurationSectionName) as AdoConfiguration;

        // Master configuration
        private SanteDBConfiguration m_masterConfig = ApplicationContext.Current.GetService<IConfigurationManager>().GetSection("santedb.core") as SanteDBConfiguration;

        /// <summary>
        /// Create signing credentials
        /// </summary>
        private SigningCredentials CreateSigningCredentials()
        {
            SigningCredentials retVal = null;
            // Signing credentials
            if (this.m_masterConfig.Security.SigningCertificate != null)
                retVal = new X509SigningCredentials(this.m_masterConfig.Security.SigningCertificate);
            else if (!String.IsNullOrEmpty(this.m_masterConfig.Security.ServerSigningSecret) ||
                this.m_masterConfig.Security.ServerSigningKey != null)
            {
                var sha = SHA256.Create();
                retVal = new SigningCredentials(
                    new InMemorySymmetricSecurityKey(this.m_masterConfig.Security.ServerSigningKey ?? sha.ComputeHash(Encoding.UTF8.GetBytes(this.m_masterConfig.Security.ServerSigningSecret))),
                    "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256",
                    "http://www.w3.org/2001/04/xmlenc#sha256",
                    new SecurityKeyIdentifier()
                );
            }
            else
                throw new SecurityException("Invalid signing configuration");
            return retVal;
        }

        /// <summary>
        /// Create and register a refresh token for the specified principal
        /// </summary>
        public byte[] CreateRefreshToken()
        {
            // First we shall set the refresh claim
            return Guid.NewGuid().ToByteArray();
        }
        
        /// <summary>
        /// Establish the session
        /// </summary>
        /// <param name="principal">The security principal for which the session is being created</param>
        /// <param name="expiry">The expiration of the session</param>
        /// <param name="aud">The audience of the session</param>
        /// <returns>A constructed <see cref="global::ThisAssembly:AdoSession"/></returns>
        public ISession Establish(ClaimsPrincipal principal, DateTimeOffset expiry, String aud)
        {
            // Validate the parameters
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));
            else if (!principal.Identity.IsAuthenticated)
                throw new InvalidOperationException("Cannot create a session for a non-authenticated principal");

            try
            {
                using (var context = this.m_configuration.Provider.GetWriteConnection())
                {
                    context.Open();
                    var refreshToken = this.CreateRefreshToken();

                    var dbSession = new DbSession()
                    {
                        ApplicationKey = Guid.Parse(principal.FindFirst(SanteDBClaimTypes.SanteDBApplicationIdentifierClaim).Value),
                        UserKey = Guid.Parse(principal.FindFirst(ClaimTypes.Sid).Value),
                        NotBefore = DateTimeOffset.Now,
                        NotAfter = expiry,
                        RefreshExpiration = expiry.AddMinutes(10),
                        Audience = aud,
                        RefreshToken = BitConverter.ToString(refreshToken).Replace("-", "")
                    };

                    if (dbSession.ApplicationKey == dbSession.UserKey) // SID == Application = Application Grant
                        dbSession.UserKey = Guid.Empty;

                    dbSession = context.Insert(dbSession);

                    var credentials = this.CreateSigningCredentials();
                    JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                    var encoder = handler.SignatureProviderFactory.CreateForSigning(credentials.SigningKey, credentials.SignatureAlgorithm);

                    var signedToken = dbSession.Key.ToByteArray().Concat(encoder.Sign(dbSession.Key.ToByteArray())).ToArray();
                    var signedRefresh = refreshToken.Concat(encoder.Sign(refreshToken)).ToArray();
                    return new GenericSession(signedToken, signedRefresh, dbSession.NotBefore, dbSession.NotAfter);
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error establishing session: {0}", e.Message);
                throw;
            }
        }

        /// <summary>
        /// Extend the session 
        /// </summary>
        /// <param name="refreshToken">The signed session token to be refreshed</param>
        /// <returns>The session that was extended</returns>
        public ISession Extend(byte[] refreshToken)
        {
            // Validate the parameters
            if (refreshToken== null)
                throw new ArgumentNullException(nameof(refreshToken));

            IDbTransaction tx = null;

            using (var context = this.m_configuration.Provider.GetWriteConnection())
            {
                try
                {

                    context.Open();

                    tx = context.BeginTransaction();

                    var credentials = this.CreateSigningCredentials();
                    JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                    var verification = handler.SignatureProviderFactory.CreateForVerifying(credentials.SigningKey, credentials.SignatureAlgorithm);

                    if (!verification.Verify(refreshToken.Take(16).ToArray(), refreshToken.Skip(16).ToArray()))
                        throw new SecurityException("Refresh token appears to have been tampered with");

                    // Get the session to be extended
                    var qToken = BitConverter.ToString(refreshToken.Take(16).ToArray()).Replace("-", "");
                    var dbSession = context.SingleOrDefault<DbSession>(o => o.RefreshToken == qToken && o.RefreshExpiration > DateTimeOffset.Now);
                    if (dbSession == null)
                        throw new FileNotFoundException(BitConverter.ToString(refreshToken));

                    // Get rid of the old session
                    context.Delete(qToken);

                    // Generate a new session for this user
                    dbSession.Key = Guid.Empty;
                    refreshToken = this.CreateRefreshToken();
                    dbSession.RefreshToken = BitConverter.ToString(refreshToken).Replace("-", "");
                    dbSession.NotAfter = DateTimeOffset.Now + (dbSession.NotAfter - dbSession.NotBefore); // Extend for original time
                    dbSession.NotBefore = DateTimeOffset.Now;
                    dbSession.RefreshExpiration = dbSession.NotAfter.AddMinutes(10);

                    // Save
                    context.Insert(dbSession);

                    var encoder = handler.SignatureProviderFactory.CreateForSigning(credentials.SigningKey, credentials.SignatureAlgorithm);
                    var signedToken = dbSession.Key.ToByteArray().Concat(encoder.Sign(dbSession.Key.ToByteArray())).ToArray();
                    var signedRefresh = refreshToken.Concat(encoder.Sign(refreshToken)).ToArray();

                    tx.Commit();

                    return new GenericSession(signedToken, signedRefresh, dbSession.NotBefore, dbSession.NotAfter);
                }
                catch (Exception e)
                {
                    tx?.Rollback();
                    this.m_traceSource.TraceError("Error getting session: {0}", e.Message);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the specified session if valid from a signed session token
        /// </summary>
        /// <param name="sessionToken">The session token to retrieve the session for</param>
        /// <returns>The fetched session token</returns>
        public ISession Get(byte[] sessionToken)
        {
            // Validate the parameters
            if (sessionToken == null)
                throw new ArgumentNullException(nameof(sessionToken));

            try
            {
                using (var context = this.m_configuration.Provider.GetReadonlyConnection())
                {
                    context.Open();

                    var credentials = this.CreateSigningCredentials();
                    JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
                    var verification = handler.SignatureProviderFactory.CreateForVerifying(credentials.SigningKey, credentials.SignatureAlgorithm);

                    if (!verification.Verify(sessionToken.Take(16).ToArray(), sessionToken.Skip(16).ToArray()))
                        throw new SecurityException("Session token appears to have been tampered with");

                    var sessionId = new Guid(sessionToken.Take(16).ToArray());
                    var dbSession = context.SingleOrDefault<DbSession>(o => o.Key == sessionId && o.NotAfter > DateTimeOffset.Now);
                    if(dbSession == null)
                        throw new FileNotFoundException(BitConverter.ToString(sessionToken));
                    return new GenericSession(sessionToken, null, dbSession.NotBefore, dbSession.NotAfter);
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error getting session: {0}", e.Message);
                throw;
            }
        }
    }
}

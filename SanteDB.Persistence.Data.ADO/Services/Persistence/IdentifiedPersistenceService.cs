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
using SanteDB.Core.Exceptions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model;
using SanteDB.Persistence.Data.ADO.Exceptions;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Generic persistnece service for identified entities
    /// </summary>
    public abstract class IdentifiedPersistenceService<TModel, TDomain> : IdentifiedPersistenceService<TModel, TDomain, TDomain>
        where TModel : IdentifiedData, new()
        where TDomain : class, IDbIdentified, new()
    {
        public IdentifiedPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }
    }

    /// <summary>
    /// Generic persistence service which can persist between two simple types.
    /// </summary>
    public abstract class IdentifiedPersistenceService<TModel, TDomain, TQueryResult> : CorePersistenceService<TModel, TDomain, TQueryResult>, IReportProgressChanged
        where TModel : IdentifiedData, new()
        where TDomain : class, IDbIdentified, new()
    {
        /// <summary>
        /// Constructor for settings provider
        /// </summary>
        public IdentifiedPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Progress has changed
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        #region implemented abstract members of LocalDataPersistenceService

        /// <summary>
        /// Return true if the specified object exists
        /// </summary>
        public override bool Exists(DataContext context, Guid key)
        {
            return context.Any<TDomain>(o => o.Key == key);
        }

        /// <summary>
        /// Performthe actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel InsertInternal(DataContext context, TModel data)
        {
            try
            {
                var domainObject = this.FromModelInstance(data, context) as TDomain;

                domainObject = context.Insert<TDomain>(domainObject);
                data.Key = domainObject.Key;

                return data;
            }
            catch (DbException ex)
            {
                throw new DataPersistenceException($"Error updating {data}", this.TranslateDbException(ex));
            }
        }

        /// <summary>
        /// Perform the actual update.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel UpdateInternal(DataContext context, TModel data)
        {
            try
            {
                // Sanity
                if (data.Key == Guid.Empty)
                    throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

                // Map and copy
                var newDomainObject = this.FromModelInstance(data, context) as TDomain;
                var oldDomainObject = context.SingleOrDefault<TDomain>(o => o.Key == newDomainObject.Key);
                if (oldDomainObject == null)
                    throw new KeyNotFoundException(data.Key.ToString());

                oldDomainObject.CopyObjectData(newDomainObject);

                // Is this an un-delete?
                if (oldDomainObject is IDbVersionedAssociation dba && dba.ObsoleteVersionSequenceId.HasValue &&
                    newDomainObject is IDbVersionedAssociation dbb && !dbb.ObsoleteVersionSequenceId.HasValue)
                {
                    dba.ObsoleteVersionSequenceId = dbb.ObsoleteVersionSequenceId;
                    dba.ObsoleteVersionSequenceIdSpecified = true;
                }

                context.Update<TDomain>(oldDomainObject);
                return data;
            }
            catch (DbException ex)
            {
                throw new DataPersistenceException($"Error updating {data}", this.TranslateDbException(ex));
            }
        }

        /// <summary>
        /// Performs the actual obsoletion
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        public override TModel ObsoleteInternal(DataContext context, TModel data)
        {
            try
            {
                if (data.Key == Guid.Empty)
                    throw new AdoFormalConstraintException(AdoFormalConstraintType.NonIdentityUpdate);

                var domainObject = context.FirstOrDefault<TDomain>(o => o.Key == data.Key);

                if (domainObject == null)
                    throw new KeyNotFoundException(data.Key.ToString());

                context.Delete(domainObject);

                return data;
            }
            catch (DbException ex)
            {
                this.m_tracer.TraceEvent(EventLevel.Error, "Error obsoleting {0} - {1}", data, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Performs the actual query
        /// </summary>
        public override IEnumerable<TModel> QueryInternal(DataContext context, Expression<Func<TModel, bool>> query, Guid queryId, int offset, int? count, out int totalResults, ModelSort<TModel>[] orderBy, bool countResults = false)
        {
            return this.DoQueryInternal(context, query, queryId, offset, count, out totalResults, orderBy, countResults).ToList().Select(o => o is Guid ? this.Get(context, (Guid)o) : this.CacheConvert(o, context));
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        internal override TModel Get(DataContext context, Guid key)
        {
            var cacheService = new AdoPersistenceCache(context);
            var retVal = cacheService?.GetCacheItem<TModel>(key);
            if (retVal != null)
                return retVal;
            else
                return this.CacheConvert(context.FirstOrDefault<TDomain>(o => o.Key == key), context);
        }

        /// <summary>
        /// Obsolete the specified objects
        /// </summary>
        protected override void BulkObsoleteInternal(DataContext context, Guid[] keysToObsolete)
        {
            context.Delete<TDomain>(o => keysToObsolete.Contains(o.Key));
        }

        /// <summary>
        /// Perform bulk purge on expression
        /// </summary>
        /// <remarks>Since there are so many dependent tables this really calls QueryKeys and then BulkPurge</remarks>
        protected override void BulkPurgeInternal(DataContext connection, Expression<Func<TModel, bool>> expression)
        {
            int totalResults = 1;
            while (totalResults > 0)
            {
                var k = this.QueryKeysInternal(connection, expression, 0, 1000, out totalResults).ToArray();
                this.BulkPurgeInternal(connection, k);
                this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs((float)k.Length / (float)totalResults, $"Purging matching {typeof(TModel).Name} - {totalResults} remain"));
            }
        }

        /// <summary>
        /// Purge the specified object
        /// </summary>
        protected override void BulkPurgeInternal(DataContext connection, Guid[] keysToPurge)
        {
            // TODO: CASCADE DELETE - SCAN THE CONTEXT DOMAIN FOR TABLES WHICH POINT AT TDOMAIN
            // AND CASCADE THE DELETION TO THEM WHERE THE FK THAT POINTS AT MY TABLE
            // IS IN THE KEYS PROVIDED
            var ofs = 0;
            while (ofs < keysToPurge.Length)
            {
                var keys = keysToPurge.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                connection.Delete<TDomain>(o => keys.Contains(o.Key));
            }
            this.PurgeCache(keysToPurge);
        }

        /// <summary>
        /// Purge cache of all keys
        /// </summary>
        protected void PurgeCache(Guid[] keysToPurge)
        {
            var cache = ApplicationServiceContext.Current.GetService<IDataCachingService>();
            foreach(var k in keysToPurge)
            {
                cache.Remove(cache.GetCacheItem(k));
            }
        }

        /// <summary>
        /// Perform the query for bulk keys with an open context
        /// </summary>
        protected override IEnumerable<Guid> QueryKeysInternal(DataContext context, Expression<Func<TModel, bool>> query, int offset, int? count, out int totalResults)
        {
            // Construct the SQL query
            var pk = TableMapping.Get(typeof(TDomain)).Columns.SingleOrDefault(o => o.IsPrimaryKey);
            var domainQuery = this.m_settingsProvider.GetQueryBuilder().CreateQuery(query, pk);

            var results = context.Query<Guid>(domainQuery);

            count = count ?? 100;
            if (this.m_settingsProvider.GetConfiguration().UseFuzzyTotals)
            {
                // Skip and take
                results = results.Skip(offset).Take(count.Value + 1);
                totalResults = offset + results.Count();
            }
            else
            {
                totalResults = results.Count();
                results = results.Skip(offset).Take(count.Value);
            }

            return results.ToList(); // exhaust the results and continue
        }

        /// <summary>
        /// Copy the specified keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            var ofs = 0;
            while (ofs < keysToCopy.Length)
            {
                var keys = keysToCopy.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                toContext.InsertOrUpdate(fromContext.Query<TDomain>(o => keys.Contains(o.Key)));
            }
        }

        #endregion implemented abstract members of LocalDataPersistenceService
    }
}
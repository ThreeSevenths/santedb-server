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
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using SanteDB.Persistence.Data.ADO.Data.Model.Security;
using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Concept persistence service
    /// </summary>
    public class ConceptPersistenceService : VersionedDataPersistenceService<Core.Model.DataTypes.Concept, DbConceptVersion, DbConcept>
    {
        // Status set keys
        private static readonly Guid[] s_statusSets = new Guid[]
        {
            ConceptSetKeys.ActStatus,
            ConceptSetKeys.EntityStatus,
            ConceptSetKeys.ConceptStatus
        };

        public ConceptPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// To morel instance
        /// </summary>
        public override Core.Model.DataTypes.Concept ToModelInstance(object dataInstance, DataContext context)
        {
            var dbConceptVersion = (dataInstance as CompositeResult)?.Values.OfType<DbConceptVersion>().FirstOrDefault() ?? dataInstance as DbConceptVersion;
            var retVal = this.m_settingsProvider.GetMapper().MapDomainInstance<DbConceptVersion, Concept>(dbConceptVersion);

            if (retVal == null) return null;

            if (dbConceptVersion != null && context.LoadState == Core.Model.LoadState.FullLoad)
            {
                var dbConcept = (dataInstance as CompositeResult)?.Values.OfType<DbConcept>().FirstOrDefault() ?? context.FirstOrDefault<DbConcept>(o => o.Key == dbConceptVersion.Key);
                retVal.IsReadonly = dbConcept.IsReadonly;
            }
            //retVal.ConceptSetsXml = de.Concept.ConceptSetMembers.Select(o => o.ConceptSetId).ToList();

            if (context.LoadState == Core.Model.LoadState.FullLoad)
            {
                retVal.LoadAssociations(context);
                retVal.LoadState = Core.Model.LoadState.FullLoad;
            }
            else
            {
                retVal.LoadState = Core.Model.LoadState.PartialLoad;
                retVal.ReferenceTerms = context.Query<DbConceptReferenceTerm>(o => o.SourceKey == retVal.Key && o.ObsoleteVersionSequenceId == null).ToArray().Select(o => new ConceptReferenceTerm(o.TargetKey, o.RelationshipTypeKey)).ToList();
            }

            if (retVal.ConceptNames == null || retVal.ConceptNames.Count == 0)
                retVal.ConceptNames = context.Query<DbConceptName>(o => o.SourceKey == retVal.Key && o.ObsoleteVersionSequenceId == null).ToArray().Select(o => new ConceptName(o.Language, o.Name)).ToList();

            return retVal;
        }

        /// <summary>
        /// Insert concept
        /// </summary>
        public override Core.Model.DataTypes.Concept InsertInternal(DataContext context, Core.Model.DataTypes.Concept data)
        {
            data.StatusConceptKey = data.StatusConceptKey ?? StatusKeys.Active;
            data.ClassKey = data.ClassKey ?? ConceptClassKeys.Other;

            // Ensure exists
            if (data.Class != null) data.Class = data.Class?.EnsureExists(context) as ConceptClass;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            data.ClassKey = data.Class?.Key ?? data.ClassKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;

            // Persist
            var retVal = base.InsertInternal(context, data);

            // Concept sets xml
            if (data.ConceptSetsXml != null)
                foreach (var i in data.ConceptSetsXml)
                {
                    context.Insert(new DbConceptSetConceptAssociation()
                    {
                        ConceptKey = retVal.Key.Value,
                        ConceptSetKey = i
                    }
                    );
                }

            // Concept names
            if (data.ConceptNames != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptName, DbConceptName>(
                   data.ConceptNames,
                    data,
                    context
                );

            if (data.ReferenceTerms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptReferenceTerm, DbConceptReferenceTerm>(
                    data.ReferenceTerms,
                     data,
                     context
                 );

            return retVal;
        }

        /// <summary>
        /// Override update to handle associated items
        /// </summary>
        public override Core.Model.DataTypes.Concept UpdateInternal(DataContext context, Core.Model.DataTypes.Concept data)
        {
            if (data.Class != null) data.Class = data.Class?.EnsureExists(context) as ConceptClass;
            if (data.StatusConcept != null) data.StatusConcept = data.StatusConcept?.EnsureExists(context) as Concept;
            data.ClassKey = data.Class?.Key ?? data.ClassKey;
            data.StatusConceptKey = data.StatusConcept?.Key ?? data.StatusConceptKey;

            var retVal = base.UpdateInternal(context, data);

            var sourceKey = data.Key.Value.ToByteArray();
            if (data.ConceptNames != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptName, DbConceptName>(
                    data.ConceptNames,
                     data,
                     context
                 );

            if (retVal.ReferenceTerms != null)
                base.UpdateVersionedAssociatedItems<Core.Model.DataTypes.ConceptReferenceTerm, DbConceptReferenceTerm>(
                    data.ReferenceTerms,
                     data,
                     context
                 );

            // Concept sets
            if (retVal.ConceptSetsXml != null)
            {
                // Special case m2m
                var existingConceptSets = context.Query<DbConceptSetConceptAssociation>(o => o.ConceptKey == retVal.Key).Select(o => o.ConceptSetKey);

                // Any new?
                var newConcepts = data.ConceptSetsXml.Where(o => !existingConceptSets.Contains(o));
                foreach (var i in newConcepts)
                {
                    //i.EnsureExists(context);
                    context.Insert(new DbConceptSetConceptAssociation() { ConceptKey = retVal.Key.Value, ConceptSetKey = i });
                }

                var delConcepts = existingConceptSets.ToList().Where(o => !data.ConceptSetsXml.Contains(o));
                foreach (var i in delConcepts)
                    context.Delete<DbConceptSetConceptAssociation>(p => p.ConceptKey == retVal.Key.Value && p.ConceptSetKey == i);
            }

            return retVal;
        }

        /// <summary>
        /// Obsolete the object
        /// </summary>
        public override Core.Model.DataTypes.Concept ObsoleteInternal(DataContext context, Core.Model.DataTypes.Concept data)
        {
            data.StatusConceptKey = StatusKeys.Obsolete;
            return base.UpdateInternal(context, data);
        }

        /// <summary>
        /// Perform bulk purge on expression
        /// </summary>
        /// <remarks>Since there are so many dependent tables this really calls QueryKeys and then BulkPurge</remarks>
        protected override void BulkPurgeInternal(DataContext connection, Expression<Func<Concept, bool>> expression)
        {
            int offset = 0, totalResults = 1;
            while (offset < totalResults)
            {
                var k = this.QueryKeysInternal(connection, expression, offset, 1000, out totalResults).ToArray();
                this.BulkPurgeInternal(connection, k);
                offset += k.Length;
            }
        }

        /// <summary>
        /// Purge the context information
        /// </summary>
        protected override void BulkPurgeInternal(DataContext context, Guid[] keysToPurge)
        {
            // Purge the related fields
            int ofs = 0;
            while (ofs < keysToPurge.Length)
            {
                var batchKeys = keysToPurge.Skip(ofs).Take(100).ToArray();
                ofs += 100;
                context.Delete<DbConceptName>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbConceptReferenceTerm>(o => batchKeys.Contains(o.SourceKey));
                context.Delete<DbConceptRelationship>(o => batchKeys.Contains(o.SourceKey));

                context.Delete<DbConceptSetConceptAssociation>(o => batchKeys.Contains(o.ConceptKey) && !s_statusSets.Contains(o.ConceptSetKey));

                // Now delete the versions but keep the mnemonic
                var cvers = context.Query<DbConceptVersion>(o => batchKeys.Contains(o.Key) && o.ObsoletionTime == null).ToArray();
                var versionKeys = context.Query<DbConceptVersion>(o => batchKeys.Contains(o.Key)).Select(o => o.VersionKey).ToArray();
                // Detach keys which are being deleted will need to be removed from the version heirarchy
                foreach (var rpl in context.Query<DbConceptVersion>(o => versionKeys.Contains(o.ReplacesVersionKey.Value)))
                {
                    rpl.ReplacesVersionKey = null;
                    rpl.ReplacesVersionKeySpecified = true;
                    context.Update(rpl);
                }

                context.Delete<DbConceptVersion>(o => batchKeys.Contains(o.Key));
                context.InsertAll(cvers.Select(o => new DbConceptVersion()
                {
                    Key = o.Key,
                    ClassKey = o.ClassKey,
                    CreatedByKey = context.ContextId,
                    Mnemonic = o.Mnemonic,
                    StatusConceptKey = StatusKeys.Purged,
                    CreationTime = DateTimeOffset.Now
                })); // Ensure there is a current version that has been PURGED
            }

            context.ResetSequence("CD_VRSN_SEQ",
                context.Query<DbConceptVersion>(o => true).Max(o => o.VersionSequenceId));

            this.PurgeCache(keysToPurge);

        }

        /// <summary>
        /// Archive the specified keys
        /// </summary>
        public override void Copy(Guid[] keysToCopy, DataContext fromContext, DataContext toContext)
        {
            // Copy all code systems
            toContext.InsertOrUpdateAll(fromContext.Query<DbCodeSystem>(o => o.ObsoletionTime == null));

            // Ensure the "TO" code system version sequence is set to accurate number
            var maxVersionSequence = toContext.Query<DbConceptVersion>(o => true).OrderByDescending(o => o.VersionSequenceId).FirstOrDefault().VersionSequenceId;
            toContext.ResetSequence("CD_VRSN_SEQ", maxVersionSequence);

            // Purge the related fields
            int ofs = 0;
            while (ofs < keysToCopy.Length)
            {
                var batchKeys = keysToCopy.Skip(ofs).Take(100).ToArray();
                ofs += 100;

                // copy core concepts
                toContext.InsertOrUpdateAll(fromContext.Query<DbConcept>(o => batchKeys.Contains(o.Key)));

                // Additional concept sreferenced
                var extraKeys = fromContext.Query<DbConceptVersion>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.StatusConceptKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbConceptReferenceTerm>(o => batchKeys.Contains(o.SourceKey))
                        .Select(o => o.RelationshipTypeKey)
                        .Distinct()
                    ).Union(
                        fromContext.Query<DbConceptRelationship>(o => batchKeys.Contains(o.SourceKey))
                        .Select(o => o.TargetKey)
                        .Distinct()
                    )
                    .ToArray();
                toContext.InsertOrUpdateAll(fromContext.Query<DbConcept>(o => extraKeys.Contains(o.Key)));

                // Users
                extraKeys = fromContext.Query<DbConceptVersion>(o => batchKeys.Contains(o.Key))
                    .Select(o => o.CreatedByKey)
                    .Distinct()
                    .Union(
                        fromContext.Query<DbConceptVersion>(o => batchKeys.Contains(o.Key))
                        .Select(o => o.ObsoletedByKey)
                        .Distinct()
                        .ToArray()
                        .Where(o => o.HasValue)
                        .Select(o => o.Value)
                    )
                    .ToArray();
                toContext.InsertOrUpdateAll(fromContext.Query<DbSecurityUser>(o => extraKeys.Contains(o.Key)));
                var extraSequence = fromContext.Query<DbConceptName>(o => batchKeys.Contains(o.SourceKey)).Select(o => o.EffectiveVersionSequenceId).Distinct().ToArray();
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptVersion>(o => batchKeys.Contains(o.Key)));

                // Insert names
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptName>(o => batchKeys.Contains(o.SourceKey)));

                // Grab reference terms
                extraKeys = fromContext.Query<DbConceptReferenceTerm>(o => batchKeys.Contains(o.SourceKey))
                    .Select(o => o.TargetKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdateAll(fromContext.Query<DbReferenceTerm>(o => extraKeys.Contains(o.Key)));

                // Insert Reference term link
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptReferenceTerm>(o => batchKeys.Contains(o.SourceKey)));

                // Insert relationship
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptRelationshipType>(o => o.ObsoletionTime != null));
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptRelationship>(o => batchKeys.Contains(o.SourceKey)));

                // Insert sets
                extraKeys = fromContext.Query<DbConceptSetConceptAssociation>(o => batchKeys.Contains(o.ConceptKey))
                    .Select(o => o.ConceptSetKey)
                    .Distinct()
                    .ToArray();
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptSet>(o => extraKeys.Contains(o.Key)));
                toContext.InsertOrUpdateAll(fromContext.Query<DbConceptSetConceptAssociation>(o => batchKeys.Contains(o.ConceptKey)));
            }

            toContext.ResetSequence("CD_VRSN_SEQ",
                toContext.Query<DbConceptVersion>(o => true).Max(o => o.VersionSequenceId));
        }
    }

    /// <summary>
    /// Persistence service for concept names
    /// </summary>
    public class ConceptNamePersistenceService : IdentifiedPersistenceService<Core.Model.DataTypes.ConceptName, DbConceptName>, IAdoAssociativePersistenceService
    {
        public ConceptNamePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Get names from source
        /// </summary>
        /// <param name="context"></param>
        /// <param name="id"></param>
        /// <param name="versionSequenceId"></param>
        /// <param name="principal"></param>
        /// <returns></returns>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            int tr = 0;
            return this.QueryInternal(context, this.BuildSourceQuery<ConceptName>(id, versionSequenceId), Guid.Empty, 0, null, out tr, null, false).ToList();
        }

        /// <summary>
        /// Insert the specified data
        /// </summary>
        public override ConceptName InsertInternal(DataContext context, ConceptName data)
        {
            // Remove concept from cache and create a new version
            ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(data.SourceEntityKey.Value);
            if (!data.EffectiveVersionSequenceId.HasValue)
            {
                var conceptVersion = context.FirstOrDefault<DbConceptVersion>(o => o.Key == data.SourceEntityKey.Value && o.ObsoletionTime == null);
                data.EffectiveVersionSequenceId = conceptVersion.VersionSequenceId;
            }
            return base.InsertInternal(context, data);
        }
    }
}
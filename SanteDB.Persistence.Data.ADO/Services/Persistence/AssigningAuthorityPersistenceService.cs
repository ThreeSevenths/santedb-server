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
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.DataType;
using System.Linq;


namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Assigning authority persistence service
    /// </summary>
    public class AssigningAuthorityPersistenceService : BaseDataPersistenceService<Core.Model.DataTypes.AssigningAuthority, DbAssigningAuthority>
    {

        public AssigningAuthorityPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Convert assigning authority to model
        /// </summary>
        public override Core.Model.DataTypes.AssigningAuthority ToModelInstance(object dataInstance, DataContext context)
        {
            var dataAA = dataInstance as DbAssigningAuthority;
            var retVal = base.ToModelInstance(dataInstance, context);
            if(retVal != null)
                retVal.AuthorityScopeXml = context.Query<DbAuthorityScope>(o => o.AssigningAuthorityKey == retVal.Key.Value).Select(o=>o.ScopeConceptKey).ToList();
            
            return retVal;
        }

        /// <summary>
        /// Insert the specified data
        /// </summary>
        public override Core.Model.DataTypes.AssigningAuthority InsertInternal(DataContext context, Core.Model.DataTypes.AssigningAuthority data)
        {
            var retVal = base.InsertInternal(context, data);

            // Scopes?
            if (data.AuthorityScopeXml != null)
            {
                foreach (var scope in data.AuthorityScopeXml)
                    context.Insert<DbAuthorityScope>(new DbAuthorityScope()
                    {
                        AssigningAuthorityKey = retVal.Key.Value,
                        ScopeConceptKey = scope
                    });
            }
            return retVal;
        }

        /// <summary>
        /// Update the specified data
        /// </summary>
        public override Core.Model.DataTypes.AssigningAuthority UpdateInternal(DataContext context, Core.Model.DataTypes.AssigningAuthority data)
        {
            var retVal = base.UpdateInternal(context, data);

            // Scopes?
            if (data.AuthorityScopeXml != null)
            {
                context.Delete<DbAuthorityScope>(o => o.AssigningAuthorityKey == retVal.Key.Value);
                foreach (var scope in data.AuthorityScopeXml)
                    context.Insert<DbAuthorityScope>(new DbAuthorityScope()
                    {
                        AssigningAuthorityKey = retVal.Key.Value,
                        ScopeConceptKey = scope
                    });
            }
            return retVal;
        }

    }
}

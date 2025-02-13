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
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Concepts;
using System;
using System.Collections;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Represents a reference term name persistence service.
    /// </summary>
    /// <seealso cref="DbReferenceTermName" />
    public class ReferenceTermNamePersistenceService : BaseDataPersistenceService<ReferenceTermName, DbReferenceTermName>, IAdoAssociativePersistenceService
    {

        public ReferenceTermNamePersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Get names from source
        /// </summary>
        public IEnumerable GetFromSource(DataContext context, Guid id, decimal? versionSequenceId)
        {
            return this.QueryInternal(context, base.BuildSourceQuery<ReferenceTermName>(id), Guid.Empty, 0, null, out int tr, null, false).ToList();

        }

        /// <summary>
        /// Performs the actual insert.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="data">Data.</param>
        /// <param name="principal">The principal.</param>
        /// <returns>Returns the inserted reference term name.</returns>
        public override ReferenceTermName InsertInternal(DataContext context, ReferenceTermName data)
		{
			// set the key if we don't have one
			if (!data.Key.HasValue || data.Key == Guid.Empty)
				data.Key = Guid.NewGuid();

			// set the creation time if we don't have one
			if (data.CreationTime == default(DateTimeOffset))
				data.CreationTime = DateTimeOffset.Now;

			return base.InsertInternal(context, data);
		}
	}
}

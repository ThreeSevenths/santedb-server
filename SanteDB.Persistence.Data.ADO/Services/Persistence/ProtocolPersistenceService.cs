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
using SanteDB.Core.Model.Acts;
using SanteDB.OrmLite;
using SanteDB.Persistence.Data.ADO.Data.Model.Acts;
using System;
using System.Linq;

namespace SanteDB.Persistence.Data.ADO.Services.Persistence
{
    /// <summary>
    /// Protocol service
    /// </summary>
    public class ProtocolPersistenceService : BaseDataPersistenceService<Protocol, DbProtocol, CompositeResult<DbProtocol, DbProtocolHandler>>
    {

        public ProtocolPersistenceService(IAdoPersistenceSettingsProvider settingsProvider) : base(settingsProvider)
        {
        }

        /// <summary>
        /// Convert to model instance
        /// </summary>
        public override Protocol ToModelInstance(object dataInstance, DataContext context)
        {
            if(dataInstance == null) return null;

            var dbProtoInstance = (dataInstance as CompositeResult<DbProtocol, DbProtocolHandler>)?.Values.OfType<DbProtocol>().FirstOrDefault() ?? dataInstance as DbProtocol;
            var dbHandlerInstance = (dataInstance as CompositeResult<DbProtocol, DbProtocolHandler>)?.Values.OfType<DbProtocolHandler>().FirstOrDefault() ?? context.FirstOrDefault<DbProtocolHandler>(o => o.Key == dbProtoInstance.HandlerKey);

            // Protocol
            return new Protocol()
            {
                Key = dbProtoInstance.Key,
                CreatedByKey = dbProtoInstance.CreatedByKey,
                CreationTime = dbProtoInstance.CreationTime,
                Definition = dbProtoInstance.Definition,
                HandlerClassName = dbHandlerInstance.TypeName,
                Name = dbProtoInstance.Name,
                ObsoletedByKey = dbProtoInstance.ObsoletedByKey,
                ObsoletionTime = dbProtoInstance.ObsoletionTime,
                Oid = dbProtoInstance.Oid
            };
        }

        /// <summary>
        /// Convert from model instance
        /// </summary>
        public override object FromModelInstance(Protocol modelInstance, DataContext context)
        {
            var existingHandler = context.FirstOrDefault<DbProtocolHandler>(o => o.TypeName == modelInstance.HandlerClassName);
            if(existingHandler == null)
            {
                existingHandler = new DbProtocolHandler()
                {
                    Key = Guid.NewGuid(),
                    CreatedByKey = context.ContextId,
                    CreationTime = DateTimeOffset.Now,
                    IsActive = true,
                    Name = modelInstance.HandlerClass.Name,
                    TypeName = modelInstance.HandlerClassName
                };
                context.Insert(existingHandler);
            }

            // DbProtocol
            return new DbProtocol()
            {
                Key = modelInstance.Key ?? Guid.NewGuid(),
                CreatedByKey = context.ContextId,
                CreationTime = modelInstance.CreationTime,
                Name = modelInstance.Name,
                ObsoletedByKey = modelInstance.ObsoletedByKey,
                ObsoletionTime = modelInstance.ObsoletionTime,
                Oid = modelInstance.Oid,
                HandlerKey = existingHandler.Key
            };
        }
    }
}

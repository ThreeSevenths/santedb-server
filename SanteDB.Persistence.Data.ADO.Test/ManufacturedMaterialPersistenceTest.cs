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

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;

namespace SanteDB.Persistence.Data.ADO.Test
{
    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "Persistence")]
    public class ManufacturedMaterialPersistenceTest : PersistenceTest<ManufacturedMaterial>
    {
        /// <summary>
        /// Test the update of a manufactured material
        /// </summary>
        [Test]
        public void TestUpdateManufacturedMaterial()
        {
            ManufacturedMaterial mmat = new ManufacturedMaterial()
            {
                LotNumber = "AAAAA",
                Identifiers = new List<EntityIdentifier>()
                {
                    new EntityIdentifier(new AssigningAuthority() { DomainName = "GTIN", Name = "Global Trade Identifier", Oid = "1.2.3.4.5.6.9098766" }, "20304303")
                },
                Names = new List<EntityName>() { new EntityName(NameUseKeys.Assigned, "ACME OPV Vaccine") },
                DeterminerConceptKey = DeterminerKeys.Specific,
                ExpiryDate = DateTime.Now,
                IsAdministrative = false
            };
            var afterTest = base.DoTestUpdate(mmat, "LotNumber");

            Assert.AreEqual(1, afterTest.Names.Count);
            Assert.AreEqual(DeterminerKeys.Specific, afterTest.DeterminerConceptKey);
            Assert.AreEqual(EntityClassKeys.ManufacturedMaterial, afterTest.ClassConceptKey);
            Assert.IsTrue(afterTest.Names.Exists(o => o.Component.Exists(c => c.Value == "ACME OPV Vaccine")));

            // Update
            
        }
    }
}

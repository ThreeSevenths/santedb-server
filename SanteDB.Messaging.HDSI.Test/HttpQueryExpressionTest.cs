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
using System.Linq;
using NUnit.Framework;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;

namespace SanteDB.Messaging.HDSI.Test
{
    /// <summary>
    /// Tests for the HTTP expression writer
    /// </summary>
    [ExcludeFromCodeCoverage]
    [TestFixture(Category = "REST API")]
    public class HttpQueryExpressionTest
    {

        /// <summary>
        /// Turn an array from the builder into a query string
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static String CreateQueryString(params KeyValuePair<String, Object>[] query)
        {
            String queryString = String.Empty;
            foreach (var kv in query)
            {
                List<String> val = kv.Value is List<Object> ? (kv.Value as List<Object>).OfType<String>().ToList() : new List<String>() { kv.Value.ToString() };
                foreach (var itm in val)
                {
                    queryString += String.Format("{0}={1}", kv.Key, Uri.EscapeDataString(itm));
                    if (!itm.Equals(val.Last()))
                        queryString += "&";
                }
                if (!kv.Equals(query.Last()))
                    queryString += "&";
            }
            return queryString;
        }
        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestWriteLookupByKey()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Key == id);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("id=00000000-0000-0000-0000-000000000000", expression);
        }

        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestWriteGuardByUuid()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Relationships.Where(g=>g.RelationshipTypeKey == EntityRelationshipTypeKeys.Mother).Any(r=>r.TargetEntity.StatusConcept.Mnemonic == "ACTIVE"));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("relationship[29ff64e5-b564-411a-92c7-6818c02a9e48].target.statusConcept.mnemonic=ACTIVE", expression);
        }

        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestChainedParse()
        {
            Guid id = Guid.Empty;
            var qstr = "classConcept.mnemonic=GenderCode&statusConcept.mnemonic=ACTIVE";
            
            var query = QueryExpressionParser.BuildLinqExpression<Place>(NameValueCollection.ParseQueryString(qstr));



        }
        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestChainedWriter()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Place>(o => o.ClassConcept.Mnemonic == "GenderCode" && o.StatusConcept.Mnemonic =="ACTIVE");
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("classConcept.mnemonic=GenderCode&statusConcept.mnemonic=ACTIVE", expression);


        }
        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestChainedWriter2()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Concept>(o => o.ConceptSets.Any(p=>p.Mnemonic == "GenderCode"));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("conceptSet.mnemonic=GenderCode", expression);

        }
        /// <summary>
        /// Test query by key
        /// </summary>
        [Test]
        public void TestWriteLookupAnd()
        {
            Guid id = Guid.Empty;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Key == id && o.GenderConcept.Mnemonic == "Male");
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("id=00000000-0000-0000-0000-000000000000&genderConcept.mnemonic=Male", expression);
        }

        /// <summary>
        /// Test query by or
        /// </summary>
        [Test]
        public void TestWriteLookupOr()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.GenderConcept.Mnemonic == "Male" || o.GenderConcept.Mnemonic == "Female");
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("genderConcept.mnemonic=Male&genderConcept.mnemonic=Female", expression);
        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupGreaterThanEqual()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth >= dt);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("dateOfBirth=%3E%3D0001-01-01T00%3A00%3A00.0000000", expression);

        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupGreaterThan()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth > dt);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("dateOfBirth=%3E0001-01-01T00%3A00%3A00.0000000", expression);

        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupLessThanEqual()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth <= dt);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("dateOfBirth=%3C%3D0001-01-01T00%3A00%3A00.0000000", expression);

        }

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupLessThan()
        {
            DateTime dt = DateTime.MinValue;
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth < dt);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("dateOfBirth=%3C0001-01-01T00%3A00%3A00.0000000", expression);

        }
       

        /// <summary>
        /// Test write of lookup greater than equal to
        /// </summary>
        [Test]
        public void TestWriteLookupNotEqual()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.GenderConcept.Mnemonic != "Male");
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("genderConcept.mnemonic=%21Male", expression);
        }


        /// <summary>
        /// Test write of lookup approximately
        /// </summary>
        [Test]
        public void TestWriteLookupApproximately()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.GenderConcept.Mnemonic.Contains("M"));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("genderConcept.mnemonic=~M", expression);
        }

        /// <summary>
        /// Test write of Any correctly
        /// </summary>
        [Test]
        public void TestWriteLookupAny()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Names.Any(p=>p.NameUse.Mnemonic == "Legal"));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("name.use.mnemonic=Legal", expression);
        }

        /// <summary>
        /// Test write of Any correctly
        /// </summary>
        [Test]
        public void TestWriteLookupAnyAnd()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Names.Any(p => p.NameUse.Mnemonic == "Legal" && p.Component.Any(n=>n.Value == "Smith")));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("name.use.mnemonic=Legal&name.component.value=Smith", expression);
        }

        /// <summary>
        /// Test write of Any correctly
        /// </summary>
        [Test]
        public void TestWriteLookupWhereAnd()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Names.Where(p => p.NameUse.Mnemonic == "Legal").Any(p => p.Component.Any(n => n.Value == "Smith")));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("name[Legal].component.value=Smith", expression);
        }


        /// <summary>
        /// Tests that the [QueryParameter] attribute is adhered to
        /// </summary>
        [Test]
        public void TestWriteNonSerializedProperty()
        {
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Extensions.Any(e => e.ExtensionDisplay == "1"));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("extension.display=1", expression);
        }

        /// <summary>
        /// Test that the query writes out extended filter
        /// </summary>
        [Test]
        public void TestWriteSimpleExtendedFilter()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtension());
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.Identifiers.Any(i => i.Value.TestExpression() <= 2));
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("identifier.value=%3A%28test%29%3C%3D2", expression);
        }

        /// <summary>
        /// Test that an HTTP query has extended filter
        /// </summary>
        [Test]
        public void TestWriteSimpleExtendedFilterWithParms()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            DateTime other = DateTime.Parse("1980-01-01");
            TimeSpan myTime = new TimeSpan(1, 0, 0, 0);
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth.Value.TestExpressionEx(other) < myTime);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("dateOfBirth=%3A%28testEx%7C1980-01-01T00%3A00%3A00.0000000%29%3C1.00%3A00%3A00", expression);
        }

        /// <summary>
        /// Test that an HTTP query has extended filter
        /// </summary>
        [Test]
        public void TestWriteSelfParameterRef()
        {
            QueryFilterExtensions.AddExtendedFilter(new SimpleQueryExtensionEx());
            DateTime other = DateTime.Parse("1980-01-01");
            TimeSpan myTime = new TimeSpan(1, 0, 0, 0);
            var query = QueryExpressionBuilder.BuildQuery<Patient>(o => o.DateOfBirth.Value.TestExpressionEx(o.CreationTime.DateTime) < myTime);
            var expression = CreateQueryString(query.ToArray());
            Assert.AreEqual("dateOfBirth=%3A%28testEx%7C%22%24_.creationTime%22%29%3C1.00%3A00%3A00", expression);
        }

    }
}

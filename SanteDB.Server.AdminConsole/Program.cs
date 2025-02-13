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
using MohawkCollege.Util.Console.Parameters;
using SanteDB.Core.Diagnostics;
using SanteDB.Server.AdminConsole.Parameters;
using SanteDB.Server.AdminConsole.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Reflection;

namespace SanteDB.Server.AdminConsole
{
    [ExcludeFromCodeCoverage]
    class Program
    {
        /// <summary>
        /// Main program entry point
        /// </summary>
        static void Main(string[] args)
        {

            Console.WriteLine("SanteDB Administration & Security Console v{0} ({1})", typeof(Program).Assembly.GetName().Version, typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            Console.WriteLine("Copyright (C) 2015 - 2020, SanteSuite Community Partners (see NOTICES)");

            var pp = new ParameterParser<ConsoleParameters>();
            var options = pp.Parse(args);

            if (options.Help)
                pp.WriteHelp(Console.Out);
            else
                try
                {
                    // Add a console trace output
                    if (!String.IsNullOrEmpty(options.Verbosity))
                        Tracer.AddWriter(new Shell.ConsoleTraceWriter(options.Verbosity, new Dictionary<String, EventLevel>()), (EventLevel)Enum.Parse(typeof(EventLevel), options.Verbosity));
                    else
                        Tracer.AddWriter(new Shell.ConsoleTraceWriter("Error", new Dictionary<String, EventLevel>()), EventLevel.Error);

                    ApplicationContext.Initialize(options);
                    if (ApplicationContext.Current.Start())
                    {
                        new InteractiveShell().Exec();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("FATAL: {0}", e);

                }
                finally
                {
                    Console.ResetColor();
                }
#if DEBUG
            Console.ReadKey();
#endif
        }
    }
}

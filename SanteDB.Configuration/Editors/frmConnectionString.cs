﻿/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-27
 */
using SanteDB.Core.Configuration.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SanteDB.Configuration.Editors
{
    [ExcludeFromCodeCoverage]
    public partial class frmConnectionString : Form
    {
        // Get the name
        private string m_name = $"conn{Guid.NewGuid().ToString().Substring(0, 8)}";

        public frmConnectionString()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Connection string name
        /// </summary>
        public ConnectionString ConnectionString
        {
            get
            {
                var retVal = this.ucConnection.ConnectionString;
                retVal.Name = this.m_name;
                return retVal;
            }
        }

        /// <summary>
        /// Cancel was clicked
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// Ok was clicked
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            if (!this.ucConnection.Validate())
                MessageBox.Show("Invalid Connection Settings");
            else
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void ucConnection_ConfigurationChanged(object sender, EventArgs e)
        {
            this.btnOk.Enabled = ucConnection.IsConfigured;
        }
    }
}

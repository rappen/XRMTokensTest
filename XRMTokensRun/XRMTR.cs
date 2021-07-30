﻿using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Rappen.XTB.Helpers;
using Rappen.XTB.Helpers.ControlItems;
using Rappen.XTB.Helpers.Controls;
using Rappen.XTB.Helpers.Extensions;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XrmToolBox.Extensibility;

namespace XRMTokensRun
{
    public partial class XRMTR : PluginControlBase
    {
        private Settings settings;

        public XRMTR()
        {
            InitializeComponent();

            cmbTokenHelp.Items.Add(" - Data -");
            cmbTokenHelp.Items.Add(new TokenHelp("Column", "{column}", 1, 6, "A simple column, of lookup.column", "https://jonasr.app/xrm-tokens/#attribute"));
            cmbTokenHelp.Items.Add(new TokenHelp("Column Raw", "{column|<value>}", 1, 6, "Like Column, but always return simlest type.", "https://jonasr.app/xrm-tokens/#raw"));
            cmbTokenHelp.Items.Add(new TokenHelp("System", "<system|value|format>", 8, 5, "Gets 'systems' like now related to 'data'.", "https://jonasr.app/xrm-tokens/#system"));
            cmbTokenHelp.Items.Add("");
            cmbTokenHelp.Items.Add(" - Formatting -");
            cmbTokenHelp.Items.Add(new TokenHelp("Left", "<Left|length>", 6, 6, "Can add to Left number of Column/System data.", "https://jonasr.app/xrm-tokens/#formatting"));
            cmbTokenHelp.Items.Add(new TokenHelp("Right", "<Right|length>", 7, 6, "Can add to Right number of Column/System data.", "https://jonasr.app/xrm-tokens/#formatting"));
            cmbTokenHelp.Items.Add(new TokenHelp("SubStr", "<SubStr|start|length>", 8, 12, "Can add to SubString of Column/System data.", "https://jonasr.app/xrm-tokens/#formatting"));
            cmbTokenHelp.Items.Add(new TokenHelp("Pad", "<Pad|R|length| >", 7, 6, "Can add to Pad number of Column/System data.", "https://jonasr.app/xrm-tokens/#formatting"));
            cmbTokenHelp.Items.Add(new TokenHelp("Replace", "<Replace|old|new>", 9, 7, "Can replace old info to new info of Column/System data.", "https://jonasr.app/xrm-tokens/#formatting"));
            cmbTokenHelp.Items.Add(new TokenHelp("Math", "<Math|operator|value>", 6, 14, "Can add to make calculate of Column/System data.", "https://jonasr.app/xrm-tokens/#formatting"));
            cmbTokenHelp.Items.Add("");
            cmbTokenHelp.Items.Add(" - Advance -");
            cmbTokenHelp.Items.Add(new TokenHelp("IIF", "<iif|value1|operator|value2|then|else>", 5, 6, "If-else to decide how info shall be returned.", "https://jonasr.app/xrm-tokens/#ifelse"));
            cmbTokenHelp.Items.Add(new TokenHelp("Expand", "<expand|entity|attribute|{attribute}|orderby|, |true|true|max>", 8, 6, "Return all children from the Column", "https://jonasr.app/xrm-tokens/#expand"));
        }

        private void XRMTR_Load(object sender, EventArgs e)
        {
            LoadSetting();
            Enable(true);
        }

        public override void ClosingPlugin(PluginCloseInfo info)
        {
            SaveSettings();
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);
            LoadSetting();
            tableselect.DataSource = newService?.LoadEntities()?.EntityMetadata;
            record.Service = newService;
            if (settings.Table != null)
            {
                tableselect.SelectedItem = tableselect.Items.OfType<EntityMetadataItem>().FirstOrDefault(t => t.Metadata.LogicalName == settings.Table);
            }
        }

        private void LoadSetting()
        {
            if (settings == null && !SettingsManager.Instance.TryLoad(GetType(), out settings))
            {
                settings = new Settings();
            }
            chkAuto.Checked = settings.ExeAuto;
        }

        private void SaveSettings()
        {
            if (settings == null)
            {
                settings = new Settings();
            }
            settings.ExeAuto = chkAuto.Checked;
            settings.Table = tableselect.SelectedEntity.LogicalName;
            if (settings.Token?.FirstOrDefault(t => t.key == tableselect.SelectedEntity.LogicalName) is KeyValuePair token)
            {
                token.value = txtTokensIn.Text;
            }
            else
            {
                settings.Token.Add(new KeyValuePair { key = tableselect.SelectedEntity.LogicalName, value = txtTokensIn.Text });
            }
            SettingsManager.Instance.Save(GetType(), settings);
        }

        private void tableselect_SelectedIndexChanged(object sender, EventArgs e)
        {
            var entity = tableselect.SelectedEntity;
            record.Record = null;
            ShowColumns();
            if (entity != null && settings != null)
            {
                var token = settings.Token?.FirstOrDefault(t => t.key == entity.LogicalName)?.value;
                txtTokensIn.Text = token;
            }
            Enable(true);
        }

        private void btnGetRecurd_Click(object sender, EventArgs e)
        {
            var look = new XRMLookupDialog
            {
                Service = Service,
                LogicalName = tableselect.SelectedEntity.LogicalName
            };
            if (look.ShowDialog() == DialogResult.OK)
            {
                record.Record = look.Record;
            }
            ShowColumns();
            if (chkAuto.Checked)
            {
                Execute();
            }
            Enable(true);
        }

        private void Execute()
        {
            timer1.Stop();
            if (record.Record == null)
            {
                return;
            }
            SaveSettings();
            try
            {
                lblError.Text = "";
                txtTokensOut.Text = "";
                txtTokensOut.Text = record.Record.Substitute(Service, txtTokensIn.Text);
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }

        private void Enable(bool on)
        {
            tableselect.Enabled = on && Service != null;
            btnGetRecord.Enabled = on && tableselect.SelectedEntity != null;
            gbTokenHelp.Enabled = on && record?.Record != null;
            gbTokens.Enabled = on && record?.Record != null;
            btnAddToken.Enabled = on && record?.Record != null && cmbTokenHelp.SelectedItem is TokenHelp;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://jonasr.app/xrm-tokens/");
        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            Process.Start("https://jonasr.app/xrm-tokens/");
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            var rec = Service.Retrieve(record.Record.LogicalName, record.Record.Id, new ColumnSet(true));
            record.Record = null;
            record.Record = rec;
            ShowColumns();
        }

        private void ShowColumns()
        {
            lstRecord.Items.Clear();
            if (record.Record != null)
            {
                lstRecord.Items.AddRange(record.Record.Attributes.Keys.OrderBy(k => k).Select(k => new ListViewItem(k)).ToArray());
            }
        }

        private void lstRecord_DoubleClick(object sender, EventArgs e)
        {
            if (lstRecord.SelectedItems == null || lstRecord.SelectedItems.Count == 0)
            {
                return;
            }
            txtTokensIn.Text += "{" + lstRecord.SelectedItems[0].Text + "}";
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            Execute();
        }

        private void chkAuto_CheckedChanged(object sender, EventArgs e)
        {
            Auto();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Execute();
        }

        private void txtTokensIn_TextChanged(object sender, EventArgs e)
        {
            Auto();
        }

        private void Auto()
        {
            timer1.Stop();
            if (chkAuto.Checked)
            {
                timer1.Start();
            }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AddToken("columb");
        }

        private void AddToken(string token)
        {
            var pos = txtTokensIn.SelectionStart;
            txtTokensIn.Paste("{" + token + "}");
            txtTokensIn.SelectionStart = pos + 1;
            txtTokensIn.SelectionLength = token.Length;
            txtTokensIn.Focus();
        }

        private void btnSmart_Click(object sender, EventArgs e)
        {
            if (cmbTokenHelp.SelectedItem is TokenHelp help)
            {
                cmbTokenHelp.SelectedIndex = -1;
                var selstart = txtTokensIn.SelectionStart;
                txtTokensIn.SelectedText = "";
                txtTokensIn.Text = txtTokensIn.Text.Insert(selstart, help.content);
                txtTokensIn.SelectionStart = selstart + help.cursorpos;
                txtTokensIn.SelectionLength = help.sellength;
                txtTokensIn.Focus();
            }
        }

        private void cmbTokenHelp_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTokenHelp.SelectedItem is TokenHelp help)
            {
                lblSmart.Text = help.help;
                linkHelp.Enabled = true;
            }
            else
            {
                lblSmart.Text = "";
                linkHelp.Enabled = false;
            }
            Enable(true);
        }

        private void linkHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (cmbTokenHelp.SelectedItem is TokenHelp help)
            {
                Process.Start(help.url);
            }
        }
    }
}
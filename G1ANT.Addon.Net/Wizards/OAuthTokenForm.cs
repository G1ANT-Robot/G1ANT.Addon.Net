using G1ANT.Addon.Net.Models;
using G1ANT.Addon.Net.Structures;
using G1ANT.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace G1ANT.Addon.Net.Wizards
{
    public partial class OAuthTokenForm : Form
    {
        private const string c_scopeImap = "imap";
        private const string c_scopeSmtp = "smtp";
        private const string c_scopeMsGraphApi = "ms-graph-api";
        private IMainForm mainForm;
        private Structure[] supportedStructures = new Structure[] { new OfficeOAuthStructure(""), new GMailOAuthStructure("") };

        public OAuthTokenForm(IMainForm mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponent();
            InitConnectionTypes();
        }

        private void InitConnectionTypes()
        {
            structuresBox.Items.AddRange(supportedStructures.Select(x => x.Attributes.Name).ToArray());
            structuresBox.SelectedIndex = 0;
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void Ok_Click(object sender, EventArgs e)
        {
            try
            {
                codeTemplate.Text = GetOfficeOAuthTemplate(UpdateStructureFromSelection());
            }
            catch (Exception ex)
            {
                var messageBuilder = new StringBuilder();
                if (ex is AggregateException aggregateException)
                {
                    messageBuilder.AppendLine(aggregateException.Message);
                    foreach (var innerException in aggregateException.InnerExceptions)
                        messageBuilder.AppendLine($" {innerException.Message}");
                }
                else
                {
                    messageBuilder.AppendLine(ex.Message);
                    if (ex.InnerException != null)
                        messageBuilder.AppendLine($" {ex.InnerException.Message}");
                }
                RobotMessageBox.Show(messageBuilder.ToString());
            }
        }

        private void AppendImapOpenExTemplate(StringBuilder template, string authenticationVariableName,
            IOauthWizardModel oauthWizardModel)
        {
            template.AppendLine(
                $"imap.openex host {SpecialChars.Text}{oauthWizardModel.ImapHost}{SpecialChars.Text} port {oauthWizardModel.ImapPort} usessl true ignorecertificateerrors true authentication {authenticationVariableName}");
            template.AppendLine("   - your code here");
            template.AppendLine($"imap.close");
        }

        private void AppendSmtpOpenExTemplate(StringBuilder template, string authenticationVariableName,
            IOauthWizardModel oauthWizardModel)
        {
            template.AppendLine(
                $"smtp.openex host {SpecialChars.Text}{oauthWizardModel.SmtpHost}{SpecialChars.Text} port {oauthWizardModel.SmtpPort} options tls ignorecertificateerrors true authentication {authenticationVariableName}");
            template.AppendLine("   - your code here");
            template.AppendLine($"smtp.close");
        }

        private string GetOfficeOAuthTemplate(Structure structure)
        {
            if (structure is IOauthWizardModel oauthWizardModel)
            {
                oauthWizardModel.RequestTokenInteractive();

                var varName = $"{SpecialChars.Variable}authorization";
                var template = new StringBuilder();
                template.AppendLine($"{varName} = {SpecialChars.IndexBegin}{structure.Attributes.Name}{SpecialChars.IndexEnd}");
                foreach (var index in oauthWizardModel.RequiredIndexes)
                {
                    template.AppendLine(
                        $"{varName}{SpecialChars.IndexBegin}{index}{SpecialChars.IndexEnd} = {SpecialChars.Text}{structure.Get(index)}{SpecialChars.Text}");
                }
                if (oauthWizardModel.IsImapRequested)
                    AppendImapOpenExTemplate(template, varName, oauthWizardModel);
                if (oauthWizardModel.IsSmtpRequested)
                    AppendSmtpOpenExTemplate(template, varName, oauthWizardModel);
                return template.ToString();
            }
            throw new ApplicationException("Bad structure type");
        }

        private void copyToClipboard_Click(object sender, EventArgs e)
        {
            Clipboard.Clear();
            Clipboard.SetText(codeTemplate.Text);
        }

        private void insertToScript_Click(object sender, EventArgs e)
        {
            mainForm.InsertTextIntoCurrentEditor(codeTemplate.Text);
        }

        private void codeTemplate_TextChanged(object sender, EventArgs e)
        {
            copyToClipboard.Enabled = !string.IsNullOrEmpty(codeTemplate.Text);
            insertToScript.Enabled = !string.IsNullOrEmpty(codeTemplate.Text);
        }

        private void structuresBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedStructure = GetSelectedStructure();
            if (selectedStructure == null)
                return;

            connectionDetails.Clear();
            switch (selectedStructure)
            {
                case OfficeOAuthStructure officeStructure:
                    FillFormControls(officeStructure);
                    break;
                case GMailOAuthStructure gmailStructure:
                    FillFormControls(gmailStructure);
                    break;
            }
        }

        private void FillFormControls(OfficeOAuthStructure officeStructure)
        {
            connectionDetails.AddRow(
                CreateTextInfo(
                    officeStructure, 
                    OfficeOAuthStructure.IndexNames.Username));
            connectionDetails.AddRow(
                CreateTextInfo(
                    officeStructure,
                    OfficeOAuthStructure.IndexNames.TenantId));
            connectionDetails.AddRow(
                CreateTextInfo(
                    officeStructure,
                    OfficeOAuthStructure.IndexNames.ClientId));
            connectionDetails.AddRow(
                CreateCheckedListInfo(
                    officeStructure,
                    OfficeOAuthStructure.IndexNames.Scope,
                    new[] { c_scopeImap, c_scopeSmtp, c_scopeMsGraphApi }));
        }

        private void FillFormControls(GMailOAuthStructure gmailStructure)
        {
            connectionDetails.AddRow(
                CreateTextInfo(
                    gmailStructure,
                    GMailOAuthStructure.IndexNames.Username));
            connectionDetails.AddRow(
                CreateTextInfo(
                    gmailStructure,
                    GMailOAuthStructure.IndexNames.ClientId));
            connectionDetails.AddRow(
                CreateTextInfo(
                    gmailStructure,
                    GMailOAuthStructure.IndexNames.ClientSecret));
        }

        private FormControlInfo CreateTextInfo(Structure structure, string index)
        {
            var value = structure.Get(index)?.ToString();
            return new FormControlInfo()
            {
                Name = index,
                Label = index,
                Control = new TextBox()
                {
                    Text = value 
                }

            };
        }

        private FormControlInfo CreateCheckedListInfo(Structure structure, string index, string[] options)
        {
            var values = structure.Get(index)?.ToString().Split(',');
            var scopeControl = new CheckedListBox();
            foreach (var option in options)
                scopeControl.Items.Add(option, values.Contains(option));
            scopeControl.Height = scopeControl.Items.Count * scopeControl.ItemHeight + scopeControl.Height - scopeControl.ClientSize.Height;
            return new FormControlInfo()
            {
                Name = index,
                Label = index,
                Control = scopeControl
            };
        }

        private string GetControlValue(Control control)
        {
            switch (control)
            {
                case TextBox textBox:
                    return textBox.Text;
                case CheckedListBox checkedList:
                    return string.Join(",", checkedList.CheckedItems.Cast<string>());
            }
            throw new ArgumentException();
        }

        private Structure GetSelectedStructure()
        {
            return supportedStructures.FirstOrDefault(x => x.Attributes.Name == structuresBox.SelectedItem.ToString());
        }

        private Structure UpdateStructureFromSelection()
        {
            var selectedStructure = GetSelectedStructure();
            if (selectedStructure == null)
                return null;
            
            foreach (var control in connectionDetails.Items)
            {
                try
                {
                    selectedStructure.Set(new TextStructure(GetControlValue(control.Control)), control.Name);
                }
                catch
                { }
            }
            return selectedStructure;
        }

        private void structuresBox_Enter(object sender, EventArgs e)
        {
            UpdateStructureFromSelection();
        }


    }
}

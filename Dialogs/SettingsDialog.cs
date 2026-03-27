using Material_Editor.Theming;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Material_Editor.Dialogs
{
    internal sealed class SettingsDialog : ThemeAwareForm
    {
        private static readonly Font DefaultAppFont = new("Segoe UI", 9f);

        private readonly ComboBox themeComboBox;
        private readonly TextBox fontPreviewTextBox;
        private readonly ComboBox bulkRemoveBehaviorComboBox;
        private readonly CheckBox showSplashAnimationCheckBox;
        private readonly CheckBox createBackupsByDefaultCheckBox;
        private readonly CheckBox retainOriginalBackupCheckBox;
        private readonly NumericUpDown maxBackupsPerFileNumericUpDown;
        private readonly NumericUpDown maxBackupFolderMegabytesNumericUpDown;
        private readonly Panel contentPanel;
        private readonly TableLayoutPanel contentLayout;
        private readonly Button designThemeButton;

        private Font selectedFont;

        public SettingsDialog(Config config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Text = "Settings";
            AutoScaleMode = AutoScaleMode.Font;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(748, 484);
            MinimumSize = new Size(680, 444);
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;

            selectedFont = config.Font ?? DefaultAppFont;
            SelectedThemeId = string.IsNullOrWhiteSpace(config.ThemeId)
                ? AppearanceService.CurrentAppearance.Theme.Id
                : ThemeService.NormalizeThemeId(config.ThemeId);
            SelectedShowSplashAnimation = config.ShowSplashAnimation;
            SelectedBulkDirtyRemoveBehavior = config.BulkDirtyRemoveBehavior;
            SelectedCreateBackupsByDefault = config.CreateBackupsByDefault;
            SelectedRetainOriginalBackup = config.RetainOriginalBackup;
            SelectedMaxBackupsPerFile = Math.Max(0, config.MaxBackupsPerFile);
            SelectedMaxBackupFolderMegabytes = Math.Max(0L, config.MaxBackupFolderMegabytes);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(mainLayout);

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0, 0, 4, 0),
                Margin = new Padding(0)
            };
            mainLayout.Controls.Add(contentPanel, 0, 0);
            contentPanel.Resize += ContentPanel_Resize;

            contentLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Location = Point.Empty,
                Margin = new Padding(0),
                Padding = new Padding(0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            contentPanel.Controls.Add(contentLayout);

            var appearanceGroup = new GroupBox
            {
                Text = "Appearance",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 12)
            };
            contentLayout.Controls.Add(appearanceGroup, 0, 0);

            var appearanceLayout = CreateInnerLayout(new Padding(10, 6, 10, 8));
            appearanceLayout.RowCount = 2;
            appearanceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            appearanceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            appearanceGroup.Controls.Add(appearanceLayout);

            appearanceLayout.Controls.Add(CreateLabel("Theme:", 4), 0, 0);
            themeComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(0, 0, 0, 6)
            };
            var themeSelectionLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            themeSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            themeSelectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            themeSelectionLayout.Controls.Add(themeComboBox, 0, 0);

            designThemeButton = CreateButton("Design...");
            designThemeButton.Click += DesignThemeButton_Click;
            themeSelectionLayout.Controls.Add(designThemeButton, 1, 0);
            appearanceLayout.Controls.Add(themeSelectionLayout, 1, 0);
            PopulateThemeChoices(SelectedThemeId);

            appearanceLayout.Controls.Add(CreateLabel("Font:", 4), 0, 1);
            var fontLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 0)
            };
            fontLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            fontLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fontLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fontLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            appearanceLayout.Controls.Add(fontLayout, 1, 1);

            fontPreviewTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Margin = new Padding(0, 0, 8, 0)
            };
            fontLayout.Controls.Add(fontPreviewTextBox, 0, 0);

            var chooseFontButton = CreateButton("Choose...");
            chooseFontButton.Click += ChooseFontButton_Click;
            fontLayout.Controls.Add(chooseFontButton, 1, 0);

            var resetFontButton = CreateButton("Reset");
            resetFontButton.Click += ResetFontButton_Click;
            fontLayout.Controls.Add(resetFontButton, 2, 0);

            var optionsGroup = new GroupBox
            {
                Text = "Options",
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0)
            };
            contentLayout.Controls.Add(optionsGroup, 0, 1);

            var optionsLayout = CreateInnerLayout(new Padding(12, 10, 12, 17));
            optionsLayout.RowCount = 6;
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            optionsGroup.Controls.Add(optionsLayout);

            optionsLayout.Controls.Add(CreateLabel("Unsaved changes:"), 0, 0);
            bulkRemoveBehaviorComboBox = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            bulkRemoveBehaviorComboBox.Items.Add(BulkDirtyRemoveBehavior.Ask);
            bulkRemoveBehaviorComboBox.Items.Add(BulkDirtyRemoveBehavior.Save);
            bulkRemoveBehaviorComboBox.Items.Add(BulkDirtyRemoveBehavior.Discard);
            bulkRemoveBehaviorComboBox.SelectedItem = SelectedBulkDirtyRemoveBehavior;
            optionsLayout.Controls.Add(bulkRemoveBehaviorComboBox, 1, 0);

            showSplashAnimationCheckBox = new CheckBox
            {
                Text = "Show splash animation on startup",
                AutoSize = true,
                Checked = SelectedShowSplashAnimation,
                Margin = new Padding(0, 8, 0, 0)
            };
            optionsLayout.Controls.Add(showSplashAnimationCheckBox, 0, 1);
            optionsLayout.SetColumnSpan(showSplashAnimationCheckBox, 2);

            createBackupsByDefaultCheckBox = new CheckBox
            {
                Text = "Create backups by default when overwriting or saving existing files",
                AutoSize = true,
                Checked = SelectedCreateBackupsByDefault,
                Margin = new Padding(0, 8, 0, 0)
            };
            optionsLayout.Controls.Add(createBackupsByDefaultCheckBox, 0, 2);
            optionsLayout.SetColumnSpan(createBackupsByDefaultCheckBox, 2);

            retainOriginalBackupCheckBox = new CheckBox
            {
                Text = "Retain original backup on first save and protect it from cleanup",
                AutoSize = true,
                Checked = SelectedRetainOriginalBackup,
                Margin = new Padding(0, 8, 0, 0)
            };
            optionsLayout.Controls.Add(retainOriginalBackupCheckBox, 0, 3);
            optionsLayout.SetColumnSpan(retainOriginalBackupCheckBox, 2);

            optionsLayout.Controls.Add(CreateLabel("Max backups per file:"), 0, 4);
            maxBackupsPerFileNumericUpDown = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Minimum = 0,
                Maximum = 10000,
                Value = Math.Min(10000, SelectedMaxBackupsPerFile),
                Width = 140
            };
            optionsLayout.Controls.Add(CreateNumericRow(maxBackupsPerFileNumericUpDown, "0 = unlimited"), 1, 4);

            optionsLayout.Controls.Add(CreateLabel("Max backup folder size (MB):"), 0, 5);
            maxBackupFolderMegabytesNumericUpDown = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Minimum = 0,
                Maximum = decimal.MaxValue,
                Value = SelectedMaxBackupFolderMegabytes > decimal.MaxValue
                    ? decimal.MaxValue
                    : SelectedMaxBackupFolderMegabytes,
                Width = 140
            };
            optionsLayout.Controls.Add(CreateNumericRow(maxBackupFolderMegabytesNumericUpDown, "0 = unlimited"), 1, 5);

            var footerLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 12, 0, 0)
            };
            mainLayout.Controls.Add(footerLayout, 0, 1);

            var cancelButton = CreateButton("Cancel", DialogResult.Cancel);
            footerLayout.Controls.Add(cancelButton);

            var okButton = CreateButton("OK", DialogResult.OK);
            okButton.Click += OkButton_Click;
            footerLayout.Controls.Add(okButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            UpdateFontPreview();
            UpdateContentLayoutWidth();
        }

        public string SelectedThemeId { get; private set; }
        public Font SelectedFont => selectedFont;
        public bool SelectedShowSplashAnimation { get; private set; }
        public BulkDirtyRemoveBehavior SelectedBulkDirtyRemoveBehavior { get; private set; }
        public bool SelectedCreateBackupsByDefault { get; private set; }
        public bool SelectedRetainOriginalBackup { get; private set; }
        public int SelectedMaxBackupsPerFile { get; private set; }
        public long SelectedMaxBackupFolderMegabytes { get; private set; }

        internal bool SplashAnimationChecked
        {
            get => showSplashAnimationCheckBox.Checked;
            set => showSplashAnimationCheckBox.Checked = value;
        }

        internal bool CreateBackupsByDefaultChecked
        {
            get => createBackupsByDefaultCheckBox.Checked;
            set => createBackupsByDefaultCheckBox.Checked = value;
        }

        internal bool RetainOriginalBackupChecked
        {
            get => retainOriginalBackupCheckBox.Checked;
            set => retainOriginalBackupCheckBox.Checked = value;
        }

        internal decimal MaxBackupsPerFileValue
        {
            get => maxBackupsPerFileNumericUpDown.Value;
            set => maxBackupsPerFileNumericUpDown.Value = ClampNumericValue(maxBackupsPerFileNumericUpDown, value);
        }

        internal decimal MaxBackupFolderMegabytesValue
        {
            get => maxBackupFolderMegabytesNumericUpDown.Value;
            set => maxBackupFolderMegabytesNumericUpDown.Value = ClampNumericValue(maxBackupFolderMegabytesNumericUpDown, value);
        }

        internal void CommitSelections()
        {
            OkButton_Click(this, EventArgs.Empty);
        }

        protected override void ApplyAppearance(AppearanceDefinition appearance)
        {
            AppearanceApplicator.ApplyToForm(this, appearance);
            AppearanceApplicator.ApplyToContainer(this, appearance, appearance.Theme.Palette.PanelBackground);
            UpdateContentLayoutWidth();
            contentPanel.PerformLayout();
        }

        private void ContentPanel_Resize(object sender, EventArgs e)
        {
            UpdateContentLayoutWidth();
        }

        private static TableLayoutPanel CreateInnerLayout(Padding? padding = null)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                Padding = padding ?? new Padding(12, 10, 12, 12),
                Margin = new Padding(0)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            return layout;
        }

        private static Label CreateLabel(string text, int bottomMargin = 6)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 3, 8, bottomMargin)
            };
        }

        private static Button CreateButton(string text, DialogResult dialogResult = DialogResult.None)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                DialogResult = dialogResult,
                Margin = new Padding(8, 0, 0, 0)
            };
        }

        private static Control CreateNumericRow(Control input, string hint)
        {
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0)
            };
            layout.Controls.Add(input);
            layout.Controls.Add(new Label
            {
                Text = hint,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(8, 6, 0, 0)
            });
            return layout;
        }

        private static decimal ClampNumericValue(NumericUpDown input, decimal value)
        {
            return Math.Max(input.Minimum, Math.Min(input.Maximum, value));
        }

        private void ChooseFontButton_Click(object sender, EventArgs e)
        {
            using var fontDialog = new FontDialog
            {
                AllowScriptChange = false,
                AllowVectorFonts = false,
                AllowVerticalFonts = false,
                FontMustExist = true,
                ShowColor = false,
                ShowEffects = false,
                MaxSize = 14,
                Font = selectedFont ?? DefaultAppFont
            };

            if (fontDialog.ShowDialog(this) != DialogResult.OK)
                return;

            selectedFont = fontDialog.Font;
            UpdateFontPreview();
        }

        private void ResetFontButton_Click(object sender, EventArgs e)
        {
            selectedFont = new Font(DefaultAppFont, FontStyle.Regular);
            UpdateFontPreview();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            if (themeComboBox.SelectedItem is ThemeChoice choice)
                SelectedThemeId = choice.Theme.Id;

            SelectedShowSplashAnimation = showSplashAnimationCheckBox.Checked;
            SelectedCreateBackupsByDefault = createBackupsByDefaultCheckBox.Checked;
            SelectedRetainOriginalBackup = retainOriginalBackupCheckBox.Checked;
            SelectedMaxBackupsPerFile = decimal.ToInt32(maxBackupsPerFileNumericUpDown.Value);
            SelectedMaxBackupFolderMegabytes = decimal.ToInt64(maxBackupFolderMegabytesNumericUpDown.Value);

            if (bulkRemoveBehaviorComboBox.SelectedItem is BulkDirtyRemoveBehavior behavior)
                SelectedBulkDirtyRemoveBehavior = behavior;
        }

        private void DesignThemeButton_Click(object sender, EventArgs e)
        {
            ThemeDefinition selectedTheme = (themeComboBox.SelectedItem as ThemeChoice)?.Theme
                ?? ThemeService.AvailableThemes.FirstOrDefault(theme => string.Equals(theme.Id, SelectedThemeId, StringComparison.OrdinalIgnoreCase))
                ?? AppearanceService.CurrentAppearance.Theme;

            using var dialog = new ThemeDesignerDialog(selectedTheme);
            if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SavedThemeId))
                return;

            HandleThemeDesigned(dialog.SavedThemeId);
        }

        internal void HandleThemeDesigned(string themeId)
        {
            PopulateThemeChoices(themeId);
            SelectedThemeId = themeId;
        }

        private void PopulateThemeChoices(string selectedThemeId)
        {
            themeComboBox.BeginUpdate();
            try
            {
                themeComboBox.Items.Clear();
                foreach (ThemeDefinition theme in ThemeService.AvailableThemes)
                    themeComboBox.Items.Add(new ThemeChoice(theme));

                ThemeChoice selectedTheme = themeComboBox.Items.Cast<ThemeChoice>()
                    .FirstOrDefault(choice => string.Equals(choice.Theme.Id, selectedThemeId, StringComparison.OrdinalIgnoreCase));
                if (selectedTheme != null)
                    themeComboBox.SelectedItem = selectedTheme;
                else if (themeComboBox.Items.Count > 0)
                    themeComboBox.SelectedIndex = 0;
            }
            finally
            {
                themeComboBox.EndUpdate();
            }
        }

        private void UpdateFontPreview()
        {
            Font displayFont = selectedFont ?? DefaultAppFont;
            fontPreviewTextBox.Font = displayFont;
            fontPreviewTextBox.Text = $"{displayFont.Name}, {displayFont.SizeInPoints:0.#} pt";
        }

        private void UpdateContentLayoutWidth()
        {
            if (contentPanel.IsDisposed || contentLayout.IsDisposed)
                return;

            int availableWidth = Math.Max(0, contentPanel.ClientSize.Width - contentPanel.Padding.Horizontal - 1);
            if (contentPanel.VerticalScroll.Visible)
                availableWidth = Math.Max(0, availableWidth - SystemInformation.VerticalScrollBarWidth);

            contentLayout.Width = availableWidth;
        }

        private sealed class ThemeChoice
        {
            public ThemeChoice(ThemeDefinition theme)
            {
                Theme = theme;
            }

            public ThemeDefinition Theme { get; }

            public override string ToString()
            {
                return Theme.DisplayName;
            }
        }
    }
}

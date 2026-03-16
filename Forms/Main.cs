using MaterialLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Material_Editor.Controls;
using Material_Editor.Dialogs;
using Material_Editor.Models;
using Material_Editor.Services;

namespace Material_Editor.Forms
{
    internal partial class Main : ThemeAwareForm
    {
        private enum WorkspaceMode
        {
            Empty,
            Single,
            Bulk
        }

        private Config config;
        private string workFilePath;
        private bool changed;
        private bool toolTipPopping;

        private BaseMaterialFile currentMaterial;
        private BaseMaterialFile originalMaterial;
        private readonly Dictionary<CollapsibleGroupBox, string[]> sectionVisibilityMap = [];
        private CollapsibleGroupBox generalPageSection;
        private CollapsibleGroupBox materialPageSection;
        private CollapsibleGroupBox effectPageSection;
        private WorkspaceMode workspaceMode = WorkspaceMode.Empty;
        private BulkMaterialEditSession bulkSession;
        private BulkMaterialEditorView bulkEditorView;
        private bool bulkBackupBeforeWrite = true;

        private ToolStripMenuItem openFolderToolStripMenuItem;
        private ToolStripMenuItem addFilesToolStripMenuItem;
        private ToolStripMenuItem addFolderToolStripMenuItem;
        private ToolStripMenuItem saveSelectedToolStripMenuItem;
        private ToolStripMenuItem removeSelectedFilesToolStripMenuItem;
        private const int DefaultVersionFO4 = 2;
        private const int DefaultVersionFO76 = 21;
        private const string ApplicationTitle = "B.G.E.M.";
        private const int DefaultEditorWidth = 1280;
        private const int DefaultEditorHeight = 860;
        private const int MinimumEditorWidth = 1024;
        private const int MinimumEditorHeight = 640;
        private static readonly Font DefaultAppFont = new("Segoe UI", 9f);

        private sealed class SectionDefinition
        {
            public SectionDefinition(string title, int pairsPerRow, bool collapsible, bool collapsedByDefault, params string[] controls)
            {
                Title = title;
                PairsPerRow = pairsPerRow;
                Collapsible = collapsible;
                CollapsedByDefault = collapsedByDefault;
                Controls = controls;
            }

            public string Title { get; }
            public int PairsPerRow { get; }
            public bool Collapsible { get; }
            public bool CollapsedByDefault { get; }
            public string[] Controls { get; }
        }

        private string WorkFileName
        {
            get
            {
                if (workFilePath != null)
                {
                    int nameIndex = workFilePath.LastIndexOf('\\');
                    if (nameIndex != -1)
                        return workFilePath.Substring(nameIndex + 1, workFilePath.Length - nameIndex - 1);
                    else
                        return workFilePath;
                }

                return null;
            }
        }

        private MaterialType CurrentMaterialType
        {
            get { return rbTypeEffect.Checked ? MaterialType.Effect : MaterialType.Material; }
        }

        private Game CurrentGame
        {
            get { return rbGameFO76.Checked ? Game.FO76 : Game.FO4; }
        }

        public Main() : this(LoadConfig())
        {
        }

        public Main(Config initialConfig)
        {
            AppearanceService.EnsureInitialized();
            config = initialConfig;
            config.ThemeId = string.IsNullOrWhiteSpace(config.ThemeId)
                ? AppearanceService.CurrentAppearance.Theme.Id
                : ThemeService.NormalizeThemeId(config.ThemeId);
            AppearanceService.SetCurrentTheme(config.ThemeId);
            config.ThemeId = AppearanceService.CurrentAppearance.Theme.Id;
            config.Font = AppearanceService.CurrentAppearance.Font;
            InitializeComponent();
            MinimumSize = new Size(MinimumEditorWidth, MinimumEditorHeight);
            Size = new Size(MinimumEditorWidth, MinimumEditorHeight);
            InitializeBulkShell();
            ControlFactory.VisibilityChangedCallback = UpdateSectionVisibility;
            InitializePageSections();
            ApplyCurrentAppearance();
            UpdateWorkspaceCommandState();
        }

        private bool IsBulkMode => workspaceMode == WorkspaceMode.Bulk;
        private bool IsSingleMode => workspaceMode == WorkspaceMode.Single;

        private void InitializeBulkShell()
        {
            openFileDialog.Multiselect = true;

            bulkEditorView = new BulkMaterialEditorView
            {
                Visible = false
            };
            bulkEditorView.StateChanged += (s, e) =>
            {
                bulkBackupBeforeWrite = bulkEditorView.BackupBeforeWrite;
                UpdateWorkspaceCommandState();
                UpdateWindowTitle();
            };
            contentScrollPanel.Controls.Add(bulkEditorView);
            bulkEditorView.BringToFront();

            openFolderToolStripMenuItem = new ToolStripMenuItem("Open Folder...");
            openFolderToolStripMenuItem.Click += OpenFolderToolStripMenuItem_Click;

            addFilesToolStripMenuItem = new ToolStripMenuItem("Add Files...");
            addFilesToolStripMenuItem.Click += AddFilesToolStripMenuItem_Click;

            addFolderToolStripMenuItem = new ToolStripMenuItem("Add Folder...");
            addFolderToolStripMenuItem.Click += AddFolderToolStripMenuItem_Click;

            saveSelectedToolStripMenuItem = new ToolStripMenuItem("Save Selected");
            saveSelectedToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveSelectedToolStripMenuItem.Click += SaveSelectedToolStripMenuItem_Click;

            removeSelectedFilesToolStripMenuItem = new ToolStripMenuItem("Remove Selected Files");
            removeSelectedFilesToolStripMenuItem.ShortcutKeys = Keys.Delete;
            removeSelectedFilesToolStripMenuItem.Click += RemoveSelectedFilesToolStripMenuItem_Click;

            int openIndex = fileToolStripMenuItem.DropDownItems.IndexOf(openToolStripMenuItem);
            if (openIndex >= 0)
                fileToolStripMenuItem.DropDownItems.Insert(openIndex + 1, openFolderToolStripMenuItem);

            int saveIndex = fileToolStripMenuItem.DropDownItems.IndexOf(saveToolStripMenuItem);
            if (saveIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 1, saveSelectedToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 2, addFilesToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 3, addFolderToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 4, removeSelectedFilesToolStripMenuItem);
            }

            toolsToolStripMenuItem.DropDownItems.Remove(bulkMaterialEditorToolStripMenuItem);
        }

        private void SetGameSelection(Game game)
        {
            switch (game)
            {
                case Game.FO76:
                    rbGameFO76.Checked = true;
                    break;
                default:
                    rbGameFO4.Checked = true;
                    break;
            }
        }

        private void SetMaterialTypeSelection(MaterialType type)
        {
            switch (type)
            {
                case MaterialType.Effect:
                    rbTypeEffect.Checked = true;
                    break;
                default:
                    rbTypeMaterial.Checked = true;
                    break;
            }
        }

        #region UI
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new AboutDialog();
            about.ShowDialog();
        }

        private void GameToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is not RadioButton radioButton || !radioButton.Checked)
                return;

            var selectedGame = CurrentGame;
            if (config.GameVersion != selectedGame)
            {
                config.GameVersion = selectedGame;

                if (currentMaterial != null)
                {
                    switch (selectedGame)
                    {
                        case Game.FO4:
                            if (currentMaterial.Version > 2)
                                currentMaterial.Version = DefaultVersionFO4;
                            break;
                        case Game.FO76:
                            if (currentMaterial.Version <= 2)
                                currentMaterial.Version = DefaultVersionFO76;
                            break;
                    }
                }

                SuspendAll();
                FillVersionDropdown();
                ControlFactory.UpdateVisibility();
                ResumeAll();

                OnChanged();
            }

            ApplyCurrentAppearance();
        }

        private void MaterialTypeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is not RadioButton radioButton || !radioButton.Checked)
                return;

            UpdateTopLevelSectionVisibility();

            string filePath = ChangeFileExtension(workFilePath);
            if (filePath != workFilePath)
            {
                workFilePath = filePath;
                OnChanged();
            }

            ApplyCurrentAppearance();
        }

        private void ListVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedVersion = listVersion.SelectedItem;
            if (selectedVersion != null && currentMaterial != null)
                currentMaterial.Version = Convert.ToUInt32(selectedVersion);

            SuspendAll();
            ControlFactory.UpdateVisibility();
            ResumeAll();

            OnChanged();
        }

        private void FillVersionDropdown()
        {
            listVersion.Items.Clear();

            int defaultVersion;
            var selectedGame = CurrentGame;
            switch (selectedGame)
            {
                case Game.FO76:
                    listVersion.Items.AddRange([20, 21, 22]);
                    defaultVersion = DefaultVersionFO76;
                    break;
                default:
                    listVersion.Items.AddRange([1, 2]);
                    defaultVersion = DefaultVersionFO4;
                    break;
            }

            uint targetVersion = currentMaterial?.Version ?? (uint)defaultVersion;
            SelectVersionInDropdown(targetVersion);
        }

        private void SelectVersionInDropdown(uint version)
        {
            for (int i = 0; i < listVersion.Items.Count; i++)
            {
                if (TryGetVersionValue(listVersion.Items[i], out var itemVersion) && itemVersion == version)
                {
                    listVersion.SelectedIndex = i;
                    return;
                }
            }

            listVersion.SelectedIndex = AddMissingVersionItem(version);
        }

        private int AddMissingVersionItem(uint version)
        {
            listVersion.Items.Add(version);
            return listVersion.Items.Count - 1;
        }

        private static bool TryGetVersionValue(object value, out uint version)
        {
            version = 0;

            if (value is uint u)
            {
                version = u;
                return true;
            }

            if (value is int i)
            {
                version = (uint)i;
                return true;
            }

            if (value is string s && uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                version = parsed;
                return true;
            }

            if (value is IConvertible convertible)
            {
                try
                {
                    version = convertible.ToUInt32(CultureInfo.InvariantCulture);
                    return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private void ToolTip_Popup(object sender, PopupEventArgs ea)
        {
            if (toolTipPopping)
                return;

            toolTipPopping = true;

            if (ea.AssociatedControl.Tag is CustomControl customControl)
            {
                var baseToolTip = customControl.BaseToolTip;
                if (baseToolTip != null)
                {
                    var newToolTip = baseToolTip;

                    if (ea.AssociatedControl.Tag is ColorControl colorControl)
                    {
                        var knownColorLookup = Enum.GetValues(typeof(KnownColor))
                            .Cast<KnownColor>()
                            .Select(Color.FromKnownColor)
                            .Where(c => !c.IsSystemColor)
                            .ToLookup(c => c.ToArgb());

                        var currentColor = colorControl.CurrentColor;
                        var knownColors = knownColorLookup[currentColor.ToArgb()];
                        if (knownColors.Any())
                        {
                            var colorList = knownColors.Aggregate("", (str, obj) => str + obj.Name + ", ").TrimEnd(' ', ',');
                            newToolTip += $"{Environment.NewLine}Color: {currentColor.R}, {currentColor.G}, {currentColor.B} ({colorList})";
                        }
                        else
                        {
                            newToolTip += $"{Environment.NewLine}Color: {currentColor.R}, {currentColor.G}, {currentColor.B}";
                        }
                    }
                    else if (ea.AssociatedControl.Tag is FileControl fileControl)
                    {
                        newToolTip += $"{Environment.NewLine}File Type: {fileControl.CurrentFileType}";
                    }

                    toolTip.SetToolTip(ea.AssociatedControl, newToolTip);
                }
            }

            toolTipPopping = false;
        }

        private void SuspendAll()
        {
            SuspendLayout();
            layoutGeneral.SuspendLayout();
            layoutMaterial.SuspendLayout();
            layoutEffect.SuspendLayout();
        }

        private void ResumeAll()
        {
            ResumeLayout();
            layoutGeneral.ResumeLayout();
            layoutMaterial.ResumeLayout();
            layoutEffect.ResumeLayout();
        }

        private void OnChanged()
        {
            if (!string.IsNullOrEmpty(workFilePath))
            {
                Text = $"*{GetTitleText()}";
                changed = true;
            }
        }

        private void Main_ResizeBegin(object sender, EventArgs e)
        {
            SuspendAll();
        }

        private void Main_ResizeEnd(object sender, EventArgs e)
        {
            ResumeAll();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            SetGameSelection(config.GameVersion);
            SetMaterialTypeSelection(MaterialType.Material);
            FillVersionDropdown();
            UpdateTopLevelSectionVisibility();
            ApplyCurrentAppearance();

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                OpenMaterialSelection(args.Skip(1).Where(path => !string.IsNullOrWhiteSpace(path)).ToArray(), appendToBulk: false);
            }
        }

        private void Main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
                OpenMaterialSelection(files, appendToBulk: false);
        }

        #endregion

    }
}

using MaterialLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool suppressSingleEditorUndoTracking;

        private BaseMaterialFile currentMaterial;
        private BaseMaterialFile originalMaterial;
        private BaseMaterialFile lastSingleEditorUndoState;
        private BaseMaterialFile pendingSingleEditorAppearanceRebuildMaterial;
        private bool forceSingleEditorControlRebuild;
        private readonly Stack<BaseMaterialFile> singleEditorUndoStates = new();
        private readonly Stack<BaseMaterialFile> singleEditorRedoStates = new();
        private readonly Dictionary<CollapsibleGroupBox, string[]> sectionVisibilityMap = [];
        private CollapsibleGroupBox generalPageSection;
        private CollapsibleGroupBox materialPageSection;
        private CollapsibleGroupBox effectPageSection;
        private WorkspaceMode workspaceMode = WorkspaceMode.Empty;
        private BulkMaterialEditSession bulkSession;
        private BulkMaterialEditorView bulkEditorView;
        private bool bulkBackupBeforeWrite = true;

        private ToolStripMenuItem openFolderToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem editUndoToolStripMenuItem;
        private ToolStripMenuItem editRedoToolStripMenuItem;
        private ToolStripMenuItem findToolStripMenuItem;
        private ToolStripMenuItem findReplaceToolStripMenuItem;
        private ToolStripMenuItem editSendToSingleEditorToolStripMenuItem;
        private ToolStripMenuItem editRevealInExplorerToolStripMenuItem;
        private ToolStripMenuItem editReloadFromDiskToolStripMenuItem;
        private ToolStripMenuItem editEditToggleToolStripMenuItem;
        private ToolStripMenuItem editCutToolStripMenuItem;
        private ToolStripMenuItem editCutRowsToolStripMenuItem;
        private ToolStripMenuItem editCopyRowsToolStripMenuItem;
        private ToolStripMenuItem editCopyFieldsToolStripMenuItem;
        private ToolStripMenuItem editPasteRowsToolStripMenuItem;
        private ToolStripMenuItem editPasteFieldsToolStripMenuItem;
        private ToolStripMenuItem editClearToolStripMenuItem;
        private ToolStripMenuItem editSelectAllToolStripMenuItem;
        private ToolStripMenuItem editSelectRowToolStripMenuItem;
        private ToolStripMenuItem editSelectPageAboveToolStripMenuItem;
        private ToolStripMenuItem editSelectPageBelowToolStripMenuItem;
        private ToolStripMenuItem editSelectAllAboveToolStripMenuItem;
        private ToolStripMenuItem editSelectAllBelowToolStripMenuItem;
        private ToolStripMenuItem editSelectDirtyRowsToolStripMenuItem;
        private ToolStripMenuItem editSelectErrorRowsToolStripMenuItem;
        private ToolStripMenuItem editFiltersToolStripMenuItem;
        private ToolStripMenuItem editAllFilesFilterToolStripMenuItem;
        private ToolStripMenuItem editUniqueFilesFilterToolStripMenuItem;
        private ToolStripMenuItem editDuplicateFilesFilterToolStripMenuItem;
        private ToolStripMenuItem editDirtyFilesFilterToolStripMenuItem;
        private ToolStripMenuItem editErroredFilesFilterToolStripMenuItem;
        private ToolStripMenuItem editCustomFilesFilterToolStripMenuItem;
        private ToolStripMenuItem editSortToolStripMenuItem;
        private ToolStripMenuItem editSortAscendingToolStripMenuItem;
        private ToolStripMenuItem editSortDescendingToolStripMenuItem;
        private ToolStripMenuItem editSortAlphabeticalToolStripMenuItem;
        private ToolStripMenuItem editSortLastModifiedToolStripMenuItem;
        private ToolStripMenuItem editSortCreationDateToolStripMenuItem;
        private ToolStripMenuItem editSortByAdditionToolStripMenuItem;
        private ToolStripMenuItem editSortGroupByFolderToolStripMenuItem;
        private ToolStripMenuItem addFilesToolStripMenuItem;
        private ToolStripMenuItem addFolderToolStripMenuItem;
        private ToolStripMenuItem saveSelectedToolStripMenuItem;
        private ToolStripMenuItem removeSelectedFilesToolStripMenuItem;
        private ToolStripMenuItem recoveryToolStripMenuItem;
        private ToolStripMenuItem recoverMostRecentBackupToolStripMenuItem;
        private ToolStripMenuItem recoverLeastRecentBackupToolStripMenuItem;
        private ToolStripMenuItem browseBackupsToolStripMenuItem;
        private const int DefaultVersionFO4 = 2;
        private const int DefaultVersionFO76 = 21;
        private const string ApplicationTitle = "B.G.E.M.";
        private const int DefaultEditorWidth = 1280;
        private const int DefaultEditorHeight = 860;
        private const int MinimumEditorWidth = 1024;
        private const int MinimumEditorHeight = 640;
        private static readonly Font DefaultAppFont = new("Segoe UI", 9f);

        private sealed class SingleEditorSectionState
        {
            public bool GeneralCollapsed { get; init; }
            public bool MaterialCollapsed { get; init; }
            public bool EffectCollapsed { get; init; }
            public Dictionary<string, bool> NestedCollapsedStates { get; } = new(StringComparer.Ordinal);
        }

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
            bulkBackupBeforeWrite = config.CreateBackupsByDefault;
            InitializeComponent();
            InitializeSingleEditorLoadingOverlay();
            MinimumSize = new Size(MinimumEditorWidth, MinimumEditorHeight);
            ClientSize = new Size(DefaultEditorWidth, DefaultEditorHeight);
            InitializeBulkShell();
            ControlFactory.VisibilityChangedCallback = UpdateSectionVisibility;
            InitializePageSections();
            ApplyCurrentAppearance();
            InitializeRecoveryMenu();
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
            bulkEditorView.SaveFilesRequested += (s, e) => SaveBulkChanges(selectedOnly: true);
            bulkEditorView.SaveFilesAsRequested += (s, e) => SaveBulkAs();
            bulkEditorView.CloseFilesRequested += (s, e) => RemoveSelectedBulkFiles();
            bulkEditorView.RevealInExplorerRequested += (s, e) => RevealSelectedBulkFilesInExplorer();
            bulkEditorView.ReloadFilesRequested += (s, e) => ReloadSelectedBulkFilesFromDisk();
            bulkEditorView.SendToSingleEditorRequested += (s, e) => SendSelectedBulkRowToSingleEditor();
            bulkEditorView.SendToGenerateVariationsRequested += (s, e) => SendSelectedBulkRowToGenerateVariations();
            bulkEditorView.SendToOverwriteFilesRequested += (s, e) => SendSelectedBulkRowToOverwriteFiles();
            contentScrollPanel.Controls.Add(bulkEditorView);
            bulkEditorView.BringToFront();

            closeToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F4;
            generateVariationsToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.G;
            overwriteFilesByFieldToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.G;
            overwriteFilesByFieldToolStripMenuItem.Text = "Overwrite Files By Field...";

            InitializeEditMenu();

            openFolderToolStripMenuItem = new ToolStripMenuItem("Open Folder...");
            openFolderToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;
            openFolderToolStripMenuItem.Click += OpenFolderToolStripMenuItem_Click;

            addFilesToolStripMenuItem = new ToolStripMenuItem("Add File(s)...");
            addFilesToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Oemplus;
            addFilesToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl++";
            addFilesToolStripMenuItem.Click += AddFilesToolStripMenuItem_Click;

            addFolderToolStripMenuItem = new ToolStripMenuItem("Add Folder...");
            addFolderToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.Oemplus;
            addFolderToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+Shift++";
            addFolderToolStripMenuItem.Click += AddFolderToolStripMenuItem_Click;

            saveSelectedToolStripMenuItem = new ToolStripMenuItem("Save Selected");
            saveSelectedToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
            saveSelectedToolStripMenuItem.Click += SaveSelectedToolStripMenuItem_Click;

            removeSelectedFilesToolStripMenuItem = new ToolStripMenuItem("Remove Selected Files");
            removeSelectedFilesToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Delete;
            removeSelectedFilesToolStripMenuItem.Click += RemoveSelectedFilesToolStripMenuItem_Click;

            findToolStripMenuItem = new ToolStripMenuItem("Find...");
            findToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            findToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteFindSelection();

            findReplaceToolStripMenuItem = new ToolStripMenuItem("Find and Replace...");
            findReplaceToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.F;
            findReplaceToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteFindReplaceSelection();

            editRevealInExplorerToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.E;
            editReloadFromDiskToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.R;

            int openIndex = fileToolStripMenuItem.DropDownItems.IndexOf(openToolStripMenuItem);
            if (openIndex >= 0)
                fileToolStripMenuItem.DropDownItems.Insert(openIndex + 1, openFolderToolStripMenuItem);

            int saveIndex = fileToolStripMenuItem.DropDownItems.IndexOf(saveToolStripMenuItem);
            if (saveIndex >= 0)
            {
                fileToolStripMenuItem.DropDownItems.Remove(saveAsToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Remove(closeToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 1, saveSelectedToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 2, saveAsToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 3, addFilesToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 4, addFolderToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 5, removeSelectedFilesToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 6, editRevealInExplorerToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 7, editReloadFromDiskToolStripMenuItem);
                fileToolStripMenuItem.DropDownItems.Insert(saveIndex + 8, closeToolStripMenuItem);
            }

            toolsToolStripMenuItem.DropDownItems.Clear();
            toolsToolStripMenuItem.DropDownItems.Add(editFiltersToolStripMenuItem);
            toolsToolStripMenuItem.DropDownItems.Add(editSortToolStripMenuItem);
            toolsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsToolStripMenuItem.DropDownItems.Add(findToolStripMenuItem);
            toolsToolStripMenuItem.DropDownItems.Add(findReplaceToolStripMenuItem);
            toolsToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            toolsToolStripMenuItem.DropDownItems.Add(generateVariationsToolStripMenuItem);
            toolsToolStripMenuItem.DropDownItems.Add(overwriteFilesByFieldToolStripMenuItem);
        }

        private void InitializeEditMenu()
        {
            editToolStripMenuItem = new ToolStripMenuItem("Edit");

            editUndoToolStripMenuItem = new ToolStripMenuItem("Undo");
            editUndoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            editUndoToolStripMenuItem.Click += EditUndoToolStripMenuItem_Click;

            editRedoToolStripMenuItem = new ToolStripMenuItem("Redo");
            editRedoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            editRedoToolStripMenuItem.Click += EditRedoToolStripMenuItem_Click;

            editSendToSingleEditorToolStripMenuItem = new ToolStripMenuItem("Send to Single-File Editor")
            {
                ShortcutKeys = Keys.Control | Keys.W
            };
            editSendToSingleEditorToolStripMenuItem.Click += (s, e) => SendSelectedBulkRowToSingleEditor();

            editRevealInExplorerToolStripMenuItem = new ToolStripMenuItem("Reveal in Explorer");
            editRevealInExplorerToolStripMenuItem.Click += (s, e) => RevealSelectedBulkFilesInExplorer();

            editReloadFromDiskToolStripMenuItem = new ToolStripMenuItem("Reload From Disk");
            editReloadFromDiskToolStripMenuItem.Click += (s, e) => ReloadSelectedBulkFilesFromDisk();

            editEditToggleToolStripMenuItem = new ToolStripMenuItem("Edit/Toggle")
            {
                ShortcutKeyDisplayString = "Enter / F2 / Space"
            };
            editEditToggleToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteEditOrToggleSelection();

            editCutToolStripMenuItem = new ToolStripMenuItem("Cut")
            {
                ShortcutKeys = Keys.Control | Keys.X
            };
            editCutToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteCutFields();

            editCutRowsToolStripMenuItem = new ToolStripMenuItem("Cut Row(s)")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.X
            };
            editCutRowsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteCutRows();

            editCopyRowsToolStripMenuItem = new ToolStripMenuItem("Copy Row(s)")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.C
            };
            editCopyRowsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteCopyRows();

            editCopyFieldsToolStripMenuItem = new ToolStripMenuItem("Copy Field(s)")
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };
            editCopyFieldsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteCopyFields();

            editPasteRowsToolStripMenuItem = new ToolStripMenuItem("Paste Row(s)")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.V
            };
            editPasteRowsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecutePasteRows();

            editPasteFieldsToolStripMenuItem = new ToolStripMenuItem("Paste Field(s)")
            {
                ShortcutKeyDisplayString = "Ctrl+V"
            };
            editPasteFieldsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecutePasteFields();

            editClearToolStripMenuItem = new ToolStripMenuItem("Clear")
            {
                ShortcutKeys = Keys.Delete
            };
            editClearToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteClearSelection();

            editSelectAllToolStripMenuItem = new ToolStripMenuItem("Select All")
            {
                ShortcutKeys = Keys.Control | Keys.A
            };
            editSelectAllToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectAll();

            editSelectRowToolStripMenuItem = new ToolStripMenuItem("Select Row")
            {
                ShortcutKeys = Keys.Control | Keys.Enter
            };
            editSelectRowToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectCurrentRow();

            editSelectPageAboveToolStripMenuItem = new ToolStripMenuItem("Select Page Above")
            {
                ShortcutKeyDisplayString = "Shift+PgUp"
            };
            editSelectPageAboveToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectPageAbove();

            editSelectPageBelowToolStripMenuItem = new ToolStripMenuItem("Select Page Below")
            {
                ShortcutKeyDisplayString = "Shift+PgDn"
            };
            editSelectPageBelowToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectPageBelow();

            editSelectAllAboveToolStripMenuItem = new ToolStripMenuItem("Select All Above")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.PageUp
            };
            editSelectAllAboveToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectAllAbove();

            editSelectAllBelowToolStripMenuItem = new ToolStripMenuItem("Select All Below")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.PageDown
            };
            editSelectAllBelowToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectAllBelow();

            editSelectDirtyRowsToolStripMenuItem = new ToolStripMenuItem("Select Dirty Record(s)")
            {
                ShortcutKeys = Keys.Control | Keys.Q
            };
            editSelectDirtyRowsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectDirtyRows();

            editSelectErrorRowsToolStripMenuItem = new ToolStripMenuItem("Select Errored Record(s)")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.Q
            };
            editSelectErrorRowsToolStripMenuItem.Click += (s, e) => bulkEditorView?.ExecuteSelectErrorRows();

            editAllFilesFilterToolStripMenuItem = new ToolStripMenuItem("All Files")
            {
                ShortcutKeys = Keys.Control | Keys.Shift | Keys.A
            };
            editAllFilesFilterToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetRowFilter(BulkMaterialRowFilter.AllFiles);

            editUniqueFilesFilterToolStripMenuItem = new ToolStripMenuItem("Unique Files Only");
            editUniqueFilesFilterToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetRowFilter(BulkMaterialRowFilter.UniqueFilesOnly);

            editDuplicateFilesFilterToolStripMenuItem = new ToolStripMenuItem("Duplicate Files");
            editDuplicateFilesFilterToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetRowFilter(BulkMaterialRowFilter.DuplicateFiles);

            editDirtyFilesFilterToolStripMenuItem = new ToolStripMenuItem("Dirty Files");
            editDirtyFilesFilterToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetRowFilter(BulkMaterialRowFilter.DirtyFiles);

            editErroredFilesFilterToolStripMenuItem = new ToolStripMenuItem("Errored Files");
            editErroredFilesFilterToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetRowFilter(BulkMaterialRowFilter.ErroredFiles);

            editCustomFilesFilterToolStripMenuItem = new ToolStripMenuItem("Custom Search Results");
            editCustomFilesFilterToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetRowFilter(BulkMaterialRowFilter.CustomFiles);

            editFiltersToolStripMenuItem = new ToolStripMenuItem("Filters");
            editFiltersToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+T <> Ctrl+Shift+T";
            editFiltersToolStripMenuItem.DropDownItems.Add(editAllFilesFilterToolStripMenuItem);
            editFiltersToolStripMenuItem.DropDownItems.Add(editUniqueFilesFilterToolStripMenuItem);
            editFiltersToolStripMenuItem.DropDownItems.Add(editDuplicateFilesFilterToolStripMenuItem);
            editFiltersToolStripMenuItem.DropDownItems.Add(editDirtyFilesFilterToolStripMenuItem);
            editFiltersToolStripMenuItem.DropDownItems.Add(editErroredFilesFilterToolStripMenuItem);
            editFiltersToolStripMenuItem.DropDownItems.Add(editCustomFilesFilterToolStripMenuItem);

            editSortAscendingToolStripMenuItem = new ToolStripMenuItem("Ascending");
            editSortAscendingToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetSortDirection(BulkMaterialSortDirection.Ascending);

            editSortDescendingToolStripMenuItem = new ToolStripMenuItem("Descending");
            editSortDescendingToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetSortDirection(BulkMaterialSortDirection.Descending);

            editSortAlphabeticalToolStripMenuItem = new ToolStripMenuItem("Alphabetical");
            editSortAlphabeticalToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetSortKey(BulkMaterialSortKey.Alphabetical);

            editSortLastModifiedToolStripMenuItem = new ToolStripMenuItem("Last Modified");
            editSortLastModifiedToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetSortKey(BulkMaterialSortKey.LastModified);

            editSortCreationDateToolStripMenuItem = new ToolStripMenuItem("Creation Date");
            editSortCreationDateToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetSortKey(BulkMaterialSortKey.CreationDate);

            editSortByAdditionToolStripMenuItem = new ToolStripMenuItem("By Addition");
            editSortByAdditionToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetSortKey(BulkMaterialSortKey.ByAddition);

            editSortGroupByFolderToolStripMenuItem = new ToolStripMenuItem("Group by Folder");
            editSortGroupByFolderToolStripMenuItem.Click += (s, e) => bulkEditorView?.SetGroupByFolder(!(bulkEditorView?.IsGroupedByFolder ?? false));

            editSortToolStripMenuItem = new ToolStripMenuItem("Sort");
            editSortToolStripMenuItem.DropDownItems.Add(editSortAscendingToolStripMenuItem);
            editSortToolStripMenuItem.DropDownItems.Add(editSortDescendingToolStripMenuItem);
            editSortToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            editSortToolStripMenuItem.DropDownItems.Add(editSortAlphabeticalToolStripMenuItem);
            editSortToolStripMenuItem.DropDownItems.Add(editSortLastModifiedToolStripMenuItem);
            editSortToolStripMenuItem.DropDownItems.Add(editSortCreationDateToolStripMenuItem);
            editSortToolStripMenuItem.DropDownItems.Add(editSortByAdditionToolStripMenuItem);
            editSortToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            editSortToolStripMenuItem.DropDownItems.Add(editSortGroupByFolderToolStripMenuItem);

            editToolStripMenuItem.DropDownItems.Add(editUndoToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editRedoToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            editToolStripMenuItem.DropDownItems.Add(editSendToSingleEditorToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            editToolStripMenuItem.DropDownItems.Add(editEditToggleToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editCutToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editCutRowsToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editCopyRowsToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editCopyFieldsToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editPasteRowsToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editPasteFieldsToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editClearToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            editToolStripMenuItem.DropDownItems.Add(editSelectAllToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectRowToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectPageAboveToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectPageBelowToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectAllAboveToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectAllBelowToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectDirtyRowsToolStripMenuItem);
            editToolStripMenuItem.DropDownItems.Add(editSelectErrorRowsToolStripMenuItem);

            menuStrip.Items.Insert(1, editToolStripMenuItem);
        }

        private void UpdateBulkProjectionMenuState(bool hasBulkSession)
        {
            if (editToolStripMenuItem == null)
                return;

            BulkMaterialRowFilter activeFilter = bulkEditorView?.ActiveFilter ?? BulkMaterialRowFilter.AllFiles;
            BulkMaterialSortKey activeSortKey = bulkEditorView?.ActiveSortKey ?? BulkMaterialSortKey.Alphabetical;
            BulkMaterialSortDirection activeSortDirection = bulkEditorView?.ActiveSortDirection ?? BulkMaterialSortDirection.Ascending;
            bool isGroupedByFolder = bulkEditorView?.IsGroupedByFolder == true;

            if (editFiltersToolStripMenuItem != null)
                editFiltersToolStripMenuItem.Enabled = hasBulkSession;
            if (editSortToolStripMenuItem != null)
                editSortToolStripMenuItem.Enabled = hasBulkSession;

            if (editAllFilesFilterToolStripMenuItem != null)
                editAllFilesFilterToolStripMenuItem.Checked = activeFilter == BulkMaterialRowFilter.AllFiles;
            if (editUniqueFilesFilterToolStripMenuItem != null)
                editUniqueFilesFilterToolStripMenuItem.Checked = activeFilter == BulkMaterialRowFilter.UniqueFilesOnly;
            if (editDuplicateFilesFilterToolStripMenuItem != null)
                editDuplicateFilesFilterToolStripMenuItem.Checked = activeFilter == BulkMaterialRowFilter.DuplicateFiles;
            if (editDirtyFilesFilterToolStripMenuItem != null)
                editDirtyFilesFilterToolStripMenuItem.Checked = activeFilter == BulkMaterialRowFilter.DirtyFiles;
            if (editErroredFilesFilterToolStripMenuItem != null)
                editErroredFilesFilterToolStripMenuItem.Checked = activeFilter == BulkMaterialRowFilter.ErroredFiles;
            if (editCustomFilesFilterToolStripMenuItem != null)
            {
                editCustomFilesFilterToolStripMenuItem.Checked = activeFilter == BulkMaterialRowFilter.CustomFiles;
                editCustomFilesFilterToolStripMenuItem.Enabled = hasBulkSession && (bulkEditorView?.HasCustomRowFilter == true);
                editCustomFilesFilterToolStripMenuItem.Text = bulkEditorView?.CustomFilterName ?? "Custom Search Results";
            }

            if (editSortAscendingToolStripMenuItem != null)
                editSortAscendingToolStripMenuItem.Checked = activeSortDirection == BulkMaterialSortDirection.Ascending;
            if (editSortDescendingToolStripMenuItem != null)
                editSortDescendingToolStripMenuItem.Checked = activeSortDirection == BulkMaterialSortDirection.Descending;
            if (editSortAlphabeticalToolStripMenuItem != null)
                editSortAlphabeticalToolStripMenuItem.Checked = activeSortKey == BulkMaterialSortKey.Alphabetical;
            if (editSortLastModifiedToolStripMenuItem != null)
                editSortLastModifiedToolStripMenuItem.Checked = activeSortKey == BulkMaterialSortKey.LastModified;
            if (editSortCreationDateToolStripMenuItem != null)
                editSortCreationDateToolStripMenuItem.Checked = activeSortKey == BulkMaterialSortKey.CreationDate;
            if (editSortByAdditionToolStripMenuItem != null)
                editSortByAdditionToolStripMenuItem.Checked = activeSortKey == BulkMaterialSortKey.ByAddition;
            if (editSortGroupByFolderToolStripMenuItem != null)
                editSortGroupByFolderToolStripMenuItem.Checked = isGroupedByFolder;
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

                SuspendAll();
                FillVersionDropdown();
                if (currentMaterial != null)
                    ControlFactory.UpdateVisibility();
                ResumeAll();
            }

            ApplyCurrentAppearance();
        }

        private void MaterialTypeToggle_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is not RadioButton radioButton || !radioButton.Checked)
                return;

            MaterialType selectedType = CurrentMaterialType;
            if (IsSingleMode
                && currentMaterial != null
                && MaterialFileTypeHelper.GetMaterialType(currentMaterial) != selectedType)
            {
                RebuildSingleEditorForMaterialType(selectedType);
            }

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
            if (selectedVersion == null || currentMaterial == null || !TryGetVersionValue(selectedVersion, out uint version))
                return;

            bool versionChanged = currentMaterial.Version != version;
            if (versionChanged)
                currentMaterial.Version = version;

            SuspendAll();
            ControlFactory.UpdateVisibility();
            ResumeAll();

            if (versionChanged)
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
            RecordSingleEditorUndoStateIfNeeded();
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

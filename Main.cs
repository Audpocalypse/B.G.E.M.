using MaterialLib;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Material_Editor
{
    public struct Config
    {
        public Game GameVersion;
        public Font Font;
        public UITheme Theme;
    }

    public enum Game
    {
        FO4,
        FO76
    }

    public enum MaterialType
    {
        Material,
        Effect
    }

    public partial class Main : Form
    {
        private static ThemePalette GetPalette(UITheme theme)
        {
            return ThemeManager.GetPalette(theme);
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

        private const int DefaultVersionFO4 = 2;
        private const int DefaultVersionFO76 = 21;
        private const string ApplicationTitle = "B.G.E.M.";
        private const int DefaultEditorWidth = 1280;
        private const int DefaultEditorHeight = 860;
        private const int MinimumEditorWidth = 1024;
        private const int MinimumEditorHeight = 640;

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
            config = initialConfig;
            InitializeComponent();
            MinimumSize = new Size(MinimumEditorWidth, MinimumEditorHeight);
            Size = new Size(MinimumEditorWidth, MinimumEditorHeight);
            ControlFactory.VisibilityChangedCallback = UpdateSectionVisibility;
            AppIconProvider.Apply(this);
            InitializePageSections();
            ApplyConfigFont();
            ApplyTheme();
            UpdateThemeMenuChecks();
        }

        private string ChangeFileExtension(string filePath)
        {
            if (filePath == null)
                return null;
            string ext = CurrentMaterialType switch
            {
                MaterialType.Effect => ".bgem",
                _ => ".bgsm",
            };
            return Path.ChangeExtension(filePath, ext);
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

        private string BuildSuggestedVariationOutputPattern()
        {
            string sourcePath = workFilePath;
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                    var extension = Path.GetExtension(sourcePath);
                    if (string.IsNullOrEmpty(extension))
                        extension = ".bgsm";

                    var fileName = Path.GetFileNameWithoutExtension(sourcePath);
                    if (string.IsNullOrEmpty(fileName))
                        fileName = "variation";

                    return Path.Combine(directory, $"{fileName}_{{index}}{extension}");
                }
                catch
                {
                }
            }

            return "variation_{index}.bgsm";
        }

        #region UI
        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            workFilePath = null;
            Text = ApplicationTitle;

            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            layoutGeneral.Enabled = true;
            layoutMaterial.Enabled = true;
            layoutEffect.Enabled = true;

            SuspendAll();

            CreateMaterialControls();

            int selectedIndex;
            if (currentMaterial.Version > 2 && currentMaterial.Version <= 22)
                selectedIndex = (int)Game.FO76;
            else
                selectedIndex = (int)Game.FO4;

            if ((int)CurrentGame != selectedIndex)
                SetGameSelection((Game)selectedIndex);
            else
                FillVersionDropdown();

            SetMaterialTypeSelection(MaterialType.Material);

            ResumeAll();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog.FileName;
                if (file.ToLower().EndsWith(".bgsm"))
                {
                    OpenMaterial(file, BGSM.Signature);
                }
                else if (file.ToLower().EndsWith(".bgem"))
                {
                    OpenMaterial(file, BGEM.Signature);
                }
                else
                {
                    MessageBox.Show(string.Format("File extension of file '{0}' not supported!", file), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(workFilePath))
            {
                SaveAsToolStripMenuItem_Click(null, null);
            }

            BaseMaterialFile material = CurrentMaterialType switch
            {
                MaterialType.Effect => new BGEM(),
                _ => new BGSM(),
            };
            GetMaterialValues(material);

            try
            {
                using var file = new FileStream(workFilePath, FileMode.Create);
                if (serializeToJSONToolStripMenuItem.Checked)
                {
                    var currentCulture = Thread.CurrentThread.CurrentCulture;
                    Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                    try
                    {
                        using var writer = JsonReaderWriterFactory.CreateJsonWriter(file, Encoding.UTF8, true, true, "  ");
                        var ser = new DataContractJsonSerializer(material.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                        ser.WriteObject(writer, material);
                        writer.Flush();
                    }
                    catch
                    {
                        MessageBox.Show(string.Format("Failed to serialize to JSON data for file '{0}'!", workFilePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        Thread.CurrentThread.CurrentCulture = currentCulture;
                    }
                }
                else
                {
                    if (!material.Save(file))
                    {
                        MessageBox.Show(string.Format("Failed to save file '{0}'!", workFilePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            catch
            {
                MessageBox.Show(string.Format("Failed to save file '{0}'!", workFilePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            currentMaterial = material;
            originalMaterial = CloneMaterial(currentMaterial);
            Text = GetTitleText();
            changed = false;
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var matType = CurrentMaterialType;
            saveFileDialog.Filter = matType switch
            {
                MaterialType.Effect => "Effect File (.bgem)|*.bgem",
                _ => "Material File (.bgsm)|*.bgsm",
            };
            string fileName = WorkFileName;
            if (fileName != null)
            {
                saveFileDialog.FileName = ChangeFileExtension(fileName);
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = saveFileDialog.FileName;

                BaseMaterialFile material;
                if (CurrentMaterialType == MaterialType.Effect)
                    material = new BGEM();
                else
                    material = new BGSM();

                GetMaterialValues(material);

                try
                {
                    using var file = new FileStream(filePath, FileMode.Create);
                    if (serializeToJSONToolStripMenuItem.Checked)
                    {
                        var currentCulture = Thread.CurrentThread.CurrentCulture;
                        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                        try
                        {
                            using var writer = JsonReaderWriterFactory.CreateJsonWriter(file, Encoding.UTF8, true, true, "  ");
                            var ser = new DataContractJsonSerializer(material.GetType(), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
                            ser.WriteObject(writer, material);
                            writer.Flush();
                        }
                        catch
                        {
                            MessageBox.Show(string.Format("Failed to serialize to JSON data for file '{0}'!", filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        finally
                        {
                            Thread.CurrentThread.CurrentCulture = currentCulture;
                        }
                    }
                    else
                    {
                        if (!material.Save(file))
                        {
                            MessageBox.Show(string.Format("Failed to save file '{0}'!", filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                }
                catch
                {
                    MessageBox.Show(string.Format("Failed to save file '{0}'!", filePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                workFilePath = filePath;

                saveToolStripMenuItem.Enabled = true;

                currentMaterial = material;
                originalMaterial = CloneMaterial(currentMaterial);
                Text = GetTitleText();
                changed = false;
            }
        }

        private void OverwriteFilesByFieldToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentMaterial == null)
            {
                MessageBox.Show("Open or create a material before using the overwrite tool.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentState = CaptureCurrentMaterialState();
            if (currentState == null)
                return;

            var descriptors = MaterialFieldRegistry.GetDescriptors(currentState);
            if (descriptors.Count == 0)
            {
                MessageBox.Show("No writable fields are available for the current material.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var baseline = originalMaterial ?? CloneMaterial(currentState) ?? currentState;

            var palette = GetPalette(config.Theme);
            using var fieldSelection = new FieldSelectionDialog(descriptors, baseline, currentState, palette, config.Theme);
            if (fieldSelection.ShowDialog(this) != DialogResult.OK)
                return;

            if (fieldSelection.SelectedFields == null || fieldSelection.SelectedFields.Count == 0)
                return;

            using var targetDialog = new TargetFileSelectionDialog(palette, config.Theme);
            if (targetDialog.ShowDialog(this) != DialogResult.OK)
                return;

            var tool = new FieldOverwriteTool();
            var results = tool.Run(currentState, fieldSelection.SelectedFields, targetDialog.TargetFiles, targetDialog.BackupBeforeWrite);

            using var summary = new OverwriteSummaryDialog(results, palette, config.Theme);
            summary.ShowDialog(this);
        }

        private void GenerateVariationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentMaterial == null)
            {
                MessageBox.Show("Open a material before generating variations.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (currentMaterial.GetType() != typeof(BGSM))
            {
                MessageBox.Show("Generating variations is only supported for BGSM materials.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var template = CaptureCurrentMaterialState();
            if (template == null)
                return;

            var descriptors = MaterialFieldRegistry.GetDescriptors(template)
                .Where(descriptor => descriptor.Category == FieldCategory.Material)
                .Where(descriptor => descriptor.GetValue(template) is string)
                .ToList();

            if (descriptors.Count == 0)
            {
                MessageBox.Show("No texture fields are available for variation.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentValues = descriptors.Select(descriptor => descriptor.GetValue(template) as string ?? string.Empty).ToList();
            var palette = GetPalette(config.Theme);
            using var dialog = new VariationGeneratorDialog(
                descriptors,
                currentValues,
                BuildSuggestedVariationOutputPattern(),
                palette,
                config.Theme);
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            if (dialog.Options == null)
                return;

            var results = MaterialVariationGenerator.Generate(template, dialog.Options, serializeToJSONToolStripMenuItem.Checked);
            using var summary = new OverwriteSummaryDialog(results, palette, config.Theme);
            summary.ShowDialog(this);
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            currentMaterial = null;
            originalMaterial = null;
            workFilePath = string.Empty;

            saveToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Enabled = false;
            layoutGeneral.Enabled = false;
            layoutMaterial.Enabled = false;
            layoutEffect.Enabled = false;

            SuspendAll();
            ControlFactory.ClearControls();
            ResumeAll();

            Text = ApplicationTitle;
            changed = false;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var fontDialog = new FontDialog()
            {
                AllowScriptChange = false,
                AllowVectorFonts = false,
                AllowVerticalFonts = false,
                FontMustExist = true,
                ShowColor = false,
                ShowEffects = false,
                MaxSize = 14,
                Font = config.Font ?? Font
            };

            var result = fontDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                config.Font = fontDialog.Font;
                ApplyConfigFont();
                MessageBox.Show("Changing the font requires a restart of the application.", "Restart required", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DefaultThemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTheme(UITheme.Default);
        }

        private void DarkThemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTheme(UITheme.PipBoy3000);
        }

        private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var about = new AboutDialog(GetPalette(config.Theme), config.Theme);
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

            ApplyTheme();
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

            ApplyTheme();
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
            ApplyConfigFont();
            SetGameSelection(config.GameVersion);
            SetMaterialTypeSelection(MaterialType.Material);
            FillVersionDropdown();
            UpdateTopLevelSectionVisibility();
            ApplyTheme();

            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                string fileName = args[1];
                if (fileName.ToLower().EndsWith(".bgsm"))
                {
                    OpenMaterial(fileName, BGSM.Signature);
                }
                else if (fileName.ToLower().EndsWith(".bgem"))
                {
                    OpenMaterial(fileName, BGEM.Signature);
                }
                else
                {
                    MessageBox.Show("File extension not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        internal static Config LoadConfig()
        {
            var loadedConfig = new Config
            {
                GameVersion = Game.FO4,
                Font = new Font("Segoe UI", 9f),
                Theme = UITheme.Default
            };

            try
            {
                var appSettings = ConfigurationManager.AppSettings;

                var gameVersion = appSettings["GameVersion"];
                if (gameVersion != null && Enum.TryParse(gameVersion, out Game parsedGame))
                {
                    loadedConfig.GameVersion = parsedGame;
                }

                var fontName = appSettings["FontName"];
                var fontSizeStr = appSettings["FontSize"];
                var themeValue = appSettings["Theme"];
                if (!string.IsNullOrEmpty(fontName) && !string.IsNullOrEmpty(fontSizeStr))
                {
                    if (!float.TryParse(fontSizeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float fontSize))
                        fontSize = 10.0f;

                    try
                    {
                        loadedConfig.Font = new Font(fontName, fontSize);
                    }
                    catch
                    {
                        loadedConfig.Font = new Font("Segoe UI", 9f);
                    }
                }

                if (!string.IsNullOrEmpty(themeValue) && Enum.TryParse(themeValue, true, out UITheme parsedTheme))
                {
                    loadedConfig.Theme = parsedTheme;
                }
            }
            catch
            {
            }

            if (loadedConfig.Font == null)
                loadedConfig.Font = new Font("Segoe UI", 9f);

            return loadedConfig;
        }

        private void ApplyConfigFont()
        {
            if (config.Font != null)
            {
                Font = config.Font;
            }
        }

        private void WriteSettings()
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                var gameVersion = configFile.AppSettings.Settings["GameVersion"];
                if (gameVersion != null)
                    gameVersion.Value = Convert.ToString(config.GameVersion);
                else
                    configFile.AppSettings.Settings.Add("GameVersion", Convert.ToString(config.GameVersion));

                var fontName = configFile.AppSettings.Settings["FontName"];
                if (fontName != null)
                    fontName.Value = config.Font.Name;
                else
                    configFile.AppSettings.Settings.Add("FontName", config.Font.Name);

                var fontSize = configFile.AppSettings.Settings["FontSize"];
                if (fontSize != null)
                    fontSize.Value = config.Font.Size.ToString(CultureInfo.InvariantCulture);
                else
                    configFile.AppSettings.Settings.Add("FontSize", config.Font.Size.ToString(CultureInfo.InvariantCulture));

                var themeSetting = configFile.AppSettings.Settings["Theme"];
                if (themeSetting != null)
                    themeSetting.Value = config.Theme.ToString();
                else
                    configFile.AppSettings.Settings.Add("Theme", config.Theme.ToString());

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch { }
        }

        private void ApplyTheme()
        {
            var palette = GetPalette(config.Theme);

            BackColor = palette.FormBackground;
            ForeColor = palette.Foreground;

            if (config.Theme == UITheme.Default)
            {
                ApplyDefaultThemeAppearance();
            }
            else
            {
                ApplyCustomThemeAppearance(palette);
            }
        }

        private void ApplyCustomThemeAppearance(ThemePalette palette)
        {
            CheckControl.OffForegroundProvider = () => Color.White;
            ApplyThemeToControl(topControlsLayout, palette, palette.ControlBackground);
            ApplyThemeToControl(contentScrollPanel, palette, palette.PanelBackground);
            ApplyThemeToControl(contentHostLayout, palette, palette.PanelBackground);
            ApplyThemeToControl(layoutGeneral, palette, palette.PanelBackground);
            ApplyThemeToControl(layoutMaterial, palette, palette.PanelBackground);
            ApplyThemeToControl(layoutEffect, palette, palette.PanelBackground);
            ApplyThemeToToolStrip(menuStrip, palette);
            ApplyComboTheme(listVersion, palette);

            ApplyDropdownTheme(ControlNames.AlphaBlendMode, palette);
        }

        private void ApplyDefaultThemeAppearance()
        {
            CheckControl.OffForegroundProvider = () => Color.Red;
            ResetControlAppearance(topControlsLayout);
            ResetControlAppearance(contentScrollPanel);
            ResetControlAppearance(contentHostLayout);
            ResetControlAppearance(layoutGeneral);
            ResetControlAppearance(layoutMaterial);
            ResetControlAppearance(layoutEffect);
            ResetComboAppearance(listVersion);
            ResetToolStripAppearance(menuStrip);
        }

        private void ResetControlAppearance(Control control)
        {
            if (control == null)
                return;

            control.ResetBackColor();
            control.ResetForeColor();

            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Standard;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = SystemColors.ControlDark;
                    button.UseVisualStyleBackColor = true;
                    break;
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.Fixed3D;
                    break;
                case ComboBox comboChild:
                    ResetComboAppearance(comboChild);
                    break;
                case CheckBox checkBox:
                    checkBox.UseVisualStyleBackColor = true;
                    if (checkBox.Tag is CheckControl)
                        CheckControl.UpdateCheckVisual(checkBox);
                    break;
                case RadioButton radioButton:
                    radioButton.FlatStyle = FlatStyle.Standard;
                    radioButton.UseVisualStyleBackColor = true;
                    break;
                case NumericUpDown numeric:
                    numeric.ResetBackColor();
                    numeric.ResetForeColor();
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ResetControlAppearance(child);
            }
        }

        private void ResetComboAppearance(ComboBox combo)
        {
            if (combo == null)
                return;

            combo.FlatStyle = FlatStyle.Standard;
            combo.DrawMode = DrawMode.Normal;
            combo.DrawItem -= ComboBox_DrawItem;
            combo.ResetBackColor();
            combo.ResetForeColor();
        }

        private void ResetToolStripAppearance(ToolStrip toolStrip)
        {
            if (toolStrip == null)
                return;

            toolStrip.BackColor = SystemColors.Control;
            toolStrip.ForeColor = SystemColors.ControlText;
            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.BackColor = SystemColors.Control;
                item.ForeColor = SystemColors.ControlText;
            }
        }

        private void ApplyThemeToControl(Control control, ThemePalette palette, Color backgroundOverride)
        {
            if (control == null)
                return;

            control.BackColor = backgroundOverride;
            control.ForeColor = palette.Foreground;
            StyleControl(control, palette, backgroundOverride);

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, palette, backgroundOverride);
            }
        }

        private static void StyleControl(Control control, ThemePalette palette, Color background)
        {
            switch (control)
            {
                case Button button:
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderSize = 1;
                    button.FlatAppearance.BorderColor = palette.Accent.IsEmpty ? Color.Gray : palette.Accent;
                    if (button.Tag is ColorControl colorControl)
                        button.BackColor = colorControl.CurrentColor;
                    else
                        button.BackColor = background;
                    if (ShouldOverrideForeColor(button))
                        button.ForeColor = palette.Foreground;
                    break;
                case TextBoxBase textBox:
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    textBox.BackColor = background;
                    if (ShouldOverrideForeColor(textBox))
                        textBox.ForeColor = palette.Foreground;
                    break;
                case ComboBox combo:
                    combo.FlatStyle = FlatStyle.Flat;
                    combo.BackColor = background;
                    if (ShouldOverrideForeColor(combo))
                        combo.ForeColor = palette.Foreground;
                    break;
                case NumericUpDown numeric:
                    numeric.BackColor = background;
                    numeric.ForeColor = palette.Foreground;
                    break;
                case CheckBox checkBox:
                    checkBox.BackColor = background;
                    if (ShouldOverrideForeColor(checkBox))
                        checkBox.ForeColor = palette.Foreground;
                    CheckControl.UpdateCheckVisual(checkBox);
                    break;
                case RadioButton radioButton:
                    radioButton.FlatStyle = FlatStyle.Flat;
                    radioButton.FlatAppearance.BorderSize = 1;
                    radioButton.FlatAppearance.BorderColor = palette.Accent.IsEmpty ? Color.Gray : palette.Accent;
                    radioButton.BackColor = radioButton.Checked
                        ? Color.Black
                        : background;
                    radioButton.ForeColor = palette.Foreground;
                    break;
                case ListView listView:
                    listView.BackColor = palette.PanelBackground;
                    listView.ForeColor = palette.Foreground;
                    listView.BorderStyle = BorderStyle.FixedSingle;
                    listView.OwnerDraw = false;
                    break;
                default:
                    break;
            }
        }

        private static bool ShouldOverrideForeColor(Control control)
        {
            var color = control.ForeColor;
            return color == Color.Empty || color == SystemColors.ControlText || color == SystemColors.WindowText || color == Color.Black;
        }

        private void ApplyComboTheme(ComboBox combo, ThemePalette palette)
        {
            if (combo == null)
                return;

            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = palette.ControlBackground;
            if (ShouldOverrideForeColor(combo))
                combo.ForeColor = palette.Foreground;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DrawItem -= ComboBox_DrawItem;
            combo.DrawItem += ComboBox_DrawItem;
        }

        private void ApplyDropdownTheme(string controlName, ThemePalette palette)
        {
            var combo = ControlFactory.Find(controlName)?.Control as ComboBox;
            if (combo != null)
            {
                ApplyComboTheme(combo, palette);
            }
        }

        private void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                var palette = GetPalette(config.Theme);
                var bounds = e.Bounds;
                var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                var background = isSelected
                    ? palette.Accent.IsEmpty ? palette.MenuBackground : palette.Accent
                    : palette.PanelBackground;

                using var brush = new SolidBrush(background);
                e.Graphics.FillRectangle(brush, bounds);

                if (e.Index >= 0)
                {
                    var text = combo.Items[e.Index]?.ToString() ?? string.Empty;
                    TextRenderer.DrawText(e.Graphics, text, combo.Font, bounds, palette.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
                else
                {
                    var text = combo.Text ?? string.Empty;
                    TextRenderer.DrawText(e.Graphics, text, combo.Font, bounds, palette.Foreground, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
            }
        }
        
        private void ApplyThemeToToolStrip(ToolStrip toolStrip, ThemePalette palette)
        {
            if (toolStrip == null)
                return;

            toolStrip.BackColor = palette.MenuBackground;
            toolStrip.ForeColor = palette.Foreground;

            foreach (ToolStripItem item in toolStrip.Items)
            {
                item.BackColor = palette.MenuBackground;
                item.ForeColor = palette.Foreground;
            }
        }

        private void SetTheme(UITheme theme)
        {
            if (config.Theme == theme)
                return;

            config.Theme = theme;
            ApplyTheme();
            UpdateThemeMenuChecks();
        }

        private void UpdateThemeMenuChecks()
        {
            if (defaultThemeToolStripMenuItem != null)
                defaultThemeToolStripMenuItem.Checked = config.Theme == UITheme.Default;
            if (darkThemeToolStripMenuItem != null)
                darkThemeToolStripMenuItem.Checked = config.Theme == UITheme.PipBoy3000;
        }

        private void Main_Closing(object sender, FormClosingEventArgs e)
        {
            if (changed)
            {
                DialogResult res = MessageBox.Show("There are unsaved changes to the file.\nDo want to save them before closing the program?",
                    "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (res == DialogResult.Yes)
                {
                    SaveToolStripMenuItem_Click(null, null);
                }
                else if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }

            WriteSettings();
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
            {
                string file = files[0];
                if (file.ToLower().EndsWith(".bgsm"))
                {
                    OpenMaterial(file, BGSM.Signature);
                }
                else if (file.ToLower().EndsWith(".bgem"))
                {
                    OpenMaterial(file, BGEM.Signature);
                }
                else
                {
                    MessageBox.Show("Format not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string GetTitleText()
        {
            string fileName = WorkFileName;
            if (string.IsNullOrEmpty(fileName))
                return ApplicationTitle;

            if (currentMaterial != null)
                return $"{ApplicationTitle} – {fileName} (Version {currentMaterial.Version})";

            return $"{ApplicationTitle} – {fileName}";
        }
        #endregion

        #region Material
        private static Color UIntToColor(uint value)
        {
            return Color.FromArgb(255,
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF));
        }

        private bool RefractionVisibility(CustomControl _)
        {
            if (ControlFactory.GetProperty(ControlNames.Refraction, out var property))
                return Convert.ToBoolean(property);
            else
                return true;
        }

        private bool SpecularColorAndMultiplierVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.SpecularEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool EmittanceColorAndMultiplierVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.EmittanceEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool HairTintColorVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.Hair, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool FalloffVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.FalloffEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private bool SoftDepthVisibility(CustomControl _)
        {
            if (!ControlFactory.GetProperty(ControlNames.SoftEnabled, out var property))
                return false;

            return Convert.ToBoolean(property);
        }

        private void PrepareFlatEditorLayout(TableLayoutPanel layout)
        {
            layout.SuspendLayout();
            layout.Controls.Clear();
            layout.RowStyles.Clear();
            layout.ColumnStyles.Clear();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(8);
            layout.RowCount = 0;
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle());
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle());
            layout.ResumeLayout();
        }

        private void PrepareHostLayout(TableLayoutPanel layout)
        {
            layout.SuspendLayout();
            layout.Controls.Clear();
            layout.RowStyles.Clear();
            layout.ColumnStyles.Clear();
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Dock = DockStyle.Top;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(8);
            layout.RowCount = 0;
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ResumeLayout();
        }

        private TableLayoutPanel CreateSectionColumn()
        {
            var column = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 0
            };
            column.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return column;
        }

        private TableLayoutPanel CreateTwoColumnHost()
        {
            var host = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return host;
        }

        private static void AddControlRow(TableLayoutPanel parent, Control control)
        {
            int row = parent.RowCount++;
            parent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            parent.Controls.Add(control, 0, row);
        }

        private void UpdateSectionVisibility()
        {
            foreach (var entry in sectionVisibilityMap)
            {
                bool hasVisibleContent = entry.Value.Any(name =>
                {
                    if (string.IsNullOrEmpty(name))
                        return false;

                    var control = ControlFactory.Find(name);
                    return control?.ShouldBeVisible() == true;
                });

                entry.Key.Visible = hasVisibleContent;
            }
        }

        private void InitializePageSections()
        {
            contentHostLayout.SuspendLayout();
            contentHostLayout.Controls.Clear();
            contentHostLayout.RowStyles.Clear();
            contentHostLayout.RowCount = 0;

            generalPageSection = CreateTopLevelPageSection("General", layoutGeneral);
            materialPageSection = CreateTopLevelPageSection("Material", layoutMaterial);
            effectPageSection = CreateTopLevelPageSection("Effect", layoutEffect);

            AddControlRow(contentHostLayout, generalPageSection);
            AddControlRow(contentHostLayout, materialPageSection);
            AddControlRow(contentHostLayout, effectPageSection);

            contentHostLayout.ResumeLayout();
            UpdateTopLevelSectionVisibility();
        }

        private static CollapsibleGroupBox CreateTopLevelPageSection(string title, TableLayoutPanel layout)
        {
            var section = new CollapsibleGroupBox(title, collapsible: true, collapsedByDefault: false)
            {
                Margin = new Padding(0, 0, 0, 8)
            };
            section.ContentLayout.Controls.Add(layout, 0, 0);
            return section;
        }

        private void UpdateTopLevelSectionVisibility()
        {
            if (generalPageSection != null)
                generalPageSection.Visible = true;

            if (materialPageSection != null)
                materialPageSection.Visible = CurrentMaterialType == MaterialType.Material;

            if (effectPageSection != null)
                effectPageSection.Visible = CurrentMaterialType == MaterialType.Effect;
        }

        private void RebuildGeneralLayout()
        {
            var leftSections = new[]
            {
                new SectionDefinition("Texture Coordinates", 2, false, false,
                    ControlNames.TileU,
                    ControlNames.TileV,
                    ControlNames.OffsetU,
                    ControlNames.OffsetV,
                    ControlNames.ScaleU,
                    ControlNames.ScaleV),
                new SectionDefinition("Alpha / Refraction", 2, false, false,
                    ControlNames.Alpha,
                    ControlNames.AlphaBlendMode,
                    ControlNames.AlphaTestReference,
                    ControlNames.AlphaTest,
                    ControlNames.Refraction,
                    ControlNames.RefractionFalloff,
                    ControlNames.RefractionPower),
            };

            var rightSections = new[]
            {
                new SectionDefinition("Rendering / Environment", 2, false, false,
                    ControlNames.ZBufferWrite,
                    ControlNames.ZBufferTest,
                    ControlNames.ScreenSpaceReflections,
                    ControlNames.WetnessControlSSR,
                    ControlNames.EnvironmentMapping,
                    ControlNames.EnvironmentMaskScale,
                    ControlNames.DepthBias,
                    ControlNames.Decal,
                    ControlNames.TwoSided,
                    ControlNames.DecalNoFade,
                    ControlNames.NonOccluder,
                    ControlNames.GrayscaleToPaletteColor),
                new SectionDefinition("Advanced Masking", 1, false, false,
                    ControlNames.MaskWrites),
            };

            PrepareHostLayout(layoutGeneral);
            BuildSectionColumns(layoutGeneral, leftSections, rightSections);
        }

        private void RebuildMaterialLayout()
        {
            PrepareHostLayout(layoutMaterial);

            var pathSection = CreatePathsSection("Paths / Textures", new[]
            {
                ControlNames.Diffuse,
                ControlNames.Normal,
                ControlNames.SmoothSpec,
                ControlNames.Greyscale,
                ControlNames.Environment,
                ControlNames.Glow,
                ControlNames.Wrinkles,
                ControlNames.InnerLayer,
                ControlNames.Displacement,
                ControlNames.RootMaterialPath,
                ControlNames.Specular,
                ControlNames.Lighting,
                ControlNames.Flow,
                ControlNames.DistanceFieldAlpha,
            }, null);
            AddControlRow(layoutMaterial, pathSection);

            var leftSections = new[]
            {
                new SectionDefinition("Specular / Surface", 2, true, true,
                    ControlNames.SpecularEnabled,
                    ControlNames.SpecularColor,
                    ControlNames.SpecularMultiplier,
                    ControlNames.Smoothness,
                    ControlNames.FresnelPower,
                    ControlNames.AnisoLighting,
                    ControlNames.GrayscaleToPaletteScale,
                    ControlNames.SkewSpecularAlpha),
                new SectionDefinition("Lighting / Emittance", 2, true, true,
                    ControlNames.RimLighting,
                    ControlNames.RimPower,
                    ControlNames.BackLighting,
                    ControlNames.BacklightPower,
                    ControlNames.SubsurfaceLighting,
                    ControlNames.SubsurfaceLightingRolloff,
                    ControlNames.Translucency,
                    ControlNames.TranslucencyThickObject,
                    ControlNames.TranslucencyAlbSubsurfColor,
                    ControlNames.TranslucencySubsurfaceColor,
                    ControlNames.TranslucencyTransmissiveScale,
                    ControlNames.TranslucencyTurbulence,
                    ControlNames.EmittanceEnabled,
                    ControlNames.ExternalEmittance,
                    ControlNames.EmittanceColor,
                    ControlNames.EmittanceMultiplier,
                    ControlNames.LumEmittance,
                    ControlNames.AdaptativeEmissive,
                    ControlNames.AdaptEmissiveExposureOffset,
                    ControlNames.AdaptEmissiveFinalExposureMin,
                    ControlNames.AdaptEmissiveFinalExposureMax),
            };

            var rightSections = new[]
            {
                new SectionDefinition("Wetness", 2, true, true,
                    ControlNames.WetSpecScale,
                    ControlNames.WetSpecPowerScale,
                    ControlNames.WetSpecMinVar,
                    ControlNames.WetEnvMapScale,
                    ControlNames.WetFresnelPower,
                    ControlNames.WetMetalness),
                new SectionDefinition("Rendering Flags", 2, true, true,
                    ControlNames.EnableEditorAlphaRef,
                    ControlNames.ModelSpaceNormals,
                    ControlNames.ReceiveShadows,
                    ControlNames.CastShadows,
                    ControlNames.AssumeShadowmask,
                    ControlNames.HideSecret,
                    ControlNames.DissolveFade,
                    ControlNames.Glowmap),
                new SectionDefinition("Special Shader Features", 2, true, true,
                    ControlNames.Hair,
                    ControlNames.Facegen,
                    ControlNames.SkinTint,
                    ControlNames.Tree,
                    ControlNames.EnvironmentMapWindow,
                    ControlNames.EnvironmentMapEye,
                    ControlNames.Tessellate,
                    ControlNames.HairTintColor,
                    ControlNames.PBR,
                    ControlNames.CustomPorosity,
                    ControlNames.PorosityValue,
                    ControlNames.DisplacementTexBias,
                    ControlNames.DisplacementTexScale,
                    ControlNames.TessellationPNScale,
                    ControlNames.TessellationBaseFactor,
                    ControlNames.TessellationFadeDistance,
                    ControlNames.Terrain,
                    ControlNames.UnkInt1BGSM,
                    ControlNames.TerrainThresholdFalloff,
                    ControlNames.TerrainTilingDistance,
                    ControlNames.TerrainRotationAngle),
            };

            BuildSectionColumns(layoutMaterial, leftSections, rightSections);
        }

        private void RebuildEffectLayout()
        {
            PrepareHostLayout(layoutEffect);

            var pathSection = CreatePathsSection("Paths / Textures", new[]
            {
                ControlNames.BaseTexture,
                ControlNames.NormalTexture,
                ControlNames.GrayscaleTexture,
                ControlNames.EnvmapTexture,
                ControlNames.EnvmapMaskTexture,
                ControlNames.GlowTexture,
                ControlNames.SpecularTexture,
                ControlNames.LightingTexture,
                ControlNames.GlassRoughnessScratch,
                ControlNames.GlassDirtOverlay,
            }, null);
            AddControlRow(layoutEffect, pathSection);

            var leftSections = new[]
            {
                new SectionDefinition("Surface / Color", 2, true, true,
                    ControlNames.BaseColor,
                    ControlNames.BaseColorScale,
                    ControlNames.EmitColor,
                    ControlNames.LightingInfluence,
                    ControlNames.EnvmapMinLOD),
                new SectionDefinition("Falloff / Softness", 2, true, true,
                    ControlNames.FalloffEnabled,
                    ControlNames.FalloffColorEnabled,
                    ControlNames.FalloffStartAngle,
                    ControlNames.FalloffStopAngle,
                    ControlNames.FalloffStartOpacity,
                    ControlNames.FalloffStopOpacity,
                    ControlNames.SoftEnabled,
                    ControlNames.SoftDepth),
            };

            var rightSections = new[]
            {
                new SectionDefinition("Environment / Glass", 2, true, true,
                    ControlNames.EnvMapping,
                    ControlNames.EnvMappingMaskScale,
                    ControlNames.GlassEnabled,
                    ControlNames.GlassFresnelColor,
                    ControlNames.GlassBlurScaleBase,
                    ControlNames.GlassBlurScaleFactor,
                    ControlNames.GlassRefractionScaleBase),
                new SectionDefinition("Effect Features", 2, true, true,
                    ControlNames.BloodEnabled,
                    ControlNames.EffectLightingEnabled,
                    ControlNames.GrayscaleToPaletteAlpha,
                    ControlNames.EffectGlowmap,
                    ControlNames.EffectPBRSpecular,
                    ControlNames.AdaptativeEmissiveExposureOffset,
                    ControlNames.AdaptativeEmissiveFinalExposureMin,
                    ControlNames.AdaptativeEmissiveFinalExposureMax),
            };

            BuildSectionColumns(layoutEffect, leftSections, rightSections);
        }

        private void BuildSectionColumns(TableLayoutPanel host, IReadOnlyList<SectionDefinition> leftSections, IReadOnlyList<SectionDefinition> rightSections)
        {
            var sectionHost = CreateTwoColumnHost();
            var leftColumn = CreateSectionColumn();
            var rightColumn = CreateSectionColumn();

            sectionHost.Controls.Add(leftColumn, 0, 0);
            sectionHost.Controls.Add(rightColumn, 1, 0);

            foreach (var section in leftSections)
                AddControlRow(leftColumn, CreateSection(section, true));

            foreach (var section in rightSections)
                AddControlRow(rightColumn, CreateSection(section, false));

            AddControlRow(host, sectionHost);
        }

        private Control CreateSection(SectionDefinition definition, bool leftColumn)
        {
            var group = new CollapsibleGroupBox(definition.Title, definition.Collapsible, definition.CollapsedByDefault)
            {
                Margin = leftColumn ? new Padding(0, 0, 5, 8) : new Padding(5, 0, 0, 8)
            };

            var grid = CreatePropertyGrid(definition.PairsPerRow);
            PopulatePropertyGrid(grid, definition.Controls, definition.PairsPerRow);
            group.ContentLayout.Controls.Add(grid, 0, 0);
            sectionVisibilityMap[group] = definition.Controls;
            return group;
        }

        private CollapsibleGroupBox CreatePathsSection(string title, IReadOnlyList<string> controlNames, string fullWidthControlName)
        {
            var group = new CollapsibleGroupBox(title)
            {
                Margin = new Padding(0, 0, 0, 8)
            };

            var grid = CreateFileGrid();
            PopulateFileGrid(grid, controlNames, fullWidthControlName);
            group.ContentLayout.Controls.Add(grid, 0, 0);
            return group;
        }

        private TableLayoutPanel CreatePropertyGrid(int pairsPerRow)
        {
            var grid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = Math.Max(1, pairsPerRow * 2 + Math.Max(0, pairsPerRow - 1)),
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 0
            };

            for (int pairIndex = 0; pairIndex < pairsPerRow; pairIndex++)
            {
                grid.ColumnStyles.Add(new ColumnStyle());
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / pairsPerRow));

                if (pairIndex < pairsPerRow - 1)
                    grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            }

            return grid;
        }

        private TableLayoutPanel CreateFileGrid()
        {
            var grid = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 7,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 0
            };

            grid.ColumnStyles.Add(new ColumnStyle());
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle());
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
            grid.ColumnStyles.Add(new ColumnStyle());
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            grid.ColumnStyles.Add(new ColumnStyle());

            return grid;
        }

        private void PopulatePropertyGrid(TableLayoutPanel grid, IReadOnlyList<string> controlNames, int pairsPerRow)
        {
            int currentRow = -1;
            int pairIndex = 0;

            foreach (var controlName in controlNames)
            {
                var control = ControlFactory.Find(controlName);
                if (control == null)
                    continue;

                ApplyCompactFieldLayout(control);

                if (NeedsFullWidthRow(control))
                {
                    if (pairIndex != 0)
                        pairIndex = 0;

                    AddFullWidthFieldRow(grid, control);
                    currentRow = -1;
                    continue;
                }

                if (pairIndex == 0)
                {
                    currentRow = grid.RowCount++;
                    grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                }

                int column = pairIndex * 3;
                grid.Controls.Add(control.LabelControl, column, currentRow);
                grid.Controls.Add(control.Control, column + 1, currentRow);

                pairIndex++;
                if (pairIndex >= pairsPerRow)
                    pairIndex = 0;
            }
        }

        private void PopulateFileGrid(TableLayoutPanel grid, IReadOnlyList<string> controlNames, string fullWidthControlName)
        {
            for (int i = 0; i < controlNames.Count; i += 2)
            {
                int row = grid.RowCount++;
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                AddFileFieldPair(grid, row, controlNames[i], 0);

                if (i + 1 < controlNames.Count)
                    AddFileFieldPair(grid, row, controlNames[i + 1], 4);
            }

            if (!string.IsNullOrEmpty(fullWidthControlName))
                AddFullWidthFieldRow(grid, ControlFactory.Find(fullWidthControlName));
        }

        private void AddFileFieldPair(TableLayoutPanel grid, int row, string controlName, int columnOffset)
        {
            if (string.IsNullOrEmpty(controlName))
                return;

            var control = ControlFactory.Find(controlName);
            if (control == null)
                return;

            ApplyCompactFieldLayout(control, true);

            grid.Controls.Add(control.LabelControl, columnOffset, row);
            grid.Controls.Add(control.Control, columnOffset + 1, row);

            if (control.ExtraControl != null)
                grid.Controls.Add(control.ExtraControl, columnOffset + 2, row);
        }

        private void AddFullWidthFieldRow(TableLayoutPanel grid, CustomControl control)
        {
            if (control == null)
                return;

            ApplyCompactFieldLayout(control, true);

            var rowLayout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = control.ExtraControl != null ? 3 : 2,
                Dock = DockStyle.Top,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            rowLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            rowLayout.ColumnStyles.Add(new ColumnStyle());
            rowLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            if (control.ExtraControl != null)
                rowLayout.ColumnStyles.Add(new ColumnStyle());

            rowLayout.Controls.Add(control.LabelControl, 0, 0);
            rowLayout.Controls.Add(control.Control, 1, 0);

            if (control.ExtraControl != null)
                rowLayout.Controls.Add(control.ExtraControl, 2, 0);

            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            grid.Controls.Add(rowLayout, 0, row);
            grid.SetColumnSpan(rowLayout, grid.ColumnCount);
        }

        private static bool NeedsFullWidthRow(CustomControl control)
        {
            return control.Control is CheckedListBox || control.ExtraControl != null;
        }

        private static void ApplyCompactFieldLayout(CustomControl control, bool pathStyle = false)
        {
            if (control?.LabelControl == null || control.Control == null)
                return;

            control.LabelControl.Anchor = AnchorStyles.Left;
            control.LabelControl.AutoSize = true;
            control.LabelControl.Margin = new Padding(0, 4, 8, 4);

            switch (control.Control)
            {
                case CheckBox checkBox:
                    checkBox.AutoSize = true;
                    checkBox.Anchor = AnchorStyles.Left;
                    checkBox.Margin = new Padding(0, 2, 0, 2);
                    break;
                case CheckedListBox checkedListBox:
                    checkedListBox.Dock = DockStyle.Fill;
                    checkedListBox.Margin = new Padding(0, 1, 0, 1);
                    checkedListBox.Height = Math.Max(checkedListBox.Height, 60);
                    break;
                default:
                    control.Control.Dock = DockStyle.Fill;
                    control.Control.Margin = new Padding(0, 1, 0, 1);
                    break;
            }

            if (control.ExtraControl != null)
            {
                control.ExtraControl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                control.ExtraControl.Margin = pathStyle ? new Padding(6, 1, 0, 1) : new Padding(6, 1, 0, 1);
            }
        }

        private void CreateMaterialControls(BaseMaterialFile file = null)
        {
            if (file == null)
            {
                file = new BGSM();

                switch (config.GameVersion)
                {
                    case Game.FO4:
                        file.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        file.Version = DefaultVersionFO76;
                        break;
                }
            }

            currentMaterial = file;
            sectionVisibilityMap.Clear();

            var fileFont = new Font("Consolas", Font.Size, FontStyle.Regular, GraphicsUnit.Point);

            ControlFactory.ClearControls();
            PrepareFlatEditorLayout(layoutGeneral);
            PrepareFlatEditorLayout(layoutMaterial);
            PrepareFlatEditorLayout(layoutEffect);
            ControlFactory.DefaultChangedCallback = (control) => OnChanged();

            ControlFactory.CreateControl(layoutGeneral, ControlNames.TileU, file.TileU);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.TileV, file.TileV);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.OffsetU, file.UOffset);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.OffsetV, file.VOffset);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ScaleU, file.UScale);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ScaleV, file.VScale);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.Alpha, file.Alpha);

            int alphaBlendMode = (int)file.AlphaBlendMode;
            if (alphaBlendMode < 0 || alphaBlendMode > 4)
                alphaBlendMode = 0;

            ControlFactory.CreateDropdownControl(layoutGeneral, ControlNames.AlphaBlendMode,
                ["Unknown", "None", "Standard", "Additive", "Multiplicative"], alphaBlendMode);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.AlphaTestReference, file.AlphaTestRef);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.AlphaTest, file.AlphaTest);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ZBufferWrite, file.ZBufferWrite);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ZBufferTest, file.ZBufferTest);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.ScreenSpaceReflections, file.ScreenSpaceReflections);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.WetnessControlSSR, file.WetnessControlScreenSpaceReflections);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.Decal, file.Decal);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.TwoSided, file.TwoSided);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.DecalNoFade, file.DecalNoFade);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.NonOccluder, file.NonOccluder);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.Refraction, file.Refraction, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.RefractionFalloff);
                ControlFactory.UpdateVisibility(ControlNames.RefractionPower);
                OnChanged();
            });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.RefractionFalloff, file.RefractionFalloff, RefractionVisibility);
            ControlFactory.CreateControl(layoutGeneral, ControlNames.RefractionPower, file.RefractionPower, RefractionVisibility);

            ControlFactory.CreateControl(layoutGeneral, ControlNames.EnvironmentMapping, file.EnvironmentMapping, (control) => { return file.Version < 10; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.EnvironmentMaskScale);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutGeneral, ControlNames.EnvironmentMaskScale, file.EnvironmentMappingMaskScale, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.EnvironmentMapping, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 10;
            });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.DepthBias, file.DepthBias, (control) => { return file.Version >= 10; });
            ControlFactory.CreateControl(layoutGeneral, ControlNames.GrayscaleToPaletteColor, file.GrayscaleToPaletteColor);
            ControlFactory.CreateFlagControl(layoutGeneral, ControlNames.MaskWrites, Enum.GetNames(typeof(BaseMaterialFile.MaskWriteFlags)), (int)file.MaskWrites, (control) => { return file.Version >= 6; });

            if (file is not BGSM bgsm)
            {
                bgsm = new BGSM();

                switch (config.GameVersion)
                {
                    case Game.FO4:
                        bgsm.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        bgsm.Version = DefaultVersionFO76;
                        break;
                }
            }

            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Diffuse, fileFont, FileControl.FileType.Texture, bgsm.DiffuseTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Normal, fileFont, FileControl.FileType.Texture, bgsm.NormalTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.SmoothSpec, fileFont, FileControl.FileType.Texture, bgsm.SmoothSpecTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Greyscale, fileFont, FileControl.FileType.Texture, bgsm.GreyscaleTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Environment, fileFont, FileControl.FileType.Texture, bgsm.EnvmapTexture, (control) => { return file.Version <= 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Glow, fileFont, FileControl.FileType.Texture, bgsm.GlowTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.InnerLayer, fileFont, FileControl.FileType.Texture, bgsm.InnerLayerTexture, (control) => { return file.Version <= 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Wrinkles, fileFont, FileControl.FileType.Texture, bgsm.WrinklesTexture);
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Displacement, fileFont, FileControl.FileType.Texture, bgsm.DisplacementTexture, (control) => { return file.Version <= 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Specular, fileFont, FileControl.FileType.Texture, bgsm.SpecularTexture, (control) => { return file.Version > 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Lighting, fileFont, FileControl.FileType.Texture, bgsm.LightingTexture, (control) => { return file.Version > 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.Flow, fileFont, FileControl.FileType.Texture, bgsm.FlowTexture, (control) => { return file.Version > 2; });
            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.DistanceFieldAlpha, fileFont, FileControl.FileType.Texture, bgsm.DistanceFieldAlphaTexture, (control) => { return file.Version > 2; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.EnableEditorAlphaRef, bgsm.EnableEditorAlphaRef);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.RimLighting, bgsm.RimLighting, (control) => { return file.Version < 8; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.RimPower);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.RimPower, bgsm.RimPower, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.RimLighting, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 8;
            });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.BacklightPower, bgsm.BackLightPower, (control) => { return file.Version < 8; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SubsurfaceLighting, bgsm.SubsurfaceLighting, (control) => { return file.Version < 8; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SubsurfaceLightingRolloff);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SubsurfaceLightingRolloff, bgsm.SubsurfaceLightingRolloff, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.SubsurfaceLighting, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 8;
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Translucency, bgsm.Translucency, (control) => { return file.Version >= 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyThickObject, bgsm.TranslucencyThickObject, (control) => { return file.Version >= 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyAlbSubsurfColor, bgsm.TranslucencyMixAlbedoWithSubsurfaceColor, (control) => { return file.Version >= 8; });

            var translucencySubsurfaceColor = UIntToColor(bgsm.TranslucencySubsurfaceColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencySubsurfaceColor, translucencySubsurfaceColor, (control) => { return file.Version >= 8; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyTransmissiveScale, bgsm.TranslucencyTransmissiveScale, (control) => { return file.Version >= 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TranslucencyTurbulence, bgsm.TranslucencyTurbulence, (control) => { return file.Version >= 8; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SpecularEnabled, bgsm.SpecularEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SpecularColor);
                ControlFactory.UpdateVisibility(ControlNames.SpecularMultiplier);
                OnChanged();
            });

            var specularColor = UIntToColor(bgsm.SpecularColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.SpecularColor, specularColor, SpecularColorAndMultiplierVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.SpecularMultiplier, bgsm.SpecularMult, SpecularColorAndMultiplierVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.Smoothness, bgsm.Smoothness);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.FresnelPower, bgsm.FresnelPower);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetSpecScale, bgsm.WetnessControlSpecScale);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetSpecPowerScale, bgsm.WetnessControlSpecPowerScale);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetSpecMinVar, bgsm.WetnessControlSpecMinvar);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetEnvMapScale, bgsm.WetnessControlEnvMapScale, (control) => { return file.Version < 10; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetFresnelPower, bgsm.WetnessControlFresnelPower);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.WetMetalness, bgsm.WetnessControlMetalness);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.PBR, bgsm.PBR, (control) => { return file.Version > 2; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.CustomPorosity, bgsm.CustomPorosity, (control) => { return file.Version >= 9; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.PorosityValue, bgsm.PorosityValue, (control) => { return file.Version >= 9; });

            ControlFactory.CreateFileControl(layoutMaterial, ControlNames.RootMaterialPath, fileFont, FileControl.FileType.Material, bgsm.RootMaterialPath);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.AnisoLighting, bgsm.AnisoLighting);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.EmittanceEnabled, bgsm.EmitEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.EmittanceColor);
                ControlFactory.UpdateVisibility(ControlNames.EmittanceMultiplier);
                OnChanged();
            });

            var emittanceColor = UIntToColor(bgsm.EmittanceColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.EmittanceColor, emittanceColor, EmittanceColorAndMultiplierVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.EmittanceMultiplier, bgsm.EmittanceMult, EmittanceColorAndMultiplierVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.ModelSpaceNormals, bgsm.ModelSpaceNormals);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.ExternalEmittance, bgsm.ExternalEmittance);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.LumEmittance, bgsm.LumEmittance, (control) => { return file.Version >= 12; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptativeEmissive, bgsm.UseAdaptativeEmissive, (control) => { return file.Version >= 13; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.AdaptEmissiveExposureOffset);
                ControlFactory.UpdateVisibility(ControlNames.AdaptEmissiveFinalExposureMin);
                ControlFactory.UpdateVisibility(ControlNames.AdaptEmissiveFinalExposureMax);
                OnChanged();
            });

            bool AdaptativeEmissiveVisibility(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.AdaptativeEmissive, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 13;
            }

            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptEmissiveExposureOffset, bgsm.AdaptativeEmissive_ExposureOffset, AdaptativeEmissiveVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptEmissiveFinalExposureMin, bgsm.AdaptativeEmissive_FinalExposureMin, AdaptativeEmissiveVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.AdaptEmissiveFinalExposureMax, bgsm.AdaptativeEmissive_FinalExposureMax, AdaptativeEmissiveVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.BackLighting, bgsm.BackLighting, (control) => { return file.Version < 8; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.ReceiveShadows, bgsm.ReceiveShadows);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.HideSecret, bgsm.HideSecret);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.CastShadows, bgsm.CastShadows);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.DissolveFade, bgsm.DissolveFade);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.AssumeShadowmask, bgsm.AssumeShadowmask);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.Glowmap, bgsm.Glowmap);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.EnvironmentMapWindow, bgsm.EnvironmentMappingWindow, (control) => { return file.Version < 7; });
            ControlFactory.CreateControl(layoutMaterial, ControlNames.EnvironmentMapEye, bgsm.EnvironmentMappingEye, (control) => { return file.Version < 7; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Hair, bgsm.Hair, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.HairTintColor);
                OnChanged();
            });

            var hairTintColor = UIntToColor(bgsm.HairTintColor);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.HairTintColor, hairTintColor, HairTintColorVisibility);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Tree, bgsm.Tree);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.Facegen, bgsm.Facegen);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.SkinTint, bgsm.SkinTint);

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Tessellate, bgsm.Tessellate, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.DisplacementTexBias);
                ControlFactory.UpdateVisibility(ControlNames.DisplacementTexScale);
                ControlFactory.UpdateVisibility(ControlNames.TessellationPNScale);
                ControlFactory.UpdateVisibility(ControlNames.TessellationBaseFactor);
                ControlFactory.UpdateVisibility(ControlNames.TessellationFadeDistance);
                OnChanged();
            });

            bool TessellateVisibility(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.Tessellate, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version < 3;
            }

            ControlFactory.CreateControl(layoutMaterial, ControlNames.DisplacementTexBias, bgsm.DisplacementTextureBias, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.DisplacementTexScale, bgsm.DisplacementTextureScale, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TessellationPNScale, bgsm.TessellationPnScale, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TessellationBaseFactor, bgsm.TessellationBaseFactor, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TessellationFadeDistance, bgsm.TessellationFadeDistance, TessellateVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.GrayscaleToPaletteScale, bgsm.GrayscaleToPaletteScale);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.SkewSpecularAlpha, bgsm.SkewSpecularAlpha, (control) => { return file.Version >= 1; });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.Terrain, bgsm.Terrain, (control) => { return file.Version >= 3; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.UnkInt1BGSM);
                ControlFactory.UpdateVisibility(ControlNames.TerrainThresholdFalloff);
                ControlFactory.UpdateVisibility(ControlNames.TerrainTilingDistance);
                ControlFactory.UpdateVisibility(ControlNames.TerrainRotationAngle);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutMaterial, ControlNames.UnkInt1BGSM, bgsm.UnkInt1, (control) =>
            {
                if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version == 3;
            });

            bool TerrainVisibility(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.Terrain, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 3;
            }

            ControlFactory.CreateControl(layoutMaterial, ControlNames.TerrainThresholdFalloff, bgsm.TerrainThresholdFalloff, TerrainVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TerrainTilingDistance, bgsm.TerrainTilingDistance, TerrainVisibility);
            ControlFactory.CreateControl(layoutMaterial, ControlNames.TerrainRotationAngle, bgsm.TerrainRotationAngle, TerrainVisibility);

            if (file is not BGEM bgem)
            {
                bgem = new BGEM();

                switch (config.GameVersion)
                {
                    case Game.FO4:
                        bgem.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        bgem.Version = DefaultVersionFO76;
                        break;
                }
            }

            ControlFactory.CreateFileControl(layoutEffect, ControlNames.BaseTexture, fileFont, FileControl.FileType.Texture, bgem.BaseTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GrayscaleTexture, fileFont, FileControl.FileType.Texture, bgem.GrayscaleTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.EnvmapTexture, fileFont, FileControl.FileType.Texture, bgem.EnvmapTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.NormalTexture, fileFont, FileControl.FileType.Texture, bgem.NormalTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.EnvmapMaskTexture, fileFont, FileControl.FileType.Texture, bgem.EnvmapMaskTexture);
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.SpecularTexture, fileFont, FileControl.FileType.Texture, bgem.SpecularTexture, (control) => { return file.Version >= 11; });
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.LightingTexture, fileFont, FileControl.FileType.Texture, bgem.LightingTexture, (control) => { return file.Version >= 11; });
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlowTexture, fileFont, FileControl.FileType.Texture, bgem.GlowTexture, (control) => { return file.Version >= 11; });

            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlassRoughnessScratch, fileFont, FileControl.FileType.Texture, bgem.GlassRoughnessScratch, (control) => { return file.Version >= 21; });
            ControlFactory.CreateFileControl(layoutEffect, ControlNames.GlassDirtOverlay, fileFont, FileControl.FileType.Texture, bgem.GlassDirtOverlay, (control) => { return file.Version >= 21; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassEnabled, bgem.GlassEnabled, (control) => { return file.Version >= 21; }, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.GlassFresnelColor);
                ControlFactory.UpdateVisibility(ControlNames.GlassBlurScaleBase);
                ControlFactory.UpdateVisibility(ControlNames.GlassBlurScaleFactor);
                ControlFactory.UpdateVisibility(ControlNames.GlassRefractionScaleBase);
                OnChanged();
            });

            bool GlassVisibilityV21(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 21;
            }

            bool GlassVisibilityV22(CustomControl _)
            {
                if (!ControlFactory.GetProperty(ControlNames.GlassEnabled, out var property))
                    return false;

                return Convert.ToBoolean(property) && file.Version >= 22;
            }

            var glassFresnelColor = UIntToColor(bgem.GlassFresnelColor);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassFresnelColor, glassFresnelColor, GlassVisibilityV21);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassBlurScaleBase, bgem.GlassBlurScaleBase, GlassVisibilityV21);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassBlurScaleFactor, bgem.GlassBlurScaleFactor, GlassVisibilityV22);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GlassRefractionScaleBase, bgem.GlassRefractionScaleBase, GlassVisibilityV21);

            ControlFactory.CreateControl(layoutEffect, ControlNames.EnvMapping, bgem.EnvironmentMapping, (control) => { return file.Version >= 10; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.EnvMappingMaskScale, bgem.EnvironmentMappingMaskScale, (control) => { return file.Version >= 10; });

            ControlFactory.CreateControl(layoutEffect, ControlNames.BloodEnabled, bgem.BloodEnabled);
            ControlFactory.CreateControl(layoutEffect, ControlNames.EffectLightingEnabled, bgem.EffectLightingEnabled);

            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffEnabled, bgem.FalloffEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.FalloffStartAngle);
                ControlFactory.UpdateVisibility(ControlNames.FalloffStopAngle);
                ControlFactory.UpdateVisibility(ControlNames.FalloffStartOpacity);
                ControlFactory.UpdateVisibility(ControlNames.FalloffStopOpacity);
                OnChanged();
            });

            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffColorEnabled, bgem.FalloffColorEnabled);
            ControlFactory.CreateControl(layoutEffect, ControlNames.GrayscaleToPaletteAlpha, bgem.GrayscaleToPaletteAlpha);

            ControlFactory.CreateControl(layoutEffect, ControlNames.SoftEnabled, bgem.SoftEnabled, null, (control) =>
            {
                ControlFactory.UpdateVisibility(ControlNames.SoftDepth);
                OnChanged();
            });

            var baseColor = UIntToColor(bgem.BaseColor);
            ControlFactory.CreateControl(layoutEffect, ControlNames.BaseColor, baseColor);

            ControlFactory.CreateControl(layoutEffect, ControlNames.BaseColorScale, bgem.BaseColorScale);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStartAngle, bgem.FalloffStartAngle, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStopAngle, bgem.FalloffStopAngle, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStartOpacity, bgem.FalloffStartOpacity, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.FalloffStopOpacity, bgem.FalloffStopOpacity, FalloffVisibility);
            ControlFactory.CreateControl(layoutEffect, ControlNames.LightingInfluence, bgem.LightingInfluence);
            ControlFactory.CreateControl(layoutEffect, ControlNames.EnvmapMinLOD, bgem.EnvmapMinLOD);
            ControlFactory.CreateControl(layoutEffect, ControlNames.SoftDepth, bgem.SoftDepth, SoftDepthVisibility);

            var emitColor = UIntToColor(bgem.EmittanceColor);
            ControlFactory.CreateControl(layoutEffect, ControlNames.EmitColor, emitColor, (control) => { return file.Version >= 11; });

            ControlFactory.CreateControl(layoutEffect, ControlNames.AdaptativeEmissiveExposureOffset, bgem.AdaptativeEmissive_ExposureOffset, (control) => { return file.Version >= 15; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.AdaptativeEmissiveFinalExposureMin, bgem.AdaptativeEmissive_FinalExposureMin, (control) => { return file.Version >= 15; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.AdaptativeEmissiveFinalExposureMax, bgem.AdaptativeEmissive_FinalExposureMax, (control) => { return file.Version >= 15; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.EffectGlowmap, bgem.Glowmap, (control) => { return file.Version >= 16; });
            ControlFactory.CreateControl(layoutEffect, ControlNames.EffectPBRSpecular, bgem.EffectPbrSpecular, (control) => { return file.Version >= 20; });

            RebuildGeneralLayout();
            RebuildMaterialLayout();
            RebuildEffectLayout();
            CreateTooltips();
            ControlFactory.UpdateVisibility();
            ApplyTheme();
            originalMaterial = CloneMaterial(currentMaterial);
        }

        private BaseMaterialFile CaptureCurrentMaterialState()
        {
            if (currentMaterial == null)
                return null;

            BaseMaterialFile snapshot = CurrentMaterialType switch
            {
                MaterialType.Effect => new BGEM(),
                _ => new BGSM(),
            };

            GetMaterialValues(snapshot);
            return snapshot;
        }

        private BaseMaterialFile CloneMaterial(BaseMaterialFile source)
        {
            return MaterialFileCloner.Clone(source);
        }

        private void CreateTooltips()
        {
            toolTip.RemoveAll();

            ControlFactory.SetTooltip(ControlNames.TileU, toolTip, "Tile the U texture coordinate (wrapping/repeating the texture).");
            ControlFactory.SetTooltip(ControlNames.TileV, toolTip, "Tile the V texture coordinate (wrapping/repeating the texture).");
            ControlFactory.SetTooltip(ControlNames.OffsetU, toolTip, "Offset the U texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.OffsetV, toolTip, "Offset the V texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.ScaleU, toolTip, "Scale the U texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.ScaleV, toolTip, "Scale the V texture coordinate.");
            ControlFactory.SetTooltip(ControlNames.Alpha, toolTip, "Fixed alpha value that applies to the entire mesh (unrelated to texture alpha).");
            ControlFactory.SetTooltip(ControlNames.AlphaBlendMode, toolTip, "Defines the mode at which alpha is blended into other meshes.");
            ControlFactory.SetTooltip(ControlNames.AlphaTestReference, toolTip, "Reference value to do alpha testing for. Transparency happens when alpha is below or exceeds the reference value (depending on modes).");
            ControlFactory.SetTooltip(ControlNames.AlphaTest, toolTip, "Toggle alpha testing using the reference value.");
            ControlFactory.SetTooltip(ControlNames.ZBufferWrite, toolTip, "The mesh writes to the z-buffer to make others aware of its depth.");
            ControlFactory.SetTooltip(ControlNames.ZBufferTest, toolTip, "The mesh tests the z-buffer to take note of other meshes depth.");
            ControlFactory.SetTooltip(ControlNames.ScreenSpaceReflections, toolTip, "Toggle screen space reflections.");
            ControlFactory.SetTooltip(ControlNames.WetnessControlSSR, toolTip, "Toggle wetness control for screen space reflections.");
            ControlFactory.SetTooltip(ControlNames.Decal, toolTip, "Toggle decal rendering.");
            ControlFactory.SetTooltip(ControlNames.TwoSided, toolTip, "Renders both sides of all faces of the mesh (double sided).");
            ControlFactory.SetTooltip(ControlNames.DecalNoFade, toolTip, "Toggle decal rendering without fade.");
            ControlFactory.SetTooltip(ControlNames.NonOccluder, toolTip, "Don't perform occlusion (line-of-sight).");
            ControlFactory.SetTooltip(ControlNames.Refraction, toolTip, "Toggle refraction of light.");
            ControlFactory.SetTooltip(ControlNames.RefractionFalloff, toolTip, "Toggles refraction falloff.");
            ControlFactory.SetTooltip(ControlNames.RefractionPower, toolTip, "Power of the refraction.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMapping, toolTip, "Toggle environment mapping.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMaskScale, toolTip, "Scale for the environment mask.");
            ControlFactory.SetTooltip(ControlNames.DepthBias, toolTip, "Toggle depth bias to prevent z-fighting.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleToPaletteColor, toolTip, "Toggle mapping of grayscale to palette colors.");
            ControlFactory.SetTooltip(ControlNames.MaskWrites, toolTip, "Masks writing of certain lighting properties.");

            ControlFactory.SetTooltip(ControlNames.Diffuse, toolTip, "Diffuse texture slot.");
            ControlFactory.SetTooltip(ControlNames.Normal, toolTip, "Normal map slot.");
            ControlFactory.SetTooltip(ControlNames.SmoothSpec, toolTip, "Smoothness/specular mask slot.");
            ControlFactory.SetTooltip(ControlNames.Greyscale, toolTip, "Greyscale (palette/lookup/heightmap) texture slot.");
            ControlFactory.SetTooltip(ControlNames.Environment, toolTip, "Environment map slot.");
            ControlFactory.SetTooltip(ControlNames.Glow, toolTip, "Glow map or other specialty slot.");
            ControlFactory.SetTooltip(ControlNames.InnerLayer, toolTip, "Inner layer mask slot.");
            ControlFactory.SetTooltip(ControlNames.Wrinkles, toolTip, "Wrinkles texture slot.");
            ControlFactory.SetTooltip(ControlNames.Displacement, toolTip, "Displacement texture slot.");
            ControlFactory.SetTooltip(ControlNames.Specular, toolTip, "PBR specular texture slot.");
            ControlFactory.SetTooltip(ControlNames.Lighting, toolTip, "PBR lighting texture slot.");
            ControlFactory.SetTooltip(ControlNames.Flow, toolTip, "PBR flow texture slot.");
            ControlFactory.SetTooltip(ControlNames.DistanceFieldAlpha, toolTip, "Distance field alpha texture slot.");
            ControlFactory.SetTooltip(ControlNames.EnableEditorAlphaRef, toolTip, "Toggle editor alpha testing reference.");
            ControlFactory.SetTooltip(ControlNames.RimLighting, toolTip, "Toggle rim lighting effect.");
            ControlFactory.SetTooltip(ControlNames.RimPower, toolTip, "Power of the rim lighting.");
            ControlFactory.SetTooltip(ControlNames.BacklightPower, toolTip, "Power of the back lighting.");
            ControlFactory.SetTooltip(ControlNames.SubsurfaceLighting, toolTip, "Toggle subsurface lighting effect.");
            ControlFactory.SetTooltip(ControlNames.SubsurfaceLightingRolloff, toolTip, "Rolloff of the subsurface lighting.");
            ControlFactory.SetTooltip(ControlNames.Translucency, toolTip, "Toggle translucency simulation.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyThickObject, toolTip, "Object on which the material is applied is thick (or a billboard if not). Used for correct shadowing.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyAlbSubsurfColor, toolTip, "Multiply the subsurface color with the albedo instead of just outputting the specified color.");
            ControlFactory.SetTooltip(ControlNames.TranslucencySubsurfaceColor, toolTip, "Color tint of the subsurface matter.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyTransmissiveScale, toolTip, "Simulate amount of light collision inside the material.");
            ControlFactory.SetTooltip(ControlNames.TranslucencyTurbulence, toolTip, "Turbulence for translucency.");
            ControlFactory.SetTooltip(ControlNames.SpecularEnabled, toolTip, "Toggle specular effect.");
            ControlFactory.SetTooltip(ControlNames.SpecularColor, toolTip, "Color for the specular effect.");
            ControlFactory.SetTooltip(ControlNames.SpecularMultiplier, toolTip, "Multiplier for the specular effect.");
            ControlFactory.SetTooltip(ControlNames.Smoothness, toolTip, "Smoothness of the specular effect.");
            ControlFactory.SetTooltip(ControlNames.FresnelPower, toolTip, "Power of the fresnel reflection and transmission (specular).");
            ControlFactory.SetTooltip(ControlNames.WetSpecScale, toolTip, "Scale of the wetness specular.");
            ControlFactory.SetTooltip(ControlNames.WetSpecPowerScale, toolTip, "Power scale of the wetness specular.");
            ControlFactory.SetTooltip(ControlNames.WetSpecMinVar, toolTip, "Minimum variance of the wetness specular.");
            ControlFactory.SetTooltip(ControlNames.WetEnvMapScale, toolTip, "Environment map scale of the wetness effect.");
            ControlFactory.SetTooltip(ControlNames.WetFresnelPower, toolTip, "Fresnel power of the wetness effect.");
            ControlFactory.SetTooltip(ControlNames.WetMetalness, toolTip, "Metalness of the wetness effect.");
            ControlFactory.SetTooltip(ControlNames.PBR, toolTip, "Enables native PBR rendering. Requires diffuse, normal, specular and lighting texture (flow optional).");
            ControlFactory.SetTooltip(ControlNames.CustomPorosity, toolTip, "Toggle custom porosity for PBR.");
            ControlFactory.SetTooltip(ControlNames.PorosityValue, toolTip, "Custom porosity value for PBR.");
            ControlFactory.SetTooltip(ControlNames.RootMaterialPath, toolTip, "Template/root file of the current material.");
            ControlFactory.SetTooltip(ControlNames.AnisoLighting, toolTip, "Toggle anisotropic lighting.");
            ControlFactory.SetTooltip(ControlNames.EmittanceEnabled, toolTip, "Toggle emittance effect.");
            ControlFactory.SetTooltip(ControlNames.EmittanceColor, toolTip, "Color for the emittance effect.");
            ControlFactory.SetTooltip(ControlNames.EmittanceMultiplier, toolTip, "Multiplier for the emittance effect.");
            ControlFactory.SetTooltip(ControlNames.ModelSpaceNormals, toolTip, "Toggle model space normals rendering.");
            ControlFactory.SetTooltip(ControlNames.ExternalEmittance, toolTip, "Toggle external emittance effect.");
            ControlFactory.SetTooltip(ControlNames.LumEmittance, toolTip, "Luminous emittance value (in Lux) of the luminous flux emitted from the surface.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissive, toolTip, "Use stable emissive over physically based emittance. If unchecked, uses luminous emittance.");
            ControlFactory.SetTooltip(ControlNames.AdaptEmissiveExposureOffset, toolTip, "Exposure offset applied while exposing the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptEmissiveFinalExposureMin, toolTip, "Minimum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptEmissiveFinalExposureMax, toolTip, "Maximum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.BackLighting, toolTip, "Toggle back lighting effect.");
            ControlFactory.SetTooltip(ControlNames.ReceiveShadows, toolTip, "Toggle if this mesh receives shadows.");
            ControlFactory.SetTooltip(ControlNames.HideSecret, toolTip, "Toggle hide secret.");
            ControlFactory.SetTooltip(ControlNames.CastShadows, toolTip, "Toggle shadow casting for this mesh.");
            ControlFactory.SetTooltip(ControlNames.DissolveFade, toolTip, "Toggle dissolve fade.");
            ControlFactory.SetTooltip(ControlNames.AssumeShadowmask, toolTip, "Toggle assuming shadowmask.");
            ControlFactory.SetTooltip(ControlNames.Glowmap, toolTip, "Toggle making use of a glowmap for emittance.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMapWindow, toolTip, "Toggle environment map window.");
            ControlFactory.SetTooltip(ControlNames.EnvironmentMapEye, toolTip, "Toggle environment map eye.");
            ControlFactory.SetTooltip(ControlNames.Hair, toolTip, "Toggle hair rendering.");
            ControlFactory.SetTooltip(ControlNames.HairTintColor, toolTip, "Color for the hair tinting.");
            ControlFactory.SetTooltip(ControlNames.Tree, toolTip, "Toggle tree rendering.");
            ControlFactory.SetTooltip(ControlNames.Facegen, toolTip, "Toggle facegen rendering.");
            ControlFactory.SetTooltip(ControlNames.SkinTint, toolTip, "Toggle skin tint rendering.");
            ControlFactory.SetTooltip(ControlNames.Tessellate, toolTip, "Toggle tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.DisplacementTexBias, toolTip, "Bias for the displacement texture.");
            ControlFactory.SetTooltip(ControlNames.DisplacementTexScale, toolTip, "Scale for the displacement texture.");
            ControlFactory.SetTooltip(ControlNames.TessellationPNScale, toolTip, "PN (point normal) scale for the tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.TessellationBaseFactor, toolTip, "Base factor for the tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.TessellationFadeDistance, toolTip, "Fade distance for the tessellation effect.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleToPaletteScale, toolTip, "Scale for the grayscale to palette mapping.");
            ControlFactory.SetTooltip(ControlNames.SkewSpecularAlpha, toolTip, "Toggle skew specular alpha.");
            ControlFactory.SetTooltip(ControlNames.Terrain, toolTip, "Toggle terrain rendering.");
            ControlFactory.SetTooltip(ControlNames.UnkInt1BGSM, toolTip, "Unknown value.");
            ControlFactory.SetTooltip(ControlNames.TerrainThresholdFalloff, toolTip, "Softness of the terrain blending.");
            ControlFactory.SetTooltip(ControlNames.TerrainTilingDistance, toolTip, "Tiling distance of the terrain.");
            ControlFactory.SetTooltip(ControlNames.TerrainRotationAngle, toolTip, "Rotation angle of the terrain.");

            ControlFactory.SetTooltip(ControlNames.BaseTexture, toolTip, "Base texture slot.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleTexture, toolTip, "Grayscale texture slot.");
            ControlFactory.SetTooltip(ControlNames.EnvmapTexture, toolTip, "Environment map slot.");
            ControlFactory.SetTooltip(ControlNames.NormalTexture, toolTip, "Normal map slot.");
            ControlFactory.SetTooltip(ControlNames.EnvmapMaskTexture, toolTip, "Environment map mask slot.");
            ControlFactory.SetTooltip(ControlNames.SpecularTexture, toolTip, "PBR specular texture slot.");
            ControlFactory.SetTooltip(ControlNames.LightingTexture, toolTip, "PBR lighting texture slot.");
            ControlFactory.SetTooltip(ControlNames.GlowTexture, toolTip, "PBR emissive palette slot.");
            ControlFactory.SetTooltip(ControlNames.EnvMapping, toolTip, "Toggle environment mapping effect.");
            ControlFactory.SetTooltip(ControlNames.EnvMappingMaskScale, toolTip, "Scale for the environment mapping mask.");
            ControlFactory.SetTooltip(ControlNames.BloodEnabled, toolTip, "Toggle blood rendering.");
            ControlFactory.SetTooltip(ControlNames.EffectLightingEnabled, toolTip, "Toggle effect lighting.");
            ControlFactory.SetTooltip(ControlNames.FalloffEnabled, toolTip, "Toggle falloff settings driving alpha.");
            ControlFactory.SetTooltip(ControlNames.FalloffColorEnabled, toolTip, "Toggle falloff settings driving color.");
            ControlFactory.SetTooltip(ControlNames.GrayscaleToPaletteAlpha, toolTip, "Toggle grayscale to palette alpha mapping.");
            ControlFactory.SetTooltip(ControlNames.SoftEnabled, toolTip, "Toggle softness effect.");
            ControlFactory.SetTooltip(ControlNames.BaseColor, toolTip, "Base color of the effect.");
            ControlFactory.SetTooltip(ControlNames.BaseColorScale, toolTip, "Scale of the base color.");
            ControlFactory.SetTooltip(ControlNames.FalloffStartAngle, toolTip, "Start angle of the falloff.");
            ControlFactory.SetTooltip(ControlNames.FalloffStopAngle, toolTip, "Stop angle of the falloff.");
            ControlFactory.SetTooltip(ControlNames.FalloffStartOpacity, toolTip, "Start opacity of the falloff.");
            ControlFactory.SetTooltip(ControlNames.FalloffStopOpacity, toolTip, "Stop opacity of the falloff.");
            ControlFactory.SetTooltip(ControlNames.LightingInfluence, toolTip, "Lighting influence value of the material.");
            ControlFactory.SetTooltip(ControlNames.EnvmapMinLOD, toolTip, "Minimum LOD for environment mapping.");
            ControlFactory.SetTooltip(ControlNames.SoftDepth, toolTip, "Softness depth value of the material.");
            ControlFactory.SetTooltip(ControlNames.EmitColor, toolTip, "Color for the PBR emittance effect.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissiveExposureOffset, toolTip, "Exposure offset applied while exposing the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissiveFinalExposureMin, toolTip, "Minimum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.AdaptativeEmissiveFinalExposureMax, toolTip, "Maximum amount of exposure on the emissive object.");
            ControlFactory.SetTooltip(ControlNames.EffectGlowmap, toolTip, "Toggle glowmap.");
            ControlFactory.SetTooltip(ControlNames.EffectPBRSpecular, toolTip, "Toggle PBR specular effect.");

            ControlFactory.SetTooltip(ControlNames.GlassRoughnessScratch, toolTip, "Path to the roughness(R) and scratch(G) texture.");
            ControlFactory.SetTooltip(ControlNames.GlassDirtOverlay, toolTip, "Path to the dirt overlay texture.");

            ControlFactory.SetTooltip(ControlNames.GlassEnabled, toolTip, "Glass rendering enabled");
            ControlFactory.SetTooltip(ControlNames.GlassFresnelColor, toolTip, "Glass fresnel color.");
            ControlFactory.SetTooltip(ControlNames.GlassBlurScaleBase, toolTip, "Possibly glass blur scale base. Might be a different property.");
            ControlFactory.SetTooltip(ControlNames.GlassBlurScaleFactor, toolTip, "Possibly glass blur scale factor. Might be a different property.");
            ControlFactory.SetTooltip(ControlNames.GlassRefractionScaleBase, toolTip, "Possibly glass refraction scale base. Might be a different property.");
        }

        private void GetMaterialValues(BaseMaterialFile file)
        {
            CustomControl control;

            if (currentMaterial != null)
            {
                file.Version = currentMaterial.Version;
            }
            else
            {
                switch (config.GameVersion)
                {
                    case Game.FO4:
                        file.Version = DefaultVersionFO4;
                        break;

                    case Game.FO76:
                        file.Version = DefaultVersionFO76;
                        break;
                }
            }

            control = ControlFactory.Find(ControlNames.TileU);
            if (control != null) file.TileU = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.TileV);
            if (control != null) file.TileV = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.OffsetU);
            if (control != null) file.UOffset = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.OffsetV);
            if (control != null) file.VOffset = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.ScaleU);
            if (control != null) file.UScale = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.ScaleV);
            if (control != null) file.VScale = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.Alpha);
            if (control != null) file.Alpha = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.AlphaBlendMode);
            if (control != null) file.AlphaBlendMode = (BaseMaterialFile.AlphaBlendModeType)Convert.ToInt32(control.GetProperty());

            control = ControlFactory.Find(ControlNames.AlphaTestReference);
            if (control != null) file.AlphaTestRef = Convert.ToByte(control.GetProperty());

            control = ControlFactory.Find(ControlNames.AlphaTest);
            if (control != null) file.AlphaTest = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.ZBufferWrite);
            if (control != null) file.ZBufferWrite = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.ZBufferTest);
            if (control != null) file.ZBufferTest = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.ScreenSpaceReflections);
            if (control != null) file.ScreenSpaceReflections = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.WetnessControlSSR);
            if (control != null) file.WetnessControlScreenSpaceReflections = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.Decal);
            if (control != null) file.Decal = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.TwoSided);
            if (control != null) file.TwoSided = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.DecalNoFade);
            if (control != null) file.DecalNoFade = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.NonOccluder);
            if (control != null) file.NonOccluder = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.Refraction);
            if (control != null) file.Refraction = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.RefractionFalloff);
            if (control != null) file.RefractionFalloff = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.RefractionPower);
            if (control != null) file.RefractionPower = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.EnvironmentMapping);
            if (control != null && control.Serialize) file.EnvironmentMapping = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.EnvironmentMaskScale);
            if (control != null && control.Serialize) file.EnvironmentMappingMaskScale = Convert.ToSingle(control.GetProperty());

            control = ControlFactory.Find(ControlNames.DepthBias);
            if (control != null) file.DepthBias = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.GrayscaleToPaletteColor);
            if (control != null) file.GrayscaleToPaletteColor = Convert.ToBoolean(control.GetProperty());

            control = ControlFactory.Find(ControlNames.MaskWrites);
            if (control != null) file.MaskWrites = (BaseMaterialFile.MaskWriteFlags)control.GetProperty();

            if (file.GetType() == typeof(BGSM))
            {
                BGSM bgsm = (BGSM)file;

                control = ControlFactory.Find(ControlNames.Diffuse);
                if (control != null) bgsm.DiffuseTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Normal);
                if (control != null) bgsm.NormalTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SmoothSpec);
                if (control != null) bgsm.SmoothSpecTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Greyscale);
                if (control != null) bgsm.GreyscaleTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Environment);
                if (control != null) bgsm.EnvmapTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Glow);
                if (control != null) bgsm.GlowTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.InnerLayer);
                if (control != null) bgsm.InnerLayerTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Wrinkles);
                if (control != null) bgsm.WrinklesTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Displacement);
                if (control != null) bgsm.DisplacementTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Specular);
                if (control != null) bgsm.SpecularTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Lighting);
                if (control != null) bgsm.LightingTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Flow);
                if (control != null) bgsm.FlowTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.DistanceFieldAlpha);
                if (control != null) bgsm.DistanceFieldAlphaTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EnableEditorAlphaRef);
                if (control != null) bgsm.EnableEditorAlphaRef = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Translucency);
                if (control != null) bgsm.Translucency = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TranslucencyThickObject);
                if (control != null) bgsm.TranslucencyThickObject = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TranslucencyAlbSubsurfColor);
                if (control != null) bgsm.TranslucencyMixAlbedoWithSubsurfaceColor = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TranslucencySubsurfaceColor);
                if (control != null) bgsm.TranslucencySubsurfaceColor = (uint)((Color)control.GetProperty()).ToArgb();

                control = ControlFactory.Find(ControlNames.TranslucencyTransmissiveScale);
                if (control != null) bgsm.TranslucencyTransmissiveScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TranslucencyTurbulence);
                if (control != null) bgsm.TranslucencyTurbulence = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.RimLighting);
                if (control != null) bgsm.RimLighting = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.RimPower);
                if (control != null) bgsm.RimPower = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.BacklightPower);
                if (control != null) bgsm.BackLightPower = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SubsurfaceLighting);
                if (control != null) bgsm.SubsurfaceLighting = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SubsurfaceLightingRolloff);
                if (control != null) bgsm.SubsurfaceLightingRolloff = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SpecularEnabled);
                if (control != null) bgsm.SpecularEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SpecularColor);
                if (control != null) bgsm.SpecularColor = (uint)((Color)control.GetProperty()).ToArgb();

                control = ControlFactory.Find(ControlNames.SpecularMultiplier);
                if (control != null) bgsm.SpecularMult = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Smoothness);
                if (control != null) bgsm.Smoothness = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FresnelPower);
                if (control != null) bgsm.FresnelPower = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.WetSpecScale);
                if (control != null) bgsm.WetnessControlSpecScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.WetSpecPowerScale);
                if (control != null) bgsm.WetnessControlSpecPowerScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.WetSpecMinVar);
                if (control != null) bgsm.WetnessControlSpecMinvar = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.WetEnvMapScale);
                if (control != null) bgsm.WetnessControlEnvMapScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.WetFresnelPower);
                if (control != null) bgsm.WetnessControlFresnelPower = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.WetMetalness);
                if (control != null) bgsm.WetnessControlMetalness = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.PBR);
                if (control != null) bgsm.PBR = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.CustomPorosity);
                if (control != null) bgsm.CustomPorosity = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.PorosityValue);
                if (control != null) bgsm.PorosityValue = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.RootMaterialPath);
                if (control != null) bgsm.RootMaterialPath = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AnisoLighting);
                if (control != null) bgsm.AnisoLighting = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EmittanceEnabled);
                if (control != null) bgsm.EmitEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EmittanceColor);
                if (control != null) bgsm.EmittanceColor = (uint)((Color)control.GetProperty()).ToArgb();

                control = ControlFactory.Find(ControlNames.EmittanceMultiplier);
                if (control != null) bgsm.EmittanceMult = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.ModelSpaceNormals);
                if (control != null) bgsm.ModelSpaceNormals = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.ExternalEmittance);
                if (control != null) bgsm.ExternalEmittance = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.LumEmittance);
                if (control != null) bgsm.LumEmittance = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AdaptativeEmissive);
                if (control != null) bgsm.UseAdaptativeEmissive = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AdaptEmissiveExposureOffset);
                if (control != null) bgsm.AdaptativeEmissive_ExposureOffset = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AdaptEmissiveFinalExposureMin);
                if (control != null) bgsm.AdaptativeEmissive_FinalExposureMin = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AdaptEmissiveFinalExposureMax);
                if (control != null) bgsm.AdaptativeEmissive_FinalExposureMax = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.BackLighting);
                if (control != null) bgsm.BackLighting = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.ReceiveShadows);
                if (control != null) bgsm.ReceiveShadows = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.HideSecret);
                if (control != null) bgsm.HideSecret = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.CastShadows);
                if (control != null) bgsm.CastShadows = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.DissolveFade);
                if (control != null) bgsm.DissolveFade = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AssumeShadowmask);
                if (control != null) bgsm.AssumeShadowmask = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Glowmap);
                if (control != null) bgsm.Glowmap = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EnvironmentMapWindow);
                if (control != null) bgsm.EnvironmentMappingWindow = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EnvironmentMapEye);
                if (control != null) bgsm.EnvironmentMappingEye = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Hair);
                if (control != null) bgsm.Hair = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.HairTintColor);
                if (control != null) bgsm.HairTintColor = (uint)((Color)control.GetProperty()).ToArgb();

                control = ControlFactory.Find(ControlNames.Tree);
                if (control != null) bgsm.Tree = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Facegen);
                if (control != null) bgsm.Facegen = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SkinTint);
                if (control != null) bgsm.SkinTint = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Tessellate);
                if (control != null) bgsm.Tessellate = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.DisplacementTexBias);
                if (control != null) bgsm.DisplacementTextureBias = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.DisplacementTexScale);
                if (control != null) bgsm.DisplacementTextureScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TessellationPNScale);
                if (control != null) bgsm.TessellationPnScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TessellationBaseFactor);
                if (control != null) bgsm.TessellationBaseFactor = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TessellationFadeDistance);
                if (control != null) bgsm.TessellationFadeDistance = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GrayscaleToPaletteScale);
                if (control != null) bgsm.GrayscaleToPaletteScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SkewSpecularAlpha);
                if (control != null) bgsm.SkewSpecularAlpha = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.Terrain);
                if (control != null) bgsm.Terrain = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.UnkInt1BGSM);
                if (control != null) bgsm.UnkInt1 = Convert.ToUInt32(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TerrainThresholdFalloff);
                if (control != null) bgsm.TerrainThresholdFalloff = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TerrainTilingDistance);
                if (control != null) bgsm.TerrainTilingDistance = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.TerrainRotationAngle);
                if (control != null) bgsm.TerrainRotationAngle = Convert.ToSingle(control.GetProperty());
            }
            else if (file.GetType() == typeof(BGEM))
            {
                BGEM bgem = (BGEM)file;

                control = ControlFactory.Find(ControlNames.BaseTexture);
                if (control != null) bgem.BaseTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GrayscaleTexture);
                if (control != null) bgem.GrayscaleTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EnvmapTexture);
                if (control != null) bgem.EnvmapTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.NormalTexture);
                if (control != null) bgem.NormalTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EnvmapMaskTexture);
                if (control != null) bgem.EnvmapMaskTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SpecularTexture);
                if (control != null) bgem.SpecularTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.LightingTexture);
                if (control != null) bgem.LightingTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GlowTexture);
                if (control != null) bgem.GlowTexture = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GlassRoughnessScratch);
                if (control != null) bgem.GlassRoughnessScratch = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GlassDirtOverlay);
                if (control != null) bgem.GlassDirtOverlay = Convert.ToString(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GlassEnabled);
                if (control != null) bgem.GlassEnabled = Convert.ToBoolean(control.GetProperty());
                control = ControlFactory.Find(ControlNames.GlassFresnelColor);
                if (control != null) bgem.GlassFresnelColor = (uint)((Color)control.GetProperty()).ToArgb();
                control = ControlFactory.Find(ControlNames.GlassBlurScaleBase);
                if (control != null) bgem.GlassBlurScaleBase = Convert.ToSingle(control.GetProperty());
                control = ControlFactory.Find(ControlNames.GlassBlurScaleFactor);
                if (control != null) bgem.GlassBlurScaleFactor = Convert.ToSingle(control.GetProperty());
                control = ControlFactory.Find(ControlNames.GlassRefractionScaleBase);
                if (control != null) bgem.GlassRefractionScaleBase = Convert.ToSingle(control.GetProperty());

                if (file.Version >= 10)
                {
                    control = ControlFactory.Find(ControlNames.EnvMapping);
                    if (control != null && control.Serialize) bgem.EnvironmentMapping = Convert.ToBoolean(control.GetProperty());

                    control = ControlFactory.Find(ControlNames.EnvMappingMaskScale);
                    if (control != null && control.Serialize) bgem.EnvironmentMappingMaskScale = Convert.ToSingle(control.GetProperty());
                }

                control = ControlFactory.Find(ControlNames.BloodEnabled);
                if (control != null) bgem.BloodEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EffectLightingEnabled);
                if (control != null) bgem.EffectLightingEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FalloffEnabled);
                if (control != null) bgem.FalloffEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FalloffColorEnabled);
                if (control != null) bgem.FalloffColorEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.GrayscaleToPaletteAlpha);
                if (control != null) bgem.GrayscaleToPaletteAlpha = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SoftEnabled);
                if (control != null) bgem.SoftEnabled = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.BaseColor);
                if (control != null) bgem.BaseColor = (uint)((Color)control.GetProperty()).ToArgb();

                control = ControlFactory.Find(ControlNames.BaseColorScale);
                if (control != null) bgem.BaseColorScale = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FalloffStartAngle);
                if (control != null) bgem.FalloffStartAngle = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FalloffStopAngle);
                if (control != null) bgem.FalloffStopAngle = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FalloffStartOpacity);
                if (control != null) bgem.FalloffStartOpacity = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.FalloffStopOpacity);
                if (control != null) bgem.FalloffStopOpacity = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.LightingInfluence);
                if (control != null) bgem.LightingInfluence = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EnvmapMinLOD);
                if (control != null) bgem.EnvmapMinLOD = Convert.ToByte(control.GetProperty());

                control = ControlFactory.Find(ControlNames.SoftDepth);
                if (control != null) bgem.SoftDepth = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EmitColor);
                if (control != null) bgem.EmittanceColor = (uint)((Color)control.GetProperty()).ToArgb();

                control = ControlFactory.Find(ControlNames.AdaptativeEmissiveExposureOffset);
                if (control != null) bgem.AdaptativeEmissive_ExposureOffset = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AdaptativeEmissiveFinalExposureMin);
                if (control != null) bgem.AdaptativeEmissive_FinalExposureMin = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.AdaptativeEmissiveFinalExposureMax);
                if (control != null) bgem.AdaptativeEmissive_FinalExposureMax = Convert.ToSingle(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EffectGlowmap);
                if (control != null) bgem.Glowmap = Convert.ToBoolean(control.GetProperty());

                control = ControlFactory.Find(ControlNames.EffectPBRSpecular);
                if (control != null) bgem.EffectPbrSpecular = Convert.ToBoolean(control.GetProperty());
            }
        }

        private void OpenMaterial(string fileName, uint signature)
        {
            BaseMaterialFile material;
            if (signature == BGSM.Signature)
                material = new BGSM();
            else if (signature == BGEM.Signature)
                material = new BGEM();
            else
                return;

            using FileStream file = new(fileName, FileMode.Open);
            char start = Convert.ToChar(file.ReadByte());
            file.Position = 0;

            // Check for JSON
            if (start == '{' || start == '[')
            {
                try
                {
                    var ser = new DataContractJsonSerializer(material.GetType());
                    if (signature == BGSM.Signature)
                        material = (BGSM)ser.ReadObject(file);
                    else if (signature == BGEM.Signature)
                        material = (BGEM)ser.ReadObject(file);
                }
                catch (Exception) { }
            }
            // Try binary
            else if (!material.Open(file))
            {
                MessageBox.Show(string.Format("Failed to open file '{0}'!", fileName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (material.Version > 22)
            {
                MessageBox.Show($"Version {material.Version} not currently supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            workFilePath = fileName;

            SuspendAll();
            ControlFactory.ClearControls();

            CreateMaterialControls(material);

            if (currentMaterial.Version > 2 && currentMaterial.Version <= 22)
                SetGameSelection(Game.FO76);
            else
                SetGameSelection(Game.FO4);

            if (signature == BGSM.Signature)
                SetMaterialTypeSelection(MaterialType.Material);
            else if (signature == BGEM.Signature)
                SetMaterialTypeSelection(MaterialType.Effect);

            FillVersionDropdown();
            UpdateTopLevelSectionVisibility();
            generalPageSection?.SetCollapsed(true);
            ResumeAll();

            saveToolStripMenuItem.Enabled = true;
            saveAsToolStripMenuItem.Enabled = true;
            closeToolStripMenuItem.Enabled = true;
            layoutGeneral.Enabled = true;
            layoutMaterial.Enabled = true;
            layoutEffect.Enabled = true;

            Text = GetTitleText();
            changed = false;
        }
        #endregion
    }
}

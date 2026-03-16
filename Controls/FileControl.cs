using System;
using System.Windows.Forms;

namespace Material_Editor.Controls
{
    public partial class FileControl : CustomControl
    {
        private Label lbLabel;
        private TextBox tbFile;
        private Button btFile;
        private OpenFileDialog textureFileDialog;
        private OpenFileDialog materialFileDialog;

        public enum FileType
        {
            Texture,
            Material
        }

        public FileType CurrentFileType;

        public FileControl(string label, System.Drawing.Font font, Func<CustomControl, bool> visibilityCallback, Action<CustomControl> changedCallback, FileType fileType = FileType.Texture, string initialPath = "") : base(label)
        {
            lbLabel.Text = label;
            CurrentFileType = fileType;

            tbFile.Font = font;
            tbFile.Text = initialPath;
            tbFile.PlaceholderText = fileType switch
            {
                FileType.Texture => "<no texture>",
                FileType.Material => "<no material>",
                _ => "<no file>",
            };

            VisibilityCallback = visibilityCallback;
            ChangedCallback = changedCallback;
        }

        public override void CreateControls()
        {
            lbLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                AutoSize = true,
                Name = "lbLabel",
                Text = "Label",
                Tag = this
            };

            tbFile = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                MaxLength = 260,
                Name = "tbFile",
                TabIndex = 0,
                Tag = this
            };
            AppearanceApplicator.SetFontRole(tbFile, AppearanceFontRole.Monospace);
            tbFile.TextChanged += new EventHandler(TbFile_TextChanged);

            btFile = new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Name = "btFile",
                TabStop = false,
                Text = "...",
                Padding = new Padding(6, 0, 6, 0),
                Tag = this
            };
            btFile.Click += new EventHandler(BtFile_Click);

            textureFileDialog = new OpenFileDialog
            {
                DefaultExt = "dds",
                Filter = "Texture File (*.dds;*.tga)|*.dds;*.tga",
                Title = "Choose a texture file..."
            };

            materialFileDialog = new OpenFileDialog
            {
                DefaultExt = "bgsm",
                Filter = "Material File (*.bgsm;*.bgem)|*.bgsm;*.bgem",
                Title = "Choose a material file..."
            };
        }

        private void TbFile_TextChanged(object sender, EventArgs e)
        {
            InvokeChangedCallback();
        }

        private void BtFile_Click(object sender, EventArgs e)
        {
            string fileName = "";
            string rootFolderName = "";
            string displayRootName = "";

            switch (CurrentFileType)
            {
                case FileType.Texture:
                    if (textureFileDialog.ShowDialog() != DialogResult.OK)
                        return;

                    fileName = textureFileDialog.FileName;
                    rootFolderName = "textures";
                    displayRootName = @"Textures\";
                    break;

                case FileType.Material:
                    if (materialFileDialog.ShowDialog() != DialogResult.OK)
                        return;

                    fileName = materialFileDialog.FileName;
                    rootFolderName = "materials";
                    displayRootName = @"Materials\";
                    break;
            }

            if (ContentPathHelper.TryGetRelativePath(fileName, rootFolderName, out string relativePath))
            {
                tbFile.Text = relativePath;
            }
            else
            {
                MessageBox.Show(
                    $"The selected file path does not contain '{displayRootName}'. This may not be a valid game-relative path, but you can continue.",
                    "Path Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                tbFile.Text = ContentPathHelper.NormalizeSeparators(fileName);
            }

            InvokeChangedCallback();
        }

        public override Label LabelControl
        {
            get { return lbLabel; }
        }

        public override Control Control
        {
            get { return tbFile; }
        }

        public override Control ExtraControl
        {
            get { return btFile; }
        }

        public override object GetProperty()
        {
            return tbFile.Text;
        }

        internal override void ApplyAppearance(AppearanceDefinition appearance)
        {
            base.ApplyAppearance(appearance);
            tbFile.Font = appearance.MonospaceFont;
        }
    }
}

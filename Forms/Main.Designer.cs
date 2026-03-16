namespace Material_Editor.Forms
{
    partial class Main
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menuStrip = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            serializeToJSONToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            generateVariationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            overwriteFilesByFieldToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            bulkMaterialEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openFileDialog = new System.Windows.Forms.OpenFileDialog();
            saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            colorDialog = new System.Windows.Forms.ColorDialog();
            topControlsLayout = new System.Windows.Forms.TableLayoutPanel();
            lbGame = new System.Windows.Forms.Label();
            panelGameToggle = new System.Windows.Forms.Panel();
            rbGameFO76 = new System.Windows.Forms.RadioButton();
            rbGameFO4 = new System.Windows.Forms.RadioButton();
            lbMaterialType = new System.Windows.Forms.Label();
            panelMaterialTypeToggle = new System.Windows.Forms.Panel();
            rbTypeEffect = new System.Windows.Forms.RadioButton();
            rbTypeMaterial = new System.Windows.Forms.RadioButton();
            lbVersion = new System.Windows.Forms.Label();
            listVersion = new System.Windows.Forms.ComboBox();
            contentScrollPanel = new System.Windows.Forms.Panel();
            contentHostLayout = new System.Windows.Forms.TableLayoutPanel();
            layoutGeneral = new System.Windows.Forms.TableLayoutPanel();
            layoutMaterial = new System.Windows.Forms.TableLayoutPanel();
            layoutEffect = new System.Windows.Forms.TableLayoutPanel();
            textureFileDialog = new System.Windows.Forms.OpenFileDialog();
            toolTip = new System.Windows.Forms.ToolTip(components);
            menuStrip.SuspendLayout();
            topControlsLayout.SuspendLayout();
            panelGameToggle.SuspendLayout();
            panelMaterialTypeToggle.SuspendLayout();
            contentScrollPanel.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, toolsToolStripMenuItem, optionsToolStripMenuItem, toolStripMenuItem1 });
            menuStrip.Location = new System.Drawing.Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new System.Drawing.Size(1280, 24);
            menuStrip.TabIndex = 0;
            menuStrip.Text = "menuStrip";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newToolStripMenuItem, openToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem, closeToolStripMenuItem, serializeToJSONToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N;
            newToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            newToolStripMenuItem.Text = "New";
            newToolStripMenuItem.Click += NewToolStripMenuItem_Click;
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O;
            openToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            openToolStripMenuItem.Text = "Open...";
            openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Enabled = false;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
            saveToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += SaveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Enabled = false;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.S;
            saveAsToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            saveAsToolStripMenuItem.Text = "Save As...";
            saveAsToolStripMenuItem.Click += SaveAsToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Enabled = false;
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W;
            closeToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            closeToolStripMenuItem.Text = "Close";
            closeToolStripMenuItem.Click += CloseToolStripMenuItem_Click;
            // 
            // serializeToJSONToolStripMenuItem
            // 
            serializeToJSONToolStripMenuItem.CheckOnClick = true;
            serializeToJSONToolStripMenuItem.Name = "serializeToJSONToolStripMenuItem";
            serializeToJSONToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.J;
            serializeToJSONToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            serializeToJSONToolStripMenuItem.Text = "Serialize to JSON";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4;
            exitToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { settingsToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            optionsToolStripMenuItem.Text = "Options";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new System.Drawing.Size(122, 22);
            settingsToolStripMenuItem.Text = "Settings...";
            settingsToolStripMenuItem.Click += SettingsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { aboutToolStripMenuItem });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(24, 20);
            toolStripMenuItem1.Text = "?";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += AboutToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { generateVariationsToolStripMenuItem, overwriteFilesByFieldToolStripMenuItem, bulkMaterialEditorToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // generateVariationsToolStripMenuItem
            // 
            generateVariationsToolStripMenuItem.Name = "generateVariationsToolStripMenuItem";
            generateVariationsToolStripMenuItem.Size = new System.Drawing.Size(257, 22);
            generateVariationsToolStripMenuItem.Text = "Generate Variations...";
            generateVariationsToolStripMenuItem.Click += GenerateVariationsToolStripMenuItem_Click;
            // 
            // overwriteFilesByFieldToolStripMenuItem
            // 
            overwriteFilesByFieldToolStripMenuItem.Name = "overwriteFilesByFieldToolStripMenuItem";
            overwriteFilesByFieldToolStripMenuItem.Size = new System.Drawing.Size(257, 22);
            overwriteFilesByFieldToolStripMenuItem.Text = "Overwrite Files by Field...";
            overwriteFilesByFieldToolStripMenuItem.Click += OverwriteFilesByFieldToolStripMenuItem_Click;
            // 
            // bulkMaterialEditorToolStripMenuItem
            // 
            bulkMaterialEditorToolStripMenuItem.Name = "bulkMaterialEditorToolStripMenuItem";
            bulkMaterialEditorToolStripMenuItem.Size = new System.Drawing.Size(257, 22);
            bulkMaterialEditorToolStripMenuItem.Text = "Bulk Material Editor...";
            bulkMaterialEditorToolStripMenuItem.Click += BulkMaterialEditorToolStripMenuItem_Click;
            // 
            // openFileDialog
            // 
            openFileDialog.Filter = "Material/Effect File (.bgsm; .bgem)|*.bgsm;*.bgem";
            openFileDialog.Title = "Choose a material file...";
            // 
            // saveFileDialog
            // 
            saveFileDialog.Filter = "Material/Effect File (.bgsm; .bgem)|*.bgsm;*.bgem";
            saveFileDialog.Title = "Save material file to...";
            // 
            // colorDialog
            // 
            colorDialog.FullOpen = true;
            // 
            // topControlsLayout
            // 
            topControlsLayout.ColumnCount = 8;
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            topControlsLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            topControlsLayout.Controls.Add(lbGame, 0, 0);
            topControlsLayout.Controls.Add(panelGameToggle, 1, 0);
            topControlsLayout.Controls.Add(lbMaterialType, 3, 0);
            topControlsLayout.Controls.Add(panelMaterialTypeToggle, 4, 0);
            topControlsLayout.Controls.Add(lbVersion, 6, 0);
            topControlsLayout.Controls.Add(listVersion, 7, 0);
            topControlsLayout.Dock = System.Windows.Forms.DockStyle.Top;
            topControlsLayout.Location = new System.Drawing.Point(0, 24);
            topControlsLayout.Name = "topControlsLayout";
            topControlsLayout.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            topControlsLayout.RowCount = 1;
            topControlsLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
            topControlsLayout.Size = new System.Drawing.Size(1280, 41);
            topControlsLayout.TabIndex = 1;
            // 
            // lbGame
            // 
            lbGame.Anchor = System.Windows.Forms.AnchorStyles.Left;
            lbGame.AutoSize = true;
            lbGame.Location = new System.Drawing.Point(11, 13);
            lbGame.Name = "lbGame";
            lbGame.Size = new System.Drawing.Size(41, 15);
            lbGame.TabIndex = 0;
            lbGame.Text = "Game:";
            // 
            // panelGameToggle
            // 
            panelGameToggle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            panelGameToggle.Controls.Add(rbGameFO76);
            panelGameToggle.Controls.Add(rbGameFO4);
            panelGameToggle.Location = new System.Drawing.Point(58, 8);
            panelGameToggle.Name = "panelGameToggle";
            panelGameToggle.Size = new System.Drawing.Size(140, 26);
            panelGameToggle.TabIndex = 1;
            // 
            // rbGameFO76
            // 
            rbGameFO76.Appearance = System.Windows.Forms.Appearance.Button;
            rbGameFO76.AutoSize = true;
            rbGameFO76.Location = new System.Drawing.Point(46, 0);
            rbGameFO76.Name = "rbGameFO76";
            rbGameFO76.Size = new System.Drawing.Size(43, 25);
            rbGameFO76.TabIndex = 1;
            rbGameFO76.TabStop = true;
            rbGameFO76.Text = "F76";
            rbGameFO76.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            rbGameFO76.UseVisualStyleBackColor = true;
            rbGameFO76.CheckedChanged += GameToggle_CheckedChanged;
            // 
            // rbGameFO4
            // 
            rbGameFO4.Appearance = System.Windows.Forms.Appearance.Button;
            rbGameFO4.AutoSize = true;
            rbGameFO4.Location = new System.Drawing.Point(0, 0);
            rbGameFO4.Name = "rbGameFO4";
            rbGameFO4.Size = new System.Drawing.Size(40, 25);
            rbGameFO4.TabIndex = 0;
            rbGameFO4.TabStop = true;
            rbGameFO4.Text = "FO4";
            rbGameFO4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            rbGameFO4.UseVisualStyleBackColor = true;
            rbGameFO4.CheckedChanged += GameToggle_CheckedChanged;
            // 
            // lbMaterialType
            // 
            lbMaterialType.Anchor = System.Windows.Forms.AnchorStyles.Left;
            lbMaterialType.AutoSize = true;
            lbMaterialType.Location = new System.Drawing.Point(231, 13);
            lbMaterialType.Name = "lbMaterialType";
            lbMaterialType.Size = new System.Drawing.Size(34, 15);
            lbMaterialType.TabIndex = 2;
            lbMaterialType.Text = "Type:";
            // 
            // panelMaterialTypeToggle
            // 
            panelMaterialTypeToggle.Anchor = System.Windows.Forms.AnchorStyles.Left;
            panelMaterialTypeToggle.Controls.Add(rbTypeEffect);
            panelMaterialTypeToggle.Controls.Add(rbTypeMaterial);
            panelMaterialTypeToggle.Location = new System.Drawing.Point(271, 8);
            panelMaterialTypeToggle.Name = "panelMaterialTypeToggle";
            panelMaterialTypeToggle.Size = new System.Drawing.Size(160, 26);
            panelMaterialTypeToggle.TabIndex = 3;
            // 
            // rbTypeEffect
            // 
            rbTypeEffect.Appearance = System.Windows.Forms.Appearance.Button;
            rbTypeEffect.AutoSize = true;
            rbTypeEffect.Location = new System.Drawing.Point(71, 0);
            rbTypeEffect.Name = "rbTypeEffect";
            rbTypeEffect.Size = new System.Drawing.Size(51, 25);
            rbTypeEffect.TabIndex = 1;
            rbTypeEffect.TabStop = true;
            rbTypeEffect.Text = "Effect";
            rbTypeEffect.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            rbTypeEffect.UseVisualStyleBackColor = true;
            rbTypeEffect.CheckedChanged += MaterialTypeToggle_CheckedChanged;
            // 
            // rbTypeMaterial
            // 
            rbTypeMaterial.Appearance = System.Windows.Forms.Appearance.Button;
            rbTypeMaterial.AutoSize = true;
            rbTypeMaterial.Location = new System.Drawing.Point(0, 0);
            rbTypeMaterial.Name = "rbTypeMaterial";
            rbTypeMaterial.Size = new System.Drawing.Size(65, 25);
            rbTypeMaterial.TabIndex = 0;
            rbTypeMaterial.TabStop = true;
            rbTypeMaterial.Text = "Material";
            rbTypeMaterial.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            rbTypeMaterial.UseVisualStyleBackColor = true;
            rbTypeMaterial.CheckedChanged += MaterialTypeToggle_CheckedChanged;
            // 
            // lbVersion
            // 
            lbVersion.Anchor = System.Windows.Forms.AnchorStyles.Left;
            lbVersion.AutoSize = true;
            lbVersion.Location = new System.Drawing.Point(1115, 13);
            lbVersion.Name = "lbVersion";
            lbVersion.Size = new System.Drawing.Size(69, 15);
            lbVersion.TabIndex = 4;
            lbVersion.Text = "File Version:";
            // 
            // listVersion
            // 
            listVersion.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            listVersion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            listVersion.FormattingEnabled = true;
            listVersion.Location = new System.Drawing.Point(1190, 9);
            listVersion.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            listVersion.Name = "listVersion";
            listVersion.Size = new System.Drawing.Size(82, 23);
            listVersion.TabIndex = 5;
            listVersion.SelectedIndexChanged += ListVersion_SelectedIndexChanged;
            // 
            // contentScrollPanel
            // 
            contentScrollPanel.AutoScroll = true;
            contentScrollPanel.Controls.Add(contentHostLayout);
            contentScrollPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            contentScrollPanel.Location = new System.Drawing.Point(0, 65);
            contentScrollPanel.Name = "contentScrollPanel";
            contentScrollPanel.Size = new System.Drawing.Size(1280, 795);
            contentScrollPanel.TabIndex = 2;
            // 
            // contentHostLayout
            // 
            contentHostLayout.AutoSize = true;
            contentHostLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            contentHostLayout.ColumnCount = 1;
            contentHostLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            contentHostLayout.Dock = System.Windows.Forms.DockStyle.Top;
            contentHostLayout.Location = new System.Drawing.Point(0, 0);
            contentHostLayout.Name = "contentHostLayout";
            contentHostLayout.Padding = new System.Windows.Forms.Padding(8);
            contentHostLayout.RowCount = 0;
            contentHostLayout.Size = new System.Drawing.Size(1280, 16);
            contentHostLayout.TabIndex = 0;
            // 
            // layoutGeneral
            // 
            layoutGeneral.AutoSize = true;
            layoutGeneral.ColumnCount = 3;
            layoutGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layoutGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            layoutGeneral.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layoutGeneral.Dock = System.Windows.Forms.DockStyle.Top;
            layoutGeneral.Location = new System.Drawing.Point(0, 0);
            layoutGeneral.Name = "layoutGeneral";
            layoutGeneral.Size = new System.Drawing.Size(1272, 777);
            layoutGeneral.TabIndex = 1;
            // 
            // layoutMaterial
            // 
            layoutMaterial.AutoSize = true;
            layoutMaterial.ColumnCount = 3;
            layoutMaterial.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layoutMaterial.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            layoutMaterial.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layoutMaterial.Dock = System.Windows.Forms.DockStyle.Top;
            layoutMaterial.Location = new System.Drawing.Point(0, 0);
            layoutMaterial.Name = "layoutMaterial";
            layoutMaterial.Size = new System.Drawing.Size(1272, 777);
            layoutMaterial.TabIndex = 1;
            // 
            // layoutEffect
            // 
            layoutEffect.AutoSize = true;
            layoutEffect.ColumnCount = 3;
            layoutEffect.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layoutEffect.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            layoutEffect.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            layoutEffect.Dock = System.Windows.Forms.DockStyle.Top;
            layoutEffect.Location = new System.Drawing.Point(0, 0);
            layoutEffect.Name = "layoutEffect";
            layoutEffect.Size = new System.Drawing.Size(1272, 777);
            layoutEffect.TabIndex = 1;
            // 
            // textureFileDialog
            // 
            textureFileDialog.DefaultExt = "dds";
            textureFileDialog.Filter = "Texture File (.dds)|*.dds";
            textureFileDialog.Title = "Choose a texture file...";
            // 
            // toolTip
            // 
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 500;
            toolTip.ReshowDelay = 100;
            toolTip.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            toolTip.ToolTipTitle = "Info";
            toolTip.Popup += ToolTip_Popup;
            // 
            // Main
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            AutoScroll = false;
            ClientSize = new System.Drawing.Size(1280, 860);
            Controls.Add(contentScrollPanel);
            Controls.Add(topControlsLayout);
            Controls.Add(menuStrip);
            DoubleBuffered = true;
            MainMenuStrip = menuStrip;
            MinimumSize = new System.Drawing.Size(1024, 640);
            Name = "Main";
            Text = "B.G.E.M.";
            FormClosing += Main_Closing;
            Load += Main_Load;
            ResizeBegin += Main_ResizeBegin;
            ResizeEnd += Main_ResizeEnd;
            DragDrop += Main_DragDrop;
            DragEnter += Main_DragEnter;
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            topControlsLayout.ResumeLayout(false);
            topControlsLayout.PerformLayout();
            panelGameToggle.ResumeLayout(false);
            panelGameToggle.PerformLayout();
            panelMaterialTypeToggle.ResumeLayout(false);
            panelMaterialTypeToggle.PerformLayout();
            contentScrollPanel.ResumeLayout(false);
            contentScrollPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem generateVariationsToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog textureFileDialog;
        private System.Windows.Forms.ToolStripMenuItem serializeToJSONToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel layoutGeneral;
        private System.Windows.Forms.TableLayoutPanel layoutEffect;
        private System.Windows.Forms.TableLayoutPanel layoutMaterial;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem overwriteFilesByFieldToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bulkMaterialEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ComboBox listVersion;
        private System.Windows.Forms.Label lbVersion;
        private System.Windows.Forms.TableLayoutPanel topControlsLayout;
        private System.Windows.Forms.Label lbGame;
        private System.Windows.Forms.Panel panelGameToggle;
        private System.Windows.Forms.RadioButton rbGameFO76;
        private System.Windows.Forms.RadioButton rbGameFO4;
        private System.Windows.Forms.Label lbMaterialType;
        private System.Windows.Forms.Panel panelMaterialTypeToggle;
        private System.Windows.Forms.RadioButton rbTypeEffect;
        private System.Windows.Forms.RadioButton rbTypeMaterial;
        private System.Windows.Forms.Panel contentScrollPanel;
        private System.Windows.Forms.TableLayoutPanel contentHostLayout;
    }
}

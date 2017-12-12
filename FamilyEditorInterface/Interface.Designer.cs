using System;
using System.Windows.Forms;
using System.Windows.Input;
namespace FamilyEditorInterface
{
    partial class Interface
    {
        private System.Windows.Forms.Button exitButton;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Interface));
            this.exitButton = new System.Windows.Forms.Button();
            this.setButton = new System.Windows.Forms.Button();
            this.mainContainer = new System.Windows.Forms.SplitContainer();
            this.mainPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitLine = new System.Windows.Forms.Label();
            this.maximizeIcon = new System.Windows.Forms.PictureBox();
            this.offPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.paramGroupBox = new System.Windows.Forms.GroupBox();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.mainContainer)).BeginInit();
            this.mainContainer.Panel1.SuspendLayout();
            this.mainContainer.Panel2.SuspendLayout();
            this.mainContainer.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maximizeIcon)).BeginInit();
            this.paramGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // exitButton
            // 
            this.exitButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.exitButton.Location = new System.Drawing.Point(277, 487);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(75, 21);
            this.exitButton.TabIndex = 5;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // setButton
            // 
            this.setButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.setButton.Location = new System.Drawing.Point(196, 487);
            this.setButton.Name = "setButton";
            this.setButton.Size = new System.Drawing.Size(75, 21);
            this.setButton.TabIndex = 4;
            this.setButton.Text = "Settings";
            this.setButton.UseVisualStyleBackColor = true;
            this.setButton.Click += new System.EventHandler(this.setButton_Click);
            // 
            // mainContainer
            // 
            this.mainContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainContainer.IsSplitterFixed = true;
            this.mainContainer.Location = new System.Drawing.Point(8, 21);
            this.mainContainer.Name = "mainContainer";
            this.mainContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // mainContainer.Panel1
            // 
            this.mainContainer.Panel1.Controls.Add(this.mainPanel);
            this.mainContainer.Panel1.Controls.Add(this.splitLine);
            this.mainContainer.Panel1.Controls.Add(this.maximizeIcon);
            this.mainContainer.Panel1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            // 
            // mainContainer.Panel2
            // 
            this.mainContainer.Panel2.AutoScroll = true;
            this.mainContainer.Panel2.Controls.Add(this.offPanel);
            this.mainContainer.Panel2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainContainer.Panel2Collapsed = true;
            this.mainContainer.Size = new System.Drawing.Size(324, 396);
            this.mainContainer.SplitterDistance = 300;
            this.mainContainer.TabIndex = 0;
            // 
            // mainPanel
            // 
            this.mainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainPanel.AutoScroll = true;
            this.mainPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.mainPanel.ContextMenuStrip = this.contextMenuStrip1;
            this.mainPanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mainPanel.Location = new System.Drawing.Point(0, 3);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(321, 358);
            this.mainPanel.TabIndex = 0;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.toolStripMenuItem1,
            this.saveToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(114, 70);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(113, 22);
            this.toolStripMenuItem1.Text = "Restore";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // splitLine
            // 
            this.splitLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitLine.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.splitLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitLine.Location = new System.Drawing.Point(2, 389);
            this.splitLine.Name = "splitLine";
            this.splitLine.Size = new System.Drawing.Size(319, 1);
            this.splitLine.TabIndex = 1;
            this.splitLine.Visible = false;
            // 
            // maximizeIcon
            // 
            this.maximizeIcon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.maximizeIcon.Image = global::FamilyEditorInterface.Properties.Resources.maximize;
            this.maximizeIcon.InitialImage = ((System.Drawing.Image)(resources.GetObject("maximizeIcon.InitialImage")));
            this.maximizeIcon.Location = new System.Drawing.Point(300, 367);
            this.maximizeIcon.Name = "maximizeIcon";
            this.maximizeIcon.Size = new System.Drawing.Size(30, 30);
            this.maximizeIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.maximizeIcon.TabIndex = 1;
            this.maximizeIcon.TabStop = false;
            this.maximizeIcon.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // offPanel
            // 
            this.offPanel.AutoScroll = true;
            this.offPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.offPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.offPanel.Location = new System.Drawing.Point(0, 0);
            this.offPanel.Margin = new System.Windows.Forms.Padding(0);
            this.offPanel.Name = "offPanel";
            this.offPanel.Size = new System.Drawing.Size(150, 46);
            this.offPanel.TabIndex = 1;
            // 
            // paramGroupBox
            // 
            this.paramGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.paramGroupBox.ContextMenuStrip = this.contextMenuStrip1;
            this.paramGroupBox.Controls.Add(this.mainContainer);
            this.paramGroupBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.paramGroupBox.Location = new System.Drawing.Point(12, 12);
            this.paramGroupBox.Name = "paramGroupBox";
            this.paramGroupBox.Size = new System.Drawing.Size(340, 423);
            this.paramGroupBox.TabIndex = 0;
            this.paramGroupBox.TabStop = false;
            this.paramGroupBox.Text = "Parameters";
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.logoPictureBox.Image = global::FamilyEditorInterface.Properties.Resources.archilizer_01;
            this.logoPictureBox.Location = new System.Drawing.Point(7, 473);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new System.Drawing.Size(153, 35);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 5;
            this.logoPictureBox.TabStop = false;
            // 
            // Interface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(364, 515);
            this.Controls.Add(this.setButton);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.paramGroupBox);
            this.Controls.Add(this.exitButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(380, 1440);
            this.MinimumSize = new System.Drawing.Size(380, 360);
            this.Name = "Interface";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Family Parameters ";
            this.GotFocus += new System.EventHandler(this.Control1_GotFocus);
            this.mainContainer.Panel1.ResumeLayout(false);
            this.mainContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainContainer)).EndInit();
            this.mainContainer.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.maximizeIcon)).EndInit();
            this.paramGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion        
        private PictureBox logoPictureBox;
        private Button setButton;
        private SplitContainer mainContainer;
        private FlowLayoutPanel offPanel;
        private Label splitLine;
        private FlowLayoutPanel mainPanel;
        private PictureBox maximizeIcon;
        private GroupBox paramGroupBox;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem toolStripMenuItem1;
        private ToolStripMenuItem refreshToolStripMenuItem;
        private ToolStripMenuItem saveToolStripMenuItem;
    }
}
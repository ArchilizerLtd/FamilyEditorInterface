using System;
using System.Windows.Forms;
using System.Windows.Input;
namespace FamilyEditorInterface
{
    partial class Interface
    {
        private System.Windows.Forms.Button btnExit;
        private Button refreshButtom;
        private FlowLayoutPanel mainPanel;
        private SplitContainer mainContainer;
        private PictureBox maximizeIcon;
        private FlowLayoutPanel offPanel;
        private Label splitLine;
        private Button defaultButton;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Interface));
            this.btnExit = new System.Windows.Forms.Button();
            this.refreshButtom = new System.Windows.Forms.Button();
            this.mainPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.mainContainer = new System.Windows.Forms.SplitContainer();
            this.splitLine = new System.Windows.Forms.Label();
            this.maximizeIcon = new System.Windows.Forms.PictureBox();
            this.offPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.defaultButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.mainContainer)).BeginInit();
            this.mainContainer.Panel1.SuspendLayout();
            this.mainContainer.Panel2.SuspendLayout();
            this.mainContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maximizeIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // btnExit
            // 
            this.btnExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExit.Location = new System.Drawing.Point(229, 513);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(100, 23);
            this.btnExit.TabIndex = 0;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // refreshButtom
            // 
            this.refreshButtom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.refreshButtom.Location = new System.Drawing.Point(15, 513);
            this.refreshButtom.Name = "refreshButtom";
            this.refreshButtom.Size = new System.Drawing.Size(100, 23);
            this.refreshButtom.TabIndex = 2;
            this.refreshButtom.Text = "Refresh ";
            this.refreshButtom.UseVisualStyleBackColor = true;
            this.refreshButtom.Click += new System.EventHandler(this.button1_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainPanel.AutoScroll = true;
            this.mainPanel.Location = new System.Drawing.Point(3, 3);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(315, 432);
            this.mainPanel.TabIndex = 0;
            // 
            // mainContainer
            // 
            this.mainContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainContainer.IsSplitterFixed = true;
            this.mainContainer.Location = new System.Drawing.Point(12, 20);
            this.mainContainer.Name = "mainContainer";
            this.mainContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // mainContainer.Panel1
            // 
            this.mainContainer.Panel1.Controls.Add(this.splitLine);
            this.mainContainer.Panel1.Controls.Add(this.mainPanel);
            this.mainContainer.Panel1.Controls.Add(this.maximizeIcon);
            // 
            // mainContainer.Panel2
            // 
            this.mainContainer.Panel2.AutoScroll = true;
            this.mainContainer.Panel2.Controls.Add(this.offPanel);
            this.mainContainer.Panel2Collapsed = true;
            this.mainContainer.Size = new System.Drawing.Size(320, 474);
            this.mainContainer.SplitterDistance = 330;
            this.mainContainer.TabIndex = 0;
            // 
            // splitLine
            // 
            this.splitLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitLine.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.splitLine.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitLine.Location = new System.Drawing.Point(2, 472);
            this.splitLine.Name = "splitLine";
            this.splitLine.Size = new System.Drawing.Size(315, 1);
            this.splitLine.TabIndex = 1;
            // 
            // maximizeIcon
            // 
            this.maximizeIcon.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.maximizeIcon.Image = global::FamilyEditorInterface.Properties.Resources.maximize;
            this.maximizeIcon.InitialImage = ((System.Drawing.Image)(resources.GetObject("maximizeIcon.InitialImage")));
            this.maximizeIcon.Location = new System.Drawing.Point(296, 439);
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
            // defaultButton
            // 
            this.defaultButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.defaultButton.Location = new System.Drawing.Point(121, 513);
            this.defaultButton.Name = "defaultButton";
            this.defaultButton.Size = new System.Drawing.Size(102, 23);
            this.defaultButton.TabIndex = 0;
            this.defaultButton.Text = "Default";
            this.defaultButton.UseVisualStyleBackColor = true;
            this.defaultButton.Click += new System.EventHandler(this.defaultButton_Click);
            this.defaultButton.KeyDown += new System.Windows.Forms.KeyEventHandler(this.defaultButton_KeyDown);
            this.defaultButton.KeyUp += new System.Windows.Forms.KeyEventHandler(this.defaultButton_KeyUp);
            // 
            // Interface
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(344, 551);
            this.Controls.Add(this.defaultButton);
            this.Controls.Add(this.mainContainer);
            this.Controls.Add(this.refreshButtom);
            this.Controls.Add(this.btnExit);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximumSize = new System.Drawing.Size(360, 1440);
            this.MinimumSize = new System.Drawing.Size(360, 360);
            this.Name = "Interface";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Family Parameters - v1.4 (debug)";
            this.GotFocus += new System.EventHandler(this.Control1_GotFocus);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Interface_KeyPress);
            this.mainContainer.Panel1.ResumeLayout(false);
            this.mainContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainContainer)).EndInit();
            this.mainContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.maximizeIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion        
    }
}
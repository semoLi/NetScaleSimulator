namespace NetScaleSimulator
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        // Native Windows Forms Controls
        private System.Windows.Forms.Panel panelIndicator;
        private System.Windows.Forms.Label lblWeightDisplay;
        private System.Windows.Forms.Label lblUnitIndicator;
        private System.Windows.Forms.Panel ledStable;
        private System.Windows.Forms.Panel ledNet;
        private System.Windows.Forms.Panel ledZero;
        private System.Windows.Forms.Panel ledOverload;
        private System.Windows.Forms.Label lblStable;
        private System.Windows.Forms.Label lblNet;
        private System.Windows.Forms.Label lblZero;
        private System.Windows.Forms.Label lblOverload;

        private System.Windows.Forms.TextBox txtManualWeight;
        private System.Windows.Forms.Button btnApplyManual;
        private System.Windows.Forms.TrackBar trackJitter;
        private System.Windows.Forms.Label lblJitterVal;

        private System.Windows.Forms.ComboBox cbProtocol;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.TextBox txtCapacity;
        private System.Windows.Forms.ComboBox cbDivision;
        private System.Windows.Forms.Button btnToggleServer;
        private System.Windows.Forms.Label lblServerStatusLabel;

        private System.Windows.Forms.TextBox txtDosageTarget;
        private System.Windows.Forms.TextBox txtDosageSpeed;
        private System.Windows.Forms.Button btnStartDosage;

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.FlowLayoutPanel flowPresets;

        private NetIndustrialScale.ScaleHandler _handler;

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

        

        #endregion
    }
}
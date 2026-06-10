using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using NetIndustrialScale;
using System.IO;

namespace NetScaleSimulator
{
    public partial class MainForm : Form
    {
        // Core simulator classes
        private TcpScaleServer _server;
        private System.Windows.Forms.Timer _uiRefreshTimer;
        private double _division = 0.01;
        private double _maxCapacity = 150.0;

        // Auto-dosage simulator variables
        private bool _isDosageRunning = false;
        private double _dosageTarget = 25.0;
        private double _dosageSpeed = 1.5; // kg per second
        private System.Windows.Forms.Timer _dosageTimer;
        private double _dosageCurrent = 0.0;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>

        private void InitializeComponent()
        {
            // General Form Styles
            this.Text = "Endüstriyel Terazi Simülasyon Laboratuvarı (C#)";
            this.Size = new System.Drawing.Size(950, 700);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.MinimumSize = new System.Drawing.Size(950, 700);

            // Root Window Layout - Main vertical container
            System.Windows.Forms.TableLayoutPanel rootLayout = new System.Windows.Forms.TableLayoutPanel();
            rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            rootLayout.RowCount = 3;
            rootLayout.ColumnCount = 1;
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 70F)); // Header banner
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));  // Main body bento layout
            rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 120F)); // Footer Terminal
            this.Controls.Add(rootLayout);

            // 1. Header Banner Panel
            System.Windows.Forms.Panel pnlHeader = new System.Windows.Forms.Panel();
            pnlHeader.BackColor = System.Drawing.Color.FromArgb(249, 115, 22); // Elegant orange theme accent
            pnlHeader.Dock = System.Windows.Forms.DockStyle.Fill;

            System.Windows.Forms.Label lblTitle = new System.Windows.Forms.Label();
            lblTitle.Text = "Endüstriyel Terazi Entegrasyon & Simülatör Paneli";
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.White;
            lblTitle.Location = new System.Drawing.Point(15, 10);
            lblTitle.AutoSize = true;
            pnlHeader.Controls.Add(lblTitle);

            System.Windows.Forms.Label lblSubtitle = new System.Windows.Forms.Label();
            lblSubtitle.Text = "C# .NET Native Masaüstü Kararlı/Kararsız Ölçüm ve Protokol Analiz Laboratuvarı";
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblSubtitle.ForeColor = Color.FromArgb(254, 215, 170);
            lblSubtitle.Location = new Point(17, 36);
            lblSubtitle.AutoSize = true;
            pnlHeader.Controls.Add(lblSubtitle);

            rootLayout.Controls.Add(pnlHeader, 0, 0);

            // 2. Bento Grid System - Main Body Container split into columns
            TableLayoutPanel bodyGrid = new TableLayoutPanel();
            bodyGrid.Dock = DockStyle.Fill;
            bodyGrid.ColumnCount = 3;
            bodyGrid.RowCount = 1;
            bodyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // Left Column: LCD + Presets
            bodyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F)); // Middle Column: Controls + AutoFlow
            bodyGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F)); // Right Column: TCP Config
            bodyGrid.Padding = new Padding(10);
            rootLayout.Controls.Add(bodyGrid, 0, 1);

            // ==========================================
            // SOL SÜTUN CONTROLS (Indicator + Presets)
            // ==========================================
            FlowLayoutPanel leftColumn = new FlowLayoutPanel();
            leftColumn.Dock = DockStyle.Fill;
            leftColumn.FlowDirection = FlowDirection.TopDown;
            leftColumn.WrapContents = false;
            leftColumn.Padding = new Padding(5);
            bodyGrid.Controls.Add(leftColumn, 0, 0);

            // LCD Panel (Digital Weighing Indicator Faceplate)
            panelIndicator = new Panel();
            panelIndicator.Width = 300;
            panelIndicator.Height = 160;
            panelIndicator.BackColor = Color.FromArgb(17, 24, 39); // Deep dark carbon
            panelIndicator.BorderStyle = BorderStyle.FixedSingle;

            // Large Digit Display
            lblWeightDisplay = new Label();
            lblWeightDisplay.Text = "0.00";
            lblWeightDisplay.Font = new Font("Consolas", 32F, FontStyle.Bold);
            lblWeightDisplay.ForeColor = Color.FromArgb(249, 115, 22); // Amber LED Glow
            lblWeightDisplay.TextAlign = ContentAlignment.MiddleRight;
            lblWeightDisplay.Size = new Size(230, 80);
            lblWeightDisplay.Location = new Point(10, 45);
            panelIndicator.Controls.Add(lblWeightDisplay);

            lblUnitIndicator = new Label();
            lblUnitIndicator.Text = "KG";
            lblUnitIndicator.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblUnitIndicator.ForeColor = Color.FromArgb(249, 115, 22);
            lblUnitIndicator.Location = new Point(245, 80);
            lblUnitIndicator.Size = new Size(40, 25);
            panelIndicator.Controls.Add(lblUnitIndicator);

            // LED Indicators Panel Row
            TableLayoutPanel ledRow = new TableLayoutPanel();
            ledRow.Width = 280;
            ledRow.Height = 35;
            ledRow.Location = new Point(10, 10);
            ledRow.ColumnCount = 4;
            ledRow.RowCount = 1;
            ledRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            ledRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            ledRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            ledRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            panelIndicator.Controls.Add(ledRow);

            // 1. Stable LED Container
            FlowLayoutPanel flpStable = new FlowLayoutPanel();
            ledStable = new Panel() { Size = new Size(12, 12), BackColor = Color.FromArgb(34, 197, 94), Margin = new Padding(2, 4, 2, 2) };
            lblStable = new Label() { Text = "STB", Font = new System.Drawing.Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true };
            flpStable.Controls.Add(ledStable);
            flpStable.Controls.Add(lblStable);
            ledRow.Controls.Add(flpStable, 0, 0);

            // 2. Net LED Container
            FlowLayoutPanel flpNet = new FlowLayoutPanel();
            ledNet = new Panel() { Size = new Size(12, 12), BackColor = Color.FromArgb(75, 85, 99), Margin = new Padding(2, 4, 2, 2) };
            lblNet = new Label() { Text = "NET", Font = new System.Drawing.Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true };
            flpNet.Controls.Add(ledNet);
            flpNet.Controls.Add(lblNet);
            ledRow.Controls.Add(flpNet, 1, 0);

            // 3. Zero LED Container
            FlowLayoutPanel flpZero = new FlowLayoutPanel();
            ledZero = new Panel() { Size = new Size(12, 12), BackColor = Color.FromArgb(34, 197, 94), Margin = new Padding(2, 4, 2, 2) };
            lblZero = new Label() { Text = "ZERO", Font = new System.Drawing.Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true };
            flpZero.Controls.Add(ledZero);
            flpZero.Controls.Add(lblZero);
            ledRow.Controls.Add(flpZero, 2, 0);

            // 4. Overload LED Container
            FlowLayoutPanel flpOL = new FlowLayoutPanel();
            ledOverload = new Panel() { Size = new Size(12, 12), BackColor = Color.FromArgb(75, 85, 99), Margin = new Padding(2, 4, 2, 2) };
            lblOverload = new Label() { Text = "O/L", Font = new System.Drawing.Font("Segoe UI", 7F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true };
            flpOL.Controls.Add(ledOverload);
            flpOL.Controls.Add(lblOverload);
            ledRow.Controls.Add(flpOL, 3, 0);

            leftColumn.Controls.Add(panelIndicator);

            // Indicator Button Row under screen
            TableLayoutPanel indBtns = new TableLayoutPanel();
            indBtns.Size = new Size(300, 40);
            indBtns.ColumnCount = 4;
            indBtns.RowCount = 1;
            indBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            indBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            indBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            indBtns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            Button btnZero = CreateStyledButton("SIFIR", Color.FromArgb(55, 65, 81), Color.White);
            btnZero.Click += (s, e) =>
            {
                if (_server != null) { _server.CurrentWeight = 0.0; LogLocal("[Eylem]: Kendi kendine sıfırlama uygulandı."); }
            };
            indBtns.Controls.Add(btnZero, 0, 0);

            Button btnTare = CreateStyledButton("DARA", Color.FromArgb(55, 65, 81), Color.White);
            btnTare.Click += (s, e) =>
            {
                if (_server != null) { _server.IsNet = !_server.IsNet; LogLocal("[Eylem]: Net / Brüt modu değiştirildi."); }
            };
            indBtns.Controls.Add(btnTare, 1, 0);

            System.Windows.Forms.Button btnToggleStb = CreateStyledButton("DENGE", System.Drawing.Color.FromArgb(55, 65, 81), Color.White);
            btnToggleStb.Click += (s, e) =>
            {
                if (_server != null) { _server.IsStable = !_server.IsStable; LogLocal("[Eylem]: Stabilite toggled."); }
            };
            indBtns.Controls.Add(btnToggleStb, 2, 0);

            Button btnOL = CreateStyledButton("ST-OL", Color.FromArgb(239, 68, 68), Color.White);
            btnOL.Click += (s, e) =>
            {
                if (_server != null) { _server.IsOverload = !_server.IsOverload; LogLocal("[Eylem]: Aşırı yük test bayrağı tetiklendi."); }
            };
            indBtns.Controls.Add(btnOL, 3, 0);
            leftColumn.Controls.Add(indBtns);

            // Quick weight presets container block
            GroupBox gbPresets = new GroupBox();
            gbPresets.Text = "Hızlı Ağırlık Kümesi Setleri";
            gbPresets.Width = 300;
            gbPresets.Height = 180;
            gbPresets.Padding = new Padding(10);

            flowPresets = new FlowLayoutPanel();
            flowPresets.Dock = DockStyle.Fill;
            flowPresets.FlowDirection = FlowDirection.LeftToRight;

            double[] presetValues = { 0, 1.5, 5, 10, 25, 50, 100, 250 };
            foreach (double val in presetValues)
            {
                Button btnPreset = new Button();
                btnPreset.Text = string.Format("{0} kg", val);
                btnPreset.Width = 60;
                btnPreset.Height = 35;
                btnPreset.Margin = new Padding(4);
                btnPreset.BackColor = Color.White;
                btnPreset.FlatStyle = FlatStyle.Flat;
                btnPreset.FlatAppearance.BorderColor = Color.FromArgb(209, 213, 219);
                double targetVal = val;
                btnPreset.Click += (s, e) =>
                {
                    if (_server != null)
                    {
                        _server.CurrentWeight = targetVal;
                        LogLocal(string.Format("[Simülatör]: Hızlı Ağırlık yüklendi: {0} kg", targetVal));
                    }
                };
                flowPresets.Controls.Add(btnPreset);
            }

            Button btnHeavyOL = new Button();
            btnHeavyOL.Text = "LIMIT+ OL";
            btnHeavyOL.Width = 135;
            btnHeavyOL.Height = 35;
            btnHeavyOL.BackColor = Color.FromArgb(254, 226, 226);
            btnHeavyOL.ForeColor = Color.FromArgb(220, 38, 38);
            btnHeavyOL.FlatStyle = FlatStyle.Flat;
            btnHeavyOL.FlatAppearance.BorderColor = Color.FromArgb(248, 113, 113);
            btnHeavyOL.Click += (s, e) =>
            {
                if (_server != null)
                {
                    _server.CurrentWeight = _server.MaxCapacity + 5.0;
                    LogLocal(string.Format("[Simülatör]: Limit aşıldı! Agirlik: {0} kg (Kapasite: {1})", _server.CurrentWeight, _server.MaxCapacity));
                }
            };
            flowPresets.Controls.Add(btnHeavyOL);

            gbPresets.Controls.Add(flowPresets);
            leftColumn.Controls.Add(gbPresets);

            // ==========================================
            // ORTA SÜTUN CONTROLS (Manual scale values + Dosage simulation)
            // ==========================================
            FlowLayoutPanel middleColumn = new FlowLayoutPanel();
            middleColumn.Dock = DockStyle.Fill;
            middleColumn.FlowDirection = FlowDirection.TopDown;
            middleColumn.WrapContents = false;
            middleColumn.Padding = new Padding(5);
            bodyGrid.Controls.Add(middleColumn, 1, 0);

            // Settings Group Box
            GroupBox gbGeneral = new GroupBox();
            gbGeneral.Text = "Genel Tartım Ayarları";
            gbGeneral.Width = 285;
            gbGeneral.Height = 175;
            gbGeneral.Padding = new Padding(10);

            Label lblManual = new Label() { Text = "Manuel Ağırlık Gir (Sanal Kütle):", Width = 260, Height = 18, Location = new Point(10, 25) };
            gbGeneral.Controls.Add(lblManual);

            txtManualWeight = new TextBox() { Text = "12.45", Width = 150, Location = new Point(10, 48) };
            gbGeneral.Controls.Add(txtManualWeight);

            btnApplyManual = CreateStyledButton("UYGULA", Color.FromArgb(249, 115, 22), Color.White);
            btnApplyManual.Size = new Size(100, 26);
            btnApplyManual.Location = new Point(170, 47);
            btnApplyManual.Click += BtnApplyManual_Click;
            gbGeneral.Controls.Add(btnApplyManual);

            Label lblJitter = new Label() { Text = "Kararsızlık Titreşimi (Sarsıntı Jitter):", Width = 260, Height = 18, Location = new Point(10, 90) };
            gbGeneral.Controls.Add(lblJitter);

            trackJitter = new TrackBar() { Minimum = 0, Maximum = 100, Value = 15, Width = 180, Height = 35, Location = new Point(10, 115), TickStyle = TickStyle.None };
            trackJitter.Scroll += (s, e) => { lblJitterVal.Text = string.Format("{0:0.00} kg", trackJitter.Value / 100.0); };
            gbGeneral.Controls.Add(trackJitter);

            lblJitterVal = new Label() { Text = "0.15 kg", Font = new Font("Segoe UI", 9F, FontStyle.Bold), Width = 70, Height = 25, Location = new Point(200, 120), TextAlign = ContentAlignment.TopRight };
            gbGeneral.Controls.Add(lblJitterVal);

            middleColumn.Controls.Add(gbGeneral);

            // Dosage automation filling machine simulator block
            GroupBox gbDosage = new GroupBox();
            gbDosage.Text = "Dozaşlama / Otomatik Dolum Simülasyonu";
            gbDosage.Width = 285;
            gbDosage.Height = 195;
            gbDosage.Padding = new Padding(10);

            Label lblDosTarget = new Label() { Text = "Hedef Kütle (kg):", Width = 120, Height = 18, Location = new Point(10, 25) };
            gbDosage.Controls.Add(lblDosTarget);
            txtDosageTarget = new TextBox() { Text = "25.0", Width = 110, Location = new Point(10, 45) };
            gbDosage.Controls.Add(txtDosageTarget);

            Label lblDosSpeed = new Label() { Text = "Akış Hızı (kg/sn):", Width = 120, Height = 18, Location = new Point(140, 25) };
            gbDosage.Controls.Add(lblDosSpeed);
            txtDosageSpeed = new TextBox() { Text = "3.2", Width = 110, Location = new Point(140, 45) };
            gbDosage.Controls.Add(txtDosageSpeed);

            btnStartDosage = CreateStyledButton("DOLUM SİMÜLASYONU BAŞLAT", Color.FromArgb(34, 197, 94), Color.White);
            btnStartDosage.Size = new Size(240, 40);
            btnStartDosage.Location = new Point(10, 85);
            btnStartDosage.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnStartDosage.Click += BtnStartDosage_Click;
            gbDosage.Controls.Add(btnStartDosage);

            Label lblWarnFlow = new Label();
            lblWarnFlow.Text = "*Akış boyunca canlı terazi çıktısı aslına uygun \"unstable\" olarak dalgalanacaktır.";
            lblWarnFlow.Font = new Font("Segoe UI", 7.5F, FontStyle.Italic);
            lblWarnFlow.ForeColor = Color.DimGray;
            lblWarnFlow.Size = new Size(250, 45);
            lblWarnFlow.Location = new Point(10, 140);
            gbDosage.Controls.Add(lblWarnFlow);

            middleColumn.Controls.Add(gbDosage);

            // ==========================================
            // SAĞ SÜTUN CONTROLS (Server management, presets)
            // ==========================================
            FlowLayoutPanel rightColumn = new FlowLayoutPanel();
            rightColumn.Dock = DockStyle.Fill;
            rightColumn.FlowDirection = FlowDirection.TopDown;
            rightColumn.WrapContents = false;
            rightColumn.Padding = new Padding(5);
            bodyGrid.Controls.Add(rightColumn, 2, 0);

            GroupBox gbServer = new GroupBox();
            gbServer.Text = "TCP/IP Sunucu Yapılandırması";
            gbServer.Width = 275;
            gbServer.Height = 380;
            gbServer.Padding = new Padding(10);

            Label lblPort = new Label() { Text = "TCP Soket Dinleme Portu:", Width = 240, Height = 18, Location = new Point(10, 25) };
            gbServer.Controls.Add(lblPort);
            txtPort = new TextBox() { Text = "1001", Width = 230, Location = new Point(10, 45) };
            gbServer.Controls.Add(txtPort);

            Label lblCap = new Label() { Text = "Maximum Kapasite Limit (OL/kg):", Width = 240, Height = 18, Location = new Point(10, 80) };
            gbServer.Controls.Add(lblCap);
            txtCapacity = new TextBox() { Text = "150.0", Width = 230, Location = new Point(10, 100) };
            gbServer.Controls.Add(txtCapacity);

            Label lblDiv = new Label() { Text = "Hassasiyet (Taksimat Division):", Width = 240, Height = 18, Location = new Point(10, 135) };
            gbServer.Controls.Add(lblDiv);
            cbDivision = new ComboBox() { Width = 230, Location = new Point(10, 155), DropDownStyle = ComboBoxStyle.DropDownList };
            cbDivision.Items.AddRange(new string[] { "0.001", "0.002", "0.005", "0.01", "0.1", "1" });
            cbDivision.SelectedIndex = 3; // Default 0.01
            gbServer.Controls.Add(cbDivision);

            Label lblProto = new Label() { Text = "Tartım Gönderim Protokolü:", Width = 240, Height = 18, Location = new Point(10, 190) };
            gbServer.Controls.Add(lblProto);
            cbProtocol = new ComboBox() { Width = 230, Location = new Point(10, 210), DropDownStyle = ComboBoxStyle.DropDownList };
            cbProtocol.Items.AddRange(new string[] { "CAS", "TOLEDO" });
            cbProtocol.SelectedIndex = 0; // Default CAS
            gbServer.Controls.Add(cbProtocol);

            // Server active status label
            lblServerStatusLabel = new Label() { Text = "[DURUM]: YAYIN AKTİF (Soket Açık)", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(34, 197, 94), Width = 240, Height = 25, Location = new Point(10, 255) };
            gbServer.Controls.Add(lblServerStatusLabel);

            btnToggleServer = CreateStyledButton("SUNUCUYU DURDUR", Color.FromArgb(55, 65, 81), Color.White);
            btnToggleServer.Size = new Size(230, 40);
            btnToggleServer.Location = new Point(10, 285);
            btnToggleServer.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnToggleServer.Click += BtnToggleServer_Click;
            gbServer.Controls.Add(btnToggleServer);

            Label lblNetworkInfo = new Label();
            lblNetworkInfo.Text = "Localhost veya yerel IP adresi üzerinden C# uygulamaları bu sokete TCP istemcisi olarak bağlanabilir.";
            lblNetworkInfo.ForeColor = Color.Gray;
            lblNetworkInfo.Font = new Font("Segoe UI", 7.5F, FontStyle.Italic);
            lblNetworkInfo.Size = new Size(240, 40);
            lblNetworkInfo.Location = new Point(10, 335);
            gbServer.Controls.Add(lblNetworkInfo);

            rightColumn.Controls.Add(gbServer);

            // 3. Bottom Terminal logger Panel
            GroupBox gbLog = new GroupBox();
            gbLog.Text = "Canlı Simülasyon Olay Günlüğü (Terminal Trafiği)";
            gbLog.Dock = DockStyle.Fill;
            gbLog.Padding = new Padding(10);

            txtLog = new TextBox();
            txtLog.Multiline = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.ReadOnly = true;
            txtLog.Dock = DockStyle.Fill;
            txtLog.BackColor = Color.FromArgb(15, 23, 42); // Navy slate background
            txtLog.ForeColor = Color.FromArgb(14, 165, 233); // Cyan text
            txtLog.Font = new Font("Consolas", 8.5F, FontStyle.Regular);
            gbLog.Controls.Add(txtLog);

            rootLayout.Controls.Add(gbLog, 0, 2);

            // Setup dosage timer with 100ms interval
            _dosageTimer = new System.Windows.Forms.Timer();
            _dosageTimer.Interval = 100;
            _dosageTimer.Tick += DosageTimer_Tick;
        }

        public MainForm(TcpScaleServer server = null, ScaleHandler handler = null)
        {
            this._server = server;
            this._handler = handler;
            InitializeComponent();

            if (this._server == null)
            {
                LoadConfigFromIni();
                InitializeServer();
            }
            else
            {
                // Sync UI elements to existing server
                txtPort.Text = "1001"; // Default or read from active
                txtCapacity.Text = _server.MaxCapacity.ToString("F1", CultureInfo.InvariantCulture);
                txtManualWeight.Text = _server.CurrentWeight.ToString("F2", CultureInfo.InvariantCulture);

                string divStr = _server.Division.ToString(CultureInfo.InvariantCulture);
                int idx = cbDivision.FindString(divStr);
                if (idx >= 0) cbDivision.SelectedIndex = idx;

                int pIdx = cbProtocol.FindString(_server.ActiveProtocol);
                if (pIdx >= 0) cbProtocol.SelectedIndex = pIdx;

                _division = _server.Division;
                _maxCapacity = _server.MaxCapacity;

                LogLocal("[Sistem]: Mevcut çalışmakta olan TCP Sunucusu arayüz ile eşlendi.");
            }

            if (this._handler != null)
            {
                this._handler.OnWeightChanged += delegate(ScaleData data)
                {
                    string stabilityText = data.Stability == ScaleStatus.Stable ? "DENGEDE (ST)" : "KARARSIZ (US)";
                    if (data.Stability == ScaleStatus.Overload) stabilityText = "AŞIRI YÜK (OL)";

                    LogLocal(string.Format("[ScaleHandler Sürücü Olayı]: Ağırlık Değişti -> {0} {1} | Denge: {2}",
                        data.Weight, data.Unit, stabilityText));
                };

                this._handler.OnConnectionStatusChanged += delegate(bool isConnected)
                {
                    LogLocal(string.Format("[ScaleHandler Sürücü Sinyali]: Bağlantı durumu değişti -> {0}",
                        isConnected ? "ÇEVRİMİÇİ / BAĞLI" : "BAĞLANTI KOPUK (Arayıp Duruyor)"));
                };

                LogLocal("[Sistem]: Mevcut sürücü dinleyicisi (ScaleHandler) arayüz olay günlüğüne bağlandı.");
            }

            // Start UI update loop (50ms interval)
            _uiRefreshTimer = new System.Windows.Forms.Timer();
            _uiRefreshTimer.Interval = 50;
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();
        }


        private void InitializeServer()
        {
            try
            {
                int port = int.Parse(txtPort.Text);
                _server = new TcpScaleServer(port);

                // Read from Inputs initialized by INI or defaults
                UpdateServerConfigFromUI();
                _server.Start();

                LogLocal(string.Format("[Sistem]: TCP Sahte Terazi Sunucusu {0} portunda başlatıldı.", port));
            }
            catch (Exception ex)
            {
                LogLocal("[HATA]: Sunucu başlatılamadı: " + ex.Message);
            }
        }

        private void LoadConfigFromIni()
        {
            try
            {
                string iniPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? "", "config.ini");
                ScaleConfig conf = IniReader.Load("config.ini");

                txtPort.Text = conf.Port.ToString();
                txtCapacity.Text = conf.MaxCapacity.ToString("F1", CultureInfo.InvariantCulture);
                txtManualWeight.Text = conf.InitialWeight.ToString("F2", CultureInfo.InvariantCulture);

                string divStr = conf.Division.ToString(CultureInfo.InvariantCulture);
                int idx = cbDivision.FindString(divStr);
                if (idx >= 0) cbDivision.SelectedIndex = idx;

                int pIdx = cbProtocol.FindString(conf.Protocol);
                if (pIdx >= 0) cbProtocol.SelectedIndex = pIdx;

                LogLocal(string.Format("[Config]: config.ini başarıyla yüklendi. Port: {0}", conf.Port));
            }
            catch (Exception ex)
            {
                LogLocal("[HATA]: Yapılandırma dosyası yüklenemedi, varsayılanlar ayarlandı: " + ex.Message);
            }
        }

        private void UpdateServerConfigFromUI()
        {
            if (_server == null) return;

            double.TryParse(txtCapacity.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _maxCapacity);
            _server.MaxCapacity = _maxCapacity;

            double.TryParse(cbDivision.SelectedItem.ToString().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _division);
            _server.Division = _division;

            _server.ActiveProtocol = cbProtocol.SelectedItem.ToString();
            lblUnitIndicator.Text = "KG";
        }

        private Button CreateStyledButton(string text, Color backColor, Color foreColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            return btn;
        }

        // Apply Manual weight button trigger
        private void BtnApplyManual_Click(object sender, EventArgs e)
        {
            if (_server == null) return;

            string raw = txtManualWeight.Text.Trim().Replace(",", ".");
            double targetWeight;
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out targetWeight))
            {
                UpdateServerConfigFromUI();
                _server.CurrentWeight = targetWeight;
                LogLocal(string.Format("[Giriş]: Kullanıcı yeni ağırlık girdi: {0} kg/Aşıyor mu: {1}", targetWeight, targetWeight > _maxCapacity ? "EVET" : "HAYIR"));
            }
            else
            {
                MessageBox.Show("Lütfen geçerli sayısal bir kütle değeri girin!", "Sanal Tartım", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        // Toggle TCP listening server state
        private void BtnToggleServer_Click(object sender, EventArgs e)
        {
            if (_server == null) return;

            if (_server.IsRunning)
            {
                _server.Stop();
                btnToggleServer.Text = "SUNUCUYU BAŞLAT";
                btnToggleServer.BackColor = Color.FromArgb(34, 197, 94);
                lblServerStatusLabel.Text = "[DURUM]: YAYIN DURDURULDU";
                lblServerStatusLabel.ForeColor = Color.FromArgb(239, 68, 68);
                LogLocal("[Sistem]: TCP Soket sunucusu kapatıldı.");
            }
            else
            {
                try
                {
                    int port = int.Parse(txtPort.Text);
                    _server = new TcpScaleServer(port);
                    UpdateServerConfigFromUI();
                    _server.Start();

                    btnToggleServer.Text = "SUNUCUYU DURDUR";
                    btnToggleServer.BackColor = Color.FromArgb(55, 65, 81);
                    lblServerStatusLabel.Text = "[DURUM]: YAYIN AKTİF (Soket Açık)";
                    lblServerStatusLabel.ForeColor = Color.FromArgb(34, 197, 94);
                    LogLocal(string.Format("[Sistem]: TCP Soket sunucusu {0} portunda tekrar başlatıldı.", port));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Sunucu başlatılırken hata: " + ex.Message);
                }
            }
        }

        // Automatic Dosage Simulator Step Tick
        private void BtnStartDosage_Click(object sender, EventArgs e)
        {
            if (_isDosageRunning)
            {
                StopDosageSim();
                LogLocal("[Dozajlama]: Simülasyon kullanıcı tarafından iptal edildi.");
            }
            else
            {
                if (_server == null || !_server.IsRunning)
                {
                    MessageBox.Show("Akış simülasyonu için öncelikle TCP Sunucusunu başlatmalısınız!", "Dozajlama Sistemi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                double.TryParse(txtDosageTarget.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _dosageTarget);
                double.TryParse(txtDosageSpeed.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out _dosageSpeed);

                _dosageCurrent = _server.CurrentWeight;
                _isDosageRunning = true;
                btnStartDosage.Text = "SİMÜLASYONU DURDUR / İPTAL";
                btnStartDosage.BackColor = Color.FromArgb(239, 68, 68);

                _server.IsStable = false; // continuous flow triggers unstable vibration
                _dosageTimer.Start();
                LogLocal(string.Format("[Dozajlama]: Otomatik akış başladı. Başlangıç: {0} kg, Hedef: {1} kg, Hız: {2} kg/sn", _dosageCurrent, _dosageTarget, _dosageSpeed));
            }
        }

        private void StopDosageSim()
        {
            _isDosageRunning = false;
            _dosageTimer.Stop();
            btnStartDosage.Text = "DOLUM SİMÜLASYONU BAŞLAT";
            btnStartDosage.BackColor = Color.FromArgb(34, 197, 94);
            if (_server != null)
            {
                _server.IsStable = true;
            }
        }

        private void DosageTimer_Tick(object sender, EventArgs e)
        {
            if (!_isDosageRunning || _server == null) return;

            double step = _dosageSpeed * 0.1; // 100ms step

            if (_dosageCurrent < _dosageTarget)
            {
                _dosageCurrent += step;
                if (_dosageCurrent >= _dosageTarget)
                {
                    _dosageCurrent = _dosageTarget;
                    StopDosageSim();
                    _server.CurrentWeight = _dosageTarget; // triggers natural dip-overshoot-settle effect!
                    LogLocal(string.Format("[Dozajlama]: Hedef kütleye ulaşıldı! {0} kg. Terazi otomatik dengeleniyor...", _dosageTarget));
                    return;
                }
            }
            else
            {
                _dosageCurrent -= step;
                if (_dosageCurrent <= _dosageTarget)
                {
                    _dosageCurrent = _dosageTarget;
                    StopDosageSim();
                    _server.CurrentWeight = _dosageTarget; // triggers natural settling
                    LogLocal(string.Format("[Dozajlama]: Hedef kütleye ulaşıldı! {0} kg. Terazi otomatik dengeleniyor...", _dosageTarget));
                    return;
                }
            }

            // Continuous flow micro fluctuation logic helper
            Random rand = new Random();
            double noise = (rand.NextDouble() - 0.5) * (trackJitter.Value / 100.0);

            _server.CurrentWeight = _dosageCurrent + noise;
        }

        // Periodic UI updates from the active background TCP server model
        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_server == null) return;

            // 1. Synchronize Display Weight String formatted by Division decimals
            double weight = _server.CurrentWeight;
            int decimals = GetDecimalPlaces(_division);
            string formatStr = "F" + decimals;
            lblWeightDisplay.Text = weight.ToString(formatStr, CultureInfo.InvariantCulture);

            // 2. Refresh simulated LED color-lamps
            ledStable.BackColor = _server.IsStable && !_server.IsOverload ? Color.FromArgb(34, 197, 94) : Color.FromArgb(75, 85, 99);
            ledNet.BackColor = _server.IsNet ? Color.FromArgb(59, 130, 246) : Color.FromArgb(75, 85, 99);
            ledZero.BackColor = Math.Abs(weight) < 0.001 ? Color.FromArgb(34, 197, 94) : Color.FromArgb(75, 85, 99);
            ledOverload.BackColor = _server.IsOverload ? Color.FromArgb(239, 68, 68) : Color.FromArgb(75, 85, 99);

            // Overload display
            if (_server.IsOverload)
            {
                lblWeightDisplay.Text = "O.L";
                lblWeightDisplay.ForeColor = Color.FromArgb(239, 68, 68);
            }
            else
            {
                lblWeightDisplay.ForeColor = Color.FromArgb(249, 115, 22);
            }
        }

        private int GetDecimalPlaces(double division)
        {
            string divStr = division.ToString(CultureInfo.InvariantCulture);
            int decimalIdx = divStr.IndexOf('.');
            if (decimalIdx < 0) return 0;
            return divStr.Length - decimalIdx - 1;
        }

        private void LogLocal(string text)
        {
            if (txtLog == null || txtLog.IsDisposed) return;

            string stampText = string.Format("[{0:HH:mm:ss}] {1}\r\n", DateTime.Now, text);
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action<string>(LogLocal), text);
            }
            else
            {
                txtLog.AppendText(stampText);
                if (txtLog.Text.Length > 10000)
                {
                    txtLog.Text = txtLog.Text.Substring(txtLog.Text.Length - 5000);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _uiRefreshTimer.Stop();
            _dosageTimer.Stop();
            if (_server != null)
            {
                _server.Stop();
            }
            base.OnFormClosing(e);
        }
    }
}

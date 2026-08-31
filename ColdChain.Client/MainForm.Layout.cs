using System.Drawing;
using System.Windows.Forms;

namespace ColdChain.Client;

/// <summary>
/// Layout half of the operator console. Everything is built in code, so there is
/// no designer file to keep in step with the logic.
/// </summary>
public partial class MainForm
{
    // --- connection bar ---
    private TextBox txtBaseUrl = null!;
    private Button btnConnect = null!;
    private Label lblConnectionStatus = null!;

    // --- devices tab ---
    private TextBox txtDeviceId = null!;
    private TextBox txtDeviceName = null!;
    private ComboBox cboDeviceType = null!;
    private ComboBox cboLocation = null!;
    private CheckBox chkIsActive = null!;
    private Button btnRegister = null!;
    private Label lblRegisterStatus = null!;

    private TextBox txtSearch = null!;
    private ComboBox cboFilterType = null!;
    private Button btnRefreshDevices = null!;
    private DataGridView dgvDevices = null!;

    private Button btnBrowse = null!;
    private Label lblSelectedFile = null!;
    private TextBox txtEvidenceDescription = null!;
    private Button btnUpload = null!;
    private Label lblEvidenceStatus = null!;

    // --- telemetry tab ---
    private Button btnRefreshTelemetry = null!;
    private CheckBox chkAutoRefresh = null!;
    private CheckBox chkOnlyAnomalies = null!;
    private ComboBox cboTelemetryDevice = null!;
    private ComboBox cboZone = null!;
    private Button btnCombineZone = null!;
    private Label lblZoneResult = null!;
    private DataGridView dgvTelemetry = null!;

    // --- anomalies tab ---
    private Button btnRefreshAnomalies = null!;
    private CheckBox chkShowAcknowledged = null!;
    private DataGridView dgvAnomalies = null!;
    private TextBox txtOperator = null!;
    private TextBox txtNote = null!;
    private Button btnAcknowledge = null!;
    private Label lblAckStatus = null!;

    private System.Windows.Forms.Timer refreshTimer = null!;

    private static readonly Color AnomalyBack = Color.FromArgb(255, 235, 235);
    private static readonly Color AnomalyFore = Color.FromArgb(150, 20, 40);
    private static readonly Color HeaderBack = Color.FromArgb(37, 55, 75);

    // Co-authored by Claude
    /// <summary>Builds every control on the form.</summary>
    private void BuildUi()
    {
        Text = "FreshRoute Cold-Chain Operator Console";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 640);
        Size = new Size(1100, 740);
        Font = new Font("Segoe UI", 9F);

        TabControl tabs = new() { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
        tabs.TabPages.Add(BuildDevicesTab());
        tabs.TabPages.Add(BuildTelemetryTab());
        tabs.TabPages.Add(BuildAnomaliesTab());

        // The fill control is added first so the docked bar keeps the top edge.
        Controls.Add(tabs);
        Controls.Add(BuildConnectionBar());

        refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        refreshTimer.Tick += RefreshTimer_Tick;
    }

    // Co-authored by Claude
    private Panel BuildConnectionBar()
    {
        Panel panel = new()
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = HeaderBack
        };

        Label lblUrl = new()
        {
            Text = "Gateway URL",
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(12, 16)
        };

        txtBaseUrl = new TextBox
        {
            Text = "http://localhost:5165",
            Location = new Point(100, 13),
            Width = 220
        };

        btnConnect = new Button
        {
            Text = "Connect",
            Location = new Point(330, 12),
            Width = 90,
            Height = 25
        };
        btnConnect.Click += BtnConnect_Click;

        lblConnectionStatus = new Label
        {
            Text = "Not connected",
            ForeColor = Color.Gainsboro,
            AutoSize = true,
            Location = new Point(436, 16)
        };

        panel.Controls.AddRange(new Control[] { lblUrl, txtBaseUrl, btnConnect, lblConnectionStatus });
        return panel;
    }

    // Co-authored by Claude
    private TabPage BuildDevicesTab()
    {
        TabPage page = new("Devices") { BackColor = Color.White, Padding = new Padding(10) };

        // ----- registration -----
        GroupBox grpRegister = new()
        {
            Text = "Register monitoring device",
            Location = new Point(12, 12),
            Size = new Size(430, 258)
        };

        grpRegister.Controls.Add(new Label { Text = "Device ID", Location = new Point(16, 32), AutoSize = true });
        txtDeviceId = new TextBox { Location = new Point(130, 29), Width = 190, CharacterCasing = CharacterCasing.Upper };
        grpRegister.Controls.Add(txtDeviceId);

        grpRegister.Controls.Add(new Label { Text = "Device name", Location = new Point(16, 66), AutoSize = true });
        txtDeviceName = new TextBox { Location = new Point(130, 63), Width = 280 };
        grpRegister.Controls.Add(txtDeviceName);

        grpRegister.Controls.Add(new Label { Text = "Device type", Location = new Point(16, 100), AutoSize = true });
        cboDeviceType = new ComboBox
        {
            Location = new Point(130, 97),
            Width = 190,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        grpRegister.Controls.Add(cboDeviceType);

        grpRegister.Controls.Add(new Label { Text = "Location", Location = new Point(16, 134), AutoSize = true });
        cboLocation = new ComboBox
        {
            Location = new Point(130, 131),
            Width = 280,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        grpRegister.Controls.Add(cboLocation);

        chkIsActive = new CheckBox
        {
            Text = "Device is active",
            Checked = true,
            Location = new Point(130, 165),
            AutoSize = true
        };
        grpRegister.Controls.Add(chkIsActive);

        btnRegister = new Button
        {
            Text = "Register device",
            Location = new Point(130, 192),
            Size = new Size(150, 32)
        };
        btnRegister.Click += BtnRegister_Click;
        grpRegister.Controls.Add(btnRegister);

        lblRegisterStatus = new Label
        {
            Text = "Device ID format: three letters, hyphen, three digits (TMP-006).",
            Location = new Point(16, 230),
            Size = new Size(400, 20),
            ForeColor = Color.DimGray
        };
        grpRegister.Controls.Add(lblRegisterStatus);

        // ----- evidence -----
        GroupBox grpEvidence = new()
        {
            Text = "Evidence file for the selected device",
            Location = new Point(12, 282),
            Size = new Size(430, 190)
        };

        btnBrowse = new Button { Text = "Browse...", Location = new Point(16, 32), Size = new Size(100, 28) };
        btnBrowse.Click += BtnBrowse_Click;
        grpEvidence.Controls.Add(btnBrowse);

        lblSelectedFile = new Label
        {
            Text = "No file selected",
            Location = new Point(126, 38),
            Size = new Size(290, 20),
            ForeColor = Color.DimGray
        };
        grpEvidence.Controls.Add(lblSelectedFile);

        grpEvidence.Controls.Add(new Label { Text = "Description", Location = new Point(16, 76), AutoSize = true });
        txtEvidenceDescription = new TextBox { Location = new Point(100, 73), Width = 310 };
        grpEvidence.Controls.Add(txtEvidenceDescription);

        btnUpload = new Button { Text = "Upload evidence", Location = new Point(16, 110), Size = new Size(150, 30) };
        btnUpload.Click += BtnUpload_Click;
        grpEvidence.Controls.Add(btnUpload);

        lblEvidenceStatus = new Label
        {
            Text = "Accepted: JPG, PNG, PDF up to 5 MB.",
            Location = new Point(16, 150),
            Size = new Size(400, 32),
            ForeColor = Color.DimGray
        };
        grpEvidence.Controls.Add(lblEvidenceStatus);

        // ----- device list -----
        GroupBox grpDevices = new()
        {
            Text = "Registered devices",
            Location = new Point(456, 12),
            Size = new Size(600, 460),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        grpDevices.Controls.Add(new Label { Text = "Search", Location = new Point(14, 30), AutoSize = true });
        txtSearch = new TextBox { Location = new Point(66, 27), Width = 170 };
        grpDevices.Controls.Add(txtSearch);

        cboFilterType = new ComboBox
        {
            Location = new Point(246, 27),
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        grpDevices.Controls.Add(cboFilterType);

        btnRefreshDevices = new Button { Text = "Apply / Refresh", Location = new Point(406, 26), Size = new Size(120, 25) };
        btnRefreshDevices.Click += BtnRefreshDevices_Click;
        grpDevices.Controls.Add(btnRefreshDevices);

        dgvDevices = BuildGrid();
        dgvDevices.Location = new Point(14, 62);
        dgvDevices.Size = new Size(570, 380);
        dgvDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        grpDevices.Controls.Add(dgvDevices);

        page.Controls.AddRange(new Control[] { grpRegister, grpEvidence, grpDevices });
        return page;
    }

    // Co-authored by Claude
    private TabPage BuildTelemetryTab()
    {
        TabPage page = new("Telemetry") { BackColor = Color.White, Padding = new Padding(10) };

        Panel toolbar = new() { Dock = DockStyle.Top, Height = 84 };

        btnRefreshTelemetry = new Button { Text = "Refresh", Location = new Point(4, 8), Size = new Size(100, 28) };
        btnRefreshTelemetry.Click += BtnRefreshTelemetry_Click;

        chkAutoRefresh = new CheckBox { Text = "Auto refresh (5s)", Location = new Point(116, 12), AutoSize = true };
        chkAutoRefresh.CheckedChanged += ChkAutoRefresh_CheckedChanged;

        chkOnlyAnomalies = new CheckBox { Text = "Anomalies only", Location = new Point(250, 12), AutoSize = true };
        chkOnlyAnomalies.CheckedChanged += Filter_Changed;

        Label lblDevice = new() { Text = "Device", Location = new Point(370, 13), AutoSize = true };
        cboTelemetryDevice = new ComboBox
        {
            Location = new Point(424, 9),
            Width = 170,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboTelemetryDevice.SelectedIndexChanged += Filter_Changed;

        Label lblZone = new() { Text = "Zone", Location = new Point(4, 52), AutoSize = true };
        cboZone = new ComboBox
        {
            Location = new Point(48, 48),
            Width = 250,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnCombineZone = new Button
        {
            Text = "Combine zone readings with the + operator",
            Location = new Point(310, 47),
            Size = new Size(280, 28)
        };
        btnCombineZone.Click += BtnCombineZone_Click;

        toolbar.Controls.AddRange(new Control[]
        {
            btnRefreshTelemetry, chkAutoRefresh, chkOnlyAnomalies, lblDevice,
            cboTelemetryDevice, lblZone, cboZone, btnCombineZone
        });

        lblZoneResult = new Label
        {
            Dock = DockStyle.Top,
            Height = 44,
            Text = "Pick a zone and combine the readings to see the overloaded + operator in action.",
            ForeColor = Color.DimGray,
            Padding = new Padding(4, 6, 4, 4)
        };

        dgvTelemetry = BuildGrid();
        dgvTelemetry.Dock = DockStyle.Fill;

        page.Controls.Add(dgvTelemetry);
        page.Controls.Add(lblZoneResult);
        page.Controls.Add(toolbar);
        return page;
    }

    // Co-authored by Claude
    private TabPage BuildAnomaliesTab()
    {
        TabPage page = new("Anomalies") { BackColor = Color.White, Padding = new Padding(10) };

        Panel toolbar = new() { Dock = DockStyle.Top, Height = 44 };

        btnRefreshAnomalies = new Button { Text = "Refresh", Location = new Point(4, 8), Size = new Size(100, 28) };
        btnRefreshAnomalies.Click += BtnRefreshAnomalies_Click;

        chkShowAcknowledged = new CheckBox
        {
            Text = "Include acknowledged",
            Location = new Point(116, 13),
            AutoSize = true
        };
        chkShowAcknowledged.CheckedChanged += Filter_Changed;

        toolbar.Controls.AddRange(new Control[] { btnRefreshAnomalies, chkShowAcknowledged });

        Panel actions = new() { Dock = DockStyle.Bottom, Height = 108 };

        actions.Controls.Add(new Label { Text = "Operator", Location = new Point(4, 14), AutoSize = true });
        txtOperator = new TextBox { Location = new Point(70, 11), Width = 160 };
        actions.Controls.Add(txtOperator);

        actions.Controls.Add(new Label { Text = "Note", Location = new Point(248, 14), AutoSize = true });
        txtNote = new TextBox
        {
            Location = new Point(288, 11),
            Width = 520,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        actions.Controls.Add(txtNote);

        btnAcknowledge = new Button
        {
            Text = "Acknowledge selected anomaly",
            Location = new Point(4, 48),
            Size = new Size(230, 32)
        };
        btnAcknowledge.Click += BtnAcknowledge_Click;
        actions.Controls.Add(btnAcknowledge);

        lblAckStatus = new Label
        {
            Text = "Select an unacknowledged row, add your name and a note, then acknowledge.",
            Location = new Point(248, 56),
            Size = new Size(600, 40),
            ForeColor = Color.DimGray,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        actions.Controls.Add(lblAckStatus);

        dgvAnomalies = BuildGrid();
        dgvAnomalies.Dock = DockStyle.Fill;

        page.Controls.Add(dgvAnomalies);
        page.Controls.Add(actions);
        page.Controls.Add(toolbar);
        return page;
    }

    // Co-authored by Claude
    /// <summary>Shared grid setup so all three tabs look and behave the same.</summary>
    private static DataGridView BuildGrid() => new()
    {
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        EnableHeadersVisualStyles = false
    };
}

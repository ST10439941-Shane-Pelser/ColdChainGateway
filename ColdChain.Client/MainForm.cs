using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using ColdChain.Shared.Models;

namespace ColdChain.Client;

/// <summary>
/// The operator console. Every screen reads and writes through ApiClient, so the
/// frontend only ever sees what the gateway chooses to expose over HTTP.
/// </summary>
public partial class MainForm : Form
{
    private ApiClient? _api;
    private string? _selectedEvidencePath;

    public MainForm()
    {
        BuildUi();
        Shown += MainForm_Shown;
    }

    // ---------------------------------------------------------------- startup

    // Co-authored by Claude
    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        // Try the default address once. If the API is not running yet the operator
        // can start it and press Connect, so a failure here is not fatal.
        await ConnectAsync(silent: true);
    }

    // Co-authored by Claude
    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        await ConnectAsync(silent: false);
    }

    // Co-authored by Claude
    private async Task ConnectAsync(bool silent)
    {
        string url = txtBaseUrl.Text.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed) || parsed.Scheme is not ("http" or "https"))
        {
            SetConnectionStatus("Invalid gateway URL", false);

            if (!silent)
                MessageBox.Show(this, "Enter a full gateway URL, for example http://localhost:5165.",
                    "Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return;
        }

        UseWaitCursor = true;

        try
        {
            var client = new ApiClient(url);

            // A cheap round trip that proves the gateway is reachable.
            await client.GetDevicesAsync();

            _api = client;
            SetConnectionStatus($"Connected to {url}", true);

            await LoadReferenceDataAsync();
            await RefreshDevicesAsync();
            await RefreshTelemetryAsync();
            await RefreshAnomaliesAsync();
        }
        catch (Exception ex)
        {
            _api = null;
            SetConnectionStatus("Not connected", false);

            if (!silent)
                ShowError("Connection failed", ex);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    // Co-authored by Claude
    /// <summary>Fills the dropdowns that come from the API: locations, types and zones.</summary>
    private async Task LoadReferenceDataAsync()
    {
        if (_api is null)
            return;

        cboDeviceType.Items.Clear();
        cboDeviceType.Items.AddRange(DeviceTypes.All);
        cboDeviceType.SelectedIndex = 0;

        cboFilterType.Items.Clear();
        cboFilterType.Items.Add("All types");
        cboFilterType.Items.AddRange(DeviceTypes.All);
        cboFilterType.SelectedIndex = 0;

        List<LocationOption> locations = await _api.GetLocationOptionsAsync();
        cboLocation.Items.Clear();
        foreach (LocationOption option in locations)
            cboLocation.Items.Add(option);

        if (cboLocation.Items.Count > 0)
            cboLocation.SelectedIndex = 0;

        List<ZoneInfo> zones = await _api.GetZonesAsync();
        cboZone.Items.Clear();
        foreach (ZoneInfo zone in zones)
            cboZone.Items.Add(zone);

        if (cboZone.Items.Count > 0)
            cboZone.SelectedIndex = 0;
    }

    // ---------------------------------------------------------------- devices

    // Co-authored by Claude
    private async void BtnRefreshDevices_Click(object? sender, EventArgs e)
    {
        await RefreshDevicesAsync();
    }

    // Co-authored by Claude
    private async Task RefreshDevicesAsync()
    {
        if (_api is null)
            return;

        try
        {
            string? type = cboFilterType.SelectedIndex > 0 ? cboFilterType.SelectedItem?.ToString() : null;
            List<Device> devices = await _api.GetDevicesAsync(txtSearch.Text.Trim(), type);

            dgvDevices.DataSource = DeviceRow.From(devices);
            StyleGrid(dgvDevices);

            // Keep the telemetry device filter in step with what is registered.
            string? previous = cboTelemetryDevice.SelectedItem?.ToString();
            cboTelemetryDevice.Items.Clear();
            cboTelemetryDevice.Items.Add("All devices");

            foreach (Device device in devices)
                cboTelemetryDevice.Items.Add(device.DeviceId);

            int index = previous is null ? 0 : cboTelemetryDevice.Items.IndexOf(previous);
            cboTelemetryDevice.SelectedIndex = index < 0 ? 0 : index;
        }
        catch (Exception ex)
        {
            ShowError("Could not load devices", ex);
        }
    }

    // Co-authored by Claude
    private async void BtnRegister_Click(object? sender, EventArgs e)
    {
        if (_api is null)
        {
            MessageBox.Show(this, "Connect to the gateway first.", "Not connected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Local checks first, so obvious mistakes never leave the machine.
        if (string.IsNullOrWhiteSpace(txtDeviceId.Text) || string.IsNullOrWhiteSpace(txtDeviceName.Text))
        {
            lblRegisterStatus.ForeColor = AnomalyFore;
            lblRegisterStatus.Text = "Device ID and device name are both required.";
            return;
        }

        if (cboLocation.SelectedItem is not LocationOption location)
        {
            lblRegisterStatus.ForeColor = AnomalyFore;
            lblRegisterStatus.Text = "Select a location for the device.";
            return;
        }

        var request = new DeviceRegistrationRequest
        {
            DeviceId = txtDeviceId.Text.Trim(),
            DeviceName = txtDeviceName.Text.Trim(),
            DeviceType = cboDeviceType.SelectedItem?.ToString() ?? string.Empty,
            LocationCode = location.Code,
            IsActive = chkIsActive.Checked
        };

        btnRegister.Enabled = false;

        try
        {
            Device created = await _api.RegisterDeviceAsync(request);

            lblRegisterStatus.ForeColor = Color.DarkGreen;
            lblRegisterStatus.Text = $"Registered {created.DeviceId} at {created.LocationPath}.";

            txtDeviceId.Clear();
            txtDeviceName.Clear();

            await RefreshDevicesAsync();
        }
        catch (ApiException ex)
        {
            // The gateway's validation messages are already readable, so show them as they are.
            lblRegisterStatus.ForeColor = AnomalyFore;
            lblRegisterStatus.Text = "Registration rejected. See the message.";
            MessageBox.Show(this, ex.Message, "Registration rejected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowError("Registration failed", ex);
        }
        finally
        {
            btnRegister.Enabled = true;
        }
    }

    // ---------------------------------------------------------------- evidence

    // Co-authored by Claude
    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select evidence file",
            Filter = "Evidence files (*.jpg;*.jpeg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _selectedEvidencePath = dialog.FileName;
        lblSelectedFile.Text = Path.GetFileName(dialog.FileName);
        lblSelectedFile.ForeColor = Color.Black;
    }

    // Co-authored by Claude
    private async void BtnUpload_Click(object? sender, EventArgs e)
    {
        if (_api is null)
        {
            MessageBox.Show(this, "Connect to the gateway first.", "Not connected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (dgvDevices.CurrentRow?.DataBoundItem is not DeviceRow selected)
        {
            lblEvidenceStatus.ForeColor = AnomalyFore;
            lblEvidenceStatus.Text = "Select a device in the grid first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedEvidencePath))
        {
            lblEvidenceStatus.ForeColor = AnomalyFore;
            lblEvidenceStatus.Text = "Browse for a JPG, PNG or PDF first.";
            return;
        }

        btnUpload.Enabled = false;

        try
        {
            AttachmentMetadata metadata = await _api.UploadEvidenceAsync(
                selected.DeviceId, _selectedEvidencePath, txtEvidenceDescription.Text);

            lblEvidenceStatus.ForeColor = Color.DarkGreen;
            lblEvidenceStatus.Text =
                $"Uploaded {metadata.OriginalFileName} ({metadata.SizeBytes / 1024d:0.0} KB) to {metadata.DeviceId}.";

            _selectedEvidencePath = null;
            lblSelectedFile.Text = "No file selected";
            txtEvidenceDescription.Clear();

            await RefreshDevicesAsync();
        }
        catch (ApiException ex)
        {
            lblEvidenceStatus.ForeColor = AnomalyFore;
            lblEvidenceStatus.Text = "Upload rejected by the gateway.";
            MessageBox.Show(this, ex.Message, "Upload rejected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowError("Upload failed", ex);
        }
        finally
        {
            btnUpload.Enabled = true;
        }
    }

    // ---------------------------------------------------------------- telemetry

    // Co-authored by Claude
    private async void BtnRefreshTelemetry_Click(object? sender, EventArgs e)
    {
        await RefreshTelemetryAsync();
    }

    // Co-authored by Claude
    private async void Filter_Changed(object? sender, EventArgs e)
    {
        if (sender == chkShowAcknowledged)
            await RefreshAnomaliesAsync();
        else
            await RefreshTelemetryAsync();
    }

    // Co-authored by Claude
    private void ChkAutoRefresh_CheckedChanged(object? sender, EventArgs e)
    {
        refreshTimer.Enabled = chkAutoRefresh.Checked && _api is not null;
    }

    // Co-authored by Claude
    private async void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        await RefreshTelemetryAsync();
        await RefreshAnomaliesAsync();
    }

    // Co-authored by Claude
    private async Task RefreshTelemetryAsync()
    {
        if (_api is null)
            return;

        try
        {
            string? deviceId = cboTelemetryDevice.SelectedIndex > 0
                ? cboTelemetryDevice.SelectedItem?.ToString()
                : null;

            List<TelemetryDto> packets = await _api.GetTelemetryAsync(deviceId, chkOnlyAnomalies.Checked);

            dgvTelemetry.DataSource = TelemetryRow.From(packets);
            StyleGrid(dgvTelemetry);
            HighlightRows(dgvTelemetry, "Status", "ANOMALY");
        }
        catch (Exception ex)
        {
            // A failing timer must not spam message boxes, so switch auto refresh off.
            refreshTimer.Enabled = false;
            chkAutoRefresh.Checked = false;
            ShowError("Could not load telemetry", ex);
        }
    }

    // Co-authored by Claude
    /// <summary>
    /// Asks the gateway to fold every temperature reading in the selected zone into
    /// one figure. The addition itself happens in TemperatureReading's + operator.
    /// </summary>
    private async void BtnCombineZone_Click(object? sender, EventArgs e)
    {
        if (_api is null || cboZone.SelectedItem is not ZoneInfo zone)
            return;

        try
        {
            ZoneTemperatureSummary summary = await _api.GetZoneAverageAsync(zone.ZoneIndex);

            lblZoneResult.ForeColor = summary.IsOutOfRange ? AnomalyFore : Color.DarkGreen;
            lblZoneResult.Text =
                $"{summary.ZoneName}: combined {summary.ReadingsCombined} reading(s) into " +
                $"{summary.AverageCelsius.ToString("0.00", CultureInfo.InvariantCulture)} C. {summary.Explanation}";
        }
        catch (Exception ex)
        {
            ShowError("Could not combine zone readings", ex);
        }
    }

    // ---------------------------------------------------------------- anomalies

    // Co-authored by Claude
    private async void BtnRefreshAnomalies_Click(object? sender, EventArgs e)
    {
        await RefreshAnomaliesAsync();
    }

    // Co-authored by Claude
    private async Task RefreshAnomaliesAsync()
    {
        if (_api is null)
            return;

        try
        {
            // Unchecked means show outstanding anomalies only.
            bool? acknowledged = chkShowAcknowledged.Checked ? null : false;

            List<AnomalyRecord> anomalies = await _api.GetAnomaliesAsync(acknowledged);

            dgvAnomalies.DataSource = AnomalyRow.From(anomalies);
            StyleGrid(dgvAnomalies);
            HighlightRows(dgvAnomalies, "Acknowledged", "No");
        }
        catch (Exception ex)
        {
            ShowError("Could not load anomalies", ex);
        }
    }

    // Co-authored by Claude
    private async void BtnAcknowledge_Click(object? sender, EventArgs e)
    {
        if (_api is null)
        {
            MessageBox.Show(this, "Connect to the gateway first.", "Not connected",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (dgvAnomalies.CurrentRow?.DataBoundItem is not AnomalyRow selected)
        {
            lblAckStatus.ForeColor = AnomalyFore;
            lblAckStatus.Text = "Select an anomaly row first.";
            return;
        }

        if (selected.Acknowledged == "Yes")
        {
            lblAckStatus.ForeColor = AnomalyFore;
            lblAckStatus.Text = $"Anomaly {selected.Id} was already acknowledged by {selected.By}.";
            return;
        }

        var request = new AcknowledgeRequest
        {
            OperatorName = txtOperator.Text.Trim(),
            Note = txtNote.Text.Trim()
        };

        btnAcknowledge.Enabled = false;

        try
        {
            AnomalyRecord updated = await _api.AcknowledgeAsync(selected.Id, request);

            lblAckStatus.ForeColor = Color.DarkGreen;
            lblAckStatus.Text = $"Anomaly {updated.Id} acknowledged by {updated.AcknowledgedBy}.";
            txtNote.Clear();

            await RefreshAnomaliesAsync();
        }
        catch (ApiException ex)
        {
            lblAckStatus.ForeColor = AnomalyFore;
            lblAckStatus.Text = "Acknowledgement rejected.";
            MessageBox.Show(this, ex.Message, "Acknowledgement rejected",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowError("Acknowledgement failed", ex);
        }
        finally
        {
            btnAcknowledge.Enabled = true;
        }
    }

    // ---------------------------------------------------------------- helpers

    // Co-authored by Claude
    private void SetConnectionStatus(string message, bool connected)
    {
        lblConnectionStatus.Text = message;
        lblConnectionStatus.ForeColor = connected ? Color.PaleGreen : Color.LightCoral;

        if (!connected)
        {
            refreshTimer.Enabled = false;
            chkAutoRefresh.Checked = false;
        }
    }

    // Co-authored by Claude
    private static void StyleGrid(DataGridView grid)
    {
        grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBack;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersHeight = 30;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 251);
    }

    // Co-authored by Claude
    /// <summary>Paints rows red when the named column holds the flagged value.</summary>
    private static void HighlightRows(DataGridView grid, string columnName, string flaggedValue)
    {
        if (!grid.Columns.Contains(columnName))
            return;

        foreach (DataGridViewRow row in grid.Rows)
        {
            string? value = row.Cells[columnName].Value?.ToString();

            if (string.Equals(value, flaggedValue, StringComparison.OrdinalIgnoreCase))
            {
                row.DefaultCellStyle.BackColor = AnomalyBack;
                row.DefaultCellStyle.ForeColor = AnomalyFore;
            }
        }
    }

    // Co-authored by Claude
    /// <summary>One place to surface any failure without letting the form crash.</summary>
    private void ShowError(string title, Exception ex)
    {
        string message = ex switch
        {
            ApiException apiEx => apiEx.Message,
            _ => $"Unexpected error: {ex.Message}"
        };

        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}

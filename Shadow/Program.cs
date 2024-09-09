using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading.Tasks;
using Microsoft.Win32;

public class ShadowingApp : Form
{
    private TextBox? ipv4TextBox;
    private TextBox? ipv6TextBox;
    private TextBox? hostnameTextBox;
    private Label? errorLabel;
    private Label? statusLabel;
    private Panel? loadingSpinnerPanel;
    private System.Windows.Forms.Timer? loadingTimer;
    private Button? okButton;
    private Button? cancelButton;
    private ListView? sessionListView;
    private Button? monitorButton;
    private Button? controlButton;
    private Button? backButton;
    private Label? instructionLabel;
    private string? selectedTarget;
    private int spinnerIndex = 0;
    private Dictionary<string, System.Windows.Forms.Timer> connectedHosts = new Dictionary<string, System.Windows.Forms.Timer>();

    private List<SmokeParticle> smokeParticles;
    private Random random;
    private System.Windows.Forms.Timer animationTimer;

    private class SmokeParticle
    {
        public PointF Position { get; set; }
        public float Size { get; set; }
        public float Opacity { get; set; }
        public float Angle { get; set; }
        public PointF Control1 { get; set; }
        public PointF Control2 { get; set; }
    }

    public ShadowingApp()
    {
        InitializeComponent();
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Initialize smoke animation
        smokeParticles = new List<SmokeParticle>();
        random = new Random();

        // Set up timer for smoke animation
        animationTimer = new System.Windows.Forms.Timer();
        animationTimer.Interval = 50;
        animationTimer.Tick += AnimationTimer_Tick;
        animationTimer.Start();

        // Enable double buffering
        this.DoubleBuffered = true;
    }

    private void InitializeComponent()
    {
        // Form settings
        this.Text = "Shadow";
        this.Size = new Size(420, 380);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.DoubleBuffered = true;

        // Set the form's paint event
        this.Paint += ShadowingApp_Paint;

        // Instruction Label
        Label instructionLabel = new Label();
        instructionLabel.Text = "Please select a method to identify a target machine:";
        instructionLabel.Location = new Point(20, 20);
        instructionLabel.Size = new Size(380, 40);
        instructionLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        instructionLabel.ForeColor = Color.FromArgb(200, 200, 200);
        this.Controls.Add(instructionLabel);

        // IPv4 Label
        Label ipv4Label = CreateLabel("IPv4:", new Point(20, 60));
        this.Controls.Add(ipv4Label);

        // IPv4 TextBox
        ipv4TextBox = CreateTextBox("Enter IPv4 address", new Point(150, 60));
        ipv4TextBox.TextChanged += (s, e) => HandleInputChanged(ipv4TextBox, ipv6TextBox, hostnameTextBox);

        // IPv6 Label
        Label ipv6Label = CreateLabel("IPv6:", new Point(20, 100));
        this.Controls.Add(ipv6Label);

        // IPv6 TextBox
        ipv6TextBox = CreateTextBox("Enter IPv6 address", new Point(150, 100));
        ipv6TextBox.TextChanged += (s, e) => HandleInputChanged(ipv6TextBox, ipv4TextBox, hostnameTextBox);

        // Hostname Label
        Label hostnameLabel = CreateLabel("Hostname:", new Point(20, 140));
        this.Controls.Add(hostnameLabel);

        // Hostname TextBox
        hostnameTextBox = CreateTextBox("Enter Hostname", new Point(150, 140));
        hostnameTextBox.TextChanged += (s, e) => HandleInputChanged(hostnameTextBox, ipv4TextBox, ipv6TextBox);

        // Error Label
        errorLabel = new Label();
        errorLabel.Location = new Point(20, 180);
        errorLabel.Size = new Size(380, 40);
        errorLabel.ForeColor = Color.Red;
        errorLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        this.Controls.Add(errorLabel);

        // Status Label
        statusLabel = new Label();
        statusLabel.Location = new Point(50, 230);
        statusLabel.Size = new Size(300, 20);
        statusLabel.ForeColor = Color.FromArgb(0, 200, 0);
        statusLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        statusLabel.Visible = false;
        this.Controls.Add(statusLabel);

        // Loading Spinner Panel
        loadingSpinnerPanel = new Panel();
        loadingSpinnerPanel.Size = new Size(20, 20);
        loadingSpinnerPanel.Location = new Point(20, 230);
        loadingSpinnerPanel.Visible = false;
        this.Controls.Add(loadingSpinnerPanel);

        // Loading Timer
        loadingTimer = new System.Windows.Forms.Timer();
        loadingTimer.Interval = 100;
        loadingTimer.Tick += LoadingTimer_Tick;

        // OK Button
        okButton = CreateButton("OK", new Point(100, 270));
        okButton.Click += OkButton_Click;
        this.Controls.Add(okButton);

        // Cancel Button
        cancelButton = CreateButton("Cancel", new Point(210, 270));
        cancelButton.Click += (s, e) => this.Close();
        this.Controls.Add(cancelButton);

        // Initialize other fields
        sessionListView = new ListView();
        monitorButton = new Button();
        controlButton = new Button();
        backButton = new Button();
        this.instructionLabel = new Label();
    }

    private Label CreateLabel(string text, Point location)
    {
        Label label = new Label();
        label.Text = text;
        label.Location = location;
        label.Size = new Size(120, 20);
        label.Font = new Font("Segoe UI", 9);
        label.ForeColor = Color.FromArgb(200, 200, 200);
        return label;
    }

    private TextBox CreateTextBox(string placeholder, Point location)
    {
        TextBox textBox = new TextBox();
        textBox.Location = location;
        textBox.Width = 200;
        textBox.Font = new Font("Segoe UI", 9);
        textBox.ForeColor = Color.FromArgb(200, 200, 200);
        textBox.BackColor = Color.FromArgb(45, 45, 45);
        textBox.PlaceholderText = placeholder;
        this.Controls.Add(textBox);
        return textBox;
    }

    private Button CreateButton(string text, Point location)
    {
        Button button = new Button();
        button.Text = text;
        button.Location = location;
        button.Size = new Size(100, 30);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(100, 100, 100);
        button.BackColor = Color.FromArgb(60, 60, 60);
        button.ForeColor = Color.FromArgb(200, 200, 200);
        button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        return button;
    }

    private void HandleInputChanged(TextBox source, TextBox? clear1, TextBox? clear2)
    {
        if (!string.IsNullOrWhiteSpace(source.Text))
        {
            clear1?.Clear();
            clear2?.Clear();
        }
    }

    private async void OkButton_Click(object? sender, EventArgs e)
    {
        if (errorLabel != null) errorLabel.Text = "";
        selectedTarget = GetValidatedInput();
        if (selectedTarget == null)
        {
            ShowError("Please enter a valid IPv4, IPv6, or Hostname.");
            return;
        }

        ShowStatus("Connecting...", true);
        await Task.Delay(1000);

        if (!AttemptConnection(selectedTarget))
        {
            ShowStatus(string.Empty);
            ShowError("Target unreachable. Please check the address or hostname and try again.");
            return;
        }

        ShowStatus(string.Empty);
        var sessions = GetRemoteUserSessionsUsingQuser(selectedTarget);
        if (sessions.Count == 0)
        {
            ShowError("No active sessions found on the target.");
            return;
        }

        ShowUserSelectionDialog(sessions);
    }

    private void ShowStatus(string message, bool showSpinner = false)
    {
        if (statusLabel != null)
        {
            statusLabel.Text = message;
            statusLabel.Visible = !string.IsNullOrEmpty(message);
        }
        if (loadingSpinnerPanel != null)
        {
            loadingSpinnerPanel.Visible = showSpinner && !string.IsNullOrEmpty(message);
            if (statusLabel != null)
            {
                loadingSpinnerPanel.Location = new Point(statusLabel.Left - 25, statusLabel.Top);
            }
        }
        if (!string.IsNullOrEmpty(message) && showSpinner)
        {
            spinnerIndex = 0;
            loadingTimer?.Start();
        }
        else
        {
            loadingTimer?.Stop();
            loadingSpinnerPanel?.Invalidate();
        }
    }

    private void LoadingTimer_Tick(object? sender, EventArgs e)
    {
        spinnerIndex = (spinnerIndex + 1) % 5;
        loadingSpinnerPanel?.Invalidate();
        if (loadingSpinnerPanel != null)
        {
            loadingSpinnerPanel.Paint += (s, pe) =>
            {
                pe.Graphics.Clear(loadingSpinnerPanel.BackColor);
                int size = 5;
                int offset = 5;

                Point[] positions = new Point[]
                {
                    new Point(offset, offset),
                    new Point(loadingSpinnerPanel.Width - offset - size, offset),
                    new Point(loadingSpinnerPanel.Width - offset - size, loadingSpinnerPanel.Height - offset - size),
                    new Point(offset, loadingSpinnerPanel.Height - offset - size),
                    new Point(offset, offset)
                };

                pe.Graphics.FillEllipse(Brushes.Green, positions[spinnerIndex].X, positions[spinnerIndex].Y, size, size);
            };
        }
    }

    private string? GetValidatedInput()
    {
        if (!string.IsNullOrEmpty(ipv4TextBox?.Text) && Regex.IsMatch(ipv4TextBox.Text, @"^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
            return ipv4TextBox.Text;

        if (!string.IsNullOrEmpty(ipv6TextBox?.Text) && Regex.IsMatch(ipv6TextBox.Text, @"^([0-9a-fA-F]{1,4}:){7}([0-9a-fA-F]{1,4}|:)$"))
            return ipv6TextBox.Text;

        if (!string.IsNullOrEmpty(hostnameTextBox?.Text) && Regex.IsMatch(hostnameTextBox.Text, @"^[a-zA-Z0-9\-\.]{1,63}$"))
            return hostnameTextBox.Text;

        return null;
    }

    private bool AttemptConnection(string target)
    {
        try
        {
            using (var ping = new System.Net.NetworkInformation.Ping())
            {
                var reply = ping.Send(target, 3000);
                return reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
        }
        catch
        {
            return false;
        }
    }

    private List<(string UserName, string SessionId, string State)> GetRemoteUserSessionsUsingQuser(string target)
    {
        var sessions = new List<(string UserName, string SessionId, string State)>();

        string[] pathsToTry = { @"C:\Windows\System32\quser.exe", @"C:\Windows\Sysnative\quser.exe", "quser" };

        foreach (var path in pathsToTry)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(path, $"/server:{target}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        continue;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    int exitCode = process.ExitCode;

                    if (!string.IsNullOrWhiteSpace(errorOutput) || exitCode != 0)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(output))
                    {
                        continue;
                    }

                    string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = Regex.Split(line, @"\s{2,}");

                        if (parts.Length >= 4)
                        {
                            string userName = parts[0];
                            string sessionName = parts.Length == 6 ? parts[1] : "";
                            string sessionId = parts[parts.Length - 4];
                            string state = parts[parts.Length - 3];

                            sessions.Add((userName, sessionId, state));
                        }
                    }

                    if (sessions.Count > 0)
                    {
                        return sessions;
                    }
                }
            }
            catch
            {
                // If an exception occurs, continue to the next path
                continue;
            }
        }

        return sessions;
    }

    private void ShowUserSelectionDialog(List<(string UserName, string SessionId, string State)> sessions)
    {
        this.Controls.Clear();

        // Clear any existing status messages
        ShowStatus(string.Empty);

        this.Text = "Session Shadowing";

        Label headerLabel = new Label();
        headerLabel.Text = "Select a user's session to shadow:";
        headerLabel.Location = new Point(20, 20);
        headerLabel.Size = new Size(350, 30);
        headerLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        headerLabel.ForeColor = Color.FromArgb(200, 200, 200);
        this.Controls.Add(headerLabel);

        sessionListView = new ListView();
        sessionListView.Location = new Point(20, 60);
        sessionListView.Size = new Size(350, 100);
        sessionListView.View = View.Details;
        sessionListView.FullRowSelect = true;
        sessionListView.Columns.Add("Username", 100);
        sessionListView.Columns.Add("Session ID", 100);
        sessionListView.Columns.Add("State", 150);
        sessionListView.Font = new Font("Segoe UI", 9);
        sessionListView.BackColor = Color.FromArgb(45, 45, 45);
        sessionListView.ForeColor = Color.FromArgb(200, 200, 200);
        sessionListView.ItemSelectionChanged += SessionListView_ItemSelectionChanged;
        this.Controls.Add(sessionListView);

        foreach (var session in sessions)
        {
            var listItem = new ListViewItem(session.UserName);
            listItem.SubItems.Add(session.SessionId);
            listItem.SubItems.Add(session.State);
            sessionListView.Items.Add(listItem);
        }

        instructionLabel = new Label();
        instructionLabel.Text = "Please select a session to shadow.";
        instructionLabel.Location = new Point(20, 170);
        instructionLabel.Size = new Size(350, 20);
        instructionLabel.Font = new Font("Segoe UI", 9, FontStyle.Italic);
        instructionLabel.ForeColor = Color.FromArgb(200, 200, 200);
        this.Controls.Add(instructionLabel);

        monitorButton = CreateButton("Monitor", new Point(100, 200));
        monitorButton.Click += (s, e) => StartShadowSession("monitor");
        monitorButton.Visible = false;
        this.Controls.Add(monitorButton);

        controlButton = CreateButton("Control", new Point(210, 200));
        controlButton.Click += (s, e) => StartShadowSession("control");
        controlButton.Visible = false;
        this.Controls.Add(controlButton);

        backButton = CreateButton("Back", new Point(20, 270));
        backButton.Click += (s, e) => ShowInitialScreen();
        this.Controls.Add(backButton);

        // Add status label and loading spinner
        statusLabel = new Label();
        statusLabel.Location = new Point(20, 240);
        statusLabel.Size = new Size(350, 20);
        statusLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        statusLabel.ForeColor = Color.FromArgb(0, 200, 0);
        statusLabel.Visible = false;
        this.Controls.Add(statusLabel);

        loadingSpinnerPanel = new Panel();
        loadingSpinnerPanel.Size = new Size(20, 20);
        loadingSpinnerPanel.Location = new Point(380, 240);
        loadingSpinnerPanel.Visible = false;
        this.Controls.Add(loadingSpinnerPanel);

        // Add error label
        errorLabel = new Label();
        errorLabel.Location = new Point(20, 180);
        errorLabel.Size = new Size(380, 40);
        errorLabel.ForeColor = Color.Red;
        errorLabel.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        this.Controls.Add(errorLabel);
    }

    private void SessionListView_ItemSelectionChanged(object? sender, ListViewItemSelectionChangedEventArgs e)
    {
        bool isItemSelected = sessionListView?.SelectedItems.Count > 0;
        if (monitorButton != null) monitorButton.Visible = isItemSelected;
        if (controlButton != null) controlButton.Visible = isItemSelected;
        if (instructionLabel != null) instructionLabel.Visible = !isItemSelected;
    }

    private void StartShadowSession(string mode)
    {
        var selectedSession = sessionListView?.SelectedItems.Count > 0 ? sessionListView.SelectedItems[0] : null;
        if (selectedSession == null || selectedTarget == null)
        {
            ShowError("Please select a session to shadow.");
            return;
        }

        string sessionId = selectedSession.SubItems[1].Text;
        int modeValue = mode == "monitor" ? 4 : 2;

        // Configure registry
        if (!ConfigureRegistry(modeValue))
        {
            ShowError("Failed to configure for shadow session.");
            return;
        }

        ShowStatus("Shadow session request sent.", false);
        StartMessageFadeTimer();

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "MSTSC.EXE",
                Arguments = $"/v:{selectedTarget} /shadow:{sessionId} /noConsentPrompt{(mode == "control" ? " /control" : "")}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process shadowProcess = new Process { StartInfo = startInfo })
            {
                shadowProcess.Start();

                // Start a timer to reset the registry after 10 seconds
                System.Windows.Forms.Timer resetTimer = new System.Windows.Forms.Timer();
                resetTimer.Interval = 10000; // 10 seconds
                resetTimer.Tick += (s, e) =>
                {
                    ConfigureRegistry(0);
                    resetTimer.Stop();
                    resetTimer.Dispose();
                    if (selectedTarget != null)
                    {
                        connectedHosts[selectedTarget] = null;
                    }
                };
                resetTimer.Start();

                // Add or update the host in our dictionary
                if (selectedTarget != null)
                {
                    if (connectedHosts.ContainsKey(selectedTarget))
                    {
                        connectedHosts[selectedTarget]?.Dispose();
                    }
                    connectedHosts[selectedTarget] = resetTimer;
                }

                // Don't wait for the process to exit
                Task.Run(async () =>
                {
                    string output = await shadowProcess.StandardOutput.ReadToEndAsync();
                    string errorOutput = await shadowProcess.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(errorOutput) || !string.IsNullOrEmpty(output))
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            if (errorOutput.Contains("The specified session is not connected") || output.Contains("The specified session is not connected"))
                            {
                                ShowError("The specified session is not connected.");
                            }
                            else
                            {
                                ShowError($"Failed to start shadow session: {errorOutput}");
                            }
                            resetTimer.Stop();
                            resetTimer.Dispose();
                            if (selectedTarget != null)
                            {
                                connectedHosts.Remove(selectedTarget);
                            }
                            ConfigureRegistry(0);
                        });
                    }
                });
            }
        }
        catch (Exception ex)
        {
            ShowError($"An error occurred while starting the shadow session: {ex.Message}");
            ConfigureRegistry(0);
        }
    }

    private bool ConfigureRegistry(int value)
    {
        if (string.IsNullOrEmpty(selectedTarget))
        {
            ShowError("Target machine is not specified.");
            return false;
        }

        try
        {
            using (RegistryKey remoteRegistry = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, selectedTarget))
            {
                using (RegistryKey key = remoteRegistry.OpenSubKey(@"Software\Policies\Microsoft\Windows NT\Terminal Services", true))
                {
                    if (key == null)
                    {
                        using (RegistryKey newKey = remoteRegistry.CreateSubKey(@"Software\Policies\Microsoft\Windows NT\Terminal Services"))
                        {
                            newKey.SetValue("Shadow", value, RegistryValueKind.DWord);
                        }
                    }
                    else
                    {
                        key.SetValue("Shadow", value, RegistryValueKind.DWord);
                    }
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to configure remote registry: {ex.Message}");
            return false;
        }
    }

    private void StartMessageFadeTimer()
    {
        System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer();
        fadeTimer.Interval = 5000;
        fadeTimer.Tick += (s, e) =>
        {
            ShowStatus(string.Empty);
            fadeTimer.Stop();
            fadeTimer.Dispose();
        };
        fadeTimer.Start();
    }

    private void ShowInitialScreen()
    {
        this.Controls.Clear();
        InitializeComponent();
    }

    private void ShowError(string message)
    {
        if (errorLabel != null)
        {
            errorLabel.Text = message;
            errorLabel.ForeColor = Color.Red;
            errorLabel.Visible = true;
        }
        ShakeWindow();
    }

    private void ShakeWindow()
    {
        var original = this.Location;
        var rnd = new Random(1337);
        const int shakeAmplitude = 10;
        for (int i = 0; i < 10; i++)
        {
            this.Location = new Point(original.X + rnd.Next(-shakeAmplitude, shakeAmplitude), original.Y + rnd.Next(-shakeAmplitude, shakeAmplitude));
            System.Threading.Thread.Sleep(20);
        }
        this.Location = original;
    }

    private void AnimationTimer_Tick(object sender, EventArgs e)
    {
        // Add new smoke particles
        if (smokeParticles.Count < 20)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float size = random.Next(30, 60);
            PointF position = new PointF(random.Next(Width), Height + size);

            smokeParticles.Add(new SmokeParticle
            {
                Position = position,
                Size = size,
                Opacity = 0.7f,
                Angle = angle,
                Control1 = new PointF(
                    position.X + (float)Math.Cos(angle) * size * 0.5f,
                    position.Y + (float)Math.Sin(angle) * size * 0.5f
                ),
                Control2 = new PointF(
                    position.X + (float)Math.Cos(angle + Math.PI) * size * 0.5f,
                    position.Y + (float)Math.Sin(angle + Math.PI) * size * 0.5f
                )
            });
        }

        // Update existing particles
        for (int i = smokeParticles.Count - 1; i >= 0; i--)
        {
            var particle = smokeParticles[i];
            particle.Position = new PointF(
                particle.Position.X + (float)(random.NextDouble() * 2 - 1),
                particle.Position.Y - (1 + random.Next(2)));
            particle.Opacity -= 0.01f;
            particle.Size += 0.5f;
            particle.Angle += (float)(random.NextDouble() * 0.1 - 0.05);

            // Update control points
            particle.Control1 = new PointF(
                particle.Position.X + (float)Math.Cos(particle.Angle) * particle.Size * 0.5f,
                particle.Position.Y + (float)Math.Sin(particle.Angle) * particle.Size * 0.5f
            );
            particle.Control2 = new PointF(
                particle.Position.X + (float)Math.Cos(particle.Angle + Math.PI) * particle.Size * 0.5f,
                particle.Position.Y + (float)Math.Sin(particle.Angle + Math.PI) * particle.Size * 0.5f
            );

            if (particle.Position.Y + particle.Size < 0 || particle.Opacity <= 0)
            {
                smokeParticles.RemoveAt(i);
            }
        }

        this.Invalidate(); // Trigger a repaint
    }

    private void ShadowingApp_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        // Draw smoke particles
        foreach (var particle in smokeParticles)
        {
            using (var path = new GraphicsPath())
            {
                path.AddBezier(
                    particle.Position.X - particle.Size / 2, particle.Position.Y,
                    particle.Control1.X, particle.Control1.Y,
                    particle.Control2.X, particle.Control2.Y,
                    particle.Position.X + particle.Size / 2, particle.Position.Y
                );

                using (var brush = new PathGradientBrush(path))
                {
                    int alpha = (int)(particle.Opacity * 255);
                    brush.CenterColor = Color.FromArgb(alpha, 150, 150, 150);
                    brush.SurroundColors = new Color[] { Color.FromArgb(0, 150, 150, 150) };

                    e.Graphics.FillPath(brush, path);
                }
            }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        ResetAllConnectedHosts();
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        ResetAllConnectedHosts();
    }

    private void ResetAllConnectedHosts()
    {
        foreach (var host in connectedHosts.Keys)
        {
            selectedTarget = host;
            ConfigureRegistry(0);
        }
        connectedHosts.Clear();
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new ShadowingApp());
    }
}
// Created By BaiJiFeiLong@gmail.com at 2024-07-16 16:58:57+0800

using NLog;

namespace DoubleClickFixer;

internal sealed class MainForm : Form
{
    private readonly App _app;
    private readonly Queue<ClickEvent> _clickEventQueue = new();
    private readonly List<Label> _clickLabels = [];
    private readonly Font _clickLabelFont;
    private readonly Label _totalFixValueLabel;
    private readonly Label _todayFixValueLabel;
    private readonly Label _thresholdLabel;
    private readonly TrackBar _thresholdTrackBar;
    private readonly Logger _logger = LogManager.GetLogger(nameof(MainForm));
    private readonly CheckBox _fixEnabledCheckBox;
    private readonly Button _resetButton;
    private readonly Label _totalFixTitleLabel;
    private readonly Label _todayFixTitleLabel;
    private readonly ComboBox _languageComboBox;

    public MainForm()
    {
        _app = App.GetApp();

        var topPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        topPanel.Margin = new Padding(5, 10, 5, 0);
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topPanel.ColumnCount = topPanel.ColumnStyles.Count;
        _fixEnabledCheckBox = new CheckBox { AutoSize = true, Dock = DockStyle.Fill };
        _fixEnabledCheckBox.Checked = _app.GetFixEnabled();
        _fixEnabledCheckBox.CheckedChanged += (_, _) => { _app.SetFixEnabled(_fixEnabledCheckBox.Checked); };
        _thresholdLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            MinimumSize = new Size(150, 0),
            BorderStyle = BorderStyle.None, TextAlign = ContentAlignment.MiddleLeft,
        };
        _thresholdTrackBar = new TrackBar
        {
            AutoSize = false, Dock = DockStyle.Fill, Width = 200
        };
        _thresholdTrackBar.Minimum = 1;
        _thresholdTrackBar.Maximum = 50;
        _thresholdTrackBar.TickFrequency = 10;
        _thresholdTrackBar.Height = 30;
        _thresholdTrackBar.Value = _app.GetThresholdMillis() / 10;
        _thresholdTrackBar.ValueChanged += OnThresholdTrackBarOnValueChanged;
        _resetButton = new Button { AutoSize = false, Dock = DockStyle.Fill };
        _resetButton.Click += OnResetButtonOnClick;
        _languageComboBox = new ComboBox
            { AutoSize = true, Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var _ in App.GetSupportedLanguages())
        {
            _languageComboBox.Items.Add("Placeholder");
        }

        _languageComboBox.SelectedIndex = App.GetSupportedLanguages().ToList().IndexOf(_app.GetLanguage());
        _languageComboBox.SelectionChangeCommitted += (_, _) =>
        {
            var language = App.GetSupportedLanguages()[_languageComboBox.SelectedIndex];
            _app.SetLanguage(language);
            RefreshTranslation();
        };

        topPanel.Controls.Add(_fixEnabledCheckBox);
        topPanel.Controls.Add(_thresholdLabel);
        topPanel.Controls.Add(_thresholdTrackBar);
        topPanel.Controls.Add(_resetButton);
        topPanel.Controls.Add(new Control());
        topPanel.Controls.Add(_languageComboBox);

        var todayFixPanel = new TableLayoutPanel()
        {
            AutoSize = true, Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle
        };
        _todayFixTitleLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 16),
            Margin = new Padding(10),
            ForeColor = Color.DimGray
        };
        _todayFixValueLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            Text = _app.GetTodayFix().ToString(),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 32, FontStyle.Bold),
            ForeColor = Color.Black
        };
        todayFixPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        todayFixPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        todayFixPanel.Controls.Add(_todayFixTitleLabel);
        todayFixPanel.Controls.Add(_todayFixValueLabel);

        var totalFixPanel = new TableLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle
        };
        _totalFixTitleLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 32),
            Margin = new Padding(20),
            ForeColor = Color.DimGray
        };
        _totalFixValueLabel = new Label
        {
            AutoSize = true, Dock = DockStyle.Fill,
            Text = _app.GetTotalFix().ToString(),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 64, FontStyle.Bold),
            ForeColor = Color.Black
        };
        totalFixPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        totalFixPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        totalFixPanel.Controls.Add(_totalFixTitleLabel);
        totalFixPanel.Controls.Add(_totalFixValueLabel);

        var rightPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        rightPanel.ColumnCount = 5;
        for (var i = 0; i < rightPanel.ColumnCount; i++)
        {
            rightPanel.ColumnStyles.Add(new ColumnStyle { SizeType = SizeType.Percent, Width = 100 });
            rightPanel.RowStyles.Add(new RowStyle { SizeType = SizeType.Percent, Height = 100 });
        }

        var leftPanel = new TableLayoutPanel() { AutoSize = true, Dock = DockStyle.Fill };
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        leftPanel.Controls.Add(todayFixPanel);
        leftPanel.Controls.Add(totalFixPanel);

        var centerPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        centerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        centerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        centerPanel.ColumnCount = centerPanel.ColumnStyles.Count;
        centerPanel.Controls.Add(leftPanel);
        centerPanel.Controls.Add(rightPanel);

        _clickLabelFont = new Font("Arial", 32);
        for (var i = 0; i < 20; i++)
        {
            // ReSharper disable once LocalizableElement
            var label = new Label { Dock = DockStyle.Fill, Text = "+", AutoSize = false, Width = 0 };
            label.Font = _clickLabelFont;
            label.BorderStyle = BorderStyle.FixedSingle;
            label.Margin = new Padding(5);
            label.TextAlign = ContentAlignment.MiddleCenter;
            rightPanel.Controls.Add(label);
            _clickLabels.Add(label);
        }

        var rootPanel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        rootPanel.Controls.Add(topPanel);
        rootPanel.Controls.Add(centerPanel);
        Controls.Add(rootPanel);

        ClientSize = new Size(1280, 720);
        CenterToScreen();

        _app.ClickEventTriggered += OnAppOnClickEventTriggered;
        RefreshTranslation();
    }


    private void OnThresholdTrackBarOnValueChanged(object? o, EventArgs eventArgs)
    {
        var thresholdMillis = _thresholdTrackBar.Value * 10;
        _app.SetThresholdMillis(thresholdMillis);
        RefreshTranslation();
    }

    private void OnResetButtonOnClick(object? sender, EventArgs args)
    {
        _logger.Info("Resetting threshold to {}...", App.GetDefaultThresholdMillis());
        _thresholdTrackBar.Value = App.GetDefaultThresholdMillis() / 10;
        RefreshTranslation();
    }

    private void RefreshTranslation()
    {
        Text = Translation.Title_Application;
        var thresholdMillis = _thresholdTrackBar.Value * 10;
        _thresholdLabel.Text = string.Format(Translation.Label_Threshold, thresholdMillis);
        _fixEnabledCheckBox.Text = Translation.Label_FixEnabled;
        _resetButton.Text = Translation.Button_Reset;
        _todayFixTitleLabel.Text = Translation.Label_TodayFix;
        _totalFixTitleLabel.Text = Translation.Label_TotalFix;
        var languages = App.GetSupportedLanguages();
        for (var i = 0; i < languages.Length; i++)
        {
            _languageComboBox.Items[i] = languages[i].ToTranslation();
        }
    }

    private void OnAppOnClickEventTriggered(object? sender, ClickEvent @event)
    {
        _clickEventQueue.Enqueue(@event);
        if (_clickEventQueue.Count > 20)
        {
            _clickEventQueue.Dequeue();
        }

        var now = DateTime.Now;
        for (var i = 0; i < _clickEventQueue.Count; i++)
        {
            var clickEvent = _clickEventQueue.ElementAt(i);
            var clickLabel = _clickLabels[i];
            var isNewClick = (now - clickEvent.TriggeredAt).TotalMilliseconds <= 500;
            var font = _clickLabelFont;
            if (isNewClick) font = new Font(font, FontStyle.Underline);
            clickLabel.Font = font;
            clickLabel.ForeColor =
                clickEvent.Accepted ? clickEvent.IsDoubleClick ? Color.Blue : Color.Green : Color.Red;
            clickLabel.Text = Math.Clamp(clickEvent.DelayMillis, 0, 999).ToString();
        }

        _todayFixValueLabel.Text = _app.GetTodayFix().ToString();
        _totalFixValueLabel.Text = _app.GetTotalFix().ToString();
    }
}
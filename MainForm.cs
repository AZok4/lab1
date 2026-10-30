using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace lab1_todo;

public class MainForm : Form
{
    private readonly TodoList _list = new();
    private readonly FlowLayoutPanel _taskPanel = new();
    private readonly TextBox _titleBox = new(), _searchBox = new();
    private readonly ComboBox _priorityBox = new(), _filterBox = new();
    private readonly DateTimePicker _dueDatePicker = new();
    private readonly CheckBox _sortBox = new();
    private readonly Label _countLabel = new();

    private readonly Color _coral = Color.FromArgb(231, 182, 184);
    private readonly Color _vanilla = Color.FromArgb(255, 249, 239);
    private readonly Color _coolWhite = Color.FromArgb(159, 178, 172);

    public MainForm()
    {
        Text = "Список дел";
        Width = 900;
        Height = 760;
        MinimumSize = new Size(680, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(248, 248, 246);
        BuildInterface();
        RefreshList();
    }

    private void BuildInterface()
    {
        Controls.Add(new Label { Text = "Название задачи:", Left = 20, Top = 22, Width = 130 });
        _titleBox.SetBounds(150, 18, 300, 30);
        _titleBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _priorityBox.SetBounds(460, 18, 130, 30);
        _priorityBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _priorityBox.Items.AddRange(new object[] { "Низкий", "Обычный", "Высокий" });
        _priorityBox.SelectedIndex = 1;

        var add = new Button { Text = "Добавить", Left = 605, Top = 17, Width = 125, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        add.Click += AddButton_Click;
        var delete = new Button { Text = "Удалить", Left = 735, Top = 17, Width = 120, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        delete.Click += DeleteButton_Click;

        Controls.Add(new Label { Text = "Поиск:", Left = 20, Top = 69, Width = 55 });
        _searchBox.SetBounds(75, 64, 260, 30);
        _searchBox.TextChanged += (_, _) => RefreshList();

        _filterBox.SetBounds(350, 64, 155, 30);
        _filterBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _filterBox.Items.AddRange(new object[] { "Все", "Активные", "Выполненные" });
        _filterBox.SelectedIndex = 0;
        _filterBox.SelectedIndexChanged += (_, _) => RefreshList();

        _sortBox.Text = "Сначала высокий приоритет";
        _sortBox.SetBounds(520, 67, 250, 25);
        _sortBox.CheckedChanged += (_, _) => RefreshList();

        Controls.Add(new Label { Text = "Срок выполнения:", Left = 20, Top = 110, Width = 125 });
        _dueDatePicker.SetBounds(150, 105, 150, 30);
        _dueDatePicker.Format = DateTimePickerFormat.Short;
        _dueDatePicker.ShowCheckBox = true;
        _dueDatePicker.Checked = false;
        _dueDatePicker.Value = DateTime.Today;

        _countLabel.SetBounds(20, 140, 800, 24);
        _countLabel.ForeColor = Color.FromArgb(90, 90, 90);
        _countLabel.Font = new Font("Segoe UI", 9);

        _taskPanel.SetBounds(20, 168, 835, 490);
        _taskPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _taskPanel.AutoScroll = true;
        _taskPanel.FlowDirection = FlowDirection.LeftToRight;
        _taskPanel.WrapContents = true;
        _taskPanel.BackColor = BackColor;
        _taskPanel.Padding = new Padding(8);

        Controls.AddRange(new Control[]
        {
            _titleBox, _priorityBox, add, delete, _searchBox, _filterBox,
            _sortBox, _dueDatePicker, _countLabel, _taskPanel
        });

        FormClosing += (_, _) =>
        {
            try { _list.Save(); }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить задачи: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };
    }

    private TodoPriority SelectedPriority() => _priorityBox.SelectedIndex switch
    {
        0 => TodoPriority.Low,
        1 => TodoPriority.Normal,
        2 => TodoPriority.High,
        _ => TodoPriority.Normal
    };

    private TodoFilter SelectedFilter() => _filterBox.SelectedIndex switch
    {
        1 => TodoFilter.Active,
        2 => TodoFilter.Completed,
        _ => TodoFilter.All
    };

    private void AddButton_Click(object? sender, EventArgs e) => RunSafely(() =>
    {
        DateTime? dueDate = _dueDatePicker.Checked ? _dueDatePicker.Value.Date : null;
        _list.Add(_titleBox.Text, SelectedPriority(), dueDate);
        _titleBox.Clear();
        _dueDatePicker.Checked = false;
        _list.Save();
        RefreshList();
    });

    private void RefreshList()
    {
        _taskPanel.SuspendLayout();
        _taskPanel.Controls.Clear();
        var items = _list.GetFiltered(SelectedFilter(), _searchBox.Text, _sortBox.Checked);
        _countLabel.Text = $"Задач: {items.Count}";
        foreach (var item in items) _taskPanel.Controls.Add(CreateTaskCard(item));
        _taskPanel.ResumeLayout();
    }

    private Control CreateTaskCard(TodoItem item)
    {
        var font = new Font("Segoe UI", 13, item.IsDone ? FontStyle.Strikeout : FontStyle.Regular);
        int textWidth = TextRenderer.MeasureText(item.Title, font, new Size(280, 0), TextFormatFlags.WordBreak).Width;
        int width = Math.Clamp(textWidth + 36, 190, 320);
        int titleHeight = Math.Max(48, TextRenderer.MeasureText(item.Title, font, new Size(width - 24, 0), TextFormatFlags.WordBreak).Height + 18);

        var card = new RoundedPanel
        {
            Width = width,
            Height = titleHeight + 62,
            Margin = new Padding(4, 4, 4, 8),
            BackColor = PriorityColor(item.Priority),
            Radius = 18,
            Cursor = Cursors.Hand
        };

        var title = new Label
        {
            Text = item.Title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = font,
            ForeColor = item.IsOverdue ? Color.Red : item.IsDone ? Color.DimGray : Color.FromArgb(35, 35, 35),
            Padding = new Padding(12, 8, 12, 0),
            AutoEllipsis = false
        };

        var deadline = new Label
        {
            Text = item.DueDate.HasValue ? $"Срок: {item.DueDate.Value:dd.MM.yyyy}" : "Срок не установлен",
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9),
            ForeColor = item.IsOverdue ? Color.Red : Color.FromArgb(75, 75, 75)
        };

        var priority = new Label
        {
            Text = $"Приоритет: {PriorityText(item.Priority)}",
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(75, 75, 75)
        };

        card.Controls.Add(title);
        card.Controls.Add(deadline);
        card.Controls.Add(priority);

        void Toggle()
        {
            item.ToggleDone();
            _list.Save();
            RefreshList();
        }

        card.Click += (_, _) => Toggle();
        title.Click += (_, _) => Toggle();
        deadline.Click += (_, _) => Toggle();
        priority.Click += (_, _) => Toggle();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Переименовать", null, (_, _) => RenameItem(item));
        menu.Items.Add("Удалить", null, (_, _) => DeleteItem(item));
        card.ContextMenuStrip = menu;
        title.ContextMenuStrip = menu;
        deadline.ContextMenuStrip = menu;
        priority.ContextMenuStrip = menu;
        return card;
    }

    private void DeleteButton_Click(object? sender, EventArgs e)
    {
        if (_list.Items.Count == 0)
        {
            MessageBox.Show("Список задач пуст.", "Удаление", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new Form
        {
            Text = "Удалить задачу", Width = 430, Height = 155,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false, MaximizeBox = false
        };

        var combo = new ComboBox { Left = 12, Top = 16, Width = 390, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var task in _list.Items) combo.Items.Add(task.Title);
        combo.SelectedIndex = 0;

        var ok = new Button { Text = "Удалить", Left = 226, Top = 65, Width = 85, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Отмена", Left = 317, Top = 65, Width = 85, DialogResult = DialogResult.Cancel };
        dialog.Controls.AddRange(new Control[] { combo, ok, cancel });
        dialog.AcceptButton = ok;
        dialog.CancelButton = cancel;

        if (dialog.ShowDialog(this) != DialogResult.OK || combo.SelectedIndex < 0) return;
        _list.Remove(combo.SelectedIndex);
        _list.Save();
        RefreshList();
    }

    private Color PriorityColor(TodoPriority priority) => priority switch
    {
        TodoPriority.High => _coral,
        TodoPriority.Normal => _vanilla,
        _ => _coolWhite
    };

    private static string PriorityText(TodoPriority priority) => priority switch
    {
        TodoPriority.High => "Высокий",
        TodoPriority.Normal => "Обычный",
        _ => "Низкий"
    };

    private void RenameItem(TodoItem item) => RunSafely(() =>
    {
        string? value = Prompt.Show("Введите новое название:", item.Title);
        if (value == null) return;
        item.Rename(value);
        _list.Save();
        RefreshList();
    });

    private void DeleteItem(TodoItem item)
    {
        if (MessageBox.Show($"Удалить задачу «{item.Title}»?", "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _list.Remove(_list.Items.ToList().IndexOf(item));
        _list.Save();
        RefreshList();
    }

    private void RunSafely(Action action)
    {
        try { action(); }
        catch (ArgumentException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось выполнить операцию: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

internal sealed class RoundedPanel : Panel
{
    public int Radius { get; set; } = 18;
    public RoundedPanel() { DoubleBuffered = true; ResizeRedraw = true; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var path = new GraphicsPath();
        int diameter = Radius * 2;
        path.AddArc(0, 0, diameter, diameter, 180, 90);
        path.AddArc(Width - diameter - 1, 0, diameter, diameter, 270, 90);
        path.AddArc(Width - diameter - 1, Height - diameter - 1, diameter, diameter, 0, 90);
        path.AddArc(0, Height - diameter - 1, diameter, diameter, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
    }
}

internal static class Prompt
{
    public static string? Show(string message, string initialValue)
    {
        using var form = new Form
        {
            Text = "Переименование", Width = 400, Height = 155,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false, MaximizeBox = false
        };

        var label = new Label { Text = message, Left = 12, Top = 12, Width = 350 };
        var input = new TextBox { Text = initialValue, Left = 12, Top = 38, Width = 355 };
        var ok = new Button { Text = "OK", Left = 200, Top = 75, Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Отмена", Left = 287, Top = 75, Width = 80, DialogResult = DialogResult.Cancel };

        form.Controls.AddRange(new Control[] { label, input, ok, cancel });
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Shown += (_, _) => input.Focus();
        return form.ShowDialog() == DialogResult.OK ? input.Text : null;
    }
}

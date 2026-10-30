using System;

namespace lab1_todo;

public enum TodoPriority { Low, Normal, High }

public class TodoItem
{
    private string _title;
    private bool _isDone;
    private TodoPriority _priority;

    public string Title => _title;
    public bool IsDone => _isDone;
    public TodoPriority Priority => _priority;
    public DateTime? DueDate { get; private set; }
    public bool IsOverdue => !IsDone && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;

    public TodoItem(string title, TodoPriority priority = TodoPriority.Normal, DateTime? dueDate = null)
    {
        _title = ValidateTitle(title);
        SetPriority(priority);
        SetDueDate(dueDate);
    }

    public void Rename(string title) => _title = ValidateTitle(title);
    public void ToggleDone() => _isDone = !_isDone;
    public void Complete() => _isDone = true;
    public void Reopen() => _isDone = false;
    public void SetDueDate(DateTime? dueDate) => DueDate = dueDate?.Date;

    public void SetPriority(TodoPriority priority)
    {
        if (!Enum.IsDefined(typeof(TodoPriority), priority))
            throw new ArgumentException("Некорректный приоритет.");
        _priority = priority;
    }

    private static string ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Название задачи не может быть пустым.");
        return title.Trim();
    }

    public TodoItemData ToData() => new()
    {
        Title = _title,
        IsDone = _isDone,
        Priority = _priority,
        DueDate = DueDate
    };

    public static TodoItem FromData(TodoItemData data)
    {
        var item = new TodoItem(data.Title, data.Priority, data.DueDate);
        if (data.IsDone) item.Complete();
        return item;
    }
}

public class TodoItemData
{
    public string Title { get; set; } = "";
    public bool IsDone { get; set; }
    public TodoPriority Priority { get; set; } = TodoPriority.Normal;
    public DateTime? DueDate { get; set; }
}

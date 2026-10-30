using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace lab1_todo;

public enum TodoFilter { All, Active, Completed }

public class TodoList
{
    private readonly List<TodoItem> _items = new();
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public TodoList(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppContext.BaseDirectory, "tasks.json");
        Load();
    }

    public IReadOnlyList<TodoItem> Items => _items.AsReadOnly();

    public TodoItem Add(string title, TodoPriority priority, DateTime? dueDate = null)
    {
        var item = new TodoItem(title, priority, dueDate);
        _items.Add(item);
        return item;
    }

    public TodoItem Find(int index)
    {
        if (index < 0 || index >= _items.Count)
            throw new ArgumentException("Выберите существующую задачу.");
        return _items[index];
    }

    public void Remove(int index) => _items.Remove(Find(index));

    public IReadOnlyList<TodoItem> GetFiltered(TodoFilter filter, string search = "", bool sortByPriority = false)
    {
        IEnumerable<TodoItem> result = _items;
        result = filter switch
        {
            TodoFilter.Active => result.Where(x => !x.IsDone),
            TodoFilter.Completed => result.Where(x => x.IsDone),
            _ => result
        };

        if (!string.IsNullOrWhiteSpace(search))
            result = result.Where(x => x.Title.Contains(search.Trim(), StringComparison.OrdinalIgnoreCase));

        if (sortByPriority)
            result = result.OrderByDescending(x => x.Priority).ThenBy(x => x.Title);

        return result.ToList();
    }

    public void Save()
    {
        var data = _items.Select(x => x.ToData()).ToList();
        File.WriteAllText(_filePath, JsonSerializer.Serialize(data, JsonOptions));
    }

    public void Load()
    {
        _items.Clear();
        if (!File.Exists(_filePath)) return;
        var data = JsonSerializer.Deserialize<List<TodoItemData>>(File.ReadAllText(_filePath), JsonOptions);
        if (data == null) return;
        foreach (var entry in data) _items.Add(TodoItem.FromData(entry));
    }
}

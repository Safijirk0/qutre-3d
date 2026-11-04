using ImGuiNET;
using Qutre3D.Core;
using ImVec2 = System.Numerics.Vector2;

namespace Qutre3D.App;

/// <summary>
/// Файловый диалог на ImGui — одинаковый на Windows и Linux.
/// Работает асинхронно: приложение просит открыть/сохранить, UI рисует окно,
/// а выбор приходит в колбэк. Блокировать главный поток нельзя.
/// </summary>
public sealed class FileDialog
{
    private const string PopupIdSuffix = "###qutre_file_dialog";
    private const string ErrorId = "##qutre_error";

    private enum DialogMode
    {
        None,
        Open,
        Save,
    }

    private sealed class Entry
    {
        public string Name;
        public bool IsDirectory;
    }

    private DialogMode _mode;
    private string _title = "";
    private string[] _extensions = { "*" };
    private string _defaultExtension = "";
    private Action<string> _onAccept;

    private string _directory = "";
    private string _fileName = "";
    private string _pathInput = "";
    private bool _requestOpen;
    private bool _focusName;
    private bool _requestRefresh;

    private readonly List<Entry> _entries = new();
    private string _cacheKey = "";

    private string _errorTitle = "";
    private string _errorMessage = "";
    private bool _requestError;

    public bool IsOpen => _mode != DialogMode.None || _requestError;

    /// <summary>Последняя папка, в которой что-то выбирали (для следующих диалогов).</summary>
    public string LastDirectory { get; set; } = "";

    public void Open(string title, string[] extensions, Action<string> onAccept, string initialName = "")
    {
        Start(DialogMode.Open, title, extensions, onAccept, initialName);
    }

    public void Save(string title, string[] extensions, Action<string> onAccept, string initialName = "")
    {
        Start(DialogMode.Save, title, extensions, onAccept, initialName);
    }

    private void Start(DialogMode mode, string title, string[] extensions, Action<string> onAccept,
        string initialName)
    {
        _mode = mode;
        _title = title;
        _extensions = extensions is { Length: > 0 } ? extensions : new[] { "*" };
        _defaultExtension = _extensions[0] == "*" ? "" : _extensions[0];
        _onAccept = onAccept;
        _fileName = initialName ?? "";
        _requestOpen = true;
        _focusName = mode == DialogMode.Save;
        _requestRefresh = true;

        string start = LastDirectory;
        if (string.IsNullOrWhiteSpace(start) || !Directory.Exists(start))
            start = PickStartDirectory();

        SetDirectory(start);
    }

    private static string PickStartDirectory()
    {
        foreach (var folder in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                     Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                 })
        {
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
                return folder;
        }

        return Directory.GetCurrentDirectory();
    }

    private void SetDirectory(string path)
    {
        try
        {
            string full = Path.GetFullPath(path);
            if (!Directory.Exists(full)) return;

            _directory = full;
            _pathInput = full;
            LastDirectory = full;
            _requestRefresh = true;
        }
        catch
        {
            // Некорректный путь — остаёмся там, где были.
        }
    }

    private void Cancel()
    {
        _mode = DialogMode.None;
        _onAccept = null;
        _entries.Clear();
        _cacheKey = "";
    }

    private void Accept()
    {
        if (_mode == DialogMode.None) return;

        string name = _fileName.Trim().Trim('"');
        if (name.Length == 0) return;

        string path;
        try
        {
            path = Path.IsPathRooted(name) ? Path.GetFullPath(name) : Path.GetFullPath(Path.Combine(_directory, name));
        }
        catch
        {
            return;
        }

        if (Directory.Exists(path))
        {
            SetDirectory(path);
            _fileName = "";
            return;
        }

        if (_mode == DialogMode.Save && !string.IsNullOrEmpty(_defaultExtension) &&
            !path.EndsWith("." + _defaultExtension, StringComparison.OrdinalIgnoreCase) &&
            !HasKnownExtension(path))
        {
            path += "." + _defaultExtension;
        }

        var callback = _onAccept;
        LastDirectory = _directory;
        Cancel();
        ImGui.CloseCurrentPopup();
        callback?.Invoke(path);
    }

    private bool HasKnownExtension(string path)
    {
        string ext = Path.GetExtension(path);
        if (ext.Length < 2) return false;

        string bare = ext[1..];
        foreach (var candidate in _extensions)
            if (string.Equals(candidate, bare, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private bool MatchesFilter(string name)
    {
        if (_extensions.Length == 0 || _extensions[0] == "*") return true;

        string ext = Path.GetExtension(name);
        if (ext.Length < 2) return false;

        string bare = ext[1..];
        foreach (var candidate in _extensions)
            if (candidate == "*" || string.Equals(candidate, bare, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private void RefreshEntries()
    {
        string key = _directory + "|" + string.Join(",", _extensions);
        if (!_requestRefresh && key == _cacheKey) return;

        _requestRefresh = false;
        _cacheKey = key;
        _entries.Clear();

        try
        {
            foreach (string dir in Directory.EnumerateDirectories(_directory))
            {
                var info = new DirectoryInfo(dir);
                if ((info.Attributes & FileAttributes.Hidden) != 0) continue;
                _entries.Add(new Entry { Name = info.Name, IsDirectory = true });
            }

            foreach (string file in Directory.EnumerateFiles(_directory))
            {
                string name = Path.GetFileName(file);
                if (!MatchesFilter(name)) continue;
                _entries.Add(new Entry { Name = name, IsDirectory = false });
            }
        }
        catch
        {
            // Нет доступа — показываем пустой список.
        }

        _entries.Sort((a, b) =>
        {
            if (a.IsDirectory != b.IsDirectory) return a.IsDirectory ? -1 : 1;
            return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
        });
    }

    public void ShowError(string title, string message)
    {
        _errorTitle = title;
        _errorMessage = message;
        _requestError = true;
    }

    // ------------------------------------------------------------------ отрисовка

    public void Draw()
    {
        DrawFileDialog();
        DrawErrorDialog();
    }

    private void DrawErrorDialog()
    {
        if (_requestError)
        {
            ImGui.OpenPopup(ErrorId);
            _requestError = false;
        }

        ImGui.SetNextWindowSize(new ImVec2(460f, 0f), ImGuiCond.Appearing);
        bool open = true;
        if (!ImGui.BeginPopupModal(ErrorId, ref open,
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextColored(ThemeColors.Accent, _errorTitle);
        ImGui.Spacing();
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 420f);
        ImGui.TextUnformatted(_errorMessage);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
        if (ImGui.Button(Loc.T("Закрыть", "Close"), new ImVec2(-1f, 0f)) || !open)
            ImGui.CloseCurrentPopup();

        ImGui.EndPopup();
    }

    private void DrawFileDialog()
    {
        if (_mode == DialogMode.None) return;

        string popupId = _title + PopupIdSuffix;

        if (_requestOpen)
        {
            ImGui.OpenPopup(popupId);
            _requestOpen = false;
        }

        ImGui.SetNextWindowSize(new ImVec2(800f, 620f), ImGuiCond.Appearing);
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new ImVec2(0.5f, 0.5f));

        bool open = true;
        if (!ImGui.BeginPopupModal(popupId, ref open,
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize))
        {
            return;
        }

        if (!open)
        {
            Cancel();
            ImGui.EndPopup();
            return;
        }

        ImGui.TextColored(ThemeColors.TextFaint, Loc.T("Файлы: ", "Files: ") + "*." +
            string.Join("  *.", _extensions));
        ImGui.Separator();

        DrawToolbar();
        DrawListing();

        ImGui.Spacing();

        if (_mode == DialogMode.Save)
        {
            ImGui.TextColored(ThemeColors.TextDim, Loc.T("Имя файла", "File name"));
            ImGui.SetNextItemWidth(-1f);
            if (_focusName) ImGui.SetKeyboardFocusHere();
            if (ImGui.InputText("##qutre_file_name", ref _fileName, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
                Accept();
            _focusName = false;
        }
        else
        {
            ImGui.TextColored(ThemeColors.TextDim, Loc.T("Выбранный файл", "Selected file"));
            ImGui.SetNextItemWidth(-1f);
            if (_focusName) ImGui.SetKeyboardFocusHere();
            if (ImGui.InputText("##qutre_file_name", ref _fileName, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
                Accept();
            _focusName = false;
        }

        ImGui.Spacing();
        ImGui.Separator();

        float buttonWidth = 130f;
        ImGui.SetCursorPosX(ImGui.GetWindowWidth() - buttonWidth * 2f - 22f);

        if (ImGui.Button(Loc.T("Отмена", "Cancel"), new ImVec2(buttonWidth, 30f)))
        {
            Cancel();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, ThemeColors.AccentDim);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ThemeColors.Accent);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, ThemeColors.Accent);

        string acceptLabel = _mode == DialogMode.Save ? Loc.T("Сохранить", "Save") : Loc.T("Открыть", "Open");
        if (ImGui.Button(acceptLabel, new ImVec2(buttonWidth, 30f)))
            Accept();

        ImGui.PopStyleColor(3);

        ImGui.EndPopup();
    }

    private void DrawToolbar()
    {
        if (ImGui.Button(Loc.T("Вверх", "Up"), new ImVec2(62f, 27f)))
        {
            var parent = Directory.GetParent(_directory);
            if (parent is not null) SetDirectory(parent.FullName);
        }

        ImGui.SameLine();
        if (ImGui.Button(Loc.T("Домой", "Home"), new ImVec2(74f, 27f)))
            SetDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        ImGui.SameLine();
        if (ImGui.Button(Loc.T("Документы", "Documents"), new ImVec2(0f, 27f)))
            SetDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        ImGui.SameLine();
        if (ImGui.Button(Loc.T("Обновить", "Refresh"), new ImVec2(0f, 27f)))
            _requestRefresh = true;

        ImGui.Spacing();

        ImGui.SetNextItemWidth(-84f);
        if (ImGui.InputText("##qutre_path", ref _pathInput, 2048, ImGuiInputTextFlags.EnterReturnsTrue))
            SetDirectory(_pathInput);

        ImGui.SameLine();
        if (ImGui.Button(Loc.T("Перейти", "Go"), new ImVec2(74f, 0f)))
            SetDirectory(_pathInput);

        ImGui.Spacing();
    }

    private void DrawListing()
    {
        RefreshEntries();

        ImGui.PushStyleColor(ImGuiCol.ChildBg, ThemeColors.Field);
        ImGui.BeginChild("##qutre_files", new ImVec2(0f, 330f), ImGuiChildFlags.Borders);

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            bool selected = !entry.IsDirectory &&
                            string.Equals(entry.Name, _fileName, StringComparison.OrdinalIgnoreCase);

            string label = (entry.IsDirectory ? "[ ] " : "    ") + entry.Name + "##" + i;
            ImGui.PushID(i);

            if (ImGui.Selectable(label, selected, ImGuiSelectableFlags.AllowDoubleClick, new ImVec2(0f, 22f)))
            {
                if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (entry.IsDirectory)
                        SetDirectory(Path.Combine(_directory, entry.Name));
                    else
                    {
                        _fileName = entry.Name;
                        Accept();
                    }
                }
                else if (entry.IsDirectory)
                {
                    SetDirectory(Path.Combine(_directory, entry.Name));
                }
                else
                {
                    _fileName = entry.Name;
                }
            }

            ImGui.PopID();
        }

        if (_entries.Count == 0)
            ImGui.TextColored(ThemeColors.TextFaint, Loc.T("  папка пуста", "  folder is empty"));

        ImGui.EndChild();
        ImGui.PopStyleColor();
    }
}

/// <summary>Небольшой мостик к палитре, чтобы FileDialog не зависел от UI-типов.</summary>
internal static class ThemeColors
{
    public static readonly System.Numerics.Vector4 Accent = new(0.055f, 0.435f, 0.435f, 1f);
    public static readonly System.Numerics.Vector4 AccentDim = new(0.36f, 0.56f, 0.55f, 1f);
    public static readonly System.Numerics.Vector4 TextDim = new(0.27f, 0.29f, 0.30f, 1f);
    public static readonly System.Numerics.Vector4 TextFaint = new(0.47f, 0.49f, 0.50f, 1f);
    public static readonly System.Numerics.Vector4 Field = new(0.935f, 0.935f, 0.918f, 1f);
}

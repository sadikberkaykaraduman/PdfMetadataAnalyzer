using CommunityToolkit.Mvvm.Input;
using PdfMetadataAnalyzer.Models;
using PdfMetadataAnalyzer.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Application = System.Windows.Application;
using Clipboard = System.Windows.Clipboard;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace PdfMetadataAnalyzer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ExtractionService _service = new();

    public ObservableCollection<FileItem> Files { get; } = new();
    public List<PdfType> AvailableTypes => PdfTypeInfo.AllTypes.ToList();

    private PdfType _selectedType = PdfType.NativeText;
    public PdfType SelectedType
    {
        get => _selectedType;
        set { _selectedType = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedTypeDescription)); }
    }

    public string SelectedTypeDescription => PdfTypeInfo.GetDescription(SelectedType);

    private FileItem? _selectedFile;
    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentResult));
            OnPropertyChanged(nameof(HasResult));
        }
    }

    public ExtractionResult? CurrentResult => SelectedFile?.Result;
    public bool HasResult => CurrentResult != null;

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsIdle)); }
    }
    public bool IsIdle => !_isBusy;

    private int _progress;
    public int Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); }
    }

    private string _statusText = "Select a PDF type, add files, then extract.";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private bool _recursive;
    public bool Recursive
    {
        get => _recursive;
        set { _recursive = value; OnPropertyChanged(); }
    }

    private BatchSummary? _batchSummary;
    public BatchSummary? BatchSummary
    {
        get => _batchSummary;
        set { _batchSummary = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBatch)); }
    }
    public bool HasBatch => _batchSummary != null;

    public ICommand AddFilesCmd { get; }
    public ICommand AddFolderCmd { get; }
    public ICommand RemoveFileCmd { get; }
    public ICommand ClearAllCmd { get; }
    public ICommand ExtractAllCmd { get; }
    public ICommand ExtractOneCmd { get; }
    public ICommand ExportJsonCmd { get; }
    public ICommand ExportCsvCmd { get; }
    public ICommand CopyFieldCmd { get; }

    public MainViewModel()
    {
        AddFilesCmd = new RelayCommand(AddFiles, () => IsIdle);
        AddFolderCmd = new RelayCommand(AddFolder, () => IsIdle);
        RemoveFileCmd = new RelayCommand<FileItem>(RemoveFile, _ => IsIdle);
        ClearAllCmd = new RelayCommand(ClearAll, () => IsIdle && Files.Count > 0);
        ExtractAllCmd = new AsyncRelayCommand(ExtractAll, () => IsIdle && Files.Count > 0);
        ExtractOneCmd = new AsyncRelayCommand(ExtractOne);
        ExportJsonCmd = new RelayCommand(ExportJson, () => HasResult || HasBatch);
        ExportCsvCmd = new RelayCommand(ExportCsv, () => HasBatch);
        CopyFieldCmd = new RelayCommand<string>(CopyField);
    }

    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "PDF Files|*.pdf",
            Multiselect = true,
            Title = "Select PDF files"
        };
        if (dialog.ShowDialog() != true) return;

        foreach (var file in dialog.FileNames)
        {
            if (Files.Any(f => f.Path == file)) continue;
            Files.Add(new FileItem(file));
        }

        StatusText = $"{Files.Count} file(s) in list.";
        RefreshCommands();
    }

    private void AddFolder()
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select a folder containing PDF files",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        var option = Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var pdfs = Directory.GetFiles(dialog.SelectedPath, "*.pdf", option);

        foreach (var file in pdfs)
        {
            if (Files.Any(f => f.Path == file)) continue;
            Files.Add(new FileItem(file));
        }

        StatusText = $"{Files.Count} file(s) in list.";
        RefreshCommands();
    }

    private void RemoveFile(FileItem? item)
    {
        if (item == null) return;
        Files.Remove(item);
        if (SelectedFile == item) SelectedFile = null;
        RefreshCommands();
    }

    private void ClearAll()
    {
        Files.Clear();
        SelectedFile = null;
        BatchSummary = null;
        StatusText = "List cleared.";
        RefreshCommands();
    }

    private async Task ExtractAll()
    {
        if (Files.Count == 0) return;

        IsBusy = true;
        Progress = 0;
        BatchSummary = null;

        try
        {
            int done = 0;
            var semaphore = new SemaphoreSlim(4);

            var tasks = Files.Select(async fileItem =>
            {
                await semaphore.WaitAsync();
                try
                {
                    fileItem.Status = FileStatus.Processing;
                    var result = await _service.ProcessFileAsync(fileItem.Path, SelectedType);
                    fileItem.Result = result;
                    fileItem.Status = FileStatus.Done;

                    Interlocked.Increment(ref done);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Progress = (int)((double)done / Files.Count * 100);
                        StatusText = $"[{done}/{Files.Count}] {fileItem.Name} done";

                        fileItem.NotifyResultChanged();

                        if (SelectedFile == fileItem)
                        {
                            OnPropertyChanged(nameof(CurrentResult));
                            OnPropertyChanged(nameof(HasResult));
                        }

                        if (done == 1) SelectedFile = fileItem;
                    });
                }
                catch (Exception ex)
                {
                    fileItem.Status = FileStatus.Error;
                    fileItem.ErrorMessage = ex.Message;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var results = Files.Where(f => f.Result != null).Select(f => f.Result!).ToList();

            if (results.Count > 0)
            {
                var scores = results.Select(r => r.Scores).ToList();
                BatchSummary = new BatchSummary
                {
                    Total = Files.Count,
                    Succeeded = results.Count,
                    Failed = Files.Count - results.Count,
                    Results = results,
                    AvgCombined = scores.Average(s => s.CombinedScore),
                    AvgPdfPig = scores.Average(s => s.PdfPigScore),
                    AvgPdfSharp = scores.Average(s => s.PdfSharpScore),
                    AvgOcr = scores.Average(s => s.OcrScore),
                    AvgMs = scores.Average(s => s.TotalMs),
                };
            }

            StatusText = $"Completed: {results.Count}/{Files.Count} succeeded.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommands();
        }
    }

    private async Task ExtractOne()
    {
        if (SelectedFile == null) return;

        IsBusy = true;
        SelectedFile.Status = FileStatus.Processing;

        try
        {
            StatusText = $"Processing: {SelectedFile.Name}";

            var result = await _service.ProcessFileAsync(SelectedFile.Path, SelectedType);

            SelectedFile.Result = result;
            SelectedFile.Status = FileStatus.Done;
            SelectedFile.NotifyResultChanged();

            var temp = SelectedFile;
            SelectedFile = null;
            SelectedFile = temp;

            StatusText = $"Done: {SelectedFile!.Name} — Score: {result.Scores.CombinedScore:F1}" +
                         (result.OcrWasUsed ? " (OCR used)" : "");
        }
        catch (Exception ex)
        {
            if (SelectedFile != null)
            {
                SelectedFile.Status = FileStatus.Error;
                SelectedFile.ErrorMessage = ex.Message;
            }
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            RefreshCommands();
        }
    }

    private void ExportJson()
    {
        var dialog = new SaveFileDialog { Filter = "JSON|*.json", FileName = "metadata_export.json" };
        if (dialog.ShowDialog() != true) return;

        if (HasBatch && BatchSummary != null)
            _service.ExportBatchJson(BatchSummary, dialog.FileName);
        else if (CurrentResult != null)
            _service.ExportJson(CurrentResult, dialog.FileName);

        StatusText = $"JSON saved: {dialog.FileName}";
    }

    private void ExportCsv()
    {
        if (BatchSummary == null) return;

        var dialog = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "metadata_export.csv" };
        if (dialog.ShowDialog() != true) return;

        _service.ExportBatchCsv(BatchSummary, dialog.FileName);
        StatusText = $"CSV saved: {dialog.FileName}";
    }

    private void CopyField(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            Clipboard.SetText(value);
    }

    private void RefreshCommands()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ((RelayCommand)AddFilesCmd).NotifyCanExecuteChanged();
            ((RelayCommand)AddFolderCmd).NotifyCanExecuteChanged();
            ((RelayCommand)ClearAllCmd).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ExtractAllCmd).NotifyCanExecuteChanged();
            ((AsyncRelayCommand)ExtractOneCmd).NotifyCanExecuteChanged();
            ((RelayCommand)ExportJsonCmd).NotifyCanExecuteChanged();
            ((RelayCommand)ExportCsvCmd).NotifyCanExecuteChanged();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum FileStatus { Pending, Processing, Done, Error }

public class FileItem : INotifyPropertyChanged
{
    public string Path { get; }
    public string Name => System.IO.Path.GetFileName(Path);
    public string SizeLabel => File.Exists(Path)
        ? $"{new FileInfo(Path).Length / 1024.0:F1} KB" : "";

    private FileStatus _status = FileStatus.Pending;
    public FileStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusIcon));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    private ExtractionResult? _result;
    public ExtractionResult? Result
    {
        get => _result;
        set { _result = value; OnPropertyChanged(); OnPropertyChanged(nameof(ScoreLabel)); OnPropertyChanged(nameof(OcrBadge)); }
    }

    public string? ErrorMessage { get; set; }

    public string StatusIcon => Status switch
    {
        FileStatus.Done => "✓",
        FileStatus.Error => "✗",
        FileStatus.Processing => "⟳",
        _ => "○"
    };

    public string StatusColor => Status switch
    {
        FileStatus.Done => "#3FB950",
        FileStatus.Error => "#F85149",
        FileStatus.Processing => "#D29922",
        _ => "#484F58"
    };

    public string ScoreLabel => Result != null ? $"{Result.Scores.CombinedScore:F0}" : "";
    public string OcrBadge => Result?.OcrWasUsed == true ? "OCR" : "";

    public FileItem(string path) => Path = path;

    public void NotifyResultChanged()
    {
        OnPropertyChanged(nameof(Result));
        OnPropertyChanged(nameof(ScoreLabel));
        OnPropertyChanged(nameof(OcrBadge));
        OnPropertyChanged(nameof(Status));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
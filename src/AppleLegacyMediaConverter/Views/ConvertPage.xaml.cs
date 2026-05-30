using System.Diagnostics;
using AppleLegacyMediaConverter.Core.Models;
using AppleLegacyMediaConverter.Helpers;
using AppleLegacyMediaConverter.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AppleLegacyMediaConverter.Views;

public sealed partial class ConvertPage : Page
{
    public ConvertPage()
    {
        InitializeComponent();
        ViewModel = App.GetService<ConvertViewModel>();
        DataContext = ViewModel;
    }

    public ConvertViewModel ViewModel { get; }

    private async void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowHelper.GetWindowHandle(App.MainWindowInstance!));
        picker.FileTypeFilter.Add("*");
        var files = await picker.PickMultipleFilesAsync();
        await ViewModel.AddPathsAsync(files.Select(file => file.Path));
    }

    private async void AddFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, WindowHelper.GetWindowHandle(App.MainWindowInstance!));
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            await ViewModel.AddPathsAsync(new[] { folder.Path });
        }
    }

    private async void SelectOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FolderPicker();
        InitializeWithWindow.Initialize(picker, WindowHelper.GetWindowHandle(App.MainWindowInstance!));
        picker.FileTypeFilter.Add("*");
        var folder = await picker.PickSingleFolderAsync();
        if (folder is not null)
        {
            ViewModel.CustomOutputFolder = folder.Path;
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
        e.DragUIOverride.Caption = "Thêm vào hàng đợi chuyển đổi";
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            return;
        }

        var storageItems = await e.DataView.GetStorageItemsAsync();
        var paths = storageItems
            .OfType<IStorageItem>()
            .Where(item => !string.IsNullOrWhiteSpace(item.Path))
            .Select(item => item.Path);
        await ViewModel.AddPathsAsync(paths);
    }

    private async void Convert_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.StartConversionAsync(ConfirmExtractAllFramesAsync);
    }

    private async Task<bool> ConfirmExtractAllFramesAsync()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Tách toàn bộ khung hình?",
            Content = "Chế độ này rất nặng và có thể tạo hàng nghìn tệp ảnh nếu video dài.",
            PrimaryButtonText = "Tách khung hình",
            CloseButtonText = "Hủy",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private void OpenSource_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MediaFileItem item })
        {
            OpenFolder(Path.GetDirectoryName(item.SourcePath));
        }
    }

    private void OpenOutput_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MediaFileItem item })
        {
            var target = item.OutputPath;
            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            OpenFolder(target.Contains('%', StringComparison.Ordinal)
                ? Path.GetDirectoryName(target)
                : Path.GetDirectoryName(target));
        }
    }

    private void CopyItemError_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MediaFileItem item })
        {
            return;
        }

        var text = string.Join(
            Environment.NewLine,
            item.FileName,
            item.SourcePath,
            item.ErrorMessage ?? "Không có lỗi hiển thị.",
            item.TechnicalDetails ?? "Không có chi tiết kỹ thuật.");
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    private void OpenOutputFolder_Click(object sender, RoutedEventArgs e)
    {
        var completed = ViewModel.Queue.FirstOrDefault(item => item.Status == ConversionStatus.Completed && !string.IsNullOrWhiteSpace(item.OutputPath));
        var folder = completed?.OutputPath is not null
            ? Path.GetDirectoryName(completed.OutputPath)
            : ViewModel.CustomOutputFolder;
        OpenFolder(folder);
    }

    private void CopyFailedReport_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(ViewModel.BuildFailedReport());
        Clipboard.SetContent(package);
    }

    private static void OpenFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "explorer.exe",
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(folder);
        Process.Start(startInfo);
    }
}

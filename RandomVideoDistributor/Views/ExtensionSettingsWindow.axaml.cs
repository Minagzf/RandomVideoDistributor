
using System.Collections.ObjectModel;
using Avalonia.Controls;
using RandomVideoDistributor.Models;
using RandomVideoDistributor.Services;
using System;
using System.Linq;

namespace RandomVideoDistributor.Views;

public partial class ExtensionSettingsWindow : Window
{
    private readonly VideoExtensionSettingsService _settingsService = new();

    public ObservableCollection<string> Extensions { get; } = [];

    public ExtensionSettingsWindow()
    {
        InitializeComponent();

        var settings = _settingsService.Load();

        foreach (var extension in settings.Extensions)
        {
            Extensions.Add("." + extension.TrimStart('.'));
        }

        ExtensionsList.ItemsSource = Extensions;
    }

    private void AddExtension_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var input = ExtensionInput.Text?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            ExtensionStatus.Text = "اكتب امتدادًا أولًا.";
            return;
        }

        var extension = input.TrimStart('.').ToLowerInvariant();

        if (extension.Length == 0 ||
            !extension.All(c =>
                char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            ExtensionStatus.Text =
                "الامتداد غير صالح. استخدم الحروف والأرقام والشرطة السفلية فقط.";
            return;
        }

        var normalizedExtension = "." + extension;

        if (Extensions.Any(existing =>
            string.Equals(
                existing,
                normalizedExtension,
                StringComparison.OrdinalIgnoreCase)))
        {
            ExtensionStatus.Text = "هذا الامتداد موجود بالفعل.";
            return;
        }

        Extensions.Add(normalizedExtension);
        ExtensionInput.Text = string.Empty;
        ExtensionStatus.Text = $"تمت إضافة {normalizedExtension} إلى القائمة.";
    }

    private void RemoveExtension_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ExtensionsList.SelectedItem is not string selectedExtension)
        {
            ExtensionStatus.Text = "اختر امتدادًا من القائمة أولًا.";
            return;
        }

        Extensions.Remove(selectedExtension);

        ExtensionStatus.Text =
            $"تم حذف {selectedExtension} من القائمة مؤقتًا.";
    }

    private void Save_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var settings = new VideoExtensionSettings
        {
            Extensions = Extensions
                .Select(extension => extension.TrimStart('.'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };

        try
        {
            _settingsService.Save(settings);
            Close();
        }
        catch (Exception ex)
        {
            ExtensionStatus.Text =
                $"تعذر حفظ الإعدادات: {ex.Message}";
        }
    }

    private void Cancel_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

}

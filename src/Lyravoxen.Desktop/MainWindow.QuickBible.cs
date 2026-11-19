using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Lyravoxen.Models;

namespace Lyravoxen;

// Digitação rápida na aba Bíblia (como no Holyrics): digite "jo 3 16" e Enter projeta.
// Enquanto digita o nome do livro, ele já fica destacado na lista.
public partial class MainWindow
{
    private static readonly Regex LeadingBookPattern = new(
        @"^\s*(?<num>[1-3])?\s*(?<word>\p{L}+)", RegexOptions.CultureInvariant);

    private static string FoldKey(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) builder.Append(c);
        }
        return builder.ToString();
    }

    /// <summary>Acha o livro pelo começo do nome ou pela abreviação ("o" → Oseias, "sl" → Salmos, "1co").</summary>
    private static BibleBookInfo? FindBookByPrefix(string text, out string rest)
    {
        rest = "";
        var match = LeadingBookPattern.Match(text);
        if (!match.Success) return null;
        var key = FoldKey(match.Groups["num"].Value + match.Groups["word"].Value);
        if (key.Length == 0) return null;
        rest = text.Substring(match.Length);

        var exact = BibleReferenceParser.Books.FirstOrDefault(b =>
            FoldKey(b.Name) == key || b.Aliases.Any(a => FoldKey(a) == key));
        if (exact is not null) return exact;

        return BibleReferenceParser.Books.FirstOrDefault(b =>
            FoldKey(b.Name).StartsWith(key, StringComparison.Ordinal) ||
            b.Aliases.Any(a => FoldKey(a).StartsWith(key, StringComparison.Ordinal)));
    }

    /// <summary>Se o texto não é uma referência completa, troca o começo do nome pelo nome do livro.</summary>
    private void ExpandTypedBookPrefix()
    {
        var text = BibleSearchBox.Text.Trim();
        if (text.Length == 0 || BibleReferenceParser.TryParseReference(text, allowBookOnly: true) is not null) return;
        var book = FindBookByPrefix(text, out var rest);
        if (book is null) return;
        BibleSearchBox.Text = book.Name + rest;
    }

    private void BibleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded || _bibleUiUpdating) return;
        var book = FindBookByPrefix(BibleSearchBox.Text, out _);
        if (book is null || ReferenceEquals(BibleBooksList.SelectedItem, book)) return;
        _bibleUiUpdating = true;
        try
        {
            BibleBooksList.SelectedItem = book;
            BibleBooksList.ScrollIntoView(book);
            BibleChaptersList.ItemsSource = Enumerable.Range(1, book.Chapters).ToList();
        }
        finally
        {
            _bibleUiUpdating = false;
        }
    }

    /// <summary>Qualquer letra ou número digitado na aba Bíblia vai para o campo de referência.</summary>
    private void MainWindow_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (WorkspaceTabs.SelectedItem != BibleWorkspaceTab || _tapActive) return;
        if (Keyboard.FocusedElement is System.Windows.Controls.TextBox or System.Windows.Controls.ComboBox or System.Windows.Controls.Primitives.TextBoxBase) return;
        if (e.OriginalSource is Microsoft.Web.WebView2.Wpf.WebView2) return;
        if (string.IsNullOrEmpty(e.Text) || !char.IsLetterOrDigit(e.Text[0])) return;

        BibleSearchBox.Focus();
        BibleSearchBox.Text = e.Text;
        BibleSearchBox.CaretIndex = e.Text.Length;
        e.Handled = true;
    }

    /// <summary>O Holyrics abre a "Apresentação Rápida" na frente; traz o Lyravoxen de volta e devolve o foco ao campo.</summary>
    private async Task BringToFrontAsync()
    {
        foreach (var delay in new[] { 300, 800 })
        {
            await Task.Delay(delay);
            try
            {
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                Topmost = true;
                if (!_pinnedOnTop) Topmost = false;
                Activate();
                if (WorkspaceTabs.SelectedItem == BibleWorkspaceTab) BibleSearchBox.Focus();
            }
            catch { /* melhor esforço */ }
        }
    }

    private bool _pinnedOnTop;

    private void PinTop_Click(object sender, RoutedEventArgs e)
    {
        _pinnedOnTop = !_pinnedOnTop;
        Topmost = _pinnedOnTop;
        PinTopButton.Content = _pinnedOnTop ? "📌 Fixado à frente" : "📌 Fixar à frente";
    }
}

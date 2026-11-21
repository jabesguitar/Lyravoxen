using System.Windows;
using System.Windows.Input;
using Lyravoxen.Models;

namespace Lyravoxen;

// Atalhos de navegação da aba Bíblia (como no Holyrics):
//   ← / →            versículo anterior / próximo (projeta)
//   Ctrl+← / Ctrl+→  capítulo anterior / próximo   (também PageUp / PageDown)
//   Ctrl+Shift+← / → livro anterior / próximo
//   Esc              limpar o telão
public partial class MainWindow
{
    private bool HandleBibleNavigationKeys(System.Windows.Input.KeyEventArgs e)
    {
        if (WorkspaceTabs.SelectedItem != BibleWorkspaceTab) return false;
        var modifiers = Keyboard.Modifiers;
        if ((modifiers & ModifierKeys.Alt) != 0) return false;
        var ctrl = (modifiers & ModifierKeys.Control) != 0;
        var shift = (modifiers & ModifierKeys.Shift) != 0;

        // Em campos de texto as setas pertencem ao campo; só o campo de referência vazio (ou todo selecionado) libera.
        var focused = Keyboard.FocusedElement;
        var inField = focused is System.Windows.Controls.ComboBox or System.Windows.Controls.ComboBoxItem ||
                      (focused is System.Windows.Controls.TextBox box &&
                       !(ReferenceEquals(box, BibleSearchBox) && (box.Text.Length == 0 || box.SelectionLength == box.Text.Length)));
        if (inField) return false;

        switch (e.Key)
        {
            case Key.Right or Key.Left when ctrl && shift:
                e.Handled = true;
                if (!e.IsRepeat) ChangeBibleBook(e.Key == Key.Right ? 1 : -1);
                return true;
            case Key.Right or Key.Left when ctrl:
                e.Handled = true;
                if (!e.IsRepeat) _ = ChangeBibleChapterAsync(e.Key == Key.Right ? 1 : -1);
                return true;
            case Key.Right or Key.Left when !shift:
                e.Handled = true;
                _ = StepBibleVerseAsync(e.Key == Key.Right ? 1 : -1);
                return true;
            case Key.PageDown or Key.PageUp:
                e.Handled = true;
                if (!e.IsRepeat) _ = ChangeBibleChapterAsync(e.Key == Key.PageDown ? 1 : -1);
                return true;
            case Key.Escape when BibleVoiceConfirmPanel.Visibility != Visibility.Visible:
                e.Handled = true;
                ClearBibleProjection_Click(this, new RoutedEventArgs());
                return true;
        }
        return false;
    }

    private async Task ChangeBibleChapterAsync(int direction)
    {
        if (BibleBooksList.SelectedItem is not BibleBookInfo book || BibleChaptersList.SelectedItem is not int chapter) return;
        var next = chapter + direction;
        if (next < 1 || next > book.Chapters)
        {
            ChangeBibleBook(direction);
            return;
        }

        _bibleTypedReference = null;
        _bibleUiUpdating = true;
        try
        {
            BibleChaptersList.SelectedItem = next;
            BibleChaptersList.ScrollIntoView(next);
        }
        finally
        {
            _bibleUiUpdating = false;
        }
        await ReloadBibleChapterTextAsync(1, null);
    }

    private void ChangeBibleBook(int direction)
    {
        var books = BibleReferenceParser.Books.ToList();
        var index = BibleBooksList.SelectedItem is BibleBookInfo current ? books.IndexOf(current) : -1;
        var target = index + direction;
        if (target < 0 || target >= books.Count) return;
        BibleBooksList.SelectedItem = books[target];
        BibleBooksList.ScrollIntoView(books[target]);
    }
}

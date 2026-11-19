using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Lyravoxen.Models;

public enum VoiceBibleCommandKind { Next, Previous, Project, Clear, Cancel, Confirm, Verse }

public sealed record VoiceBibleCommand(VoiceBibleCommandKind Kind, int? VerseStart = null, int? VerseEnd = null);

/// <summary>Reconhece comandos curtos falados na aba Bíblia ("próximo", "limpar o telão", "versículo dezessete").</summary>
public static class VoiceBibleCommandInterpreter
{
    private static readonly Dictionary<string, VoiceBibleCommandKind> Exact = new()
    {
        ["proximo"] = VoiceBibleCommandKind.Next, ["proximo versiculo"] = VoiceBibleCommandKind.Next,
        ["avancar"] = VoiceBibleCommandKind.Next, ["seguinte"] = VoiceBibleCommandKind.Next,
        ["anterior"] = VoiceBibleCommandKind.Previous, ["versiculo anterior"] = VoiceBibleCommandKind.Previous,
        ["voltar"] = VoiceBibleCommandKind.Previous,
        ["projetar"] = VoiceBibleCommandKind.Project, ["projeta"] = VoiceBibleCommandKind.Project,
        ["limpar"] = VoiceBibleCommandKind.Clear, ["limpar telao"] = VoiceBibleCommandKind.Clear,
        ["limpar o telao"] = VoiceBibleCommandKind.Clear, ["limpar tela"] = VoiceBibleCommandKind.Clear,
        ["cancelar"] = VoiceBibleCommandKind.Cancel, ["descartar"] = VoiceBibleCommandKind.Cancel,
        ["sim"] = VoiceBibleCommandKind.Confirm, ["ok"] = VoiceBibleCommandKind.Confirm,
        ["confirmar"] = VoiceBibleCommandKind.Confirm, ["confirma"] = VoiceBibleCommandKind.Confirm
    };

    private static readonly Regex VersePattern = new(
        @"^versiculos?\s+(\d{1,3})(?:\s+(?:ao?|ate)\s+(\d{1,3}))?$", RegexOptions.CultureInvariant);

    public static VoiceBibleCommand? TryInterpret(string? heard)
    {
        if (string.IsNullOrWhiteSpace(heard)) return null;
        var text = Normalize(VoiceBibleReferenceParser.ConvertNumberWords(heard));
        if (text.Length == 0) return null;

        if (Exact.TryGetValue(text, out var kind)) return new VoiceBibleCommand(kind);

        var match = VersePattern.Match(text);
        if (!match.Success) return null;
        var start = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int? end = match.Groups[2].Success ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) : null;
        if (start < 1 || (end.HasValue && end.Value < start)) return null;
        return new VoiceBibleCommand(VoiceBibleCommandKind.Verse, start, end);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }
}

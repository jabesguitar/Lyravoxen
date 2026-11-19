using System.IO;
using System.Globalization;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using Lyravoxen.Models;

namespace Lyravoxen.Services;

/// <summary>
/// Reconhecimento de voz do próprio Windows (sem DLL nativa e sem download), com vocabulário restrito:
/// nomes dos livros, números e comandos curtos. Usado quando o Whisper não carrega.
/// </summary>
internal static class WindowsSpeechReferenceRecognizer
{
    private static readonly string[] Units = { "", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove" };
    private static readonly string[] Teens = { "dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove" };
    private static readonly string[] Tens = { "", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa" };

    private static RecognizerInfo? FindRecognizer()
    {
        var installed = SpeechRecognitionEngine.InstalledRecognizers();
        return installed.FirstOrDefault(r => r.Culture.Name.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
            ?? installed.FirstOrDefault(r => r.Culture.TwoLetterISOLanguageName == "pt");
    }

    public static string Recognize(short[] samples16k)
    {
        var info = FindRecognizer();
        if (info is null)
        {
            var names = string.Join(", ", SpeechRecognitionEngine.InstalledRecognizers().Select(r => r.Culture.Name));
            throw new InvalidOperationException(
                "O Windows não tem reconhecimento de fala em português instalado (instalados: " +
                (names.Length == 0 ? "nenhum" : names) + "). Em Configurações > Hora e idioma > Idioma e região, " +
                "abra Português (Brasil) > Opções e instale o recurso \"Reconhecimento de fala\".");
        }

        using var engine = new SpeechRecognitionEngine(info);
        engine.LoadGrammar(BuildGrammar(info.Culture));

        // 16 bits mono 16 kHz + meio segundo de silêncio no fim para fechar a frase.
        var bytes = new byte[(samples16k.Length + 8000) * 2];
        Buffer.BlockCopy(samples16k, 0, bytes, 0, samples16k.Length * 2);
        using var stream = new MemoryStream(bytes);
        engine.SetInputToAudioStream(stream, new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Mono));

        var result = engine.Recognize(TimeSpan.FromSeconds(15));
        return result?.Text?.Trim() ?? "";
    }

    private static GrammarBuilder Builder(CultureInfo culture, Choices choices) => new(choices) { Culture = culture };
    private static GrammarBuilder Builder(CultureInfo culture, string text) => new(text) { Culture = culture };

    private static Grammar BuildGrammar(CultureInfo culture)
    {
        var books = new Choices();
        foreach (var book in BibleReferenceParser.Books) books.Add(SpokenBookName(book.Name));

        var numbers = new Choices();
        for (var n = 1; n <= 176; n++) numbers.Add(NumberWords(n));

        GrammarBuilder Number() => Builder(culture, numbers);
        GrammarBuilder Optional(GrammarBuilder builder)
        {
            var wrapper = new GrammarBuilder { Culture = culture };
            wrapper.Append(builder, 0, 1);
            return wrapper;
        }
        GrammarBuilder RangeOpt()
        {
            var range = new GrammarBuilder { Culture = culture };
            range.Append(Builder(culture, new Choices("a", "ao", "até")));
            range.Append(Number());
            return Optional(range);
        }

        var reference = new GrammarBuilder { Culture = culture };
        reference.Append(Builder(culture, books));
        reference.Append(Optional(Builder(culture, "capítulo")));
        reference.Append(Number());
        reference.Append(Optional(Builder(culture, "versículo")));
        reference.Append(Optional(Number()));
        reference.Append(RangeOpt());

        var verse = new GrammarBuilder { Culture = culture };
        verse.Append(Builder(culture, new Choices("versículo", "versículos")));
        verse.Append(Number());
        verse.Append(RangeOpt());

        var commands = Builder(culture, new Choices(
            "próximo", "próximo versículo", "avançar", "seguinte", "anterior", "versículo anterior", "voltar",
            "projetar", "projeta", "limpar", "limpar tela", "limpar o telão", "limpar telão",
            "cancelar", "descartar", "sim", "ok", "confirmar", "confirma"));

        var root = new Choices(reference, verse, commands);
        return new Grammar(new GrammarBuilder(root) { Culture = culture }) { Name = "Lyravoxen" };
    }

    private static string SpokenBookName(string name)
    {
        if (name.StartsWith("1 ", StringComparison.Ordinal)) return "primeiro " + name[2..];
        if (name.StartsWith("2 ", StringComparison.Ordinal)) return "segundo " + name[2..];
        if (name.StartsWith("3 ", StringComparison.Ordinal)) return "terceiro " + name[2..];
        return name;
    }

    private static string NumberWords(int n)
    {
        if (n < 10) return Units[n];
        if (n < 20) return Teens[n - 10];
        if (n < 100) return n % 10 == 0 ? Tens[n / 10] : Tens[n / 10] + " e " + Units[n % 10];
        if (n == 100) return "cem";
        var rest = n - 100;
        return "cento e " + NumberWords(rest);
    }
}

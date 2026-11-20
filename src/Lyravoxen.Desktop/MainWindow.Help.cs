using System.Windows;
using Lyravoxen.Services;

namespace Lyravoxen;

// ❓ Ajuda: a última aba (ou a tecla F1) abre uma janelinha explicando a aba atual e cada botão.
public partial class MainWindow
{
    private sealed record HelpItem(string Name, string Text);
    private sealed record HelpPage(string Title, string Intro, HelpItem[] Items);

    private System.Windows.Controls.TabItem? _helpTab;
    private object? _lastTab;
    private Window? _helpWindow;

    private void InitializeHelp()
    {
        _helpTab = new System.Windows.Controls.TabItem { Header = "❓ Ajuda" };
        WorkspaceTabs.Items.Add(_helpTab);
        _lastTab = WorkspaceTabs.SelectedItem;
        WorkspaceTabs.SelectionChanged += (_, e) =>
        {
            if (!ReferenceEquals(e.OriginalSource, WorkspaceTabs)) return;
            if (ReferenceEquals(WorkspaceTabs.SelectedItem, _helpTab))
            {
                var target = _lastTab ?? WorkspaceTabs.Items[0];
                WorkspaceTabs.SelectedItem = target;
                ShowHelp(target);
            }
            else
            {
                _lastTab = WorkspaceTabs.SelectedItem;
            }
        };
    }

    private HelpPage GetHelpPage(object? tab)
    {
        if (ReferenceEquals(tab, BibleWorkspaceTab)) return BibleHelp;
        if (ReferenceEquals(tab, ManualSyncTab)) return RegisterHelp;
        if (tab is System.Windows.Controls.TabItem item && item.Header is string header && header.Contains("Próxima", StringComparison.Ordinal))
            return NextSongHelp;
        return LiveHelp;
    }

    private void ShowHelp(object? tab)
    {
        var page = GetHelpPage(tab);
        _helpWindow?.Close();

        var text = Hex("#1A2430");
        var muted = Hex("#4A5A6B");
        var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(18) };
        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = page.Title, FontSize = 22, FontWeight = FontWeights.Bold, Foreground = text, Margin = new Thickness(0, 0, 0, 8)
        });
        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = page.Intro, FontSize = 14, TextWrapping = TextWrapping.Wrap, Foreground = text, Margin = new Thickness(0, 0, 0, 14)
        });
        stack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "BOTÕES E RECURSOS (clique para abrir)", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = muted, Margin = new Thickness(0, 0, 0, 6)
        });

        var expanders = new List<System.Windows.Controls.Expander>();
        foreach (var item in page.Items)
        {
            var expander = new System.Windows.Controls.Expander
            {
                Header = item.Name,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = text,
                Margin = new Thickness(0, 0, 0, 4),
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = item.Text, FontSize = 13, FontWeight = FontWeights.Normal, TextWrapping = TextWrapping.Wrap,
                    Foreground = muted, Margin = new Thickness(18, 4, 0, 8)
                }
            };
            expanders.Add(expander);
            stack.Children.Add(expander);
        }

        var toggle = new System.Windows.Controls.Button { Content = "Abrir tudo", Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 8, 0) };
        var opened = false;
        toggle.Click += (_, _) =>
        {
            opened = !opened;
            foreach (var expander in expanders) expander.IsExpanded = opened;
            toggle.Content = opened ? "Recolher tudo" : "Abrir tudo";
        };
        var close = new System.Windows.Controls.Button { Content = "Fechar", Padding = new Thickness(20, 6, 20, 6), IsCancel = true, IsDefault = true };

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        buttons.Children.Add(toggle);
        buttons.Children.Add(close);

        var dock = new System.Windows.Controls.DockPanel();
        System.Windows.Controls.DockPanel.SetDock(buttons, System.Windows.Controls.Dock.Bottom);
        dock.Children.Add(buttons);
        dock.Children.Add(new System.Windows.Controls.ScrollViewer { Content = stack, VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto });

        var window = new Window
        {
            Title = "Ajuda — " + page.Title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 580,
            Height = 640,
            Background = Hex("#F5F7FA"),
            Content = dock
        };
        close.Click += (_, _) => window.Close();
        window.Closed += (_, _) => { if (ReferenceEquals(_helpWindow, window)) _helpWindow = null; };
        _helpWindow = window;
        window.Show();
    }

    private static readonly HelpPage LiveHelp = new(
        "Ao vivo",
        "Aqui você toca a música (YouTube ou arquivo), acompanha as frases sincronizadas e projeta na tela própria e no Holyrics. " +
        "A barra de botões do topo cuida da conexão, do backup e da projeção.",
        new[]
        {
            new HelpItem("Carregar YouTube", "Cole o link do vídeo no campo ao lado e clique. O vídeo abre no player e o app procura a letra no catálogo."),
            new HelpItem("Tocar próxima", "Toca a música que você deixou preparada na aba \"＋ Próxima música\". Só fica ativo depois de preparar uma."),
            new HelpItem("Conectar Holyrics", "Informe endereço, porta e token da API do Holyrics e teste. Sem isso o app projeta só na tela própria."),
            new HelpItem("🩺 Diagnóstico", "Confere Holyrics e permissões do token, microfones, modelo de voz, monitores e internet. O resultado é copiado para colar numa conversa de suporte."),
            new HelpItem("☁ Backup", "Guarda uma cópia da biblioteca (letras, tempos, mídia) numa pasta de sua escolha, como OneDrive ou Google Drive. É automático ao abrir, a cada 10 minutos e ao fechar. Também restaura em outro computador."),
            new HelpItem("📱 Celular", "Mostra o QR code e o PIN para controlar a aba Bíblia pelo celular, na mesma rede Wi-Fi."),
            new HelpItem("📌 Fixar à frente", "Mantém o Lyravoxen por cima das outras janelas."),
            new HelpItem("📋 Relatório", "Copia o registro de uso e de erros para você colar numa conversa de suporte."),
            new HelpItem("Abrir projeção própria", "Abre ou fecha a tela independente de projeção (tela cheia, no outro monitor)."),
            new HelpItem("Escolher fundo", "Importa uma imagem JPG ou PNG para ser o fundo da projeção própria."),
            new HelpItem("Holyrics ativo", "Liga ou desliga o envio ao Holyrics, sem fechá-lo."),
            new HelpItem("Sair da projeção", "Fecha a projeção própria e a apresentação atual do Holyrics."),
            new HelpItem("Encerrar música", "Para a música e fecha a projeção no Lyravoxen e no Holyrics."),
            new HelpItem("Sub-aba Sincronização", "Mostra a letra e o andamento da música. \"Buscar letra\" procura no catálogo online."),
            new HelpItem("Sub-aba Controles manuais", "◀ Anterior / Próxima ▶ / Projetar selecionada: navegam e projetam frases à mão.\n" +
                "Modo automático: a projeção acompanha o tempo da música.\n" +
                "Editar selecionada, Atualizar frase, Excluir frase, ↑/↓ Reordenar: corrigem a letra.\n" +
                "Adicionar tempo, Marcar frase no tempo atual: acertam o instante de cada frase.\n" +
                "Importar letra sincronizada (.LRC / .SRT): traz uma letra com tempos de um arquivo."),
            new HelpItem("Sub-aba Histórico de vídeos", "Lista os vídeos já usados. Use a lupa (⌕) para filtrar e clique para reabrir."),
            new HelpItem("Sub-aba Biblioteca", "Suas músicas organizadas em pastas. Criar pasta, Renomear, Excluir, Adicionar vídeo atual a esta pasta e Importar áudio MP3/WAV para uso offline.")
        });

    private static readonly HelpPage RegisterHelp = new(
        "Cadastrar letra",
        "Aqui você cadastra a letra de uma música e acerta os tempos. Cole a letra na caixa, deixando uma linha em branco entre as estrofes. " +
        "Depois escolha como sincronizar.",
        new[]
        {
            new HelpItem("Salvar e sincronizar", "Salva a letra colada e inicia a sincronização (automática, quando disponível, ou no primeiro play)."),
            new HelpItem("Iniciar reconhecimento automático", "O app ouve a música e marca os tempos sozinho. Precisa do motor de voz Whisper, que não funciona em computadores sem AVX2."),
            new HelpItem("Iniciar marcação por toque", "Você toca a cada estrofe enquanto a música toca. Funciona em qualquer computador e em músicas só com playback."),
            new HelpItem("TOQUE · próxima estrofe (Espaço)", "Marca que a estrofe começou agora. A estrofe é projetada na hora. Um toque a mais depois da última encerra e salva."),
            new HelpItem("Desfazer (Backspace)", "Desfaz o último toque."),
            new HelpItem("Concluir e salvar", "Termina a marcação e salva os tempos."),
            new HelpItem("Cancelar", "Sai da marcação sem salvar."),
            new HelpItem("Dica", "Durante a marcação, não clique no vídeo do YouTube: o Espaço iria para o player.")
        });

    private static readonly HelpPage NextSongHelp = new(
        "Próxima música",
        "Pesquise a próxima música (vídeo, letra, cifra) num navegador embutido e deixe-a preparada para tocar quando a atual terminar.",
        new[]
        {
            new HelpItem("Pesquisar", "Faz a busca na aba de navegador aberta."),
            new HelpItem("Voltar / Avançar / Atualizar", "Controles de navegação, como num navegador comum."),
            new HelpItem("Copiar link", "Copia o endereço da página aberta."),
            new HelpItem("＋ Nova aba / Fechar aba", "Abre ou fecha abas de pesquisa."),
            new HelpItem("Atalhos de sites", "YouTube, Google, Cifra Club, Vagalume, Letras.mus.br, Ouvir Música e Harpa Cristã abrem direto na aba atual."),
            new HelpItem("Preparar próxima", "Abra o vídeo desejado (não apenas a página de resultados) e clique. Ela fica pronta para o botão \"Tocar próxima\" na aba Ao vivo.")
        });

    private static readonly HelpPage BibleHelp = new(
        "Bíblia",
        "Central Bíblica: escolha ou digite o versículo, confira a prévia e projete no Holyrics. O texto vem da Bíblia instalada no Holyrics.",
        new[]
        {
            new HelpItem("Versão / ↻ Holyrics", "Escolha a versão da Bíblia. O botão ↻ consulta de novo as versões do Holyrics (use quando ele for aberto depois do Lyravoxen)."),
            new HelpItem("Campo de referência e Localizar", "Digite, por exemplo, jo 3 16 e pressione Enter: o versículo é localizado e projetado. Digitando só o começo do nome (o, sl, ger), o livro já é destacado na lista."),
            new HelpItem("Digitação rápida", "Com a aba Bíblia aberta, qualquer letra ou número digitado vai para o campo de referência."),
            new HelpItem("Livros, capítulos e versículos", "Clique para escolher. Clique duas vezes num versículo para projetar. Dá para selecionar mais de um versículo."),
            new HelpItem("◀ Anterior / Próximo ▶ / Projetar / Limpar telão", "Anterior e Próximo andam de versículo em versículo e projetam. Projetar envia a seleção. Limpar telão tira o texto da tela."),
            new HelpItem("Atalhos do teclado", "→ / ←: próximo / anterior versículo (projeta)\nCtrl + → / ←  ou  PageDown / PageUp: capítulo\nCtrl + Shift + → / ←: livro\nEsc: limpar o telão\nEnter: projetar o que foi digitado\nF2 / F3: versículo anterior / próximo"),
            new HelpItem("🎙 Segure para falar (F4)", "Reconhecimento de voz por microfone. Precisa do Whisper, que não funciona em computadores sem AVX2; nesse caso use a digitação ou o celular."),
            new HelpItem("✔ Projetar (Enter) / ✖ Descartar (Esc)", "Confirmação da referência entendida por voz, antes de ir ao telão."),
            new HelpItem("📱 Controle pelo celular", "Pelo botão 📱 Celular (aba Ao vivo): o celular digita ou fala a referência pelo teclado e controla versículo, capítulo e Limpar telão.")
        });
}

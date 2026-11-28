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
        _helpTab = new System.Windows.Controls.TabItem
        {
            Header = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Children =
                {
                    new System.Windows.Controls.Border
                    {
                        Width = 29, Height = 29, CornerRadius = new CornerRadius(15),
                        Background = Hex("#8B4DE8"), BorderBrush = Hex("#FFFFFF"),
                        BorderThickness = new Thickness(1.5), Margin = new Thickness(0, 0, 8, 0),
                        Child = new System.Windows.Controls.TextBlock
                        {
                            Text = "?", Foreground = Hex("#FFFFFF"), FontSize = 14,
                            FontWeight = FontWeights.Black,
                            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                            VerticalAlignment = System.Windows.VerticalAlignment.Center
                        }
                    },
                    new System.Windows.Controls.TextBlock
                    {
                        Text = "Ajuda", VerticalAlignment = System.Windows.VerticalAlignment.Center,
                        FontSize = 12, FontWeight = FontWeights.Bold
                    }
                }
            }
        };
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
        if (ReferenceEquals(tab, OfflineLibraryTab)) return OfflineLibraryHelp;
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

        var text = Hex(App.IsLightTheme ? "#17324D" : "#F2F6F8");
        var muted = Hex(App.IsLightTheme ? "#536D83" : "#B5C8E7");
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
                Background = Hex(App.IsLightTheme ? "#EAF2F8" : "#0B2450"),
                BorderBrush = Hex(App.IsLightTheme ? "#A8C3D5" : "#245B88"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 7, 10, 7),
                Margin = new Thickness(0, 0, 0, 6),
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = item.Text, FontSize = 13, FontWeight = FontWeights.Normal, TextWrapping = TextWrapping.Wrap,
                    Foreground = muted, Margin = new Thickness(18, 4, 0, 8)
                }
            };
            expanders.Add(expander);
            stack.Children.Add(expander);
        }

        var toggle = new System.Windows.Controls.Button { Content = "Abrir tudo", Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 0), Background = Hex("#123A66"), Foreground = Hex("#F2F6F8"), BorderBrush = Hex("#00D9F5"), BorderThickness = new Thickness(1) };
        var opened = false;
        toggle.Click += (_, _) =>
        {
            opened = !opened;
            foreach (var expander in expanders) expander.IsExpanded = opened;
            toggle.Content = opened ? "Recolher tudo" : "Abrir tudo";
        };
        var close = new System.Windows.Controls.Button { Content = "Fechar", Padding = new Thickness(20, 8, 20, 8), IsCancel = true, IsDefault = true, Background = Hex("#087F9E"), Foreground = Hex("#FFFFFF"), BorderBrush = Hex("#00D9F5"), BorderThickness = new Thickness(1) };

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
            Background = Hex(App.IsLightTheme ? "#F4F8FC" : "#06152F"),
            Content = dock
        };
        close.Click += (_, _) => window.Close();
        window.Closed += (_, _) => { if (ReferenceEquals(_helpWindow, window)) _helpWindow = null; };
        _helpWindow = window;
        window.Show();
    }

    private static readonly HelpPage OfflineLibraryHelp = new(
        "Biblioteca Offline",
        "Organize cópias locais para tocar músicas mesmo sem internet. Os arquivos de áudio ficam separados das letras e dos dados.",
        new[]
        {
            new HelpItem("📁 Escolher pasta de armazenamento", "Escolha a pasta principal onde o Lyravoxen guardará músicas, letras e dados. O aplicativo cria Musicas, Letras, Dados e Temporarios e lembra o caminho nas próximas aberturas. A mudança de pasta preserva o arquivo de letras anterior e copia uma cópia para o novo local, sem apagar a pasta antiga."),
            new HelpItem("Letras permanentes", "As letras completas e as frases sincronizadas são salvas em Letras/letras-permanentes.json, com cópia de segurança. Apagar uma cópia de áudio ou remover um atalho da biblioteca não apaga esse arquivo. Ao carregar novamente o mesmo vídeo ou URL, o aplicativo tenta recuperar a letra e os tempos arquivados."),
            new HelpItem("Pedidos recebidos", "O botão Baixar offline nos pedidos guarda os arquivos em Musicas e cria a coleção Pedidos recebidos. Links diretos MP3/WAV são validados; links do YouTube podem gerar uma cópia M4A pública pelo yt-dlp oficial, com verificação SHA-256 e confirmação de uso autorizado na primeira utilização. O arquivo M4A é reproduzido pelo MediaElement do Windows. A disponibilidade depende do vídeo; não são usados cookies nem contornados DRM, paywalls ou anúncios."),
            new HelpItem("Validade das cópias", "Ao baixar pelo pedido, escolha 5, 7, 14 ou 30 dias. Ao vencer, a cópia local pode ser removida se não houver outro vínculo válido. Os dados da música e as letras permanentes são preservados."),
            new HelpItem("Escolher destino", "No bloco DOWNLOAD MANUAL — LINK DIRETO MP3/WAV, use Escolher destino para selecionar a pasta física onde o arquivo será salvo. Se não escolher outra, o aplicativo usa a pasta Musicas da Biblioteca Offline."),
            new HelpItem("⬇ Baixar link para esta coleção", "Cole um endereço HTTP ou HTTPS que entregue diretamente um arquivo MP3 ou WAV autorizado. Selecione uma coleção na coluna Pastas e Coleções, escolha o destino físico e clique em Baixar link para esta coleção. O arquivo é validado, registrado na coleção selecionada e recebe validade de 5, 7, 14 ou 30 dias. Páginas de vídeo do YouTube não são links diretos de arquivo e não são aceitas neste campo.")
        });

    private static readonly HelpPage LiveHelp = new(
        "Ao vivo",
        "Aqui você toca a música (YouTube ou arquivo), acompanha as frases sincronizadas e projeta na tela própria e no Holyrics. " +
        "A barra de botões do topo cuida da conexão, do backup e da projeção.",
        new[]
        {
            new HelpItem("Carregar YouTube", "Cole o link do vídeo no campo ao lado e clique. Mesmo antes de clicar, logo abaixo da barra o app já diz se o vídeo tem letra: ✅ sincronizada (pronta para projetar), 🟡 só texto, ❌ não achada. Ao carregar, o INTÉRPRETE e o TÍTULO DA MÚSICA são separados sozinhos a partir do título do vídeo, sem Playback, Legendado, Ao Vivo, \"um tom abaixo\" etc."),
            new HelpItem("Tocar próxima", "Toca a música que você deixou preparada na aba \"＋ Próxima música\". Só fica ativo depois de preparar uma."),
            new HelpItem("Conectar Holyrics", "Informe endereço, porta e token da API do Holyrics e teste. Sem isso o app projeta só na tela própria."),
            new HelpItem("🩺 Diagnóstico", "Confere Holyrics e permissões do token, microfones, modelo de voz, monitores e internet. O resultado é copiado para colar numa conversa de suporte."),
            new HelpItem("☁ Backup", "Guarda uma cópia da biblioteca (letras, tempos, mídia) numa pasta de sua escolha, como OneDrive ou Google Drive. É automático ao abrir, a cada 10 minutos e ao fechar. Também restaura em outro computador."),
            new HelpItem("Janela Pedidos recebidos", "Os pedidos aparecem em cartões separados, com o título da música, o nome do conjunto ou cantor, link e situação offline. Use Preparar próxima, Baixar offline, Abrir link ou Descartar pedido. Bloquear IP e Desbloquear IP controlam remetentes. Configurar internet, Configurar WhatsApp, Colar pedido, Importar conversa .txt e Copiar link da rede local ficam em Configurar recebimento de pedidos e links."),
            new HelpItem("📥 Pedidos", "Lista de músicas enviadas pelas pessoas, com o nome de quem enviou e, abaixo, se a música já tem letra (✅ sincronizada, 🟡 só texto, ❌ não achada). Para cada uma: Preparar como próxima (YouTube), Abrir, Copiar link ou Descartar.\nComo as pessoas enviam:\n• Link da rede local (no topo da janela): funciona no mesmo Wi-Fi.\n• ☁ Pedidos pela internet: link que funciona em qualquer rede, por um serviço gratuito do Cloudflare. O botão gera o código e o passo a passo (LEIA-ME). Informe o endereço e a senha, teste a conexão e divulgue o link /pedido. O app busca os pedidos sozinho.\n• ⚙ WhatsApp: cadastre seu número e o link do grupo e gere o formulário .html para enviar. Ele funciona mesmo no celular sem JavaScript. 💬 copia o texto de convite para colar no grupo.\nPedidos feitos dentro de um grupo: copie as mensagens no WhatsApp Web (Ctrl+C) e elas entram sozinhas; ou use 📋 Importar pedido / 🗂 Importar conversa exportada (.txt). Links repetidos há menos de 2 dias são ignorados."),
            new HelpItem("📁 Escolher pasta de armazenamento", "Na Biblioteca Offline, escolha a pasta principal onde o Lyravoxen guardará os arquivos. O aplicativo cria automaticamente as subpastas Musicas, Letras, Dados e Temporarios e lembra a escolha nas próximas aberturas. As letras e sincronizações também são preservadas em um arquivo independente, separado dos áudios; apagar uma cópia de áudio não deve apagar a letra arquivada."),
            new HelpItem("⬇ Baixar offline no pedido", "Na ficha de cada pedido, Baixar offline guarda uma cópia local. Endereços diretos MP3/WAV são baixados e validados. Para links do YouTube, o Lyravoxen pode obter a faixa pública M4A com o yt-dlp oficial, baixado sob demanda e verificado por SHA-256; na primeira utilização, confirme que tem autorização para usar esse conteúdo. Não são usados cookies nem contas, e não há contorno de DRM, paywalls ou anúncios. A disponibilidade depende do vídeo e da plataforma. Se falhar, você pode associar um MP3/WAV autorizado. O pedido é salvo na pasta Pedidos recebidos e vinculado à cópia offline. Escolha 5, 7, 14 ou 30 dias."),
            new HelpItem("🛡 Gerenciar IPs bloqueados / Atualizar bloqueios da internet / Bloquear este IP / Desbloquear", "Gerenciar IPs bloqueados permite revisar remetentes bloqueados. Atualizar bloqueios da internet consulta a lista guardada no Worker. Cada pedido online mostra o IP público de origem quando o Worker atualizado está publicado. Bloquear este IP impede novos pedidos daquele endereço; Desbloquear este IP remove a restrição. O Worker atualizado limita cada IP a 5 envios por 10 minutos. O formulário avisa que IP e horário são registrados para prevenção de abuso. Endereço MAC não é coletado nem pode ser visto pelo site."),
            new HelpItem("Botões da janela de Pedidos", "No topo da janela fica o QR code do link de pedidos pela internet: a pessoa aponta a câmera e abre a página de pedidos (no celular, o botão 📲 QR code dos pedidos online mostra o mesmo QR em tela cheia, para outra pessoa escanear). Copiar link de pedidos: copia o link da rede local. Copiar link da internet: copia o link /pedido do serviço na nuvem.\n☁ Pedidos pela internet (qualquer rede): endereço do serviço e senha, 🧪 Testar conexão, 🔑 Nova senha e 📄 Gerar código do serviço (worker.js), que vem com o passo a passo (LEIA-ME).\n⚙ WhatsApp: número e grupo / formulário: cadastra seu número e o grupo; 📄 Gerar formulário (.html) cria o arquivo para enviar; 💬 Copiar texto de convite para o grupo copia o texto para colar no grupo.\n📋 Importar pedido (colar mensagem) e 🗂 Importar conversa exportada (.txt) trazem pedidos de mensagens do WhatsApp."),
            new HelpItem("🎧 Som do computador no celular", "Na aba ▶ Ao vivo do celular, o cartão \"Som do computador\" tem um medidor que mostra se está saindo som no PC (🔊) ou se está em silêncio (🔇) e o botão \"Ouvir a música do PC\": LIGADO / DESLIGADO, que toca no celular o mesmo som que o computador está tocando, para você ter certeza de que a música rolou. Funciona pela rede Wi-Fi local, sem internet, com um atraso de menos de 1 segundo. O som é captado do Windows e só é enviado enquanto o botão estiver ligado. Se o medidor mostrar silêncio com música tocando, confira o volume e a saída de áudio do PC. No iPhone, desligue o modo silencioso."),
            new HelpItem("📱 Celular", "Mostra o QR code e o PIN para controlar o Lyravoxen pelo celular, na mesma rede Wi-Fi. Trocar PIN gera um PIN novo.\nAba Bíblia: digite ou fale a referência, versículo e capítulo anterior/próximo (o capítulo novo já é projetado desde o versículo 1), Limpar telão. 📲 QR code dos pedidos online: mostra o QR do link de pedidos em tela cheia.\nNa aba ▶ Ao vivo a Buscar na biblioteca e as Últimas músicas salvas ficam no topo.\nAba ▶ Ao vivo:\n• No telão agora: a frase projetada, ◀ Frase / Frase ▶ (modo manual), 🔄 Voltar ao automático, 🧹 Limpar telão (igual ao da Bíblia: fecha a projeção própria e limpa o Holyrics; Voltar ao automático reabre e retoma), ⏪/⏩ 10 s, volume e mudo, pausar/continuar e encerrar.\n• Próxima música: miniatura, prévia da letra, ✅ Confirmar letra ou ✖ Não usar, ✎ Criar letra (cole o texto), ⇄ Trocar campos, ✨ Corrigir nomes, 🔎 Verificar com estes nomes e Tocar próxima.\n• Sem letra com tempos: ⏱ Estimar tempos e 👆 Marcar por toque (botão grande TOQUE no celular).\n• Som do computador: medidor de nível e botão para ouvir a música do PC no celular.\n• 🔗 Colar link do YouTube: vira a próxima (▶ Como próxima) ou entra na fila (➕ Na fila). A 📋 Fila guarda até 20 músicas; quando a próxima começa a tocar, a primeira da fila assume o lugar. Pedidos, biblioteca e últimas salvas também têm ➕ Fila.\n• ⏩ Tocar a próxima sozinha ao terminar e 📝 Buscar o texto automaticamente (com a chave do Vagalume).\n• Pedidos (Preparar), Últimas músicas salvas e busca na biblioteca (Próxima / Tocar já)."),
            new HelpItem("📌 Fixar à frente", "Mantém o Lyravoxen por cima das outras janelas."),
            new HelpItem("📋 Relatório", "Copia o registro de uso e de erros para você colar numa conversa de suporte."),
            new HelpItem("Abrir projeção própria", "Abre ou fecha a tela independente de projeção (tela cheia, no outro monitor)."),
            new HelpItem("Escolher fundo", "Importa uma imagem JPG ou PNG para ser o fundo da projeção própria."),
            new HelpItem("Holyrics ativo", "Liga ou desliga o envio ao Holyrics, sem fechá-lo."),
            new HelpItem("Sair da projeção", "Fecha a projeção própria e a apresentação atual do Holyrics."),
            new HelpItem("Encerrar música", "Para a música e fecha a projeção no Lyravoxen e no Holyrics."),
            new HelpItem("Sub-aba Sincronização", "Mostra a letra e o andamento da música. Os campos são INTÉRPRETE e TÍTULO DA MÚSICA: o app preenche sozinho a partir do título do vídeo. ✨ Corrigir nomes refaz a separação (e o catálogo ajuda a descobrir quem é o intérprete). Buscar letra procura no catálogo online. A linha de status abaixo dos campos mostra se a música já tem letra; quando o catálogo tem, o botão \"Usar esta letra\" aplica e salva (sincronizada) ou leva o texto para Cadastrar letra."),
            new HelpItem("Sub-aba Controles manuais", "◀ Anterior / Próxima ▶ / Projetar selecionada: navegam e projetam frases à mão.\n" +
                "Modo automático: a projeção acompanha o tempo da música.\n" +
                "Editar selecionada, Atualizar frase, Excluir frase, ↑/↓ Reordenar: corrigem a letra.\n" +
                "Adicionar tempo, Marcar frase no tempo atual: acertam o instante de cada frase.\n" +
                "Importar letra sincronizada (.LRC / .SRT): traz uma letra com tempos de um arquivo."),
            new HelpItem("Biblioteca Offline — Histórico de vídeos", "Na aba principal Biblioteca Offline, lista os vídeos já usados. Use a lupa (⌕) para filtrar e clique para reabrir."),
            new HelpItem("Editar música e letra selecionada", "Na aba Biblioteca, selecione uma música e use este botão para editar o título, o intérprete e a letra completa sem sair da Biblioteca."),
            new HelpItem("Salvar alterações", "Dentro do editor da Biblioteca, grava os dados e a letra atualizados na coleção local, preservando a sincronização já existente."),
            new HelpItem("Biblioteca Offline — Biblioteca", "Na aba principal Biblioteca Offline, suas músicas ficam organizadas em pastas. Criar pasta, Renomear e Excluir organizam as coleções. Adicionar vídeo atual a esta pasta guarda o link/ID do vídeo aberto e copia os dados da letra e sincronização; não baixa o vídeo. ＋ Importar áudio MP3/WAV para uso offline permite colar um endereço direto autorizado de MP3/WAV para baixar e validar ou deixar o campo vazio para importar arquivos locais. Se colar um link do YouTube, o app explica a limitação e abre a seleção de MP3/WAV autorizado no computador. A validade é de 5, 7, 14 ou 30 dias. Editar música e letra selecionada abre o editor da faixa selecionada; altere o título, intérprete e letra e clique em Salvar alterações. Mover para outra pasta muda a música de coleção sem apagar o arquivo; Remover desta pasta apaga só o registro da coleção e preserva o arquivo. No seletor de destino, Usar pasta selecionada confirma a coleção escolhida e Criar nova pasta adiciona uma coleção. A busca considera título, intérprete e compositor. Ao vencer, o arquivo local é removido, mas a ficha e a letra permanecem. Corrigir nomes de todas as músicas é uma ação em lote: separa intérprete e título e limpa sufixos como Playback, Legendado e um tom abaixo em toda a biblioteca e histórico; revise antes de usar."),
        });

    private static readonly HelpPage RegisterHelp = new(
        "Cadastrar letra",
        "Aqui você cadastra a letra de uma música e acerta os tempos. Pesquise pelo título, intérprete ou trecho da letra. Confira a versão antes de importar. " +
        "Depois marque cada cartão no instante em que a frase começa ou use o reconhecimento automático para ajudar.",
        new[]
        {
            new HelpItem("⌕ Pesquisar letras / Buscar letra por título", "⌕ Pesquisar letras consulta o catálogo gratuito LRCLIB por título, intérprete ou álbum. A busca desse catálogo não pesquisa o corpo da letra. A janela mostra os resultados, prévia, álbum e duração. Quando houver LRC, use Usar letra + tempos para importar os tempos encontrados; para letras sem sincronização, escolha Usar texto para revisar e marque os cartões. Buscar letra por título abre o caminho de busca baseado no título e intérprete. O catálogo não é o banco privado do Holyrics; confira sempre a versão, autoria e permissões de uso antes de projetar."),
            new HelpItem("＋ Criar cartões", "Analisa a letra-fonte e transforma cada frase em um cartão individual. Títulos como [Coro], [Estrofe] e marcadores 2x ajudam a separar blocos e repetições. A letra-fonte permanece separada da sequência de execução."),
            new HelpItem("Salvar cadastro / Concluir e salvar", "Salva o texto-fonte, a ordem dos blocos, as ocorrências repetidas e os tempos marcados, vinculados à música atual. Os cartões sem tempo continuam identificados como pendentes, sem receber tempos inventados."),
            new HelpItem("● MARCAR CARTÃO SELECIONADO / {Binding MarkButtonText}", "Dê play na música. Selecione o cartão e clique em MARCAR CARTÃO SELECIONADO ou MARCAR AGORA no próprio cartão exatamente quando a frase começar. O cartão avança automaticamente. Você também pode usar Espaço para marcar o selecionado."),
            new HelpItem("↶ Desfazer / Limpar marcação", "Desfaz a última marcação e restaura o tempo e a situação anteriores daquele cartão."),
            new HelpItem("＋ Repetir bloco", "Cria outra ocorrência do bloco selecionado na sequência sem duplicar o texto-fonte. Marque essa nova ocorrência quando o refrão ou trecho voltar a ser cantado."),
            new HelpItem("↑ / ↓", "Move o cartão selecionado para cima ou para baixo na sequência. Salve o cadastro para gravar a nova ordem."),
            new HelpItem("⚡ Reconhecer cantado (gratuito/offline)", "Usa o Whisper local para ouvir o áudio que está saindo do computador e comparar a transcrição com a sequência dos cartões. A música precisa estar tocando. Frases que o reconhecimento não conseguir localizar com segurança ficam sinalizadas para revisão; o reconhecimento atual é incremental durante a reprodução, não uma análise completa prévia de toda a gravação."),
            new HelpItem("Outras fontes", "Abre fontes alternativas de pesquisa na internet quando o catálogo principal não encontra a música. Confira o site, a versão e a autoria antes de copiar uma letra."),
            new HelpItem("📋 Colar e limpar letra", "Cola o que você copiou de um site (Letras.mus.br, Cifra Club...) e limpa: tira cifras, [Refrão], créditos, links e \"(2x)\", e separa as estrofes por uma linha em branco."),
            new HelpItem("⚡ Cadastro rápido", "O caminho mais curto para uma música sem letra: pega a letra do catálogo (se só tiver o texto) ou da área de transferência, salva, reinicia a música do começo e já liga a marcação por toque. Sem letra copiada, ele abre as fontes de pesquisa; copie a letra (Ctrl+C) e clique de novo."),
            new HelpItem("🔎 Buscar texto da letra / ⚙ Fonte de letras", "Busca o TEXTO da letra (sem tempos) pelo intérprete e título, na API gratuita do Vagalume. Para usar, clique em ⚙ Fonte de letras e cole sua chave (crie uma conta em vagalume.com.br e peça a chave da API). Com a chave, o texto também é buscado sozinho quando o catálogo de letras sincronizadas não tem a música, no ⚡ Cadastro rápido e no celular (📝 Buscar o texto automaticamente). 🧪 Testar chave confere se a chave funciona. Depois do texto, use ⏱ Estimar tempos ou marque por toque."),
            new HelpItem("⏱ Estimar tempos (rascunho)", "Distribui as estrofes pela duração da música em segundos, proporcional ao tamanho do texto. Se a música estiver tocando, o instante atual vira o começo da 1ª estrofe. É aproximado (não sabe de solos nem refrões repetidos): acompanhe e corrija com Frase ◀/▶ ou marque por toque."),
            new HelpItem("Pelo celular", "A aba ▶ Ao vivo do celular também cola a letra, estima os tempos e tem o botão TOQUE para marcar as estrofes, sem precisar estar no computador."),
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
            new HelpItem("Atalhos de sites", "▶ YouTube, G Google, ♫ Cifra Club, V Vagalume, L Letras.mus.br, ♪ Ouvir Música e ✝ Harpa Cristã abrem direto na aba atual."),
            new HelpItem("Tocar a próxima sozinha quando a música terminar", "Caixa de seleção no painel PRÓXIMA MÚSICA (também no celular). Ligada, quando a música atual termina sozinha o app espera 3 segundos e começa a próxima já preparada, com a letra. Fica desligada por padrão e a sua escolha é lembrada. Se você encerrar a música manualmente, ela não toca a próxima."),
            new HelpItem("🔎 Verificar letra / ✅ Confirmar letra", "Ao preparar a próxima, o painel mostra a miniatura do que foi achado e as primeiras linhas da letra (✅ sincronizada, 🟡 só texto, ❌ não achada). Se for a música certa, clique em Confirmar letra: ela é salva na biblioteca e roda sozinha no telão quando tocar. Verificar letra refaz a consulta."),
            new HelpItem("Preparar próxima", "Abra o vídeo desejado (não apenas a página de resultados) e clique. Ela fica pronta para o botão \"Tocar próxima\" na aba Ao vivo."),
            new HelpItem("🛡 Proteção do navegador", "Proteção ligada filtra domínios conhecidos de anúncios e rastreadores nas abas do navegador. O contador mostra regras carregadas e solicitações bloqueadas. O botão Permitir site (também identificado como Permitir neste site) cria uma exceção persistente para o domínio da aba atual. Para usar: abra o site, clique em Permitir site e recarregue a página. A proteção não garante bloquear anúncios servidos pelo próprio YouTube ou pela mesma origem do vídeo; alguns domínios do YouTube são permitidos para preservar pesquisa e reprodução.")
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

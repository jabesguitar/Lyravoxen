# Bíblia, microfone e identidade visual

## Requisitos aprovados
- Integração Holyrics com liga/desliga, mantendo o processo aberto quando a integração for desativada.
- Microfone bíblico com liga/desliga próprio, seleção de entrada e captação independente do áudio musical.
- Bíblia integrada com versões disponíveis no Holyrics apenas quando o uso/redistribuição estiver licenciado.
- Fundos modernos, discretos e legíveis, com centro limpo e elementos decorativos nas bordas.

## Implementado nesta branch
- Interruptor persistente de integração Holyrics na barra principal.
- Serviço independente de microfone usando entrada selecionada do Windows e Whisper.net local.
- Transcrição aparece em prévia; o parser Midvash pode sugerir referências faladas em português. Não há projeção automática de sugestões.
- Leitor bíblico com seleção de tradução, livro, capítulo e versículo, projeção manual e cache local por capítulo.
- Versões iniciais: ALM1911, Almeida Livre, Bíblia Portuguesa Mundial e Bíblia Livre Para Todos, respeitando as licenças publicadas pelo catálogo de dados.

## Licenciamento
O Holyrics lista traduções fornecidas por editoras e sociedades bíblicas parceiras. A existência dessas traduções no Holyrics não transfere ao Lyravoxen direitos de cópia ou redistribuição. Traduções com copyright ativo (por exemplo NVI, ARA, NAA, NVT e outras) só poderão ser incorporadas mediante autorização/licença. O seletor inicial usa versões de domínio público ou licenças abertas; o crédito informado pela API acompanha o texto exibido.

Fontes:
- https://holyrics.com.br/about.php
- https://api.midvash.com/pt-br
- https://api.midvash.com/docs
- https://github.com/midvash/bible-data

## Comportamento do microfone
- Desligado por padrão.
- Operador escolhe o dispositivo de entrada explicitamente.
- Áudio é transcrito localmente, sem upload para nuvem.
- Parser de referência depende de conexão à API Midvash; transcrição continua local.
- Resultado é sugestão para conferência, não é enviado ao telão automaticamente.
- Desativar ou fechar o aplicativo encerra a captura.

## Fundos
O usuário forneceu referências visuais com azul-marinho, iluminação azul, detalhes dourados, oliveira, rocha texturizada, coroa de espinhos e tecido vinho. Foram criados quatro fundos 16:9 e quatro adaptações 4:3, com espaço central limpo. Ainda precisam ser incorporados ao seletor de fundos do projetor próprio.


## Almeida Revista e Corrigida — SBB 2009
A prioridade para cultos é a edição **Almeida Revista e Corrigida © 2009 (ARC)** da Sociedade Bíblica do Brasil, não a Almeida 1911 histórica, não a Almeida Corrigida Fiel (ACF), e não a Almeida Revista e Atualizada (ARA).

A ARC SBB 2009 é protegida por direitos autorais. O Holyrics declara parceria com a SBB para essa tradução. O Lyravoxen identifica a edição brasileira pelo ID `pt_arc` (o ID `pt_rc69` corresponde à edição 1969 e não será usado como substituto). O Lyravoxen não deve baixar, embutir, copiar ou redistribuir o texto sem autorização. Em vez disso, a integração consulta as traduções instaladas no Holyrics (`GetBibleVersionsV2`) e solicita a projeção da referência na versão selecionada (`ShowVerse`). Isso preserva o texto oficial fornecido pelo próprio Holyrics, sem paráfrases, correção automática, revisão ortográfica, normalização ou alteração artificial pelo Lyravoxen.

A edição de 1969 (ARC69) é outra edição e deve aparecer separadamente se instalada. A edição 1911 de domínio público também é separada e nunca será rotulada como ARC SBB 2009.

A API documentada do Holyrics permite listar versões e projetar referências, mas não expõe uma função documentada para descarregar o texto integral da Bíblia ao Lyravoxen. Por isso, a leitura ARC no app independente depende de autorização/licença e fornecimento de um arquivo oficial pela SBB.

Fontes:
- SBB, Almeida Revista e Corrigida: https://www.sbb.org.br/almeida-revista-e-corrigida
- Holyrics, parcerias e versões: https://holyrics.com.br/about.php
- Holyrics API Server: https://github.com/holyrics/API-Server
- SBB+, lista ARC69 e ARC 2009: https://play.google.com/store/apps/details?id=org.bb.app


## Ajustes de usabilidade — 17/11/2026
- A Bíblia fica em uma aba principal exclusiva, separada do fluxo musical. Dentro dela, microfone e biblioteca possuem subabas próprias.
- Seletores de dispositivo e versão usam texto escuro sobre superfície clara; opções da lista têm contraste e realce visíveis.
- O seletor de microfone mostra o nome do dispositivo, informa qual está selecionado e fica bloqueado enquanto a captação está ativa.
- A transcrição de fala permanece visível mesmo quando a interpretação online falha. O app tenta interpretar referências bíblicas comuns localmente antes de consultar o parser gratuito online. A referência identificada é uma sugestão e exige conferência antes da projeção.
- O leitor livre pode carregar de 1 a 6 capítulos consecutivos, com identificação explícita de capítulo e versículo, fundo claro e rolagem.
- A prévia de letras recebeu mais espaço horizontal e vertical, tipografia maior e contraste reforçado.
- A ARC SBB 2009 continua separada das versões livres: a interface não troca silenciosamente uma tradução pela outra.
- O reconhecimento local e a consulta online não substituem a validação do operador; microfone, ambiente acústico, conexão e versão licenciada instalada no Holyrics ainda exigem teste no computador de destino.


## Ajustes após validação visual — 17/11/2026
- O controle de microfone passou a mostrar caixa marcada com alto contraste e estado textual: DESLIGADO, INICIANDO, PREPARANDO, OUVINDO, VERIFICAR, ERRO ou SEM DISPOSITIVO.
- O primeiro uso informa explicitamente o download do modelo Whisper gratuito. A indicação OUVINDO só é apresentada depois que o serviço inicia a captura.
- Medidor de nível de áudio RMS em tempo real distingue dispositivo selecionado de entrada que realmente recebe sinal.
- Se nenhum dispositivo for encontrado, a interface informa a necessidade de verificar a entrada e as permissões do Windows.
- A navegação bíblica segue a ordem solicitada: Bíblia/versão → livro → capítulo → versículo. O seletor de versículo e a lista ficam sincronizados; trocar livro/capítulo limpa a seleção antiga.
- Os textos de versículos e itens de seleção usam templates com contraste explícito para evitar texto claro sobre fundo claro.
- A projeção própria agora apresenta referência bíblica separada do texto do versículo, abre a janela de saída em primeiro plano e informa o monitor de destino. O caminho da ARC SBB 2009 continua separado e usa o Holyrics com a versão pt_arc.
- O parser de fala foi extraído para componente testável e cobre referências numéricas, números por extenso, números mistos e livros com ordinal. A referência é sempre sugerida para conferência; nunca projetada automaticamente.
- A prévia musical mantém 16:9 e pode ocupar mais altura disponível.
- Validação CI: run #513 (ID 44151904422), commit a7331eb4e7977a2c8035b4b8e940133797f92bc9 — build Windows x64, testes de linha do tempo e parser bíblico, pacote portátil e instalador aprovados.
- Ainda exigem teste no computador do usuário: permissões e dispositivo de áudio, download do modelo, nível de sinal real, precisão do reconhecimento no ambiente, tela externa e comunicação efetiva com Holyrics.


## Letra completa e sincronização automática — 17/11/2026
- A letra integral é armazenada em LibraryVideo.FullLyrics, separada da linha do tempo de marcações (Cues).
- A aba de edição concentra o fluxo em um campo de letra completa, ação de salvar e ação de reconhecimento automático. Os controles de marcação frase a frase deixam de ser o caminho principal.
- A letra é vinculada ao item atual e recuperada quando o vídeo/áudio é carregado novamente; também acompanha cópias do item para pastas da biblioteca.
- Ao ativar o reconhecimento, o texto é salvo antes da captação. WASAPI loopback captura o áudio que o Windows está reproduzindo, Whisper transcreve localmente e LiveLyricsMatcher compara a transcrição com a próxima parte da letra.
- A correspondência pode avançar até duas linhas à frente quando a transcrição agrupa frases; a sequência não retrocede. Cada trecho aceito gera cue de tempo, atualiza a prévia e é enviado ao Holyrics se a integração estiver ativa.
- Não confundir com o microfone da Bíblia: são serviços separados. O reconhecimento de música captura o áudio de saída do computador, não o microfone bíblico.
- O desempenho real depende do sinal reproduzido, voz, mixagem, reverberação, playback instrumental, blocos de transcrição e similaridade textual. A sincronização precisa ser testada com músicas reais antes de ser considerada pronta.

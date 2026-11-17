# MEMORIA_CONTINUA — Lyravoxen

## Estado atual — 2026-11-17

### Objetivo
Evoluir o Lyravoxen sem perder os fluxos musicais existentes: player YouTube interno, sincronização de letras, projeção, biblioteca, integração opcional Holyrics, leitura bíblica, reconhecimento por microfone e fundos elegantes.

### Decisões confirmadas pelo usuário
- O aplicativo deve permanecer gratuito e leve, com suporte a Windows 10/11 e processadores mais antigos.
- A integração Holyrics precisa de interruptor independente.
- Ao desativar Holyrics, fechar a apresentação rápida e impedir novos envios, mas manter o processo Holyrics aberto.
- O microfone para reconhecer referências bíblicas deve ter liga/desliga próprio e seleção explícita do dispositivo.
- Reproduções musicais e captação do microfone bíblico são fluxos separados.
- O usuário quer as mesmas traduções do Holyrics quando houver direito de uso; não distribuir textos protegidos sem licença.
- Fundos de projeção: azul-marinho, luz azul, dourado, oliveira e pedra, centro limpo para letras; proporções 16:9 e 4:3.
- O usuário autorizou implementar o pacote e pediu continuidade com relatórios, sem perguntas desnecessárias.

### Trabalho implementado nesta branch
- Branch criada sobre a branch de estabilização do Holyrics (PR #7), preservando main.
- Interruptor persistente Holyrics na barra principal; desligar invalida envios pendentes, fecha apresentação rápida quando configurado e mantém o programa Holyrics aberto.
- Serviço separado de captação de microfone com NAudio WaveInEvent e Whisper.net local.
- Aba Bíblia e microfone: seleção de dispositivo, ativação explícita, status, transcrição e sugestão de referência via parser de referência da API Midvash; não projeta automaticamente a referência sugerida.
- Biblioteca bíblica: versões portuguesas com licença livre, seleção de livro/capítulo, leitura e projeção de versículo selecionado.
- Cache local por versão/livro/capítulo: capítulos já consultados ficam disponíveis offline.
- Documentação de licença: versões protegidas como NVI, ARA, NAA e NVT não podem ser empacotadas sem autorização; o catálogo do Holyrics lista traduções de editoras parceiras.
- Fundos gráficos criados em pacote externo: quatro conceitos 16:9 e respectivas adaptações 4:3. Ainda não integrados como assets embutidos no aplicativo.

### Fontes de Bíblia/licenciamento consultadas
- Holyrics lista parcerias com SBTB, SBB, Biblica, Abba Press, Tyndale e outras editoras.
- Midvash oferece API gratuita sem chave com textos de domínio público/licenças abertas, fornece crédito por versão e cache HTTP.
- Versões livres incluídas no seletor inicial: ALM1911 (domínio público), Almeida Livre (CC BY), Bíblia Portuguesa Mundial (domínio público, conforme catálogo da API), Bíblia Livre Para Todos (CC BY-SA 4.0).
- Atribuição de copyright da resposta é mostrada junto da leitura. A disponibilidade e os metadados de licença devem ser revalidados antes de qualquer release.

### Limitações e riscos conhecidos
- A identificação de referências pela voz usa transcrição em blocos de cinco segundos; latência e precisão dependem do microfone, ambiente e CPU.
- A interpretação natural da referência usa API gratuita Midvash, portanto requer internet; a transcrição Whisper continua local. A leitura de capítulos já armazenados funciona offline.
- O cache offline cresce conforme os capítulos são consultados; ainda não há download completo de uma tradução.
- A biblioteca ainda não importa pacotes oficiais do Holyrics; textos protegidos exigem arquivo/licença fornecido legalmente pelo usuário.
- Os fundos gerados ainda não são selecionáveis dentro do app.
- Nenhum build desta branch foi confirmado até o momento; aguardar GitHub Actions.

### Próximas tarefas
1. Compilar e corrigir todos os erros do Windows CI.
2. Adicionar testes unitários para parser, cache e controles de sessão.
3. Validar o reconhecimento com microfones reais e fala em português.
4. Implementar download de uma Bíblia inteira licenciada para uso offline.
5. Integrar fundos como assets selecionáveis na projeção própria do Lyravoxen.
6. Testar Holyrics on/off com API real, incluindo troca de música e projeção rápida persistente.
7. Depois do CI e revisão, mesclar a cadeia de PRs na main.

### Branch / PR
- Branch de integração: `feature/integrated-bible-microphone-holyrics`
- Base temporária: `fix/holyrics-connect-and-exit-projection` (PR #7)
- PR desta branch será aberta como PR empilhada, sem alterar main até validação.


### Atualização — ARC SBB 2009 e fidelidade textual (2026-11-17)
- Solicitação do usuário: incluir João Ferreira de Almeida — Almeida Revista e Corrigida, a versão usada nos cultos, sem interferências artificiais, reescrita, paráfrase ou correção automática.
- Pesquisa confirmou que a SBB identifica ARC 2009 como a versão usada pela Assembleia de Deus; a SBB também disponibiliza ARC69 (1969) como uma edição distinta.
- O Holyrics informa parceria com a SBB para Almeida Revista e Corrigida © 2009. O catálogo de versões da Bíblia no app oficial SBB+ também lista ARC 2009 e ARC69 separadamente.
- Decisão técnica: não copiar nem redistribuir o texto protegido. Integrar a ARC por meio da instalação licenciada do Holyrics, consultando `GetBibleVersionsV2` e projetando referências com `ShowVerse`, após autorização de permissões do token.
- Implementado nesta branch: métodos para consultar versões do Holyrics e solicitar projeção de uma referência na versão escolhida; UI usa exclusivamente o identificador `pt_arc`, mapeado à ARC SBB © 2009. Não usa apenas semelhança do título e não oferece fallback para outra edição.
- O leitor Midvash local continua separado. Almeida 1911 é identificada explicitamente como edição histórica distinta, não como ARC SBB 2009.
- Limitação: a API pública documentada do Holyrics lista versões e permite projetar referências, mas não oferece endpoint documentado para extrair o texto completo da Bíblia para o leitor próprio. A projeção ARC original é feita pelo Holyrics. Para exibição independente/offline no telão próprio, será necessária uma licença/arquivo autorizado pela SBB e um formato de importação compatível.
- Validação pendente: configurar o Holyrics no computador de teste, verificar que a lista retorna ARC 2009, conferir referências, testar permissões e confirmar visualmente que o texto projetado é o original.


### Marco de validação — pacote Windows de teste (2026-11-17)
- CI Windows aprovado no commit `4b4f66bfb9ea3634e0faca730f58d072d2340179` (workflow run #451, job build).
- Etapas aprovadas: restore, dotnet publish/build, testes de linha do tempo, pacote portátil ZIP, upload do build, instalação do Inno Setup, compilação do instalador e upload do instalador.
- Artefatos GitHub Actions do run #451:
  - Installer ZIP artifact ID: `14349994648` (contém o instalador `Lyravoxen-Setup-Windows-x64.exe`).
  - Portable ZIP artifact ID: `14349859844`.
  - Build raw artifact ID: `14351112328`.
- O pacote está estruturado para teste manual no Windows x64. O CI não testa microfone físico, API Holyrics conectada ao equipamento do usuário, projeção em telão, nem desempenho real em Intel i5/Xeon.
- PR #9 continua aberta em draft, sem mesclar na main até a validação funcional.
- Próxima etapa: usuário instala o pacote, configura token Holyrics com permissões `GetBibleVersionsV2` e `ShowVerse`, confirma que a lista retorna `pt_arc`, testa João 3:16 e realiza os testes de microfone/projeção. Registrar erros e corrigir antes de liberar a versão.


### Atualização de interface e usabilidade — 2026-11-17
- Solicitação do usuário após instalar a versão portátil: corrigir seleção/legibilidade do dispositivo de microfone e das versões bíblicas; tornar a leitura de capítulos ampla e legível; separar Bíblia em uma aba exclusiva; ampliar a prévia principal do telão; redesenhar abas e botões; manter o interruptor Holyrics aprovado.
- Decisão aprovada: manter o controle liga/desliga Holyrics sem alteração funcional e preservar os fluxos musicais já existentes.
- Implementado:
  - Bíblia movida para uma aba principal exclusiva chamada **✦ Bíblia**, com subáreas próprias para microfone e biblioteca bíblica.
  - ComboBoxes receberam cores contrastantes, texto escuro legível no campo fechado e lista clara com estados de seleção/realce visíveis.
  - Lista de microfones ampliada para mostrar melhor o nome do fabricante/modelo; dispositivo e botão de atualização ficam bloqueados durante a captação, evitando troca acidental de entrada.
  - Status de seleção e de reconhecimento informa claramente dispositivo escolhido, transcrição, referência sugerida e falhas de interpretação.
  - Reconhecimento bíblico tenta primeiro interpretar referências comuns localmente (livro/capítulo/versículo, inclusive formas faladas como “João capítulo três, versículo dezesseis”) e só então usa o parser online gratuito; transcrição continua aparecendo mesmo se a internet falhar. A sugestão preenche o campo de referência, mas não projeta automaticamente.
  - Leitor bíblico permite selecionar de 1 a 6 capítulos consecutivos; a lista mostra capítulo e versículo em cada linha, com fundo claro, contraste alto e rolagem.
  - A prévia de letras ganhou mais espaço horizontal, altura mínima maior e fonte ampliada.
  - Abas receberam estilo arredondado e estados visuais selecionado/hover; botões duplicados de seleção de fundo foram reduzidos a um único botão.
- Validação CI no commit `89741b39af651de4ccd7203f7de69b67ae352973`, workflow run #465 (ID `44144443153`): restore, build, testes da linha do tempo, pacote portátil, upload do build, upload do pacote portátil, instalação do Inno Setup, compilação do instalador e upload do instalador concluídos com sucesso.
- Artefatos do run #465:
  - `Lyravoxen-Windows-x64-portable`, ID `14354999057`, SHA256 `132bef15ef7b2c588e95911b59ef5d1dbd59041b16fef9805e13e76c43a7e204`.
  - `Lyravoxen-Installer-Windows-x64`, ID `14355433245`, SHA256 `dad8eb47f280b672f0a0abd98e77195b99558ef19684311fa8b5d93baf145051`.
  - `Lyravoxen-Windows-x64`, ID `14355847175`, SHA256 `0cca5932e64f1649f8df4bf9c68c2382f293c06b544eefb7b7933b26198c94c4`.
- O CI confirma compilação, testes automatizados já existentes e geração dos pacotes. Não comprova ainda funcionamento físico do microfone, precisão da interpretação em ambiente de culto, resposta da API Holyrics/token, fidelidade visual em projetor, nem desempenho em Xeon/i5 antigo.
- Estado: build de teste atualizado pronto para validação manual do usuário. PR #9 continua em draft, sem merge na main.
- Próxima etapa: usuário instalar o artefato do run #465, validar os novos seletores, leitura multi-capítulo, fala/transcrição, prévia e projeção; reportar qualquer falha com captura de tela. Corrigir regressões antes de solicitar aprovação para merge.


### Correções após teste visual do usuário — 2026-11-17
- Solicitação do usuário, baseada na captura do aplicativo:
  - O controle de ativação do microfone não mostrava claramente se estava marcado/ativo; o usuário não conseguia confirmar se havia captação.
  - A atualização/seleção de microfone não dava retorno claro.
  - A lista de versículos tinha texto quase invisível por baixo contraste.
  - Os seletores da biblioteca não comunicavam a sequência desejada: Bíblia/versão, livro, capítulo e versículo.
  - O comando de projetar capítulo/versículo não entregava uma saída clara.
  - O cabeçalho da ARC do Holyrics tinha texto explicativo desnecessário (“sem alteração do texto”).
  - A prévia de letras ao vivo precisava de readequação para uma área 16:9 maior e mais legível.
- Correções implementadas:
  1. **Microfone:** redesenhado o checkbox com caixa maior e marca de seleção de alto contraste; estado textual explícito DESLIGADO / INICIANDO / PREPARANDO / OUVINDO / VERIFICAR / ERRO / SEM DISPOSITIVO.
  2. **Nível de áudio:** o serviço de microfone calcula o nível RMS de cada bloco de áudio e a interface mostra uma barra de sinal em tempo real (“Sem sinal”, “Sinal baixo”, “Áudio captado”, “Sinal forte”). Isso distingue microfone selecionado de microfone que realmente recebe áudio.
  3. **Primeiro uso:** o estado informa que o modelo Whisper gratuito precisa ser baixado antes da captação; não declara “ouvindo” até a captura iniciar. Erros de enumeração e início informam verificar Configurações > Sistema > Som > Entrada e permissões do Windows.
  4. **Biblioteca:** sequência numerada e explícita de seleção: 1. Bíblia/versão, 2. Livro, 3. Capítulo, 4. Versículo. O seletor de versículo acompanha a lista e a lista acompanha o seletor; ao trocar livro/capítulo, seleção antiga é limpa.
  5. **Contraste:** ItemTemplate explícito para texto dos versículos e dos combos, com foreground escuro e fundo claro; nome da edição histórica Almeida 1911 encurtado para caber no seletor, mantendo licença/identificação separada da ARC SBB 2009.
  6. **Projeção bíblica:** adicionada saída específica com referência em cabeçalho separado do texto do versículo; a janela de projeção é trazida para frente e o estado informa qual monitor recebeu a projeção. A projeção da biblioteca livre segue pela janela própria; a ARC SBB 2009 continua usando o Holyrics e seu ID pt_arc.
  7. **Parser de fala:** extraído para Models/BibleReferenceParser.cs, ampliado para livros do cânon e números falados/digitados, incluindo “João capítulo três, versículo dezesseis”, “João capítulo 3 versículo 16”, “primeiro Samuel capítulo dois versículo quatro” e números compostos como “vinte e três”. O parser sugere a referência; não projeta sem confirmação.
  8. **Prévia musical:** mantida a proporção 16:9 e removido o limite de altura muito restritivo; a prévia agora pode ocupar mais da área disponível.
  9. **Holyrics:** o cabeçalho não inclui mais a frase redundante “sem alteração do texto”; mensagens de busca, versão não encontrada e envio passaram a aparecer na própria área bíblica.
- Durante a primeira compilação desta rodada, o CI apontou erro XAML: um Border recebeu múltiplos filhos. A estrutura foi corrigida envolvendo checkbox, estado e medidor em um StackPanel horizontal. Não ocultar esse erro do histórico.
- Testes automatizados acrescentados ao projeto tests/Lyravoxen.TimelineTests para referências numéricas, fala por extenso, números mistos, livros com ordinal, números compostos, rejeição de fala sem referência e capítulo inválido.
- Validação final do código no commit a7331eb4e7977a2c8035b4b8e940133797f92bc9: GitHub Actions run #513, ID 44151904422, conclusão **success**. Restore, Build Windows x64, testes da linha do tempo + parser bíblico, pacote portátil, upload, Inno Setup, build do instalador e upload do instalador concluídos.
- Artefatos do run #513:
  - Lyravoxen-Windows-x64-portable, ID 14360310405, SHA256 42db12577ba546820696c32fd1dfdc0e217673020dbbc263aec2d5d2b5a11c54.
  - Lyravoxen-Installer-Windows-x64, ID 14360150730, SHA256 34b1fe43373f6eb528f962e22f6d8a8304d03fa740d5dee2898b0c86dc671f6b.
  - Lyravoxen-Windows-x64, ID 14360075798, SHA256 40e1604d29279e10f0303c9419c03c228d904bab035f773c41a6a28e8e9ab4f9.
- Limite de validação: CI e testes automatizados aprovados não provam que o Windows do usuário permite acesso ao microfone, que o modelo será baixado em sua rede, que o áudio terá nível suficiente, nem que a tela de saída está corretamente configurada. Esses pontos exigem o próximo teste físico.
- Estado atual: versão atualizada pronta para novo teste no computador do usuário. PR #9 continua draft e sem merge na main.
- Próxima ação: baixar os artefatos do run #513; testar primeiro o microfone observando barra de nível e estado OUVINDO; depois reconhecer João 3:16; carregar e selecionar um versículo; projetar na tela externa; verificar a prévia ao vivo. Registrar qualquer falha com nova captura.


### Nova etapa — letra completa salva e sincronização automática por reconhecimento (2026-11-17)
- Solicitação do usuário:
  - A aba de sincronização ainda não está funcional para o fluxo esperado.
  - O controle “Próxima frase a marcar” não permite realizar a marcação como desejado.
  - Em vez de marcar cada linha manualmente, o usuário quer colar/editar a letra inteira, salvá-la e deixar o programa reconhecer o trecho cantado, sincronizar e projetar automaticamente pelo Holyrics.
  - O usuário observou estes pontos na versão anterior enquanto ainda está testando o pacote mais recente; esse teste manual permanece pendente.
- Decisão de produto desta etapa: letra completa é o conteúdo-fonte editável e reutilizável; as marcações de tempo são geradas automaticamente a partir da comparação entre áudio e letra. Não obrigar o operador a clicar frase por frase.
- Implementado nesta branch:
  1. Modelo LibraryVideo ganhou propriedade FullLyrics, separada dos tempos Cues, preservando compatibilidade com JSON existente (campo novo opcional).
  2. Aba “Criar / editar letra” reorganizada: campo grande de letra completa, botão “Salvar letra completa”, botão único de reconhecimento automático e mensagens de progresso/trecho reconhecido.
  3. Removidos da interface os botões “Iniciar sincronização ao vivo”, “Marcar próxima frase no tempo atual” e “Buscar versão sincronizada desta letra”, além do cartão “Próxima frase a marcar” e explicação de marcação manual. Métodos antigos permanecem no código por ora, mas não são expostos na interface; remoção definitiva só após validação para evitar regressão.
  4. Salvar a letra associa o texto ao vídeo/áudio atual e grava na biblioteca local. Persistência cria a entrada na pasta “Músicas sincronizadas” quando for o primeiro salvamento.
  5. Ao carregar novamente uma música da biblioteca, o campo de letra completa é preenchido com o conteúdo salvo. A propriedade é copiada ao adicionar/atualizar músicas em pastas e ao salvar metadados.
  6. Ao iniciar reconhecimento automático, o texto atual é salvo antes da captura. O áudio é capturado por WASAPI loopback no próprio computador, transcrito localmente pelo Whisper e comparado com as frases da letra em sequência. Quando uma frase é reconhecida, a linha do tempo é construída e a frase é enviada para a prévia e para o Holyrics, se a integração estiver ligada.
  7. Corrigido um erro lógico no avanço: o matcher já podia retornar uma frase até duas posições à frente, mas o chamador rejeitava qualquer resultado que não fosse exatamente a próxima linha. Agora aceita a correspondência adiante e avança o cursor para depois da frase reconhecida, sem voltar a trechos anteriores.
  8. Acrescentados testes para transcrição que corresponde à próxima frase e para transcrição agrupada que corresponde a até duas linhas adiante.
- Distinção importante: o reconhecimento usa o áudio de saída do computador (WASAPI loopback), não o microfone bíblico. A faixa reproduzida precisa estar audível na saída capturada do Windows. A transcrição é local e gratuita, sem serviço pago/API.
- Limitação atual a validar: o reconhecimento ainda depende de transcrição em blocos de áudio e correspondência textual; ruído, playback instrumental, coral, voz baixa, eco, letras com variações e trechos repetidos podem exigir ajuste. Não declarar sincronização perfeita antes de testes com faixas reais.
- CI desta nova etapa ainda pendente. Consultar o HEAD atual e o workflow mais recente antes de entregar novo pacote.
- Estado: código atualizado na branch feature/integrated-bible-microphone-holyrics; PR #9 segue draft e sem merge. A versão anterior run #513 continua sendo a compilação que o usuário está testando; esta nova funcionalidade só deve ser testada depois do novo CI e novo artefato.
- Próxima ação: validar o build e testes, corrigir erros de compilação/regressões, atualizar PR e documentação; depois fornecer um pacote novo claramente identificado como contendo letra completa salva e reconhecimento automático.


### Validação CI da etapa de letra completa (2026-11-17)
- Commit validado: `4b243ad858c4d824d83a64eee4220499c23979b6`.
- GitHub Actions run `44156072620`: concluído com sucesso. Restore, Build, Test cue timeline, pacote portátil, upload, instalação do Inno Setup, compilação do instalador e upload do instalador concluídos com sucesso.
- Run duplicado `44156062780`: também concluído com sucesso.
- Artefatos gerados no run principal:
  - `Lyravoxen-Windows-x64-portable`, ID `14361705947`, SHA256 `9446597e202ac345e6581e43cc83cb1ee47d1a54417db4b5be836d589074c517`.
  - `Lyravoxen-Windows-x64`, ID `14361591233`, SHA256 `8e135c94acd7aa16eedfab57bea37b3918df98c9c9612f055c6c140ebea4d24d`.
  - `Lyravoxen-Installer-Windows-x64`, ID `14360673385`, SHA256 `b778c0d87fbd505de5ee036e2350eb9d4f375d5756ca1b73e5f6586a89e95a6f`.
- O CI valida compilação e testes automatizados existentes, mas não substitui teste real de reprodução, reconhecimento de voz, sincronização e projeção Holyrics.
- Próxima etapa: usuário instalar/abrir esta compilação nova e testar letra completa salva, reconhecimento automático e projeção. Registrar capturas/erros antes de qualquer merge. PR #9 permanece draft e sem merge.


### Ajuste focado — recuperação de letras ausentes sem alterar o que funciona (2026-11-17)
- Nova orientação expressa do usuário: preservar pesquisa do YouTube, reprodução, sincronização com letras já encontradas e controles aprovados; o problema principal é encontrar letras de músicas que não aparecem no catálogo automático. Retirar apenas botões repetidos/sem utilidade após inspecionar as ações reais.
- Inspeção da interface e código:
  - A busca principal consulta LRCLIB e tenta reutilizar letras sincronizadas da biblioteca.
  - Havia uma ação separada “Pesquisar em outras fontes (hinos e louvores)” que repetia o fluxo de busca, e só era necessária quando a busca principal não encontrava resultado.
  - “Controles manuais” não são duplicados: anterior/próxima/projetar/modo automático, editar/atualizar/excluir/reordenar, adicionar por tempo, marcar no tempo atual e importar LRC/SRT têm funções distintas; foram preservados.
  - Ações do YouTube (carregar, copiar link, próxima música, pesquisar, voltar/avançar/atualizar, abas) também têm funções diferentes e foram preservadas.
  - Botão de buscar letra e botão separado de busca alternativa foram consolidados: o botão principal agora aciona automaticamente as fontes alternativas quando LRCLIB não retorna resultados.
- Alterações de interface:
  1. Rótulo “Buscar letra sincronizada” simplificado para “Buscar letra”.
  2. Removido apenas o botão separado “Pesquisar em outras fontes (hinos e louvores)”; o fallback passou a ocorrer automaticamente quando a busca primária não encontra letra.
  3. Aba “Criar / editar letra” renomeada “Cadastrar letra”; cabeçalho e instrução explicam que é o cadastro da letra ausente para a música atual.
  4. Não foram removidos os controles manuais úteis nem alteradas as abas de Bíblia, microfone, player ou pesquisa YouTube.
- Fontes de busca adicionais verificadas por pesquisa pública em 17/11/2026:
  - O Gospel — Harpa Cristã (ogospel.com.br/harpa-crista): catálogo declarado de 640 hinos, busca por número/título/tema/trecho.
  - mBíblia — Harpa (mbiblia.com.br/harpa): índice de 640 hinos com busca por número ou nome.
  - Hinos da Harpa (hinosdaharpa.com.br): catálogo dos 640 hinos.
  - Hinoteca (hinoteca.com.br): acervo declara incluir Harpa Cristã e outros hinários.
  - Letra Sonora (letrasonora.com.br): letrário declara incluir Harpa Cristã, Cantor Cristão e HCC.
  - Glow Bible — Hinário (glowbible.org/hinario): pesquisa por número, título ou letra e versão Harpa Cristã.
  - Mantidas também as fontes gerais Cifra Club, Vagalume, Letras.mus.br, Ouvir Música e Letras.com.
- As fontes são abertas como pesquisas internas no navegador incorporado; o programa não raspa nem redistribui automaticamente letras protegidas. O usuário confere autoria/versão e cola o texto no cadastro, salvando para uso local.
- Commits desta rodada: XAML `83c59d1beae78cf4e5b38d8a1bfa03f429b58ba5`; lógica de fallback `a8e96f321da70cd0d642f11c5de05d4bdf1e4a7d`; catálogo verificado `cc7e3c2c3a9ead2b5466ba3a0780cde5736bb8ee`.
- Estado: PR #9 continua draft e sem merge. Build/CI destas alterações ainda precisa ser conferido; não entregar novo instalador até CI ficar verde.
- Próxima ação: validar referências XAML/C# e o build completo; corrigir eventuais falhas, documentar e gerar artefato novo. Teste manual posterior: carregar música, buscar letra não encontrada, confirmar abertura das fontes no browser interno, copiar letra, salvar, iniciar reconhecimento com reprodução ativa e confirmar envio ao Holyrics. Também testar que letras previamente encontradas continuam a ser aplicadas como antes.

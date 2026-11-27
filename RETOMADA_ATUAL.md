# Lyravoxen — Registro de retomada
**Atualizado em:** 26/11/2026  
**Branch:** `feature/offline-library-on-redesign`  
**Commit de referência:** `0f5b591aa417c408cc7ca55df43096ae26043130`

## Objetivo imediato
Consolidar as correções da aba **Próxima música**, o fluxo real de mídia offline e a documentação, antes de pedir uma única rodada de testes manuais ao usuário.

## Evidências recebidas do usuário
Foram recebidas duas capturas de tela em 26/11/2026.

### 1. Aba Próxima música
- O painel lateral esquerdo ainda apresenta barra de rolagem vertical.
- Os controles de preparar, confirmar/verificar, proteção, permitir, voltar/avançar, atualizar/copiar link e fontes continuam ocupando espaço vertical e parte das opções fica abaixo da área visível.
- Pedido do usuário: reorganizar os controles em layout compacto/minimalista, mantendo as ações necessárias visíveis sem rolar.
- Restrição: não reduzir a área útil do navegador/WebView2 nem prejudicar pesquisa, abas e reprodução.

### 2. Botão “Baixar offline” nos Pedidos
- A ação atual abre uma caixa de diálogo para escolher um arquivo MP3/WAV já existente.
- O usuário esperava que o botão baixasse a mídia da fonte autorizada e a salvasse automaticamente na Biblioteca Offline, sem abrir um seletor de arquivo para localizar algo.
- Correção de produto necessária: distinguir claramente **Baixar de URL direta autorizada** de **Importar arquivo MP3/WAV local**. A importação local deve continuar disponível como alternativa.
- Ao baixar de uma URL direta autorizada, salvar em armazenamento do Lyravoxen, validar tipo MIME/extensão/tamanho/timeout, calcular hash, evitar duplicatas e associar a cópia à identidade exata do pedido/gravação.
- Se o pedido contiver somente URL do YouTube, preservar URL/ID e explicar que o link por si só não fornece um arquivo offline autorizado. Não implementar extração de áudio, DRM bypass ou contorno de restrições da plataforma.
- Ao preparar/tocar novamente, priorizar a cópia local vinculada à versão exata e não procurar correspondência apenas pelo título.

## CI concluído — execução 45874275889
**Status:** sucesso em 26/11/2026.  
**Branch:** `feature/offline-library-on-redesign`  
**SHA:** `0f5b591aa417c408cc7ca55df43096ae26043130`

Jobs aprovados:
- `cloud-worker-tests`: sucesso.
  - Check Help coverage
  - Extract and test Cloudflare Worker
  - Install Playwright Chromium
  - Test mobile remote page
- `build`: sucesso.
  - Restore e Build .NET
  - Test cue timeline
  - Pacote portátil ZIP
  - Instalador Inno Setup
  - Upload dos artefatos

Artefatos gerados:
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15144741646`
- `Lyravoxen-Windows-x64-portable` — artifact ID `15144651792`
- `Lyravoxen-Windows-x64` — artifact ID `15143443015`

A execução comprova build e testes automatizados indicados acima. **Não comprova** inspeção visual em diferentes resoluções, reprodução real offline, nem download de áudio por URL.

## Decisões e restrições preservadas
- Não fazer merge na `main` sem aprovação explícita.
- Não pedir ao usuário para instalar builds intermediários.
- O usuário só fará uma rodada manual quando o conjunto estiver consolidado.
- Manter o app leve e preservar o redesign aprovado.
- Preservar histórico, letras, metadados, URLs e identidade exata das gravações.
- Manter importação manual MP3/WAV.
- Não prometer remoção de anúncios first-party/in-stream do YouTube.

## Checklist de trabalho
### A fazer
- [ ] Inspecionar e reorganizar o painel esquerdo de Próxima música para remover a necessidade de rolagem, sem clipping.
- [ ] Manter editor/prévia/navegador com áreas úteis preservadas.
- [ ] Implementar download de URL direta de áudio autorizada, separado da importação local.
- [ ] Validar MIME, extensão, tamanho, timeout, hash, deduplicação e mensagens de erro.
- [ ] Garantir que o arquivo baixado seja reproduzido offline pelo fluxo de preparar/tocar a próxima música.
- [ ] Implementar/validar expiração automática enquanto o app estiver aberto, apagando somente o arquivo e preservando ficha/letra/metadados.
- [ ] Revisar proteção do navegador e exceção “Permitir neste site”.
- [ ] Atualizar Ajuda e “Novidades desta versão”.
- [ ] Executar build, testes automatizados e verificações estáticas/visuais possíveis.
- [ ] Gerar uma única compilação consolidada para a rodada manual do usuário.

### Feito e confirmado
- [x] Capturas de tela analisadas e problemas reproduzidos visualmente no relato.
- [x] Run `45874275889` terminou com sucesso.
- [x] Artefatos Windows x64, portátil e instalador foram gerados.
- [x] Testes do Worker, página remota mobile e cobertura da Ajuda passaram no run citado.

### Testes manuais — somente após consolidação
- [ ] Próxima música: verificar que todos os controles necessários ficam visíveis sem rolagem.
- [ ] Próxima música: verificar pesquisa, abas e reprodução no navegador.
- [ ] Pedidos: testar URL direta autorizada e confirmar download/salvamento automático.
- [ ] Pedidos: testar URL apenas do YouTube e confirmar mensagem transparente sem falsa indicação de download.
- [ ] Pedidos: importar arquivo MP3/WAV local como alternativa.
- [ ] Preparar/tocar: desligar internet e confirmar reprodução da cópia local exata.
- [ ] Biblioteca Offline: conferir validade e preservação de letras/metadados após expiração.
- [ ] Proteção: alternar proteção e exceção de domínio sem quebrar sites necessários.

## Próxima ação
Continuar diretamente na branch `feature/offline-library-on-redesign`, inspecionando os XAML e serviços existentes antes de alterar. Não pedir nenhuma ação ao usuário agora. Atualizar este registro e `MEMORIA_CONTINUA.md` após mudanças significativas.


## Atualização após as capturas — 26/11/2026
- Alterado o painel esquerdo da aba Próxima música no commit 7580049ac07b6096e1b86fd602bb8716541df236.
- Removido o ScrollViewer que permitia rolagem vertical; os controles foram reorganizados em grade compacta, com botões de menor altura/padding e fontes menores. As ações de proteção, navegação, atualização, copiar link, fontes e abas foram mantidas.
- A coluna direita que contém o navegador/WebView2 foi preservada sem alteração intencional de largura/estrutura.
- Workflow acionado automaticamente pelo push: 45875746475 (status queued no último acompanhamento). Não instalar/testar até o CI terminar e até a rodada consolidada.
- A alteração visual ainda precisa passar por build e inspeção real; não declarar como validada visualmente antes disso.

## Registro documental
- Arquivo completo de retomada: este arquivo, mantido no repositório.
- Histórico cronológico: MEMORIA_CONTINUA.md atualizado no commit 8a115be77d78aab91945537d425a7b8c6bc65134.
- Cópia local para o usuário: RETOMADA_ATUAL_LYRAVOXEN.md.


## Pacote único de correções solicitado — 26/11/2026

O usuário pediu que as correções abaixo sejam tratadas como um único bloco integrado, sem entregas ou testes fragmentados. Nenhuma dessas tarefas deve ser considerada concluída apenas por estar planejada.

### A. Navegador com proteção contra anúncios e rastreadores
- O usuário já havia solicitado que o navegador interno se comporte de forma semelhante a um navegador com proteção, reduzindo anúncios intrusivos, pop-ups e rastreadores.
- A proteção atual baseada em listas/domínios e a exceção Permitir neste site foram relatadas como não funcionando corretamente; precisam ser investigadas, corrigidas e testadas.
- Melhorar bloqueio de anúncios e rastreadores de terceiros, pop-ups e redirecionamentos indesejados, mantendo exceções por site e controles claros de ligar/desligar proteção.
- Preservar compatibilidade com YouTube, pesquisa, login, abas, sites de letras, downloads autorizados e reprodução WebView2.
- Limite técnico a comunicar: não prometer bloqueio de anúncios first-party/in-stream servidos pelo próprio YouTube ou pela mesma origem do conteúdo. Não quebrar player ou APIs essenciais para tentar removê-los.
- Incluir explicação honesta na Ajuda sobre o que a proteção bloqueia, o que pode permanecer e como permitir um site.

### B. Painel da aba Próxima música
- Corrigir a rolagem vertical do painel lateral esquerdo, deixando os comandos principais acessíveis sem rolar.
- Usar botões minimalistas/compactos e reorganização em grade, sem cortar comandos.
- Preservar integralmente a área útil do navegador/WebView2, pesquisa, abas e reprodução.
- A primeira alteração visual foi enviada no commit 7580049ac07b6096e1b86fd602bb8716541df236; ainda depende do CI e inspeção visual real.

### C. Sistema único de Biblioteca Offline — Pedidos e Biblioteca
O usuário confirmou que o mesmo problema do botão Baixar offline dos Pedidos também ocorre na aba Biblioteca Offline: a ação abre seletor/pesquisador de arquivos ou pastas e não salva a mídia na Biblioteca como esperado. Corrigir ambos os pontos com um único serviço central.

**Pedidos**
- Baixar URL direta de arquivo de áudio autorizada e salvar automaticamente no armazenamento interno do Lyravoxen, sem pedir pasta de destino nem abrir seletor de arquivo de origem.
- Associar o arquivo ao pedido e à identidade exata da gravação/versão.
- Se a URL for apenas do YouTube, preservar URL/ID e informar que não há arquivo offline autorizado disponível; não extrair áudio nem contornar restrições da plataforma.

**Biblioteca Offline**
- A ação para adicionar/preparar mídia offline deve salvar a cópia no armazenamento interno do Lyravoxen e registrar o item na coleção/pasta selecionada.
- Não abrir seletor de pasta de destino no fluxo normal de download/salvamento.
- Manter ação separada Importar MP3/WAV para selecionar um arquivo já existente no computador; após selecionar, copiar para o armazenamento interno e criar/atualizar o registro da Biblioteca.

**Serviço comum e critérios técnicos**
- Um diretório interno padrão, criação automática de pastas, registro persistente, progresso/estado e mensagens claras.
- Validar protocolo, MIME, extensão, tamanho máximo e timeout para URL direta autorizada.
- Calcular hash e deduplicar sem confundir gravações ou tons diferentes; nunca casar somente pelo título.
- Unificar metadados e prazo de expiração (5, 7, 14 ou 30 dias), limpeza automática enquanto o app estiver aberto e após reinicialização.
- Expiração remove somente o arquivo local; preservar ficha, letra, histórico, metadados e link.
- Garantir que preparar/tocar use a cópia local exata, inclusive após reiniciar o app e sem internet.

### D. Documentação, testes e entrega
- Atualizar Ajuda e Novidades da versão para navegador/proteção, Pedidos, Biblioteca, importação, download e limites.
- Atualizar MEMORIA_CONTINUA.md e RETOMADA_ATUAL.md cronologicamente.
- Rodar build e testes automatizados; fazer inspeção estática e visual possível; não afirmar teste manual que não ocorreu.
- Só depois entregar uma única compilação consolidada e um checklist de testes para o usuário.
- Não solicitar instalação de builds intermediários e não fazer merge na main sem aprovação explícita.

### Checklist integrado de aceitação
- [ ] Próxima música: comandos principais visíveis sem rolagem e sem clipping; navegador mantém tamanho útil.
- [ ] Navegador: proteção liga/desliga; bloqueio de terceiros/pop-ups funciona; Permitir neste site cria exceção efetiva; YouTube/pesquisa/login/abas/letras/reprodução continuam funcionais.
- [ ] Navegador: Ajuda explica limites de anúncios first-party/in-stream do YouTube.
- [ ] Pedidos: URL direta autorizada baixa e salva automaticamente na pasta interna.
- [ ] Pedidos: URL somente YouTube não é falsamente apresentada como arquivo baixado; URL/ID permanecem vinculados.
- [ ] Biblioteca: adicionar mídia registra e salva automaticamente na coleção selecionada, sem pedir pasta de destino.
- [ ] Biblioteca: importar MP3/WAV seleciona origem, copia para armazenamento interno e cadastra o item.
- [ ] Pedidos e Biblioteca usam o mesmo serviço, hash/deduplicação, metadados e validade.
- [ ] Reprodução da cópia correta funciona offline e após reiniciar o app.
- [ ] Expiração remove apenas mídia e preserva registros/letras/metadados.
- [ ] Ajuda, Novidades, testes CI e build consolidado aprovados.


## 26/11/2026 — Correção do erro de cobertura da Ajuda
- Erro reportado pelo usuário: `tests/help_coverage.py` falhou porque o botão `Permitir site` não era citado literalmente na Ajuda.
- Atualizado `MainWindow.Help.cs` para documentar o rótulo exato `Permitir site`, seu alias `Permitir neste site` e os passos para criar exceção persistente e recarregar a página.
- A validação local do conteúdo atual encontrou zero rótulos ausentes na comparação textual executada nesta sessão. O resultado oficial será o workflow do GitHub após o commit.
- Commit da correção: `4e70f3846631deac46ba686082e4f183516ccbbb`.
- O build completo ainda não foi declarado pronto. O próximo passo é conferir o workflow acionado por esse commit e corrigir qualquer falha antes de solicitar teste manual.

## Observação funcional importante sobre Biblioteca Offline
- Inspeção do código confirmou que `ImportAudio_Click` abre `OpenFileDialog` para escolher um arquivo-fonte MP3/WAV e depois copia o arquivo para `%LOCALAPPDATA%/Lyravoxen/Media`, cria `LibraryVideo` offline e chama `SaveLibrary`. Esse seletor é de origem do arquivo, não de destino. O fluxo de URL direta ainda não existe na Biblioteca; `AddCurrentVideoToFolder_Click` guarda apenas link/ID e letra, sem baixar mídia.
- Em Pedidos, `ImportOfflineForRequest` também abre seletor de arquivo-fonte, copia para a pasta interna e associa ao pedido; não é download da URL do pedido.
- Para atender ao pedido do usuário, o pacote deve tornar explícito o fluxo: URL direta autorizada = download automático; arquivo local = selecionar origem e copiar automaticamente para a biblioteca interna; link YouTube isolado = não oferecer falso download nem extrair áudio.


## Continuação executada — 26/11/2026

### Estado atual da implementação
- Branch preservada: `feature/offline-library-on-redesign`. A `main` não foi alterada.
- Criado o projeto compartilhado `src/Lyravoxen.Offline`, sem dependência de WPF, para permitir testes funcionais multiplataforma.
- Implementado `OfflineMediaService` para download de URLs diretas autorizadas e importação local MP3/WAV:
  - limite padrão de 50 MB, timeout de 45 segundos, cancelamento e remoção de arquivos temporários após falha;
  - validação HTTP/HTTPS, MIME quando informado, extensão e assinatura MP3/WAV;
  - validação DNS/IP público, bloqueio de redes privadas e redirecionamentos limitados; conexão usa o IP público validado para reduzir risco de DNS rebinding;
  - hash SHA-256 e deduplicação por conteúdo;
  - links do YouTube são recusados como fonte de arquivo de áudio.
- Aba Pedidos: URLs diretas de áudio são baixadas; links somente do YouTube explicam a limitação e oferecem seleção de arquivo autorizado local. A cópia é associada ao pedido e à Biblioteca.
- Biblioteca: aceita URL direta ou importação local. Adicionado editor de título, intérprete e letra na própria aba Biblioteca, sem depender da aba Cadastrar letras. A sincronização existente é preservada.
- Pastas distintas podem referenciar a mesma mídia deduplicada sem duplicar os bytes.
- Expiração: limpeza a cada minuto e na inicialização. O arquivo só é excluído quando não há outra referência ativa na Biblioteca ou em Pedidos; fichas, letras e metadados permanecem.
- Aba Próxima música: painel de comandos reorganizado em layout compacto, mantendo a coluna do navegador/WebView2.
- Proteção: política de bloqueio extraída para componente puro e testável. Os domínios protegidos do YouTube/Googlevideo permanecem permitidos; domínios de anúncios podem ser bloqueados e exceções do usuário são respeitadas.

### Testes automatizados adicionados
Projeto: `tests/Lyravoxen.OfflineTests`.
- Importação local WAV e MP3.
- Download direto WAV e MP3 com respostas HTTP controladas.
- Hash/deduplicação e preservação dos bytes.
- Recusa de YouTube como fonte de áudio.
- Bloqueio de DNS privado e redirecionamento para rede privada.
- Redirecionamento HTTPS permitido, limite de tamanho e recusa de HTML.
- Política do navegador: preservar YouTube/googlevideo, bloquear domínio de anúncio, respeitar exceções e evitar correspondência parcial.
- Expiração: preservar arquivo ainda referenciado por outra pasta ou pedido e permitir excluir quando não houver referências.

### Evidência de CI
- Run `45888141360`, commit `1c888e98`: job `cloud-worker-tests` concluído com sucesso. Serviço offline, cobertura da Ajuda, C# static checks, testes do Worker e página remota mobile aprovados. O teste offline nessa revisão cobria os fluxos de WAV, URL direta, redirecionamento, limite de tamanho e política do navegador; ainda não incluía os testes de expiração e MP3 acrescentados depois.
- Run `45887612874`, commit `5003f831`: jobs `cloud-worker-tests` e `build` concluídos com sucesso. A compilação Windows confirmou a integração inicial do serviço com Pedidos/Biblioteca e o timer de expiração, mas ocorreu antes do editor da Biblioteca e da compactação final da aba Próxima música. Não é a compilação final desta rodada.
- Run `45888415155`, commit `2b9f8418`: no último acompanhamento, o teste offline, cobertura da Ajuda, C# static checks e testes do Worker estavam aprovados; Playwright / teste mobile ainda estava em execução. Esse run contém os testes adicionais de MP3 e política de expiração. Aguardar conclusão antes de liberar build.

### Limitações que ainda impedem declarar pronto
- Os testes de mídia usam arquivos sintéticos e handlers HTTP controlados; ainda não comprovam reprodução real de MP3/WAV pelo MediaElement do Windows nem download contra diferentes servidores externos.
- A política do bloqueador está testada, mas ainda falta teste de integração com WebView2 real confirmando que a pesquisa, login e reprodução do YouTube permanecem funcionais e que a exceção “Permitir site” funciona em navegação real.
- A nova disposição da aba Próxima música foi validada por XAML/build, mas ainda requer inspeção visual em uma janela real e em diferentes resoluções.
- A execução Windows `45887612874` é evidência de build intermediário, não da versão final com o editor e o layout compactado.
- O workflow contém cron diário `17 10 * * *`, mas GitHub Actions executa `schedule` somente a partir da branch padrão. Como não foi autorizado merge na `main`, a rotina diária ainda não está ativa para esta branch. Os testes continuam sendo executados a cada push e podem ser acionados manualmente pela interface do GitHub.

### Próximas ações
1. Aguardar o término do run `45888415155` e conferir os logs completos dos 21 testes funcionais.
2. Fazer uma compilação Windows consolidada com todas as alterações atuais; corrigir qualquer falha de build e repetir.
3. Atualizar este registro com o SHA final, os jobs e os artefatos reais.
4. Só então preparar a única compilação para a rodada de testes do usuário, com checklist específico de navegação real, reprodução offline, expiração e editor da Biblioteca.
5. Não fazer merge na `main` sem aprovação explícita.


## Resultado da compilação consolidada — 26/11/2026

<badge color="success">CI e build Windows aprovados</badge>

- Workflow: [45889091270](https://github.com/jabesguitar/Lyravoxen/actions/runs/45889091270)
- SHA validado: `76e1f018a9e84c65505de05064eb50534ae0cd86`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - 21 verificações do serviço offline e políticas de navegador/expiração.
  - Cobertura da Ajuda: sucesso.
  - C# static checks: sucesso.
  - Cloudflare Worker tests: sucesso.
  - Playwright Chromium / mobile remote page: sucesso.
- Job `build`: sucesso.
  - Geração de ícone e splash compacta: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.

### Artefatos desta compilação
- `Lyravoxen-Windows-x64` — artifact ID `15150797165` — 76,260,202 bytes.
- `Lyravoxen-Windows-x64-portable` — artifact ID `15150578815` — 76,294,856 bytes.
- `Lyravoxen-Installer-Windows-x64` — artifact ID `15150673234` — 53,834,443 bytes.
- Os três artefatos constavam como disponíveis e não expirados no momento da consulta.

### Correção realizada durante a compilação
- A primeira tentativa completa falhou por uma referência ambígua a `HorizontalAlignment.Right` no editor da Biblioteca.
- Corrigido para `System.Windows.HorizontalAlignment.Right`.
- A compilação seguinte no SHA acima passou por completo.

### Situação para a rodada única do usuário
- A compilação Windows consolidada foi gerada e está disponível nos artefatos do workflow.
- Ainda não há comprovação manual em um PC com WebView2 real de anúncios, pesquisa/login/reprodução do YouTube; a política de bloqueio foi testada isoladamente.
- A reprodução real de MP3/WAV offline e o comportamento visual do editor/layout em diferentes resoluções também dependem da validação manual no computador anfitrião.
- Não fazer merge na `main` sem aprovação explícita.
- Rotina diária: cron está no workflow da branch, mas GitHub Actions só dispara `schedule` na branch padrão. Como a `main` permanece intacta, os testes diários ainda não estão ativos; o CI roda a cada push e pode ser iniciado manualmente pela interface do GitHub.

### Próxima ação
Entregar ao usuário esta única compilação consolidada e o checklist manual final. Se os testes manuais forem aprovados, registrar a aprovação antes de considerar merge na `main`.


## Revisão corretiva — Pedidos, Biblioteca Offline e anúncios — 26/11/2026

### Solicitações do usuário
- A aba Próxima música ainda mostra anúncios no início do vídeo.
- Pedidos e Biblioteca Offline não estavam concluindo o fluxo esperado para links do YouTube; o usuário relatou dificuldade em baixar, salvar em pasta e renomear/organizar músicas.
- Exibir IP/endereço de origem de quem usa o formulário público, com bloqueio e desbloqueio para reduzir spam/sabotagem.
- Fazer uma única revisão ampla e uma única rodada manual, sem builds vazios intermediários.

### Limites técnicos e de plataforma
- Não implementar extração de áudio do YouTube nem contorno de anúncios/restrições. O player incorporado pode exibir anúncios servidos pela própria origem do YouTube; o bloqueador de domínios de terceiros não garante removê-los.
- Para offline, são suportados: arquivo MP3/WAV local autorizado ou URL HTTPS direta que entregue o arquivo de áudio. URL de página do YouTube não é URL direta de áudio.
- MAC não é disponibilizado ao servidor quando alguém acessa um site pela internet; o Worker pode registrar o IP público fornecido pelo Cloudflare. Não exibir campo MAC fictício.
- O código do Worker foi atualizado na branch, mas o serviço em produção `lyravoxen-pedidos.ferkt319.workers.dev` não foi implantado por esta sessão: não há conexão autorizada com a conta Cloudflare. Para ativar IP/rate-limit na URL pública, é necessária uma implantação única do novo código pelo painel Cloudflare. Pedidos antigos, criados antes do Worker novo, podem não ter IP.

### Implementações desta rodada
- Worker Cloudflare:
  - captura e armazena IP público `CF-Connecting-IP` junto com texto e horário do pedido;
  - bloqueio/desbloqueio autenticado por IP via `/bloqueios`;
  - limite básico de 5 envios por IP por janela de 10 minutos, além da fila global de até 150 pedidos;
  - aviso no formulário sobre registro do IP público e horário para prevenção de abuso, esclarecendo que MAC não é coletado;
  - documentação orienta a atualizar o Worker existente sem recriar KV ou trocar `ADMIN_TOKEN`.
  - KV é eventualmente consistente; o rate-limit no Worker é proteção básica. Para ataques concorrentes/coordenados, complementar com regra Rate Limiting no Cloudflare.
- App Pedidos:
  - sincroniza a lista de bloqueios remotos quando abre a janela;
  - exibe IP público por pedido quando o Worker atualizado o fornece;
  - oferece Bloquear este IP / Desbloquear este IP e Atualizar bloqueios da internet;
  - se IP vier vazio, avisa que o Worker público pode estar desatualizado;
  - normaliza e valida endereços IP ao cadastrar bloqueios.
- Biblioteca:
  - importar URL direta que falhe agora oferece selecionar arquivo MP3/WAV local;
  - colar URL do YouTube apresenta explicação e abre diretamente o seletor de arquivo autorizado;
  - botão de importação continua aceitando múltiplos MP3/WAV;
  - seleção de destino ao salvar pedido offline; não fixa tudo numa pasta genérica;
  - novos comandos Mover para outra pasta e Remover desta pasta (o segundo preserva o arquivo de áudio);
  - seletor permite criar nova pasta;
  - busca inclui título, intérprete e compositor;
  - renomear pasta valida nomes duplicados;
  - adicionar vídeo atual à pasta usa identidade exata (ID do vídeo ou caminho da mídia offline), preservando metadados;
  - SaveLibrary agora retorna sucesso/falha; operações críticas não exibem sucesso falso quando a persistência falha.
- Ajuda atualizada para os novos botões e comportamento.

### Validação final
- Workflow: [45894030900](https://github.com/jabesguitar/Lyravoxen/actions/runs/45894030900)
- SHA: `4f39f92562b6eb470b16be1d3513e9d1ecbe8543`
- Branch: `feature/offline-library-on-redesign`
- Job `cloud-worker-tests`: sucesso.
  - Serviço offline: 21 verificações aprovadas (MP3/WAV, URL direta, deduplicação, SSRF, redirecionamentos, limite de tamanho, política de anúncios e expiração compartilhada).
  - Worker: 44 testes aprovados, incluindo bloqueio/desbloqueio por IP e rate-limit.
  - Ajuda: cobertura aprovada.
  - C# static checks: aprovados.
  - Formulário público em Chromium: 25 testes aprovados.
  - Página mobile em Chromium: 112 testes aprovados.
- Job `build`: sucesso.
  - Ícone e splash: sucesso.
  - Restore e `dotnet publish` Windows x64: sucesso.
  - Cue timeline tests: sucesso.
  - ZIP portátil: sucesso.
  - Instalador Inno Setup: sucesso.
  - Upload dos três artefatos: sucesso.
- Artefatos gerados:
  - `Lyravoxen-Windows-x64`: ID `15153435863`, 76,266,769 bytes.
  - `Lyravoxen-Windows-x64-portable`: ID `15153276186`, 76,302,238 bytes.
  - `Lyravoxen-Installer-Windows-x64`: ID `15153231348`, 53,845,674 bytes.
  - Expiração prevista dos artefatos: 24/02/2027.
- `main` continua no SHA `2051135a6e4915c13dda5730fa0248ed7dbcfe2f`; não foi alterada nem recebeu merge.

### Limitações ainda abertas
- Anúncios in-stream/first-party do YouTube continuam fora do alcance garantido do bloqueador; não prometer reprodução YouTube sem anúncios.
- O Worker em produção precisa ser atualizado manualmente uma única vez para IP e bloqueios remotos aparecerem nos pedidos futuros.
- Testes automatizados de mídia usam fixtures sintéticas e servidor HTTP controlado; reprodução real via WebView2/MediaElement e navegação real no PC anfitrião ainda exigem validação do usuário.
- O pacote está pronto para uma única rodada manual no PC anfitrião; não pedir testes de builds intermediários.

### Próxima ação
1. Usuário instala a versão consolidada.
2. Uma única implantação do Worker atualizado no Cloudflare, mantendo namespace KV e segredo existentes.
3. Uma única rodada manual com MP3/WAV real, salvar/mover/renomear, pedido público com IP e navegação YouTube.
4. Registrar os resultados antes de qualquer merge na `main`.

## 27/11/2026 — Esclarecimento do requisito de mídia offline do YouTube

### Solicitação e justificativa do usuário
- O usuário esclareceu que insiste no YouTube porque muitas gravações necessárias não existem em outras fontes, especialmente versões específicas de playback.
- Exemplos citados: mesma música em meio tom abaixo, dois tons abaixo e outras tonalidades/arranjos preparados por terceiros.
- Para o evento, não basta encontrar outra gravação com o mesmo título: precisa ser exatamente a versão selecionada, preservando tom, arranjo e identidade do vídeo.
- Objetivo operacional: ter previamente uma cópia local utilizável quando faltar energia ou internet, sem comprometer a continuidade do evento.
- Restrição de custo reafirmada: solução gratuita, sem APIs ou assinaturas pagas adicionais.

### Decisão de produto / interpretação
- Preservar o vínculo exato entre item de repertório e gravação: YouTube videoId + URL original + metadados/descrição da versão (ex.: -1 semitom, -2 tons, playback).
- Não substituir automaticamente uma gravação por outra que tenha o mesmo título.
- A interface deve distinguir claramente: disponível online, arquivo offline pronto, fonte não baixável diretamente e requer arquivo autorizado.
- O requisito do usuário é continuidade offline da versão exata, não apenas cache de letras ou link.

### Limite e alternativas técnicas
- Sites de terceiros que aceitam URL do YouTube normalmente extraem ou convertem o fluxo de mídia; o fato de existirem não significa que o Lyravoxen possa reproduzir essa técnica ou que ela seja estável/segura/permitida.
- Não implementar extração de streams protegidos, DRM bypass, nem contorno de anúncios/restrições do YouTube.
- YouTube Premium/offline mantém a mídia no ambiente oficial do YouTube e não disponibiliza um MP3/WAV comum para o Lyravoxen acessar como arquivo local.
- Caminhos gratuitos viáveis para manter a gravação exata: arquivo fornecido pelo próprio criador/intérprete/autor do playback; link direto de download disponibilizado oficialmente; arquivo MP3/WAV que o usuário já possua e tenha direito de usar; ou gravação/produção própria autorizada.
- O fluxo do Lyravoxen deve permitir anexar/importar essa cópia ao item já cadastrado no YouTube, preservando o videoId como referência da versão e o hash do arquivo como identidade da cópia local.
- Se não houver arquivo autorizado, manter o link exato para reprodução online pelo player oficial e informar que ainda não há cópia offline; não declarar “Baixado” quando só foi salvo o link.

### Pendência
- Avaliar UX para associar um arquivo offline ao item do YouTube sem perder título, intérprete, autor, tom e videoId; permitir que o operador escolha explicitamente qual gravação/arquivo corresponde à versão selecionada.
- Testar reprodução offline real dessa cópia local, com internet desligada, antes de declarar o fluxo aprovado.

## 27/11/2026 — Confirmação do fluxo desejado para “Baixar offline” e retenção

### Requisito confirmado pelo usuário
- Na janela **Pedidos**, ao receber um pedido com link do YouTube, deve existir a ação **Baixar offline** no próprio pedido, sem exigir que o operador procure manualmente um arquivo no computador.
- O usuário espera que o download seja iniciado a partir do link recebido e que o arquivo fique guardado localmente para uso no evento, evitando depender de internet/energia.
- O mesmo princípio de armazenamento temporário deve atender à Biblioteca Offline.
- O usuário quer escolher o prazo de retenção dentre as opções já planejadas: **5, 7, 14 ou 30 dias**.
- Ao vencer o prazo, apagar somente o arquivo de mídia local para evitar acúmulo e peso excessivo no computador; preservar pedido, título, link, letra, metadados e histórico.
- A seleção do prazo deve ser clara no fluxo de download/armazenamento; o vencimento precisa ser visível e a limpeza automática deve ser confiável.
- O usuário reforçou que a experiência desejada é pelo link do pedido, sem seletor de arquivos como caminho principal.

### Limite técnico e comportamento honesto
- Um link de página/vídeo do YouTube não é uma URL direta de arquivo MP3/WAV. O serviço de mídia atual rejeita explicitamente hosts YouTube.
- Não implementar extração do fluxo de áudio protegido do YouTube, contorno de DRM ou mecanismos de proteção/anúncios.
- Portanto, não apresentar a ação como se qualquer URL YouTube fosse baixável pelo Lyravoxen. A ação deve verificar a origem e explicar claramente quando o link não fornece um arquivo direto.
- Caminhos compatíveis para obter a cópia exata: URL direta de arquivo publicada para download pelo criador/fornecedor; arquivo fornecido pelo criador; ou arquivo MP3/WAV local que o usuário possua e esteja autorizado a usar. Associar essa cópia ao pedido original, preservando o YouTube videoId e os metadados da versão.
- Não substituir uma versão específica (ex.: meio tom abaixo ou dois tons abaixo) por outra gravação de mesmo título.

### Implementação necessária
- Rever a experiência do botão **Baixar offline**: iniciar tentativa de download direto apenas para fontes que realmente entreguem arquivo de áudio permitido; se a URL for página YouTube, não abrir imediatamente seletor como se o download estivesse acontecendo. Exibir estado/motivo e oferecer associação de cópia autorizada como alternativa explícita.
- Unificar o serviço de armazenamento entre Pedidos e Biblioteca Offline.
- Registrar por item: caminho local, hash, formato, tamanho, data de armazenamento e data de expiração.
- Aplicar retenção configurável 5/7/14/30 dias; executar limpeza automática na inicialização e periodicamente enquanto o app estiver aberto.
- A limpeza deve remover somente mídia expirada, não registros, letras, metadados, links, histórico ou pastas do usuário.
- Garantir atomicidade do download (arquivo temporário e promoção após validação), validação real MP3/WAV, deduplicação por hash e associação à gravação exata.
- Permitir que o usuário identifique claramente se a música está apenas online ou se a cópia offline está pronta.

### Estado
- [x] Usuário confirmou o fluxo pretendido e as opções de retenção 5, 7, 14 e 30 dias.
- [ ] Ajustar UI e lógica de Baixar offline nos Pedidos para distinguir link YouTube de URL direta de arquivo.
- [ ] Unificar cache temporário de Pedidos e Biblioteca Offline.
- [ ] Implementar/testar retenção e limpeza automática, preservando metadados.
- [ ] Validar reprodução offline da cópia exata com internet desligada.


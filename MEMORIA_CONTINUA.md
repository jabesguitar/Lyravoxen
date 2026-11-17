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

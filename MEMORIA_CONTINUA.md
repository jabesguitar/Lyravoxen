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

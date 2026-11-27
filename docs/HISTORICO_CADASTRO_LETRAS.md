# Histórico de alterações — cadastro de letras Lyravoxen

Este arquivo registra decisões, código, testes e limitações por etapa. Não substitui os commits; complementa-os para retomada.

## 27/11/2026 — Especificação e pesquisa
- Requisitos confirmados: sincronização automática e manual; identificar estrofes/coro; preservar repetições na ordem cantada; salvamento permanente; aba equivalente no controle remoto; rapidez ao vivo; preservar layout moderno e funções úteis; remover apenas botões sem função.
- Pesquisa: Librosa recurrence/novelty para recorrência e fronteiras; WhisperX com modelo português para forced alignment; sherpa-onnx (modelo português de forced alignment não confirmado); Essentia (AGPLv3/comercial, revisar licença); keytool como referência estrutural; hookfinder não reutilizável devido à licença source-available.
- Criada branch isolada `feature/lyrics-structure-fast-sync` a partir de `feature/offline-library-on-redesign`.

## 27/11/2026 — Protótipo de estrutura textual
- Novo arquivo: `src/Lyravoxen.Desktop/Models/LyricsStructureAnalyzer.cs`.
- Parser de cabeçalhos de estrofe/verso, coro/refrão, ponte, intro e final; marcador de repetição `2x`/`x2`.
- Blocos textualmente idênticos compartilham identidade; execução preserva ocorrências separadas e ordem.
- Partes sem evidência permanecem incertas e exigem revisão.
- Testes adicionados a `tests/Lyravoxen.TimelineTests/Program.cs`; arquivo incluído no projeto de testes.
- Workflow atualizado para disparar na branch.
- **Não validado:** .NET SDK ausente no ambiente desta sessão; nenhuma execução Actions foi observada. Não declarar compilação ou testes aprovados.

## Próxima etapa
Executar build/testes no GitHub; corrigir falhas; então prototipar análise de áudio preparatória para encontrar seções musicais recorrentes e alinhar a letra em português. Depois integrar somente na aba Cadastro de letras e nos serviços mínimos, mantendo XAML/tema/layout moderno e recursos existentes fora do escopo.


## 27/11/2026 — Modelo e arquivo permanente
- `LibraryVideo` recebeu `Sections` e `ExecutionOrder`, com compatibilidade aditiva para registros antigos via inicializadores padrão.
- `PermanentLyricsArchive` agora clona e persiste esses dados; também permite salvar quando há ocorrências estruturais mesmo sem cues temporais.
- Teste de persistência acrescentado: salvar registro com estrofes/coro e reabrir, verificando que as ocorrências de coro continuam separadas.
- **CI pendente:** execuções Actions foram canceladas por concorrência de commits. O teste foi escrito, mas não executado/aprovado ainda.
- Nenhuma alteração visual foi feita; a interface moderna permanece intacta.

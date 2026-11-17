# MEMORIA_CONTINUA — Lyravoxen

## Estado atual — 2026-11-17

### Objetivo
Evoluir o Lyravoxen sem perder as funções musicais existentes: controle independente do Holyrics, módulo bíblico, captação de voz sob comando explícito, traduções bíblicas com licenças respeitadas e fundos profissionais para projeção.

### Decisões do usuário
- Tudo deve permanecer gratuito, leve e adequado a Windows 10/11 e CPUs mais antigas.
- O Holyrics deve poder ser ativado/desativado dentro do Lyravoxen.
- Ao desativar a integração, o Holyrics deve continuar aberto; a apresentação rápida deve ser fechada e novos envios interrompidos.
- A captação do microfone para identificação bíblica deve ter seu próprio controle liga/desliga.
- O usuário quer as mesmas versões bíblicas disponíveis no Holyrics, condicionadas à disponibilidade e às licenças de redistribuição/uso.
- Fundos devem ser modernos, elegantes e simples, com contraste e área central limpa para letras.
- Referências visuais fornecidas: azul-marinho, iluminação azul e dourada, oliveira, rocha/mesa texturizada, coroa de espinhos e tecido vinho.

### Trabalho realizado nesta etapa
- Criada branch `feature/bible-holyrics-mic-backgrounds`, separada da `main`, partindo do head da PR #2.
- Adicionado interruptor persistente "Holyrics ativo" na barra principal.
- Desativar invalida envios pendentes, fecha a apresentação rápida pela API quando configurada e não encerra o processo Holyrics.
- Reativar volta a permitir envios e tenta republicar a frase local atual.
- Teste de conexão e diagnóstico respeitam o estado do interruptor.
- README atualizado para explicitar que o microfone e o reconhecimento bíblico ainda não estão implementados.
- Criado `docs/BIBLE_VOICE_AND_VISUAL_DESIGN.md` com requisitos, decisões de licenciamento e critérios de aceite.
- Gerados 4 conceitos visuais em 16:9 e adaptações em 4:3; pacote entregue separadamente ao usuário.

### Estado do GitHub
- Repositório: https://github.com/jabesguitar/Lyravoxen
- PR #2 permanece aberta como rascunho.
- PR #8: https://github.com/jabesguitar/Lyravoxen/pull/8 — rascunho, não mesclada.
- Head atual da branch: `266ba7995275d7b2ebd188f527cae779bf29e87b`.
- GitHub reportou `mergeable=false` e não retornou status checks/workflow runs no momento da consulta. Não declarar compilação ou testes aprovados.
- A branch principal não foi alterada por esta etapa.

### Pendências obrigatórias
1. Resolver o estado de merge/conflitos da PR #8 e revalidar a base antes de mesclar.
2. Executar build e testes de CI; não há confirmação de execução/aprovação no registro atual.
3. Testar no Windows a desativação do Holyrics e confirmar que a projeção rápida fecha sem encerrar o Holyrics.
4. Implementar captura real de microfone, seleção de dispositivo, indicador de nível e interrupção imediata ao desligar.
5. Integrar motor local de reconhecimento de voz com CPU sem AVX2 testada; nunca projetar referência ambígua sem confirmação.
6. Inventariar as traduções bíblicas do Holyrics e verificar licenças. Não copiar arquivos protegidos nem redistribuir textos sem autorização.
7. Implementar biblioteca bíblica offline, busca, favoritos, histórico, comparação e projeção.
8. Integrar os fundos gerados ao aplicativo como opções selecionáveis; o pacote de imagens entregue ainda não está incorporado ao repositório/aplicativo.
9. Testar saída 16:9 e 4:3 em monitor/projetor físico.

### Próxima ação
Auditar a diferença entre a branch de trabalho e a `main`, resolver o bloqueio de merge e obter uma compilação verificável antes de avançar para a implementação do motor de microfone e da biblioteca bíblica.

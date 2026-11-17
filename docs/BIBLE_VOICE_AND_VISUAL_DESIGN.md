# Bíblia, captação de voz e identidade visual

## Objetivo aprovado
Adicionar ao Lyravoxen um módulo bíblico independente, reconhecimento de referências por voz com controle explícito do microfone e fundos próprios para projeção. Preservar a aplicação musical e a projeção local já existentes.

## Controle do Holyrics — implementado nesta etapa
- Interruptor persistente "Holyrics ativo" na barra principal.
- Desativar interrompe novos envios e solicita o fechamento da apresentação rápida via `CloseCurrentQuickPresentation`.
- O processo Holyrics não é encerrado; permanece aberto.
- Reativar permite o envio novamente e republica a frase local atual, se houver.
- O estado é salvo em `preferences.json`.
- O controle depende de API local e permissões compatíveis já descritas no README.

## Microfone e reconhecimento bíblico — ainda não implementados
O controle visual não deve ser apresentado como funcional antes de existir captura real, seleção de dispositivo, cancelamento e motor de reconhecimento. A barra principal informa que o microfone bíblico está na etapa seguinte; não há captura de áudio ativada silenciosamente.

### Requisitos de implementação
1. Interruptor independente para escuta bíblica, inicialmente desligado.
2. Seleção explícita de microfone interno, USB ou interface/mesa reconhecida pelo Windows.
3. Indicador de nível e teste de entrada.
4. Captura interrompida imediatamente ao desligar, fechar o app ou perder o dispositivo.
5. Pipeline local de reconhecimento, sem serviço pago e sem envio de áudio para nuvem.
6. Identificação de livro/capítulo/versículo com pontuação de confiança e confirmação do operador em casos ambíguos.
7. A captação do microfone não pode ser confundida com o áudio do player musical.
8. Compatibilidade com CPU sem AVX2 deve ser medida antes de declarar suporte.

## Biblioteca bíblica e traduções
- Criar catálogo local de traduções e livros/capítulos/versículos.
- Busca por referência e texto, favoritos, histórico, comparação de versões e projeção.
- Importação de pacotes bíblicos somente de fontes com licença compatível.
- A lista de traduções instaladas no Holyrics deve ser descoberta/confirmada na instalação do usuário; não copiar arquivos protegidos do Holyrics nem prometer equivalência automática.
- Cada tradução precisa guardar nome, abreviação, origem, licença, atribuição exigida e possibilidade de uso offline.
- Traduções com restrição de redistribuição podem ser acessadas por integração autorizada ou importação pelo próprio usuário, sem serem empacotadas pelo Lyravoxen.

## Direção de arte aprovada
Referências fornecidas pelo usuário: azul-marinho profundo, iluminação azul fria, detalhes dourados, ramos de oliveira, rocha/mesa texturizada e, em variações bíblicas, coroa de espinhos e tecido vinho.

Regras de composição:
- Centro amplo, escuro e uniforme para letras.
- Elementos decorativos concentrados nas bordas e cantos.
- Contraste alto e textura discreta; evitar excesso de detalhes atrás do texto.
- Criar versões 16:9 e adaptações 4:3 com área segura.
- Fundos devem ser selecionáveis sem alterar o texto ou a sincronização.

## Critérios de aceite
- Desativar Holyrics fecha apenas a apresentação rápida; não encerra o processo Holyrics.
- Nenhuma frase nova pode ser enviada enquanto a integração estiver desligada.
- O estado do interruptor sobrevive ao reinício.
- Falha de comunicação com Holyrics não interrompe player ou projeção local.
- Microfone só pode ser capturado após ativação explícita e deve parar ao desativar.
- Reconhecimento nunca projeta automaticamente referência ambígua sem política de confiança aprovada.
- Licenças e atribuições das traduções ficam registradas.
- Build, testes automatizados e teste manual em Windows são necessários antes de chamar a entrega de pronta.

## Estado
- [x] Branch isolada criada a partir do head da PR #2, preservando a branch principal.
- [x] Interruptor persistente de integração Holyrics adicionado.
- [x] Ao desligar, tenta fechar a apresentação rápida e mantém Holyrics aberto.
- [ ] Build e testes de CI desta branch.
- [ ] Implementar captura real de microfone e reconhecimento local.
- [ ] Inventariar traduções bíblicas e licenças.
- [ ] Implementar biblioteca bíblica e projeção de versículos.
- [ ] Integrar os fundos gráficos finais como assets do aplicativo.
- [ ] Validar 16:9 e 4:3 em monitor/projetor físico.

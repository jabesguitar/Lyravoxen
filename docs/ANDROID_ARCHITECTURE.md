# Lyravoxen Android — arquitetura e plano de paridade

## Decisão arquitetural
O aplicativo Android será um projeto nativo Kotlin + Jetpack Compose em `/android`, independente do desktop WPF em `/src/Lyravoxen.Desktop`. Não compartilhar assemblies WPF, APIs Windows, WinForms, WebView2 ou serviços dependentes de Windows com o Android.

A identidade visual segue o produto: azul-noite, ciano neon, azul e violeta, cantos arredondados, marca Lyravoxen e temas claro/escuro. A preferência de tema é persistida localmente.

## Camadas planejadas
- **presentation**: telas Compose (Ao vivo, Cadastro de letras, Próxima música, Bíblia, Ajuda), componentes de prévia e navegação.
- **domain**: modelos e casos de uso independentes de plataforma (música, metadados, estrofes, marcações, fila, referência bíblica e projeção).
- **data**: repositórios locais, preferências, biblioteca/histórico e adaptadores de serviços externos.
- **player**: player oficial incorporado do YouTube e reprodução de mídia permitida, sem extrair/baixar áudio do YouTube.
- **sync**: sincronização por marcação manual, tempos LRC/SRT e reconhecimento local quando tecnicamente viável no Android.
- **projection**: prévia e saída para tela externa compatível; integração opcional com Holyrics pela rede.
- **remote**: cliente HTTP opcional para o servidor Windows existente, com PIN.

## Estado implementado nesta fundação
- Projeto Android Gradle independente, Kotlin 2.0.21, AGP 8.7.3, JDK 17 e compileSdk 35.
- Ícone adaptativo vetorial próprio, com símbolo de lira/equalizador.
- Esqueleto Compose com cinco áreas principais e paleta Lyravoxen.
- Alternância claro/escuro persistida em SharedPreferences.
- Cliente HTTP para comandos já expostos pelo servidor remoto Windows (`/api?pin=...&c=...&a=...`).
- Workflow de GitHub Actions para compilar APK debug e publicar artefato.

## Compatibilidade da integração Windows
O servidor atual do Windows aceita requisições GET em `/api` autenticadas por PIN. O cliente Android usa somente comandos existentes (`state`, `playnext`, `endlive`, `lyricsnow`, `setnext`, `ref`). A ligação por IP é um modo complementar; o app Android deve continuar iniciando e evoluindo mesmo sem PC na rede.

## Etapas de desenvolvimento
1. **Fundação e CI** — projeto, tema, navegação, ícone, cliente de rede e workflow APK.
2. **Biblioteca local** — modelos, banco local, histórico, pastas, pesquisa e persistência offline.
3. **Player** — player oficial YouTube incorporado, pesquisa, fila e metadados.
4. **Letras e sincronização** — editor, marcação ao vivo, LRC/SRT, prévia configurável e sincronização.
5. **Bíblia e projeção** — leitura local/licenciada, seleção de referência, prévia, apresentação e tela externa.
6. **Controle remoto e integração** — ampliar cliente HTTP, reconexão, feedback e QR/PIN.
7. **Qualidade e distribuição** — testes em aparelhos reais, acessibilidade, consumo de CPU/bateria, assinatura release e APK/AAB de distribuição.

## Não implementado ainda
A primeira fundação não entrega paridade funcional com Windows. Não estão implementados player Android, busca integrada, banco de dados, sincronização efetiva, reconhecimento de áudio, biblioteca offline, leitura bíblica, projeção local nem build release assinado. O workflow atual gera APK debug para validação inicial.

# Lyravoxen — Lote 01

Primeiro lote do aplicativo Windows: estrutura WPF, player oficial incorporado do YouTube,
janela de projeção independente e sincronização manual por marcações de tempo.

## Requisitos
- Windows 10 (1809 ou superior) ou Windows 11, 64 bits
- .NET 8 SDK
- WebView2 Runtime (normalmente já instalado no Windows 11; pode ser instalado no Windows 10)

## Abrir
1. Instale o .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
2. Abra `src/Lyravoxen.Desktop/Lyravoxen.Desktop.csproj` no Visual Studio 2022
   ou execute `dotnet run --project src/Lyravoxen.Desktop`.
3. Cole um link de vídeo público do YouTube e clique em **Carregar YouTube**.
4. Clique em **Abrir projeção** para abrir a janela de saída.
5. Adicione linhas de letra com tempo (segundos) e texto; clique numa linha para projetá-la.

## Limites deste lote
- O player usa o player oficial incorporado do YouTube; não baixa nem extrai áudio.
- A sincronização é manual por timestamps neste primeiro lote.
- Reconhecimento automático de voz, alinhamento de playback e integração Holyrics ainda serão implementados nos próximos lotes.
- Não é ainda um instalador EXE nem APK.

## Compatibilidade
O projeto não exige AVX2, CUDA ou placa de vídeo dedicada. A compatibilidade real será validada
nos computadores Intel Core i5-3470 e Xeon E5-2689 durante os testes.

## Estrutura
- `src/Lyravoxen.Desktop`: aplicativo Windows
- `docs/ROADMAP.md`: plano de desenvolvimento

# Lyravoxen Android — fundação independente

Este é um aplicativo Android nativo em Kotlin + Jetpack Compose, separado do projeto Windows WPF.

## Build local
Requisitos: JDK 17, Android SDK 35 e Gradle 8.9.

```bash
cd android
gradle :app:assembleDebug
```

APK: `app/build/outputs/apk/debug/app-debug.apk`.

## Build pelo GitHub Actions
Workflow: **Android APK build** em `.github/workflows/android-build.yml`.
Execute manualmente em Actions ou envie alterações para `android/**`. O artefato `Lyravoxen-Android-APK-debug` contém o APK instalável de depuração.

## Arquitetura inicial
- `app/src/main/java/com/lyravoxen/android/MainActivity.kt`: interface Compose, navegação entre as cinco áreas e preferência claro/escuro.
- `data/RemoteControlClient.kt`: integração opcional com a API HTTP local do Lyravoxen Windows.
- `res/`: identidade visual e ícone Android vetorial independente.
- `AndroidManifest.xml`: permissões de rede e entrada do aplicativo.

## Limite desta primeira fundação
O APK inicial é uma base de interface e conectividade, não uma versão funcional completa equivalente ao Windows. As telas e a comunicação com o servidor Windows são o ponto de partida. Player oficial do YouTube, armazenamento local de letras, sincronização, biblioteca offline, projeção Android, leitura bíblica local e operação independente serão implementados em etapas próprias e testados separadamente. Não há extração nem download de áudio do YouTube nesta fundação.

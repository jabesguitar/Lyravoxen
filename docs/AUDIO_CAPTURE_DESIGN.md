# Audio capture design

## Goal
Capture only the audio rendered by the Lyravoxen YouTube player. Never silently fall back to system-wide loopback, because that could mix in Holyrics, browser tabs, notifications, or unrelated applications.

## Windows API
Use WASAPI process-loopback activation through `ActivateAudioInterfaceAsync` and the virtual endpoint `VAD\\Process_Loopback`. Select `PROCESS_LOOPBACK_MODE_INCLUDE_TARGET_PROCESS_TREE` so audio rendered by the target process and its descendants is included.

Microsoft references:
- https://learn.microsoft.com/windows/win32/api/audioclientactivationparams/ns-audioclientactivationparams-audioclient_process_loopback_params
- https://learn.microsoft.com/windows/win32/api/mmdeviceapi/nf-mmdeviceapi-activateaudiointerfaceasync
- https://github.com/microsoft/Windows-classic-samples/tree/main/Samples/ApplicationLoopback

## Process selection and isolation
- Obtain the WebView2 browser process ID from the active player control, not the Lyravoxen UI process by assumption.
- Include the browser process tree to capture WebView2 renderer/audio utility descendants.
- Re-resolve the process ID for each player session; never reuse a PID after navigation/restart.
- The separate YouTube search tabs may share the WebView2 environment/process tree with the player. Therefore process-tree capture may include audio from another WebView2 tab. This must be tested before claiming perfect isolation.
- If the search tabs can produce audio in the same process tree, isolate the player in a dedicated WebView2 environment/process or provide a dedicated player process. Do not mask this with system-wide capture.

## Compatibility
- Process loopback is supported on Windows 10 version 2004 (build 19041) and later according to the API sample and current API documentation. Detect support at runtime.
- If unavailable or activation fails, report that capture is unavailable. Keep YouTube playback working and retain LRCLIB/LRC/SRT/manual lyric paths.
- Do not silently capture the microphone or the full system mix.

## Streaming pipeline
1. Activate process loopback asynchronously for the player process tree.
2. Capture 44.1 kHz stereo float PCM (or the exact supported negotiated format) into a bounded ring buffer.
3. Convert to mono/resample only in the recognition worker, never on the audio callback.
4. Feed overlapping short windows to the local speech-recognition engine.
5. Tag every block and transcript with the current player session ID and media-time anchor.
6. Cancel and drain the old pipeline on pause, seek, track switch, player failure, or window close.
7. Publish partial text to operator preview first. Project only after confidence and timing checks.
8. Measure capture-to-preview and capture-to-projection latency; display actual values rather than estimates.

## Acceptance tests
- Player audio is captured while unrelated system audio is playing; unrelated audio must not appear in the captured stream.
- Holyrics audio and YouTube search-tab audio must not contaminate the player capture.
- Pause produces silence/no new speech segments; resume continues without duplicate timestamps.
- Switching tracks cancels the previous session and stale recognition results are discarded.
- Seek/restart resets the media-time anchor.
- Capture failure does not stop playback or the app's own projection.
- Test on supported Windows builds and the target older CPUs; record CPU, memory, and end-to-end latency.


## .NET and NAudio implementation decision (2026-11-15)
- NAudio's current `AudioClient.ActivateProcessLoopbackAsync(uint, ProcessLoopbackMode)` implementation provides a managed wrapper for the Windows process-loopback activation path.
- The current NAudio 3.x Wasapi packages target .NET 9. The desktop application currently targets .NET 8.
- Do not add NAudio 3.x to the current .NET 8 project without verifying package target compatibility.
- Before implementation, create a dedicated upgrade branch or trial CI matrix targeting .NET 9, verify WebView2, WPF, ProtectedData, self-contained win-x64 publish, and timeline tests.
- Keep the existing .NET 8 branch/build available until the .NET 9 candidate passes all CI checks and a manual Windows smoke test.
- Alternative: implement the small process-loopback interop layer against the Microsoft ApplicationLoopback sample while retaining .NET 8; compare maintenance and test costs before choosing.
- NAudio process-loopback API source: https://github.com/naudio/NAudio/blob/main/src/NAudio.Wasapi/CoreAudioApi/AudioClient.cs
- Microsoft API sample: https://github.com/microsoft/Windows-classic-samples/tree/main/Samples/ApplicationLoopback


## First implementation spike and live transcription
- The experimental branch targets .NET 9 and uses `NAudio.Wasapi 3.0.0` for process-loopback capture.
- `ProcessAudioCapture` uses `WasapiRecorderBuilder.WithProcessLoopback`, requests a 50 ms buffer, includes the target process tree, copies callback data, and preserves sample format plus device/QPC timestamps. It never falls back to system-wide capture.
- The main window starts capture only when the YouTube player reports the playing state; it stops on pause/end, track switch, and application close.
- The player WebView2 control uses a dedicated user-data folder. Capture targets its `CoreWebView2.BrowserProcessId` process tree. Isolation from all renderer/audio utility processes and search-tab audio still requires physical verification.
- `LiveTranscriptionEngine` converts captured PCM to mono 16 kHz, uses bounded audio buffering, and processes overlapping 8-second windows with a 3-second hop.
- The first local model is Whisper Tiny Q5_0, downloaded once to the user's LocalAppData model folder. CPU runtime selection includes both the optimized CPU runtime and the no-AVX fallback; thread count follows the detected hardware profile.
- Recognized segments are anchored to the player's media clock and added to the editable cue timeline/library. The recognizer is experimental for sung vocals; it can omit or misrecognize lyrics and should be reviewed before public projection.
- Process-loopback capture requires Windows 10 2004 (build 19041) or later. The packaged Whisper native runtimes currently document Windows 11 / Windows Server 2022 and Microsoft Visual C++ 2015-2022 x64 as prerequisites. On older Windows, playback/manual/catalog lyric paths remain available but local transcription is disabled.
- CI can prove restore/build/publish only. Still required: real Windows capture test, no contamination from Holyrics/search tabs, end-to-end latency measurement, and CPU/RAM benchmark on both the i5 and Xeon E5-2689 v0.
- NAudio process-loopback API documentation/source: https://github.com/naudio/NAudio/blob/main/src/NAudio.Wasapi/WasapiRecorderBuilder.cs and https://github.com/naudio/NAudio/blob/main/src/NAudio.Wasapi/WasapiRecorder.cs


## Minimum-hardware target: church PC
Previously supplied target hardware:
- Intel Xeon E5-2689 v0, 8 cores / 16 threads, 2.60 GHz base, x64 (Sandy Bridge generation; AVX but no AVX2).
- 8 GB DDR3 RAM.
- NVIDIA GeForce GT 710 2 GB.
- SATA SSD, Windows 64-bit.

Compatibility requirements:
- Do not require AVX2, AVX-512, CUDA, or a modern GPU.
- Build the capture and DSP path for x64 baseline/SSE2; AVX may be an optional optimized path only after runtime CPU feature detection.
- Keep audio capture independent of inference. Capture must remain responsive while recognition is paused, slow, or unavailable.
- Use bounded buffers and back-pressure/drop policy for recognition windows; never allow unbounded RAM growth.
- Run inference at low priority / below playback and UI, with configurable CPU thread count (default 2 on this target) and a pause/throttle control.
- Start with a quantized tiny model and benchmark a quantized base model as an optional quality mode. Do not download/load large models by default.
- Do not depend on GT 710 GPU acceleration. CPU-only mode is the required baseline.
- Target memory budget: keep the recognition worker/model footprint configurable and avoid loading multiple models simultaneously; record actual peak working set during benchmark.
- Provide a low-resource mode that reduces recognition window frequency/threads and shows that transcription may lag rather than stalling playback.
- Required benchmark machines: Intel i5 development PC and Xeon E5-2689 v0 church PC (or equivalent Sandy Bridge AVX-only CPU), with 8 GB RAM.
- CI must include a CPU-feature-safe build configuration; CI cannot replace a real benchmark on the Xeon-class machine.


## Multi-church distribution: broad hardware compatibility policy

The product is intended for distribution to churches with unknown and varied hardware. The Xeon E5-2689 v0 is one low-resource reference machine, not the minimum supported CPU.

### Compatibility tiers
- Tier A (legacy x64): older Intel/AMD x64 CPUs with SSE2, no AVX requirement. Core playback, local projection, manual LRC/SRT, and catalog lookup must remain available.
- Tier B (AVX-capable): optional accelerated local transcription using a compatible CPU build/model.
- Tier C (AVX2-capable): optional faster inference backend/model, selected only after runtime feature detection.
- GPU acceleration is optional only. NVIDIA CUDA, modern GPUs, AVX2 and AVX-512 must never be mandatory to launch or play audio.

### Runtime selection
- Detect OS version, process-loopback availability, CPU instruction sets, physical/logical cores and available memory at startup.
- Select a compatible inference backend/model at runtime. Never ship one binary path that assumes AVX2.
- Provide an explicit compatibility mode that disables unsupported acceleration.
- If process-loopback is unavailable on an older Windows build, keep playback and manual/catalog lyric features operational and explain that live audio transcription requires a newer Windows API.
- Do not fall back to system-wide audio capture, microphone capture, or an unverified capture source.

### Performance profiles
- Low-resource: one inference worker, smallest quantized model, reduced recognition cadence, bounded queue, lower CPU priority.
- Balanced: up to two inference workers, compact quantized model.
- Quality: optional higher-accuracy model and more workers on capable CPUs; never block audio playback or UI.
- Allow users to change profiles and show an estimated hardware capability, not promise a fixed transcription latency.

### Packaging and release
- Keep the baseline app architecture x64 and avoid CPU-specific compiler flags in the main executable.
- Package inference backends separately where their native binaries have different CPU requirements; select only a compatible backend at runtime.
- Include a diagnostic report with OS, CPU name/features, RAM, GPU, WebView2 runtime, capture API availability and selected inference profile.
- Maintain a feature matrix and test on at least one SSE2-only/no-AVX x64 CPU, one AVX-only CPU (Xeon E5-2689 v0), and one AVX2 CPU.
- CI builds prove compilation only. Hardware compatibility requires runtime tests on representative physical machines or trustworthy emulation; do not label a tier supported based only on CI.

### Product rule
Core playback and projection must not be held hostage by transcription support. On unsupported or very slow machines, the app remains useful: play the YouTube source, use synchronized catalog lyrics when available, and allow LRC/SRT/manual lyrics.


## Product priority clarification: modern and legacy computers are equal priorities

Both modern/high-performance computers and older/low-resource computers are first-class targets. Do not optimize exclusively for the Xeon reference machine.

- Modern systems must be able to use higher-quality transcription models, more inference workers, faster partial-result cadence and optional hardware acceleration when supported.
- Older systems must retain a lightweight CPU-only path with a compact quantized model, conservative worker count and bounded memory.
- Runtime adaptation chooses the best available backend/model/profile from detected CPU features, RAM, OS APIs and user preference.
- The user can choose Auto, Low Resource, Balanced or High Quality. Auto is the default; manual choice is respected but must never enable an unsupported instruction set.
- Capture and playback remain independent of inference. If high-quality recognition cannot keep up, reduce inference load or report lag; never stall music playback.
- The app should not impose a universal two-thread cap: thread count is profile- and hardware-dependent, with safe headroom left for playback, UI and Windows.
- Modern and legacy acceptance tests are both release blockers. The Xeon E5-2689 v0 is a compatibility reference, not the performance ceiling or sole priority.

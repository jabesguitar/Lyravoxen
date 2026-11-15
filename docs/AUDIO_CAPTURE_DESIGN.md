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

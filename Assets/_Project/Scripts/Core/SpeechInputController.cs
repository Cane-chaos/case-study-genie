using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Input chính của người chơi: Thu âm từ Microphone → STT API → gửi text lên Agent.
///
/// Luồng:
///   [Người chơi giữ nút Mic / phím Space]
///       → Microphone.Start() — thu âm
///   [Thả phím]
///       → Microphone.End() → convert AudioClip → WAV bytes
///       → POST {BackendBaseUrl}/api/stt → nhận text
///       → EventManager.TriggerSTTResult(text)
///       → CaseService.SendTurn(text) — gửi lên AI Agent
///
/// ⚙️ Để thay đổi STT provider: chỉnh SttEndpointUrl hoặc
///    override phương thức SendAudioToSTT().
/// </summary>
public class SpeechInputController : MonoBehaviour
{
    public static SpeechInputController Instance { get; private set; }

    [Header("Trigger Config")]
    [Tooltip("Phím để giữ và nói (Push-to-Talk)")]
    public KeyCode pushToTalkKey = KeyCode.Space;

    [Tooltip("Nếu true: giữ phím để nói, thả phím để dừng.\n" +
             "Nếu false: nhấn 1 lần để bắt đầu, nhấn lần nữa để dừng.")]
    public bool holdToTalk = true;

    [Header("Microphone Config")]
    [Tooltip("Tên microphone (để trống = dùng mic mặc định)")]
    public string microphoneDevice = null;

    [Tooltip("Thời gian thu âm tối đa (giây)")]
    public int maxRecordingSeconds = 30;

    [Tooltip("Sample rate")]
    public int sampleRate = 16000;

    [Header("STT Config")]
    [Tooltip("Chỉ thu âm khi đang ở trạng thái Simulator")]
    public bool requireSimulatorState = true;

    // ── Internal ──
    private AudioClip _recordingClip;
    private bool _isRecording = false;
    private bool _toggleState = false; // Dùng cho toggle mode

    // ── Properties ──
    public bool IsRecording => _isRecording;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Chỉ nhận input khi đang Simulator
        if (requireSimulatorState &&
            SimulationManager.Instance?.CurrentState != SimulationState.Simulator)
            return;

        if (holdToTalk)
        {
            HandleHoldToTalk();
        }
        else
        {
            HandleToggleTalk();
        }
    }

    // ───────────────────────────────────────────
    // INPUT HANDLERS
    // ───────────────────────────────────────────

    private void HandleHoldToTalk()
    {
        if (Input.GetKeyDown(pushToTalkKey))
            StartRecording();
        else if (Input.GetKeyUp(pushToTalkKey) && _isRecording)
            StopRecordingAndProcess();
    }

    private void HandleToggleTalk()
    {
        if (Input.GetKeyDown(pushToTalkKey))
        {
            if (!_isRecording)
                StartRecording();
            else
                StopRecordingAndProcess();
        }
    }

    // ───────────────────────────────────────────
    // RECORDING
    // ───────────────────────────────────────────

    /// <summary>Bắt đầu thu âm từ microphone.</summary>
    public void StartRecording()
    {
        if (_isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            EventManager.TriggerSTTError("Không tìm thấy microphone trên thiết bị.");
            return;
        }

        _recordingClip = Microphone.Start(microphoneDevice, false, maxRecordingSeconds, sampleRate);
        _isRecording = true;

        Debug.Log("[SpeechInput] Bắt đầu thu âm...");
        EventManager.TriggerSTTRecordingStarted();
    }

    /// <summary>Dừng thu âm và bắt đầu xử lý STT.</summary>
    public void StopRecordingAndProcess()
    {
        if (!_isRecording) return;

        // Lấy số sample thực sự đã thu
        int recordedSamples = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);
        _isRecording = false;

        Debug.Log($"[SpeechInput] Dừng thu âm. Samples: {recordedSamples}");
        EventManager.TriggerSTTRecordingStopped();

        if (recordedSamples <= 0 || _recordingClip == null)
        {
            EventManager.TriggerSTTError("Không thu được âm thanh.");
            return;
        }

        // Trim AudioClip theo đúng độ dài thực tế đã thu
        AudioClip trimmedClip = TrimAudioClip(_recordingClip, recordedSamples);

        // Gửi lên STT API
        StartCoroutine(SendAudioToSTT(trimmedClip));
    }

    // ───────────────────────────────────────────
    // STT API CALL
    // ───────────────────────────────────────────

    /// <summary>
    /// Gửi AudioClip lên STT endpoint và nhận text.
    /// Endpoint: POST {BackendBaseUrl}/api/stt
    /// Content-Type: audio/wav
    /// Response: { "text": "...", "language": "vi", "confidence": 0.95 }
    ///
    /// ⚙️ Để dùng OpenAI Whisper trực tiếp: đổi url thành
    ///    "https://api.openai.com/v1/audio/transcriptions"
    ///    và thêm header "Authorization: Bearer {OpenAI_API_Key}"
    /// </summary>
    private IEnumerator SendAudioToSTT(AudioClip clip)
    {
        string url = $"{SimulationManager.Instance.BackendBaseUrl}/api/stt";
        byte[] wavBytes = AudioClipToWav(clip);

        Debug.Log($"[SpeechInput] Gửi audio lên STT: {url} ({wavBytes.Length} bytes)");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(wavBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "audio/wav");

            // Attach JWT nếu có
            if (!string.IsNullOrEmpty(SimulationManager.Instance?.AuthToken))
                request.SetRequestHeader("Authorization",
                    $"Bearer {SimulationManager.Instance.AuthToken}");

            request.timeout = 30;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                STTResponse sttResp = JsonUtility.FromJson<STTResponse>(request.downloadHandler.text);

                if (!string.IsNullOrEmpty(sttResp?.text))
                {
                    Debug.Log($"[SpeechInput] STT Result: \"{sttResp.text}\"");
                    EventManager.TriggerSTTResult(sttResp.text);

                    // Tự động gửi text lên Agent Server
                    CaseService.Instance?.SendTurn(sttResp.text);
                }
                else
                {
                    EventManager.TriggerSTTError("STT trả về text rỗng.");
                }
            }
            else
            {
                string error = $"STT API lỗi: {request.responseCode} {request.error}";
                Debug.LogWarning($"[SpeechInput] {error}");
                EventManager.TriggerSTTError(error);
            }
        }
    }

    // ───────────────────────────────────────────
    // AUDIO HELPERS
    // ───────────────────────────────────────────

    private AudioClip TrimAudioClip(AudioClip original, int sampleCount)
    {
        float[] data = new float[sampleCount * original.channels];
        original.GetData(data, 0);
        AudioClip trimmed = AudioClip.Create("Recording_Trimmed",
            sampleCount, original.channels, original.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    /// <summary>
    /// Convert AudioClip → WAV byte array (PCM 16-bit, chuẩn Whisper API).
    /// </summary>
    private byte[] AudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert float samples → 16-bit PCM
        short[] pcm = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
            pcm[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);

        // Build WAV file header + data
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
        {
            int byteRate = clip.frequency * clip.channels * 2;
            int dataSize = pcm.Length * 2;

            // WAV Header
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            stream.Write(BitConverter.GetBytes(36 + dataSize), 0, 4);
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
            stream.Write(BitConverter.GetBytes(16), 0, 4);        // Chunk size
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);  // PCM format
            stream.Write(BitConverter.GetBytes((short)clip.channels), 0, 2);
            stream.Write(BitConverter.GetBytes(clip.frequency), 0, 4);
            stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
            stream.Write(BitConverter.GetBytes((short)(clip.channels * 2)), 0, 2);
            stream.Write(BitConverter.GetBytes((short)16), 0, 2); // Bits per sample
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
            stream.Write(BitConverter.GetBytes(dataSize), 0, 4);

            // PCM Data
            foreach (short s in pcm)
                stream.Write(BitConverter.GetBytes(s), 0, 2);

            return stream.ToArray();
        }
    }
}

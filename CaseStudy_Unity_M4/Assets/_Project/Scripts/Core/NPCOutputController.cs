using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Điều phối đầu ra của NPC sau mỗi lượt phản hồi từ Agent:
///   1. Tải và phát audio TTS (giọng nói NPC)
///   2. Chạy lip-sync đồng bộ với audio
///   3. Trigger animation cảm xúc (angry, calm, happy...)
///
/// Gắn script này vào cùng GameObject với NPC (Mr. Viktor hoặc nhân vật khác).
///
/// Lắng nghe: EventManager.OnChatResponseReceived
/// Fire ra:   EventManager.OnNPCSpeakStart / OnNPCSpeakEnd / OnNPCEmotionChanged
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class NPCOutputController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("AudioSource để phát giọng NPC (auto-detect nếu để trống)")]
    public AudioSource npcAudioSource;

    [Tooltip("Animator của NPC để trigger cảm xúc và lip-sync")]
    public Animator npcAnimator;

    [Header("Lip-sync Config")]
    [Tooltip("Dùng amplitude-based lip-sync đơn giản (không cần plugin)")]
    public bool useAmplitudeLipSync = true;

    [Tooltip("Tên BlendShape trên SkinnedMeshRenderer để điều khiển miệng")]
    public string mouthOpenBlendShapeName = "MouthOpen";

    [Tooltip("Hệ số khuếch đại amplitude → độ mở miệng (1.0 = chuẩn)")]
    public float lipSyncAmplitude = 150f;

    [Header("Animator Parameters")]
    [Tooltip("Tên parameter Animator để trigger cảm xúc")]
    public string emotionAnimParam = "Emotion";

    [Tooltip("Tên parameter float cho độ mở miệng (BlendTree)")]
    public string mouthOpenAnimParam = "MouthOpen";

    [Tooltip("Tên parameter bool để biết NPC đang nói")]
    public string isSpeakingAnimParam = "IsSpeaking";

    // ── Internal ──
    private SkinnedMeshRenderer _faceMesh;
    private int _mouthBlendShapeIndex = -1;
    private bool _isSpeaking = false;

    // ── Properties ──
    public bool IsSpeaking => _isSpeaking;

    void Awake()
    {
        if (npcAudioSource == null)
            npcAudioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        // Tìm BlendShape miệng trên SkinnedMeshRenderer của NPC
        if (useAmplitudeLipSync)
        {
            _faceMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_faceMesh != null)
            {
                for (int i = 0; i < _faceMesh.sharedMesh.blendShapeCount; i++)
                {
                    if (_faceMesh.sharedMesh.GetBlendShapeName(i) == mouthOpenBlendShapeName)
                    {
                        _mouthBlendShapeIndex = i;
                        break;
                    }
                }
                if (_mouthBlendShapeIndex < 0)
                    Debug.LogWarning($"[NPCOutput] Không tìm thấy BlendShape '{mouthOpenBlendShapeName}'. " +
                                     "Lip-sync sẽ dùng Animator parameter thay thế.");
            }
        }

        // Đăng ký lắng nghe event
        EventManager.OnChatResponseReceived += HandleAgentResponse;
    }

    void OnDestroy()
    {
        EventManager.OnChatResponseReceived -= HandleAgentResponse;
        StopAllCoroutines();
    }

    void Update()
    {
        // Lip-sync theo amplitude khi đang phát audio
        if (_isSpeaking && useAmplitudeLipSync)
        {
            UpdateLipSync();
        }
    }

    // ───────────────────────────────────────────
    // EVENT HANDLER
    // ───────────────────────────────────────────

    private void HandleAgentResponse(AgentTurnResponse response)
    {
        // 1. Trigger emotion animation
        if (!string.IsNullOrEmpty(response.persona_emotion))
        {
            SetEmotion(response.persona_emotion);
        }

        // 2. Phát audio nếu có
        if (response.HasAudio())
        {
            StopAllCoroutines();
            StartCoroutine(PlayNPCAudio(response));
        }
        else
        {
            // Không có audio → chỉ log (backend chưa hỗ trợ TTS)
            Debug.LogWarning("[NPCOutput] Response không có audio_url. " +
                             "Đảm bảo backend trả về audio và skip_tts=false.");
        }
    }

    // ───────────────────────────────────────────
    // AUDIO PLAYBACK
    // ───────────────────────────────────────────

    private IEnumerator PlayNPCAudio(AgentTurnResponse response)
    {
        AudioClip clip = null;

        if (!string.IsNullOrEmpty(response.audio_url))
        {
            // Tải audio từ URL
            yield return StartCoroutine(FetchAudioFromUrl(response.audio_url,
                fetchedClip => clip = fetchedClip));
        }
        else if (!string.IsNullOrEmpty(response.audio_base64))
        {
            // Decode base64 → AudioClip
            clip = DecodeBase64ToAudioClip(response.audio_base64);
        }

        if (clip == null)
        {
            Debug.LogWarning("[NPCOutput] Không thể load audio clip.");
            yield break;
        }

        // Phát audio
        npcAudioSource.clip = clip;
        npcAudioSource.Play();
        _isSpeaking = true;

        // Set Animator
        if (npcAnimator != null)
            npcAnimator.SetBool(isSpeakingAnimParam, true);

        EventManager.TriggerNPCSpeakStart(response.audio_url ?? "base64");

        // Chờ audio phát xong
        yield return new WaitForSeconds(clip.length);

        // Kết thúc
        _isSpeaking = false;
        CloseMouth();

        if (npcAnimator != null)
            npcAnimator.SetBool(isSpeakingAnimParam, false);

        EventManager.TriggerNPCSpeakEnd();
    }

    private IEnumerator FetchAudioFromUrl(string url, System.Action<AudioClip> onDone)
    {
        Debug.Log($"[NPCOutput] Tải audio: {url}");

        // Xác định AudioType từ extension
        AudioType audioType = url.EndsWith(".ogg") ? AudioType.OGGVORBIS : AudioType.MPEG;

        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                onDone?.Invoke(clip);
            }
            else
            {
                Debug.LogWarning($"[NPCOutput] Tải audio thất bại: {request.error}");
                onDone?.Invoke(null);
            }
        }
    }

    private AudioClip DecodeBase64ToAudioClip(string base64Audio)
    {
        try
        {
            byte[] wavBytes = System.Convert.FromBase64String(base64Audio);
            // Decode WAV header để lấy sample rate, channels
            int channels = System.BitConverter.ToInt16(wavBytes, 22);
            int frequency = System.BitConverter.ToInt32(wavBytes, 24);
            int dataStart = 44; // Standard WAV header size
            int sampleCount = (wavBytes.Length - dataStart) / 2;

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short pcmSample = System.BitConverter.ToInt16(wavBytes, dataStart + i * 2);
                samples[i] = pcmSample / (float)short.MaxValue;
            }

            AudioClip clip = AudioClip.Create("NPC_Audio_Base64", sampleCount, channels, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NPCOutput] Decode base64 audio lỗi: {ex.Message}");
            return null;
        }
    }

    // ───────────────────────────────────────────
    // LIP-SYNC (Amplitude-based)
    // ───────────────────────────────────────────

    private void UpdateLipSync()
    {
        float[] samples = new float[256];
        npcAudioSource.GetOutputData(samples, 0);

        // Tính RMS amplitude
        float sum = 0f;
        foreach (float s in samples) sum += s * s;
        float rms = Mathf.Sqrt(sum / samples.Length);
        float mouthOpenValue = Mathf.Clamp01(rms * lipSyncAmplitude);

        // Áp dụng vào BlendShape
        if (_faceMesh != null && _mouthBlendShapeIndex >= 0)
        {
            _faceMesh.SetBlendShapeWeight(_mouthBlendShapeIndex, mouthOpenValue * 100f);
        }

        // Hoặc qua Animator parameter
        if (npcAnimator != null)
        {
            npcAnimator.SetFloat(mouthOpenAnimParam, mouthOpenValue);
        }
    }

    private void CloseMouth()
    {
        if (_faceMesh != null && _mouthBlendShapeIndex >= 0)
            _faceMesh.SetBlendShapeWeight(_mouthBlendShapeIndex, 0f);

        if (npcAnimator != null)
            npcAnimator.SetFloat(mouthOpenAnimParam, 0f);
    }

    // ───────────────────────────────────────────
    // EMOTION / ANIMATION
    // ───────────────────────────────────────────

    /// <summary>
    /// Chuyển cảm xúc NPC sang emotion state mới.
    /// Animator phải có parameter "Emotion" kiểu string hoặc int (dùng hash).
    /// </summary>
    public void SetEmotion(string emotion)
    {
        Debug.Log($"[NPCOutput] Emotion → {emotion}");
        EventManager.TriggerNPCEmotionChanged(emotion);

        if (npcAnimator == null) return;

        // Map emotion string → Animator trigger
        // Điều chỉnh tên trigger cho khớp với Animator của dự án
        switch (emotion.ToLower())
        {
            case "angry":
                npcAnimator.SetTrigger("Angry");
                break;
            case "happy":
            case "satisfied":
                npcAnimator.SetTrigger("Happy");
                break;
            case "neutral":
            case "calm":
                npcAnimator.SetTrigger("Neutral");
                break;
            case "impatient":
                npcAnimator.SetTrigger("Impatient");
                break;
            default:
                npcAnimator.SetTrigger("Neutral");
                break;
        }
    }
}

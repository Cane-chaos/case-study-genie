using System;
using System.Collections.Generic;

// ═══════════════════════════════════════════════════════════════
// Legacy compatibility layer.
// Các class mới nên dùng CaseStudyState và PersonaData
// từ StateModels.cs thay vì file này.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Config đơn giản cho một Persona (Legacy).
/// Dùng PersonaData từ StateModels.cs cho dữ liệu đầy đủ.
/// </summary>
[Serializable]
public class PersonaConfig
{
    public string personaId;
    public string name;
    public string ai_traits;    // Ví dụ: "Khó tính, tỷ phú, ghét chờ đợi"
    public string start_prompt;

    /// <summary>Chuyển đổi sang PersonaData đầy đủ.</summary>
    public PersonaData ToPersonaData()
    {
        return new PersonaData
        {
            name = this.name,
            prefab_id = this.personaId,
            traits = !string.IsNullOrEmpty(ai_traits)
                ? ai_traits.Split(',')
                : new string[0],
            initial_trust = 50
        };
    }
}

/// <summary>
/// Dữ liệu case study đơn giản (Legacy).
/// Dùng CaseStudyState từ StateModels.cs cho dữ liệu đầy đủ.
/// </summary>
[Serializable]
public class CaseStudyData
{
    public string caseId;
    public string environmentId;
    public List<PersonaConfig> personas;

    /// <summary>Chuyển đổi sang CaseStudyState đầy đủ.</summary>
    public CaseStudyState ToCaseStudyState()
    {
        var state = new CaseStudyState
        {
            case_metadata = new CaseMetadata { case_id = this.caseId },
            environment = new EnvironmentData { scene_id = this.environmentId }
        };

        // Lấy persona đầu tiên (demo hiện tại chỉ dùng 1 persona)
        if (personas != null && personas.Count > 0)
        {
            state.persona = personas[0].ToPersonaData();
        }

        return state;
    }
}

 
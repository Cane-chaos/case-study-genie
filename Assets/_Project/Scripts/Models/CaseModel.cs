using System;
using System.Collections.Generic;

[Serializable]
public class PersonaConfig
{
    public string personaId;
    public string name;
    public string ai_traits; // Ví dụ: "Khó tính, tỷ phú, ghét chờ đợi"
    public string start_prompt;
}

[Serializable]
public class CaseStudyData
{
    public string caseId;
    public string environmentId;
    public List<PersonaConfig> personas;
}
 
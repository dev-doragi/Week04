using System.Collections.Generic;
public static class TextUtil
{
    /*public static string TranslateKorean(Gender g)
    {
        return g switch
        {
            Gender.Male => "남성",
            Gender.Female => "여성",
            _ => "알수없음"
        };
    }

    public static string TranslateKorean(TraitType t)
    {
        return t switch
        {
            TraitType.Hikikomori => "히키코모리",
            TraitType.Psychopath => "사이코패스",
            TraitType.MisogynyMisandry => "성별 혐오",
            TraitType.Depression => "우울증",
            TraitType.Kind => "다정다감",
            TraitType.Tough => "강인함",
            TraitType.Diligent => "근면함",
            TraitType.Optimistic => "낙천적",
            TraitType.AllRounder => "만능형",
            TraitType.Inept => "둔재",
            TraitType.Artisan => "장인",
            TraitType.Hunter => "사냥꾼",
            TraitType.Chatterbox => "수다쟁이",
            TraitType.Coward => "겁쟁이",
            _ => t.ToString()
        };
    }

    public static string TranslateKorean(StatType s)
    {
        return s switch
        {
            StatType.Combat => "전투",
            StatType.Craft => "제작",
            StatType.Build => "건축",
            StatType.Gather => "수집",
            StatType.Social => "매력",
            _ => s.ToString()
        };
    }
    public static string TranslateKorean(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Mild => "온화한 날씨가 지속됩니다",
            WeatherType.Hot => "약한 더위가 발생합니다",
            WeatherType.Cold => "약한 추위가 발생합니다",
            WeatherType.Heatwave => "폭염이 내리쬡니다",
            WeatherType.Drought => "가뭄이 발생합니다",
            //WeatherType.Storm => "폭풍이 건물을 파괴합니다",
            //eatherType.Flood => "홍수가 발생합니다",
            //WeatherType.Snowstorm => "폭설이 내립니다",
            WeatherType.ExtremeCold => "혹한이 다가옵니다",
            _ => weather.ToString()
        };
    }

    public static string TranslateKorean(WorldEventType worldEvent)
    {
        return worldEvent switch
        {
            WorldEventType.None => "아무 일도 없음",
            WorldEventType.Visitor => "조우자 방문",
            WorldEventType.Raid => "습격 발생",
            _ => worldEvent.ToString()
        };
    }

    public static string TranslateKorean(PolicyType policy)
    {
        return policy switch
        {
            PolicyType.CombatFirst => "전투 우선 정책",
            PolicyType.CraftFirst => "제작 우선 정책",
            PolicyType.BuildFirst => "건축 우선 정책",
            PolicyType.GatherFirst => "수집 우선 정책",
            PolicyType.SocialFirst => "사교 우선 정책",
            _ => policy.ToString()
        };
    }*/

    private static readonly Dictionary<string, KeyValuePair<string, string>> _koreanParticles
    = new Dictionary<string, KeyValuePair<string, string>>
    {
        { "을/를", new KeyValuePair<string, string>("을", "를") },
        { "이/가", new KeyValuePair<string, string>("이", "가") },
        { "은/는", new KeyValuePair<string, string>("은", "는") },
    };

    public static string ApplyKoreanParticles(string text)
    {
        foreach (KeyValuePair<string, KeyValuePair<string, string>> particle in _koreanParticles)
        {
            int markerIndex = text.IndexOf(particle.Key);
            while (markerIndex > 0)
            {
                int prevIndex = markerIndex - 1;
                char prevChar = text[prevIndex];

                bool hasFinalConsonant = prevChar >= 0xAC00 && prevChar <= 0xD7A3 && ((prevChar - 0xAC00) % 28 > 0);
                string replaced = hasFinalConsonant ? particle.Value.Key : particle.Value.Value;

                text = text.Remove(prevIndex + 1, particle.Key.Length).Insert(prevIndex + 1, replaced);
                markerIndex = text.IndexOf(particle.Key);
            }
        }

        return text;
    }
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Wrapper around communications with the sound engine
 */
public class SoundEngineTuner : MonoBehaviour
{
    public const float BASE_TEMPO = 120;
    public const float MAX_DELAY = 0.5f;

    private Dictionary<System.Type, string> m_KeywordFamily;
    //Used to retrieve tempo range from a tempo type
    private Dictionary<InstrumentFamily.TempoType, RTPCRange<InstrumentFamily.TempoType>> m_TempoRanges;
    //Used to retrieve intensity ranges from an intensity type
    private Dictionary<InstrumentFamily.IntensityType, RTPCRange<InstrumentFamily.IntensityType>> m_IntensityRanges;

    private void Awake()
    {
        m_KeywordFamily = new Dictionary<System.Type, string>()
        {
            { typeof(WoodsFamily), "Woods" },
            { typeof(BrassFamily), "Brass" },
            { typeof(PercussionsFamily), "Percussions" },
            { typeof(StringsFamily), "Strings" },
        };

        m_TempoRanges = new Dictionary<InstrumentFamily.TempoType, RTPCRange<InstrumentFamily.TempoType>>()
        {
            { InstrumentFamily.TempoType.Lento, new RTPCRange<InstrumentFamily.TempoType>(45, 75, 60, InstrumentFamily.TempoType.Lento) },
            { InstrumentFamily.TempoType.Andante, new RTPCRange<InstrumentFamily.TempoType>(75, 105, 90, InstrumentFamily.TempoType.Andante) },
            { InstrumentFamily.TempoType.Allegro, new RTPCRange<InstrumentFamily.TempoType>(105, 135, 120, InstrumentFamily.TempoType.Allegro) },
            { InstrumentFamily.TempoType.Presto, new RTPCRange<InstrumentFamily.TempoType>(135, 165, 150, InstrumentFamily.TempoType.Presto) },
        };

        m_IntensityRanges = new Dictionary<InstrumentFamily.IntensityType, RTPCRange<InstrumentFamily.IntensityType>>()
        {
            { InstrumentFamily.IntensityType.Pianissimo, new RTPCRange<InstrumentFamily.IntensityType>(0, 0.22f, 25, InstrumentFamily.IntensityType.Pianissimo) },
            { InstrumentFamily.IntensityType.MezzoForte, new RTPCRange<InstrumentFamily.IntensityType>(0.22f, 0.35f, 50, InstrumentFamily.IntensityType.MezzoForte) },
            { InstrumentFamily.IntensityType.Fortissimo, new RTPCRange<InstrumentFamily.IntensityType>(0.35f, 1000, 75, InstrumentFamily.IntensityType.Fortissimo) }
        };
    }


    /* FOCUS */
    //Highlight a family with a volume offset
    public void FocusFamily(InstrumentFamily family)
    {
        AkSoundEngine.SetState("Focus", GetFocusRTPCRequest(m_KeywordFamily[family.GetType()]));
    }

    //Reset the highlight
    public void UnfocusFamily()
    {
        AkSoundEngine.SetState("Focus", "Focus_None");
    }


    /* TEMPO */
    public void SetTempo(float bpm)
    {
        AkSoundEngine.SetRTPCValue("RTPC_Tempo", bpm / BASE_TEMPO);
    }

    public RTPCRange<InstrumentFamily.TempoType> GetTempoRange(float bpm)
    {
        foreach (RTPCRange<InstrumentFamily.TempoType> range in m_TempoRanges.Values) {
            if (range.IsInRange(bpm)) {
                return range;
            }
        }
        return m_TempoRanges[InstrumentFamily.TempoType.Allegro];
    }

    //Used to describe the min and max bpm ranges of a type, and the value that should be send to the RTPC
    public struct RTPCRange<T>
    {
        public float min;
        public float max;
        public float value;
        public T type;
        public RTPCRange(float _min, float _max, float _value, T _type)
        {
            min = _min;
            max = _max;
            value = _value;
            type = _type;
        }
        public bool IsInRange(float a)
        {
            return (a <= max && a >= min);
        }
    }


    /* DELAY */
    public void SetDelay(InstrumentFamily family, float delay)
    {
        try {
            AkSoundEngine.SetRTPCValue(GetDelayRTPCRequest(m_KeywordFamily[family.GetType()]), delay);
        }
        catch (KeyNotFoundException) {
            Debug.LogError("Error : " + family.GetType() + " family doesn't exist in the RTPC keyword dictionnary");
        }
    }


    /* ARTICULATION */
    public void SetArticulation(InstrumentFamily family, int indexType)
    {
        if (family.articulationTypes.Length > 1) {
            //See sound documentation for the explaining of the formula
            float value = (float)indexType / (float)(family.articulationTypes.Length);
            value *= 100;

            try {
                AkSoundEngine.SetRTPCValue(GetArticulationRTPCRequest(m_KeywordFamily[family.GetType()]), value);
            }
            catch (KeyNotFoundException) {
                Debug.LogError("Error : " + family.GetType() + " family doesn't exist in the RTPC keyword dictionnary");
            }
        }
    }


    /* INTENSITY */
    //Intensity : [0, 100], default at 50
    public void SetIntensity(InstrumentFamily family, float intensity)
    {
        try {
            AkSoundEngine.SetRTPCValue(GetIntensityRTPCRequest(m_KeywordFamily[family.GetType()]), intensity);
        }
        catch (KeyNotFoundException) {
            Debug.LogError("Error : " + family.GetType() + " family doesn't exist in the RTPC keyword dictionnary");
        }
    }

    public void SetGlobalIntensity(float intensity)
    {
        foreach (string keywordFamily in m_KeywordFamily.Values) {
            AkSoundEngine.SetRTPCValue(GetIntensityRTPCRequest(keywordFamily), intensity);
        }
    }

    public RTPCRange<InstrumentFamily.IntensityType> GetIntensityRange(float amplitude)
    {
        foreach (RTPCRange<InstrumentFamily.IntensityType> range in m_IntensityRanges.Values) {
            if (range.IsInRange(amplitude)) {
                return range;
            }
        }
        return m_IntensityRanges[InstrumentFamily.IntensityType.MezzoForte];
    }


    /* REQUESTS TRANSLATOR */
    private string GetDelayRTPCRequest(string familyKeyword)
    {
        return "RTPC_Time_Delay_" + familyKeyword;
    }

    private string GetArticulationRTPCRequest(string familyKeyword)
    {
        return "RTPC_Articulation_" + familyKeyword;
    }

    private string GetIntensityRTPCRequest(string familyKeyword)
    {
        return "RTPC_Intensity_" + familyKeyword;
    }

    private string GetFocusRTPCRequest(string familyKeyword)
    {
        return "Focus_" + familyKeyword;
    }
}

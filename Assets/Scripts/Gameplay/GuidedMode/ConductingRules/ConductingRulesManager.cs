﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Compute the conducting rules of the guided mode
 * Checks if the player follows them or not
 */
public class ConductingRulesManager : MonoBehaviour
{
    [Header("Callbacks")]
    public SoundEngineTuner soundEngineTuner;
    public ArticulationManager articulationManager;
    public IntensityManager intensityManager;
    public TempoManager tempoManager;

    [Header("UI")]
    public UIArticulationToast UIArticulationToast;
    public UITempoToast UITempoToast;
    public UIIntensityToast UIIntensityToast;

    [Header("Score")]
    public ScoreManager scoreManager;
    public ScoringParametersScriptableObject scoringParameters;
    private bool m_IsProfilingTransition;
    private int m_TransitionProfilerScore;

    private GuidedModeManager.TrackType m_CurrentTrackType; 
    
    /* Rules */
    private Dictionary<string, OrchestraState> m_Rules;
    private OrchestraState m_CurrentRules;
    private OrchestraState m_CurrentOrchestraState;
    
    public struct OrchestraState
    {
        public InstrumentFamily.ArticulationType articulationType;
        public InstrumentFamily.IntensityType intensityType;
        public InstrumentFamily.TempoType tempoType;

        public OrchestraState(InstrumentFamily.ArticulationType articulation, InstrumentFamily.IntensityType intensity,
            InstrumentFamily.TempoType tempo)
        {
            articulationType = articulation;
            intensityType = intensity;
            tempoType = tempo;
        }
    }
    
    /* Score timer */
    private float m_TimeSinceLastScore = 0.0f;
    

    private void Awake()
    {
        m_Rules = new Dictionary<string, OrchestraState>()
        {
            {"Start", new OrchestraState(InstrumentFamily.ArticulationType.Pizzicato, InstrumentFamily.IntensityType.MezzoForte, InstrumentFamily.TempoType.Andante)},
            {"Transition1", new OrchestraState(InstrumentFamily.ArticulationType.Staccato, InstrumentFamily.IntensityType.MezzoForte, InstrumentFamily.TempoType.Allegro)},
            {"Transition2", new OrchestraState(InstrumentFamily.ArticulationType.Legato, InstrumentFamily.IntensityType.Fortissimo, InstrumentFamily.TempoType.Allegro)},
            {"Transition3", new OrchestraState(InstrumentFamily.ArticulationType.Legato, InstrumentFamily.IntensityType.Pianissimo, InstrumentFamily.TempoType.Lento)}
        };

        m_CurrentOrchestraState = new OrchestraState(InstrumentFamily.ArticulationType.Legato, InstrumentFamily.IntensityType.MezzoForte, InstrumentFamily.TempoType.Andante);
    }

    public void OnStartOrchestra()
    {
        articulationManager.OnArticulationChange += OnArticulationChange;
        intensityManager.OnIntensityChange += OnIntensityChange;
        tempoManager.OnTempoChange += OnTempoChange;
    }
    
    public void GetNewRules(string stateName)
    {
        if (m_Rules.TryGetValue(stateName, out OrchestraState rules)) {
            m_CurrentRules = rules;
            DrawRules();
            ShowRules(true);
        }
    }

    public void SetNewRules(OrchestraState rules, bool draw = true)
    {
        m_CurrentRules = rules;
        if (draw) {
            DrawRules();
            ShowRules(true);
        }
    }
    
    public void SetCurrentOrchestraState(OrchestraState state, bool draw = true)
    {
        m_CurrentOrchestraState = state;
        if (draw) {
            DrawRules();
            ShowRules(true);
        }
    }
    
    public void SetCurrentTrackType(GuidedModeManager.TrackType type, bool draw = true)
    {
        m_CurrentTrackType = type;
        if (draw) {
            DrawRules();
            ShowRules(true);
        }
    }
    
    public void ProfileTransition()
    {
    
        if (m_CurrentOrchestraState.articulationType == m_CurrentRules.articulationType &&
            m_CurrentOrchestraState.tempoType == m_CurrentRules.tempoType &&
            m_CurrentOrchestraState.intensityType == m_CurrentRules.intensityType) {

            m_TransitionProfilerScore = 1;
        }
        else {
            m_TransitionProfilerScore = 0;
        }

        scoreManager.AddTransitionScore(m_TransitionProfilerScore);
    }
    
    //Updates the score
    public void Check()
    {
        int mistakes = 0;

        if (m_CurrentOrchestraState.articulationType != m_CurrentRules.articulationType) {
            mistakes += 1;
        }
        if (m_CurrentOrchestraState.tempoType != m_CurrentRules.tempoType) {
            mistakes += 1;
        }
        if (m_CurrentOrchestraState.intensityType != m_CurrentRules.intensityType) {
            mistakes += 1;
        }

        m_TimeSinceLastScore += Time.deltaTime;
        if (m_TimeSinceLastScore > scoringParameters.checkPerSeconds) {
            if (mistakes == 0) {
                scoreManager.AddScore(scoringParameters.noMistake);
            }
            else {
                scoreManager.AddScore(scoringParameters.perMistake * (float) mistakes);
            }
            m_TimeSinceLastScore %= scoringParameters.checkPerSeconds;
        }
    }

    /* Callbacks */
    public void OnArticulationChange(InstrumentFamily.ArticulationType type, bool fromPotion, GameObject potion)
    {
        m_CurrentOrchestraState.articulationType = type;
        UIArticulationToast.Draw(m_CurrentOrchestraState.articulationType, m_CurrentRules.articulationType, 
            m_CurrentTrackType == GuidedModeManager.TrackType.Transition, fromPotion);

        if (fromPotion) {
            if (m_CurrentOrchestraState.articulationType == m_CurrentRules.articulationType) {
                OnGoodArticulationChange?.Invoke();
                SoundEngineTuner.SetSwitchPotionBonusMalus(SoundEngineTuner.SFXPotionScoreType.Bonus, potion);
            }
            else {
                SoundEngineTuner.SetSwitchPotionBonusMalus(SoundEngineTuner.SFXPotionScoreType.Malus, potion);
            }
        }
    }

    public void OnTempoChange(InstrumentFamily.TempoType type, float bpm, bool fromConducting)
    {
        m_CurrentOrchestraState.tempoType = type;
        UITempoToast.Draw(m_CurrentOrchestraState.tempoType, m_CurrentRules.tempoType, bpm,
            m_CurrentTrackType == GuidedModeManager.TrackType.Transition, fromConducting);
        
        if (m_CurrentOrchestraState.tempoType == m_CurrentRules.tempoType) {
            OnGoodTempoChange?.Invoke();
        }
    }

    public void OnIntensityChange(InstrumentFamily.IntensityType type, bool fromConducting)
    {
        m_CurrentOrchestraState.intensityType = type;
        UIIntensityToast.Draw(m_CurrentOrchestraState.intensityType, m_CurrentRules.intensityType, 
            m_CurrentTrackType == GuidedModeManager.TrackType.Transition, fromConducting);
        
        if (m_CurrentOrchestraState.intensityType == m_CurrentRules.intensityType) {
            OnGoodIntensityChange?.Invoke();
        }
    }
    
    public bool IsArticulationGood()
    {
        return (m_CurrentOrchestraState.articulationType == m_CurrentRules.articulationType);
    }
    public bool IsTempoGood()
    {
        return (m_CurrentOrchestraState.tempoType == m_CurrentRules.tempoType);
    }
    
    public bool IsIntensityGood()
    {
        return (m_CurrentOrchestraState.intensityType == m_CurrentRules.intensityType);
    }
    
    
    public event Action OnGoodArticulationChange;
    public event Action OnGoodTempoChange;
    public event Action OnGoodIntensityChange;

    public void DrawRules()
    {
        bool isTransition = m_CurrentTrackType == GuidedModeManager.TrackType.Transition;
        UIArticulationToast.Draw(m_CurrentOrchestraState.articulationType, m_CurrentRules.articulationType, isTransition);
        UITempoToast.Draw(m_CurrentOrchestraState.tempoType, m_CurrentRules.tempoType, tempoManager.bpm, isTransition);
        UIIntensityToast.Draw(m_CurrentOrchestraState.intensityType, m_CurrentRules.intensityType, isTransition);
    }

    public void ShowRules(bool show)
    {
        UIArticulationToast.Show(show);
        UITempoToast.Show(show);
        UIIntensityToast.Show(show);
    }
}

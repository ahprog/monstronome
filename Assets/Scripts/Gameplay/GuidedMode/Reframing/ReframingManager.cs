﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * Manages the instruments failures and the reframing phases
 */
public class ReframingManager : MonoBehaviour
{
    [Header("Reframing")]
    public SoundEngineTuner soundEngineTuner;

    [Header("Degradation")]
    public Timeline timeline;
    public TempoManager tempoManager;
    public ReframingParametersScriptableObject reframingParameters;
    
    [Header("Score")]
    public ScoreManager scoreManager;
    public ScoringParametersScriptableObject scoringParameters;
    public InstrumentFamilySelector instrumentFamilySelector;
    private float m_ScoreTimer;

    [Header("SFX")]
    public AK.Wwise.Event SFXOnFamilyDegradation;
    public AK.Wwise.Event SFXOnReframingSuccess;
    public AK.Wwise.Event SFXOnPotionRight;
    public AK.Wwise.Event SFXOnPotionWrong;
    public AK.Wwise.Event SFXOnTransitionBeforeReframing;
    
    private InstrumentFamily[] m_InstrumentFamilies;
    private InstrumentFamily m_ReframingFamily;
    
    
    private bool m_CanPickNewFamily = false;
    private bool m_CanCheckPotionType = false;
    private bool m_IsDegrading = false;
    private bool m_CanDegrade = false;
    private bool m_IsBlock = false;
    
    public enum DegradationState
    {
        Left_0,
        Left_1,
        Left_2,
        Left_3
    }
    private DegradationState m_CurrentDegradationState;

    public struct ReframingRules
    {
        public ReframingPotion.ReframingPotionType[] rules;
        public ReframingRules(ReframingPotion.ReframingPotionType[] _rules)
        {
            rules = _rules;
        }
    }
    private ReframingRules m_CurrentReframingRules;
    private int m_ReframingPotionIndex = 0;
    
    /* Score timer */
    private float m_TimeSinceLastScore = 0.0f;

    public void LoadFamilies(InstrumentFamily[] families)
    {
        m_InstrumentFamilies = families;
    }

    public void InitStart()
    {
        PickNewReframingFamily(true);
    }

    public void Check(bool isBlock)
    {
        CheckLaunchFail(isBlock);
        UpdateScore(isBlock);
    }
    
    public void UpdateScore(bool setScore)
    {
        if (setScore) {
            m_TimeSinceLastScore += Time.deltaTime;
            if (m_TimeSinceLastScore > scoringParameters.checkPerSeconds) {

                if (m_IsDegrading) {
                    if (instrumentFamilySelector.selectedFamily == m_ReframingFamily) {
                        scoreManager.AddScore(scoringParameters.isReframing);
                    }
                    else {
                        scoreManager.AddScore(scoringParameters.degradation);
                    }
                }

                m_TimeSinceLastScore %= scoringParameters.checkPerSeconds;
            }
        }
    }
    
    /* REFRAMING */

    public void CheckReframingPotionType(ReframingPotion potion, Collision other)
    {
        if (m_IsDegrading) {
            if (m_ReframingFamily.gameObject == other.gameObject) {
                if (m_CanCheckPotionType) {
                    if (m_CurrentReframingRules.rules[m_ReframingPotionIndex] == potion.type) {
                        m_ReframingFamily.drawableReframingRules.HighlightRule(m_ReframingPotionIndex, m_ReframingFamily.drawableReframingRules.greenMaterial);

                        SoundEngineTuner.SetSwitchPotionBonusMalus(SoundEngineTuner.SFXPotionScoreType.Bonus, potion.gameObject);
                        SFXOnPotionRight.Post(potion.gameObject);

                        if ((int) m_CurrentDegradationState > 1) {
                            //There are still rules to process
                            m_CurrentDegradationState -= 1;
                            m_ReframingPotionIndex += 1;
                            UpdateDegradation(m_CurrentDegradationState);
                        }
                        else {
                            //Success
                            m_ReframingPotionIndex = 0;
                            UpdateDegradation(DegradationState.Left_0);
                            StartCoroutine(OnSuccessCoroutine());
                        }
                    }
                    else {
                        //Failure
                        SoundEngineTuner.SetSwitchPotionBonusMalus(SoundEngineTuner.SFXPotionScoreType.Malus, potion.gameObject);
                        SFXOnPotionWrong.Post(potion.gameObject);

                        m_ReframingPotionIndex = 0;
                        UpdateDegradation(DegradationState.Left_3);
                        StartCoroutine(OnFailureCoroutine());
                    }
                }
            }
        }
    }

    private IEnumerator BlinkAnimation(Material mat1, Material mat2)
    {
        for (int repeat = 0; repeat < 4; ++repeat) {
            for (int i = 0; i < m_CurrentReframingRules.rules.Length; ++i) {
                m_ReframingFamily.drawableReframingRules.HighlightRule(i, mat1);
            }
            yield return new WaitForSeconds(0.2f);
        
            for (int i = 0; i < m_CurrentReframingRules.rules.Length; ++i) {
                m_ReframingFamily.drawableReframingRules.HighlightRule(i, mat2);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    public event Action OnSuccess;
    
    private IEnumerator OnSuccessCoroutine()
    {
        OnSuccess?.Invoke();
        scoreManager.AddScore(scoringParameters.reframingSuccess);
        scoreManager.SuccessReframing(Time.time - m_ScoreTimer);
        SFXOnReframingSuccess.Post(m_ReframingFamily.gameObject);
        
        m_CanCheckPotionType = false;
        yield return BlinkAnimation(m_ReframingFamily.drawableReframingRules.greenMaterial, m_ReframingFamily.drawableReframingRules.yellowMaterial);
        
        m_ReframingFamily.drawableReframingRules.ResetColors();
        m_CanCheckPotionType = true;
        OnEndReframing();
    }
    
    private IEnumerator OnFailureCoroutine()
    {
        m_CanCheckPotionType = false;
        yield return BlinkAnimation(m_ReframingFamily.drawableReframingRules.redMaterial, m_ReframingFamily.drawableReframingRules.blackMaterial);

        m_ReframingFamily.drawableReframingRules.ResetColors();
        
        m_CurrentReframingRules = GenerateRandomReframingRules();
        m_ReframingFamily.drawableReframingRules.DrawReframingRule(m_CurrentReframingRules);
        m_CanCheckPotionType = true;
    }
    
    private void OnEndReframing()
    {
        m_IsDegrading = false;
        m_ReframingFamily.OnExitDegradation();
        m_ReframingFamily.drawableReframingRules.Show(false);
        PickNewReframingFamily();
        StartCoroutine(WaitCanDegrade(reframingParameters.timeBetweenFails));
    }

    private ReframingRules GenerateRandomReframingRules()
    {
        ReframingPotion.ReframingPotionType[] rules = new ReframingPotion.ReframingPotionType[3];
        List<ReframingPotion.ReframingPotionType> possibilities = new List<ReframingPotion.ReframingPotionType>();
        foreach (ReframingPotion.ReframingPotionType possibility in Enum.GetValues(typeof(ReframingPotion.ReframingPotionType))) {
            possibilities.Add(possibility);
        }
        
        //First potion
        int randomIndex = Random.Range(0, possibilities.Count);
        rules[0] = possibilities[randomIndex];
        possibilities.RemoveAt(randomIndex);
        
        //Second potion
        randomIndex = Random.Range(0, possibilities.Count);
        rules[1] = possibilities[randomIndex];
        possibilities.RemoveAt(randomIndex);
        
        //Third potion
        randomIndex = Random.Range(0, possibilities.Count);
        rules[2] = possibilities[randomIndex];

        return new ReframingRules(rules);
    }

    private void UpdateDegradation(DegradationState state)
    {
        m_CurrentDegradationState = state;
        soundEngineTuner.SetDegradation(m_CurrentDegradationState);
        m_ReframingFamily.SetBrokenAnimation(m_CurrentDegradationState);
    }
    
    
    /* LAUNCH FAILS */
    
    //Check and launch a fail if it is possible
    public void CheckLaunchFail(bool isBlock)
    {
        if (isBlock) {
            if (!m_IsBlock) {
                m_IsBlock = true;
                OnEnterBlock();
            }
            else {
                if (m_CanDegrade) {
                    float secondsUntilNextStep = timeline.GetBeatsUntilNextStep() * (60.0f / tempoManager.bpm);
                    if (secondsUntilNextStep > reframingParameters.minTimeUntilBlockEnd) {
                        LaunchFail();
                    }
                }
            }
        }
        else {
            if (m_IsBlock) {
                m_IsBlock = false;
                OnExitBlock();
            }
        }
    }

    IEnumerator WaitCanDegrade(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        m_CanDegrade = true;
    }
    
    public void LaunchFail()
    {
        m_CanDegrade = false;
        m_IsDegrading = true;
        m_CanCheckPotionType = true;
        
        //We start the reframing family degradation
        m_ReframingFamily.OnEnterDegradation();
        SFXOnFamilyDegradation.Post(m_ReframingFamily.gameObject);
        UpdateDegradation(DegradationState.Left_3);

        m_ScoreTimer = Time.time;

        m_CurrentReframingRules = GenerateRandomReframingRules();
        
        m_ReframingFamily.drawableReframingRules.ResetColors();
        m_ReframingFamily.drawableReframingRules.Show(true);
        m_ReframingFamily.drawableReframingRules.DrawReframingRule(m_CurrentReframingRules);
    }

    private void PickNewReframingFamily(bool force = false)
    {
        if (m_CanPickNewFamily || force) {
            //We pick the family which will fail
            int pick = Random.Range(0, m_InstrumentFamilies.Length);
            m_ReframingFamily = m_InstrumentFamilies[pick];
            soundEngineTuner.SetSolistFamily(m_ReframingFamily);
        }
        else {
            //We can't pick a random family when we enter the first block - it should be set before
            m_CanPickNewFamily = true;
        }
    }

    public void SetReframingFamily(InstrumentFamily family)
    {
        m_ReframingFamily = family;
        soundEngineTuner.SetSolistFamily(m_ReframingFamily);
    }
    
    private void OnEnterBlock()
    {
        PickNewReframingFamily();
        m_CanDegrade = false;
        StartCoroutine(WaitCanDegrade(Random.Range(reframingParameters.minTimeFirstFail, reframingParameters.maxTimeFirstFail)));
    }

    private void OnExitBlock()
    {
        if (m_IsDegrading) {
            //If there was still a degradation when the block ended
            SFXOnTransitionBeforeReframing.Post(m_ReframingFamily.gameObject);

            scoreManager.FailReframing();
        }
        
        m_CanDegrade = false;
        m_IsDegrading = false;
        m_ReframingFamily.OnExitDegradation();
        m_ReframingPotionIndex = 0;
        m_CanCheckPotionType = true;
        
        //We reset the degradation
        UpdateDegradation(DegradationState.Left_0);
        m_ReframingFamily.drawableReframingRules.Show(false);
    }
}

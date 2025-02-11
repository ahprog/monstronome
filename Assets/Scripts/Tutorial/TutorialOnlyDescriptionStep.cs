﻿using System.Collections;
using System.Collections.Generic;
using AK.Wwise;
using TMPro;
using UnityEngine;

/**
 * A step in a TutorialSequence with an audio + subtitles description
 */
public class TutorialOnlyDescriptionStep : TutorialDescriptionStep
{
    public TutorialOnlyDescriptionStep(TutorialSequence sequence, Instruction instruction, TextMeshPro subtitlesDisplay,
        GameObject voiceReference)
        : base(sequence, instruction, subtitlesDisplay, voiceReference)
    { }

    protected override IEnumerator Launch(MonoBehaviour coroutineHandler)
    {
        coroutineHandler.StartCoroutine(base.Launch(coroutineHandler));

        if (m_HasSucceeded) yield break;
        m_Instruction.mainInstruction.SFXVoice.Post(m_VoiceReference, (uint)AkCallbackType.AK_EndOfEvent | (uint)AkCallbackType.AK_Marker, InstructionVoiceCallback);
        m_CurrentVoice = m_Instruction.mainInstruction;
        m_SubtitleIndex = 0;
        m_SubtitlesDisplay.text = m_CurrentVoice.subtitles[m_SubtitleIndex];
        m_IsSpeaking = true;

        while (m_IsSpeaking) {
            yield return null;
        }
        
        yield return new WaitForSeconds(2f);
        
        OnSuccess();
    }
}
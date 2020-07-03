﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

public class WwiseCallBack : MonoBehaviour
{
    public SoundEngineTuner soundEngineTuner;
    private string m_MusicCueName;
    public AK.Wwise.Event voice;

    public void StopMusic()
    {
        AkSoundEngine.StopAll();
    }

    public void LoadTuning()
    {
        AkSoundEngine.PostEvent("Music_Tuning", gameObject);
    }

    public void StartOrchestra()
    {
        AkSoundEngine.SetState("Music", "Start");
    }
    
    public void LoadOrchestra()
    {
        AkSoundEngine.PostEvent("Play_Music", gameObject, (uint)AkCallbackType.AK_MusicSyncUserCue | (uint)AkCallbackType.AK_MusicSyncBeat, CallbackFunction, this);
        AkSoundEngine.SetState("Music", "Metronome");
        AkSoundEngine.SetState("PotionCount", "Left_0");   // Nombre de potions restantes que le joueur doit lancer pour corriger la famille
        AkSoundEngine.SetSwitch("SW_Family_Solist", "Woods", gameObject);  //Famille soliste qui devra être recaller
        soundEngineTuner.SetTempo(SoundEngineTuner.START_TEMPO);
    }

    void CallbackFunction(object in_cookie, AkCallbackType in_type, object in_info)
    {

        if (in_type == AkCallbackType.AK_MusicSyncUserCue)                                       // Se déclenche a chaque custom cue placé dans manuellement dans la musique sur Wwise.
        {
            AkMusicSyncCallbackInfo musicInfo = in_info as AkMusicSyncCallbackInfo;
            m_MusicCueName = musicInfo.userCueName;
            Debug.Log(m_MusicCueName);                                                               // Permet de déclencher des actions selon le noms du cue placé dans la musique sur Wwise.

            if (in_type == AkCallbackType.AK_MusicSyncBeat)                                                // Permet de déclencher des actions a chaque battements
            {
                Debug.Log("BEAT - WWise");
            }
            
            OnCue?.Invoke(m_MusicCueName);
        }
    }

    
    void MarkerCallback(object in_cookie, AkCallbackType in_type, object in_info)
    {
        if (in_type == AkCallbackType.AK_Marker)
        { 
            print("Antonin<3");                                        //Appel a chaque fois que le cursur detecte un marqueur 
            AkMarkerCallbackInfo info = (AkMarkerCallbackInfo)in_info;
            //print(info.strLabel);                                    //J'ai mis la fonction pour avoir le nom du marqueur au cas ou pour l'instant j'ai rien mis dans wwise
        }
    }

    public void LoadVoice()
    {
        voice.Post(gameObject, (uint)AkCallbackType.AK_Marker, MarkerCallback,this);
    }
    public event Action<string> OnCue;
}
 
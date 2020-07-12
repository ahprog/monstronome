﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Launch behaviors based on orchestra events when the player is in free mode
 */
public class FreeModeManager : MonoBehaviour
{
    [Header("Callbacks")]
    public WwiseCallBack wwiseCallback;
    public TempoManager tempoManager;
    public ArticulationManager articulationManager;
    public IntensityManager intensityManager;
    public OrchestraLauncher orchestraLauncher;
    public InstrumentFamily[] families = new InstrumentFamily[4];
    
    private void Start()
    {
        orchestraLauncher.InitLauncher(families);

        foreach (InstrumentFamily family in families) {
            OnStartOrchestra += family.StartPlaying;
            orchestraLauncher.OnLoadOrchestra += family.StopPlaying;
        }
        
        OnStartOrchestra += tempoManager.OnStartOrchestra;
        OnStartOrchestra += intensityManager.OnStartOrchestra;
        
        wwiseCallback.OnCue += LaunchState;
    }
    
    private void LaunchState(string stateName)
    {
        switch (stateName) {
            case "Start":
                StartOrchestra();
                break;
        }
    }

    private void StartOrchestra()
    {
        OnStartOrchestra?.Invoke();
        articulationManager.SetArticulation(InstrumentFamily.ArticulationType.Pizzicato);
    }
    
    private Action OnStartOrchestra;
}

﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/**
 * A slider that can be grabbed by a XRGrabber
 * Computes a value between 0 and 1
 */
public class XRSlider : XRGrabbable
{
    public Transform min;
    public Transform max;
    
    private float m_Value;
    public float value
    {
        get
        {
            return m_Value;
        }
    }

    private Vector3 m_CachedMin;
    private Vector3 m_CachedMax;

    protected override void Start()
    {
        base.Start();
        m_Value = 0.0f;
        m_CachedMin = min.localPosition;
        m_CachedMax = max.localPosition;
    }
    
    public override void OnUpdateGrab(XRGrabber xrGrabber)
    {
        Vector3 nextPos = transform.InverseTransformPoint(xrGrabber.transform.position);
        Debug.Log(nextPos.x);
        
        if (nextPos.x > m_CachedMax.x) {
            nextPos = m_CachedMax;
        }
        else if (nextPos.x < m_CachedMin.x){
            nextPos = m_CachedMin;
        }
        
        transform.localPosition = new Vector3(nextPos.x, transform.localPosition.y, transform.localPosition.z);
        m_Value = Mathf.InverseLerp(m_CachedMin.x, m_CachedMax.x, transform.localPosition.x);
    }
}

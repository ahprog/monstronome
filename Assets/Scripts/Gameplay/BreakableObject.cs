﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Represents an object that can be breaked
 * 
 */
[RequireComponent(typeof(Rigidbody))]
public class BreakableObject : MonoBehaviour
{
    [Header("Breakable Object")]
    public Transform defaultObject;
    public Transform breakedObject;
    public float speedUntilBreak = 4.0f;
    private bool m_HasBroken = false;
    public bool canBreak;

    [Header("Sound")]
    public AK.Wwise.Event SFXOnObjectBreak;
    public AK.Wwise.Event SFXOnObjectCollision;

    protected Rigidbody m_Rigidbody;
    private Rigidbody[] m_RigidbodyPieces;
    private Collider[] m_Colliders;
    private ParticleSystem[] m_ParticleSystems;

    private void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_RigidbodyPieces = breakedObject.GetComponentsInChildren<Rigidbody>();
        m_ParticleSystems = GetComponentsInChildren<ParticleSystem>();
        m_Colliders = GetComponents<Collider>();
    }

    protected virtual void Start()
    {}

    private void OnCollisionEnter(Collision other)
    {
        if (m_HasBroken || !canBreak) return;
        
        float speed = other.relativeVelocity.magnitude;
        if (speed > speedUntilBreak) {
            breakedObject.gameObject.SetActive(true);
            
            //Used to set wwise switches and rtpcs on potion break
            OnBreak(other);
            
            float explosionForce = speed * 20f;
            foreach (Rigidbody rb in m_RigidbodyPieces) {
                rb.AddForce(((rb.position - transform.position) * explosionForce) + m_Rigidbody.velocity * 5f);
            }
            m_Rigidbody.isKinematic = true;
            
            foreach (ParticleSystem ps in m_ParticleSystems) {
                ps.gameObject.SetActive(true);
                ps.Play();
            }
            
            foreach (Collider co in m_Colliders) {
                co.enabled = false;
            }
            
            SFXOnObjectBreak.Post(gameObject);
            
            m_HasBroken = true;
            Destroy(defaultObject.gameObject);
            Destroy(this);
            Destroy(this.GetComponent<XRGrabbable>());
            Destroy(this.gameObject, 4.0f);
        }
        else {
            OnCollisionSFX(other);
        }
    }

    protected virtual void OnBreak(Collision other)
    { }

    protected virtual void OnCollisionSFX(Collision other)
    {
        if (m_Rigidbody.velocity.magnitude > 0.25f) {
            SFXOnObjectCollision.Post(gameObject);
        }
    }

    /* WIND FORCE */
    private bool m_FlagWind;
    public void ApplyWind(Vector3 windDirection)
    {
        m_FlagWind = true;
        StartCoroutine(ApplyWindCoroutine(windDirection));
    }

    public void DisableWind()
    {
        m_FlagWind = false;
    }

    private IEnumerator ApplyWindCoroutine(Vector3 windDirection)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        while (m_FlagWind) {
            rb.velocity = windDirection;
            yield return new WaitForSeconds(Time.fixedDeltaTime);
        }
    } 
}

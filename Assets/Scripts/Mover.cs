using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{
    public Transform target;
    public float speed = 2.0f;
    public bool pingPong = true;

    private bool m_MovingToTarget = true;
    private Vector3 m_InitialPos;

    private void Awake() => m_InitialPos = transform.position;

    private void Update()
    {
        if (!target) return;

        var currTarget = m_MovingToTarget ? target.transform.position : m_InitialPos;
        transform.position = Vector3.MoveTowards(transform.position, currTarget, Time.deltaTime * speed);
        if (pingPong && transform.position == currTarget) m_MovingToTarget = !m_MovingToTarget;
    }
}

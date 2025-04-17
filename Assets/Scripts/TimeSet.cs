using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSet : MonoBehaviour
{
    [Range(0.1f, 100f)]
    public float gameSpeed = 1f;

    void Update()
    {
        Time.timeScale = gameSpeed;
    }
}

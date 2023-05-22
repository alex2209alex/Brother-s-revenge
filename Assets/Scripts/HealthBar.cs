using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private  Player player; 
    public Slider slider;

    private void OnEnable()
    {
        player.HpUpdate += SetHealth;
    }
    
    private void OnDisable()
    {
        player.HpUpdate -= SetHealth;
    }


    void Start()
    {
        SetHealth();
    }
    
    /*public void SetMaxHealth(double health)
    {
        slider.maxValue = (float)health;
        slider.value = (float)health;
    }*/

    private void SetHealth()
    {
        slider.value = (float)player.CurrentHP;
    }
    
}

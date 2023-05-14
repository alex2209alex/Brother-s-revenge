using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private  PlayerHP playerHP; 
    public Slider slider;

    private void OnEnable()
    {
        playerHP.OnDamageTaken += SetHealth;
    }
    
    private void OnDisable()
    {
        playerHP.OnDamageTaken -= SetHealth;
    }


    void Start()
    {
        SetHealth();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void SetMaxHealth(double health)
    {
        slider.maxValue = (float)health;
        slider.value = (float)health;
    }

    public void SetHealth()
    {
        slider.value = (float)playerHP.currentHP;
    }
    
}

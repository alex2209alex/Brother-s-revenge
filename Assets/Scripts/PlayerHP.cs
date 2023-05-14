using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHP : MonoBehaviour
{
    public double currentHP;
    private double maxHP = 100;
    public event Action OnDamageTaken; 


    // Start is called before the first frame update
    void Start()
    {
        currentHP = maxHP;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDamage(int amount) 
    {
        currentHP -= amount;

        if(currentHP <= 0)
        {
            Destroy(gameObject);
        }
        OnDamageTaken?.Invoke();
    }
}

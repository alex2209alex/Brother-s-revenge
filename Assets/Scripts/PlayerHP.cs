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
    public float armor;
    public event Action HpUpdate;

    public double CurrentHP
    {
        get => currentHP;
        set
        {
            if (currentHP + value > 100)
                currentHP = maxHP;
            else
                currentHP += value;
            HpUpdate?.Invoke();
        }
    }

    public float Armor
    {
        get => armor;
        set => armor = value;
    }


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
        currentHP -= amount * (1 -  (0.5 * armor / 100));

        if(currentHP <= 0)
        {
            Destroy(gameObject);
        }
        HpUpdate?.Invoke();
    }
}

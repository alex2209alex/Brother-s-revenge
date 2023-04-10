using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHP : MonoBehaviour
{
    public double currentHP;
    private double maxHP = 10;

    // Start is called before the first frame update
    void Start()
    {
        currentHP = maxHP;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(double amount) 
    {
        currentHP -= amount;

        if(currentHP <= 0)
        {
            Destroy(gameObject);
        }
    }
}

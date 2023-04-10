using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMeleeAttack : MonoBehaviour
{
    public float visionDistance;
    public float attackDistance;
    public float damage = 1;

    public PlayerHP playerHP;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            playerHP.TakeDamage(damage);
        }
    }
}

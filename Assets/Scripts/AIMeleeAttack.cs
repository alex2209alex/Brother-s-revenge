using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMeleeAttack : MonoBehaviour
{
    public float visionDistance;
    public float attackDistance;
    public float damage = 1;
    private float lastAttackTime;
    public float attackCooldown;

    public PlayerHP playerHP;

    // Start is called before the first frame update
    void Start()
    {
        lastAttackTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        if(Time.time - lastAttackTime < attackCooldown)
        {
            return;
        }
        if(collision.gameObject.CompareTag("Player"))
        {
            playerHP.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }
}

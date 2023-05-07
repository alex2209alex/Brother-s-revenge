using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class Player : MonoBehaviour
{
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator animator;
    [SerializeField] private double maxHP;
    [SerializeField] private double currentHP;
    [SerializeField] private double meleeAttackRange;
    private double rangedAttackRange;
    [SerializeField] private double meleeDamage;
    private double rangedDamage;
    private float attackCooldown;
    private float lastAttackTime;
    private bool isAttackRanged;
    [SerializeField] private float movementSpeed;

    // Start is called before the first frame update
    void Start()
    {
        maxHP = 10;
        currentHP = maxHP;
        meleeAttackRange = 2;
        rangedAttackRange = 0;
        meleeDamage = 1;
        rangedDamage = 0;
        attackCooldown = 5;
        lastAttackTime = Time.time - attackCooldown;
        isAttackRanged = false;
        movementSpeed = 8;
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        animator = GetComponent<Animator>();
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
            gameObject.SetActive(false);
        }
    }

    public void Attack(Enemy enemy)
    {
        if(isAttackRanged)
        {
            RangedAttack(enemy);
        }
        else
        {
            MeleeAttack(enemy);
        }
    }

    private void RangedAttack(Enemy enemy)
    {
        // TO DO
    }

    private void MeleeAttack(Enemy enemy)
    {
        double distance = Vector2.Distance(transform.position, enemy.transform.position);
        if(distance <= meleeAttackRange && attackCooldown <= Time.time - lastAttackTime)
        {
            enemy.TakeDamage(meleeDamage);
            lastAttackTime = Time.time;
        }    
    }

    private void OnMovement(InputValue value)
    {
        movement = value.Get<Vector2>();
        if(movement.x != 0 || movement.y != 0)
        {
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
            animator.SetBool("IsWalking", true);
        }
        else
        {
            animator.SetBool("IsWalking", false);
        }
    }

    private void FixedUpdate()
    {
        if(movement.x != 0 || movement.y != 0)
        {
            rb.velocity = movement * movementSpeed;
        }
    }
}

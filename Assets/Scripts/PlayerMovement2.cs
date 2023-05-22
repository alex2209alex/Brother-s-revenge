using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour {

    [SerializeField] private float speed;
    [SerializeField] private float turnSpeed; // noua variabila pentru viteza de rotatie
    private Vector2 movement;
    private Rigidbody2D rb;
    private Animator animator;
    private Quaternion targetRotation; // noua variabila pentru rotatia tinta

    private void Awake() {
        speed = 8f;
        turnSpeed = 10f; // setam viteza de rotatie
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        animator = GetComponent<Animator>();
    }   

    private void OnMovement(InputValue value) {
        movement = value.Get<Vector2>();
        if(movement.x !=0 || movement.y !=0) {
            animator.SetFloat("X", movement.x);
            animator.SetFloat("Y", movement.y);
            animator.SetBool("IsWalking", true);

            targetRotation = Quaternion.LookRotation(Vector3.forward, movement); // calculam rotatia tinta
        }
        else {
            animator.SetBool("IsWalking", false);
        }
    }

    private void FixedUpdate() {
        if(movement.x !=0 || movement.y !=0) {
            rb.velocity = movement * speed;

            // utilizam slerp pentru a face o tranzitie lina intre rotatia curenta si rotatia tinta
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
    }
}

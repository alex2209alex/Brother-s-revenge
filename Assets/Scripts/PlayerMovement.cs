using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour{

    [SerializeField] private float speed;
    private Vector2 movement;
    private Rigidbody2D rb;
    private void Awake(){
        speed = 8f;
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnMovement(InputValue value){
        movement = value.Get<Vector2>();

    }
    private void FixedUpdate(){
        if(movement.x !=0 || movement.y !=0){
            rb.velocity = movement * speed;
        }
    }
}

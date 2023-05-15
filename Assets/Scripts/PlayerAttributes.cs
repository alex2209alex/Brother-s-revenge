using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttributes : MonoBehaviour
{

    [SerializeField] private PlayerHP playerHp;

    [SerializeField] private PlayerMovement playerMovement;

    [SerializeField] private Collector collector;

    public PlayerHP PlayerHp
    {
        get => playerHp;
    }

    public PlayerMovement PlayerMovement
    {
        get => playerMovement;
    }

    public Collector Collector
    {
        get => collector;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

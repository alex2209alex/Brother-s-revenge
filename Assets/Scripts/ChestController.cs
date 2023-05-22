using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestController : MonoBehaviour
{
    public bool isOpen;
    public float stateOfChest;
    public Animator animator;
    public bool IsOpen
    {
        get { return isOpen; }
        set { isOpen = value; }
    }
    public List<Item> items = new List<Item>();
    public List<Item> Items
    {
        get { return items; }
        set { items = value; }
    }
    public int numberOfItems
    {
        get { return items.Count; }
    }

    public void AddItem(Item item)
    {
        items.Add(item);
    }
    //stateOfChest Legend:
    //0 Loot closed
    //1 Loot open
    //2 Empty closed
    //3 Empty open
    public void InteractWithChest()
    {
        if(stateOfChest == 0) //closed loot
            {
                animator.SetTrigger("OpenLoot");
                //set state of chest 1 from animator
                animator.SetFloat("stateOfChest", 1);
                stateOfChest = 1;
            }
        else if(stateOfChest == 1) //opened loot
            {
                
                animator.SetTrigger("CloseLoot");
                animator.SetFloat("stateOfChest", 0);
                stateOfChest = 0;
            }
        if(stateOfChest == 2) //closed empty
            {
                animator.SetTrigger("OpenEmpty");
                animator.SetFloat("stateOfChest", 3);
                stateOfChest = 3;
            }
        else if(stateOfChest == 3) //opened empty
            {
                animator.SetTrigger("CloseEmpty");
                animator.SetFloat("stateOfChest", 2);
                stateOfChest = 2;
            }
    }
    public void interactWithItems()
    {
        if(stateOfChest == 1) //opened loot
            {
                //add items to inventory
                
                //delete items from chest
                items.Clear();
                //set state of chest 2 from animator
                animator.SetFloat("stateOfChest", 2);
                stateOfChest = 2;
            }
        else if(stateOfChest == 3) //opened empty
            {
                //set state of chest 0 from animator
                animator.SetFloat("stateOfChest", 0);
                stateOfChest = 0;
            }
    }
    // Start is called before the first frame update
    void Start()
    {
        // animator = GetComponent<Animator>();
        stateOfChest = 0;
        //add some weapons to chest
        // Weapon weapon1 = new Weapon();
        // AddItem(weapon1);
        //ideea e ca aici am pus un item ca chestul sa aiba animatia cu cufarul plin
        //daca chestul e gol, atunci chestul apare chel
        // set state of chest depending on if it has items or not
        if(numberOfItems > 0)
            {
                stateOfChest = 0;
            }
        else
            {
                stateOfChest = 2;
            }
    }
    void collectItems()
    {
        //delete all items in chest
        items.Clear();

    }
    // Update is called once per frame
    void Update()
    { 
    }
}

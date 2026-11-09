using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodMove : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (GameFacade.instance.GetIsGameOver()) return;
        transform.Translate(Vector3.forward * 20 * Time.deltaTime);
    }
}

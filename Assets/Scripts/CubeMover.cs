using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This is an example script to interface with the networkmanager script
 * It generates scriptable objects that are then converted into json, which can be sent over the network
 * This makes the system very extensible, by sneding arbitary SOs over the network that can be used for different events, such as an SO containing a ship id, a team id and a position to create a new ship
 */



[RequireComponent(typeof(Camera))]
public class CubeMover : MonoBehaviour
{
    public GameObject MyCube;
    public GameObject OtherCube;

    private Camera cam;

    public StringEvent positionEvent;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {        
        /*
         * If the player clicks, and they are over the plane, then move their cube to the selected position
         * Then, create a new scriptable object to hold that position, and convert it to json data
         * prepend a packet id to identify the correct scriptable object to decode it as
         * Send it out over the network
         */

        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 WorldPosition = hit.point;
                MyCube.transform.position = WorldPosition;

                CubePositionEvent cubeEvent = (CubePositionEvent)ScriptableObject.CreateInstance("CubePositionEvent");
                cubeEvent.Position = WorldPosition;
                string encoded = "cp|" + JsonUtility.ToJson(cubeEvent);
                positionEvent.Invoke(encoded);
            }            
        }
    }

    public void RecievePosition(string message)
    {
        /*
         * When a message is recieved from over the network, check if it starts with the correct packet id
         * If it does, then its a packet we want to listen to, so attempt to decode it into the correct scriptable object
         * Then use that scriptable object to move the cube position
         */
        if (message.StartsWith("cp|"))
        {
            message = message.Substring(3);
            CubePositionEvent createdEvent = (CubePositionEvent)ScriptableObject.CreateInstance("CubePositionEvent");
            JsonUtility.FromJsonOverwrite(message, createdEvent);
            OtherCube.transform.position = createdEvent.Position;
        }        
    }
}

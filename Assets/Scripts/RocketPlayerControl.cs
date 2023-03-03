using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketPlayerControll : MonoBehaviour
{
    [Header("Player Rocket Parameters")]
    [SerializeField] private bool playerControlled;
    [SerializeField] private float steeringSens;

    // Start is called before the first frame update
    void Start()
    {
        if (playerControlled)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(playerControlled)
        {
            RocketSteering();
        }
    }


    private void RocketSteering()
    {
        float mX = Input.GetAxis("Mouse X") * steeringSens * Time.deltaTime;
        float mY = Input.GetAxis("Mouse Y") * steeringSens * Time.deltaTime * -1;

        transform.Rotate(mY, mX, 0f);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0f);
    }
}

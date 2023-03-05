using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketPlayerControll : MonoBehaviour
{
    [Header("Rocket Parameters")]
    [SerializeField] private float steeringSens;


    // Update is called once per frame
    void Update()
    {
        float mX = Input.GetAxis("Mouse X") * steeringSens * Time.deltaTime;
        float mY = Input.GetAxis("Mouse Y") * steeringSens * Time.deltaTime;

        Vector3 target = transform.position + transform.forward * 10 + transform.right * mX + transform.up * mY;
        transform.GetComponent<RocketBehaviour>().RotationToTarget(target);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketPlayerControl : MonoBehaviour
{
    [Header("Rocket Parameters")]
    [SerializeField] private float steeringSens;

    private RocketBehaviour _rocketLogic;

    private void Start()
    {
        _rocketLogic = transform.GetComponent<RocketBehaviour>();
    }

    // Update is called once per frame
    void Update()
    {
        float mX = Input.GetAxis("Mouse X") * steeringSens * Time.deltaTime;
        float mY = Input.GetAxis("Mouse Y") * steeringSens * Time.deltaTime;

        Vector3 target = transform.position + transform.forward * 10 + transform.right * mX + transform.up * mY;
        _rocketLogic.RotationToTarget(target);
    }
}

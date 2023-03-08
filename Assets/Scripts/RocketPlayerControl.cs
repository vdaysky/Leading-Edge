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
        
        var cam = new GameObject("RocketCamera").AddComponent<Camera>();

        var camTransform = cam.transform;
        
        camTransform.SetParent(transform);
        camTransform.localPosition = new Vector3(0, 0, 0);
        camTransform.localRotation = Quaternion.identity;
        camTransform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        
        cam.rect = new Rect(0.75f, 0, 0.25f, 0.25f);
        cam.GetComponent<AudioListener>().enabled = false;
    }

    public void SetSteeringSens(float sens) {
        steeringSens = sens;
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

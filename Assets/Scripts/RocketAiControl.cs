using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketAiControl : MonoBehaviour
{
    [Header("Rocket Targets")]
    [SerializeField] private GameObject targetMain;

    private AirController _planeController;
    private RocketBehaviour _rocketLogic;

    //flare offset
    private float _aimOffsetVertical = 0f;
    private float _aimOffsetHorizontal = 0f;
    private bool _createNewOffset = true;

    private void Start()
    {
        _planeController = targetMain.GetComponent<AirController>();
        _rocketLogic = transform.GetComponent<RocketBehaviour>();
    }
    
    public void SetTarget(GameObject target) {
        targetMain = target;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target = targetMain.transform.position;
        if (_planeController.HasTriggeredFlares())
        {
            if (_createNewOffset)
            {
                _aimOffsetVertical = Random.Range(-100f, 100);
                _aimOffsetHorizontal = Random.Range(-100f, 100);
                _createNewOffset = false;
            }
            target += transform.up * _aimOffsetVertical + transform.right * _aimOffsetHorizontal;
        }
        else
        {
            _createNewOffset = true;
        }
        _rocketLogic.RotationToTarget(target);
    }
}
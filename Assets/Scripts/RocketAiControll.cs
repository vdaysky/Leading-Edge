using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketAiControll : MonoBehaviour
{
    [Header("Rocket Targets")]
    [SerializeField] private GameObject targetMain;
    [SerializeField] private GameObject targetSecondary;

    //flare offset
    private float aimOffsetVertical = 0f;
    private float aimOffsetHorizontal = 0f;
    private bool createNewOffset = true;

    // Update is called once per frame
    void Update()
    {
        Vector3 target = targetMain.transform.position;
        if (targetMain.GetComponent<AirController>().HasTriggeredFlares())
        {
            if (createNewOffset)
            {
                aimOffsetVertical = Random.Range(-100f, 100);
                aimOffsetHorizontal = Random.Range(-100f, 100);
                createNewOffset = false;
            }
            target += transform.up * aimOffsetVertical + transform.right * aimOffsetHorizontal;
        }
        else
        {
            createNewOffset = true;
        }
        transform.GetComponent<RocketBehaviour>().RotationToTarget(target);
    }
}
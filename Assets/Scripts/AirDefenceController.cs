using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using util;

public class AirDefenceController : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private GameObject rocketPrefab;
    [SerializeField] private float detectionRange;
    [SerializeField] private float detectionHeight;
    [SerializeField] private long scanCooldownMs;
    
    private readonly Recharge _recharge = new();
    
    private void FixedUpdate() {
        var distance = Vector3.Distance(transform.position, target.transform.position);
        
        // target out of range
        if (distance > detectionRange) {
            return;
        }

        if (!_recharge.TryAbility("Scan", scanCooldownMs)) {
            return;
        }

        var distanceDetectionFactor = 1f - distance / detectionRange;
        float heightDetectionFactor;
        
        var targetHigherBy = target.transform.position.y - transform.position.y;
        
        // the closer target is to the ground, the harder it is to detect
        if (targetHigherBy < 0) {
            heightDetectionFactor = 0.1f;
        } else if (targetHigherBy >= detectionHeight) {
            heightDetectionFactor = 0f;
        }
        else {
            heightDetectionFactor = 0.3f + (targetHigherBy / detectionHeight) * 0.7f;
        }
        
        var detectionProbability = distanceDetectionFactor * heightDetectionFactor;
        
        Debug.Log("Attempt, probability: " + detectionProbability);
        if (Random.Range(0f, 1f) < detectionProbability) {
            Debug.Log("launch");
            DetectTarget();
        }
    }

    private void DetectTarget() {
        var spawnPos = transform.position + transform.up * 10;
        var rocket = Instantiate(rocketPrefab, spawnPos, Quaternion.Euler(0, 90, 0));
        var rocketControl = rocket.GetComponent<RocketAiControl>();
        var rocketLogic = rocket.GetComponent<RocketBehaviour>();
        var humanControl = rocket.GetComponent<RocketPlayerControl>();
        var tagHolder = rocket.GetComponent<TagHolder>();
        
        tagHolder.RemoveTag(SharedTag.MainRocket);
        
        rocketLogic.SetSpeed(70);
        rocketLogic.SetMaxSpeed(90);
        
        humanControl.enabled = false;
        rocketControl.enabled = true;
        rocketControl.SetTarget(target);
    }
}

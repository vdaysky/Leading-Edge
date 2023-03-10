using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using util;

internal class PlanePart
{
    public Vector3 ParticleOffset;
    public float Health = 1f;
    public readonly List<ParticleSystem> BreakageParticles = new();
    public readonly List<Vector3> RandomOffsets = new();

    public float Breakage {
        get => Math.Max(0, Math.Min(1, 1 - Health));
        set => Health = Math.Max(0, Math.Min(1, 1 - value));
    }

    public PlanePart(Vector3 particleOffset)
    {
        ParticleOffset = particleOffset;
    }
}

public enum PlanePartType {
    LeftWing,
    RightWing,
    Tail,
    Engine,
    Cockpit,
}

internal class Plane : IEnumerable<PlanePart>
{
    private readonly Dictionary<PlanePartType, PlanePart> _parts = new();
    
    public PlanePart this[PlanePartType partType] {
        get => _parts[partType];
        set => _parts[partType] = value;
    }

    public void StopSmoke() {
        foreach (var part in this) {
            part.BreakageParticles.ForEach(p => p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear));
        }
    }

    public PlanePart LeftWing {
        get => _parts[PlanePartType.LeftWing];
        set => _parts[PlanePartType.LeftWing] = value;
    }
    
    public PlanePart RightWing {
        get => _parts[PlanePartType.RightWing];
        set => _parts[PlanePartType.RightWing] = value;
    }
    
    public PlanePart Tail {
        get => _parts[PlanePartType.Tail];
        set => _parts[PlanePartType.Tail] = value;
    }
    
    public PlanePart Engine {
        get => _parts[PlanePartType.Engine];
        set => _parts[PlanePartType.Engine] = value;
    }
    
    public PlanePart Cockpit {
        get => _parts[PlanePartType.Cockpit];
        set => _parts[PlanePartType.Cockpit] = value;
    }
    
    public Plane()
    {
        _parts.Add(PlanePartType.LeftWing, new(new Vector3(-2, 0, 0)));
        _parts.Add(PlanePartType.RightWing,  new(new Vector3(2, 0, 0)));
        _parts.Add(PlanePartType.Tail, new(new Vector3(0, 0, -2)));
        _parts.Add(PlanePartType.Engine, new(new Vector3(0, 0, 2)));
        _parts.Add(PlanePartType.Cockpit, new(new Vector3(0, 0, 0)));
    }

    public IEnumerator<PlanePart> GetEnumerator()
    {
        return _parts.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}




public class AirController : MonoBehaviour
{
    private enum CameraView {
        Back,
        Front
    }

    private enum GameState {
        Playing,
        OutOfControl,
        Destroyed
    }
    
    [SerializeField] private Rigidbody planeRigidBody;
    
    [SerializeField] private Camera mainCamera;
    
    [SerializeField] private GameObject rocketPrefab;
    
    [SerializeField] private ParticleSystem explosionEffect;
    
    [SerializeField] private ParticleSystem flaresEffect;
    
    [SerializeField] private ParticleSystem smokeEffect;
    
    [SerializeField] private TextMeshProUGUI speedText;
    
    [SerializeField] private TextMeshProUGUI torqueText;
    
    [SerializeField] private TextMeshProUGUI rotationText;
    
    [SerializeField] private TextMeshProUGUI breakageText;

    private CameraView _cameraView = CameraView.Back;
    private readonly Plane _plane = new();
    private GameState _state = GameState.Playing;
    
    private const float MaxSpeed = 52f;
    private const float SteeringVSens = 26;
    private const float SteeringHSens = 52;
    private const long FlaresCooldownMs = 10000;
    private const float EngineLiftFactor = 0.3f;
    private const float BrokenTailSlide = 1.2f;

    
    private int _rocketsLeftOnBoard = 5;
    private GameObject _rocket;
    private readonly Recharge _planeRecharge = new();

    private void Start()
    {
        planeRigidBody.maxDepenetrationVelocity = 0.01f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void AnimateLostControl() {
        planeRigidBody.AddRelativeTorque(new Vector3(10f, 10f, 60f), ForceMode.Force);
        mainCamera.transform.position = planeRigidBody.transform.position + new Vector3(0, 30, 0);
        
        Quaternion cameraLookDown = Quaternion.Euler(90, 0, 0);

        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, cameraLookDown, 0.125f);
        planeRigidBody.velocity = planeRigidBody.transform.forward * 140;
    }

    public void LooseControl() {
        var planeRot = planeRigidBody.transform.rotation;
        var x = UnityEngine.Random.Range(70f, 110f);
        var y = planeRot.eulerAngles.y;
        var z = planeRot.eulerAngles.z;
        planeRigidBody.transform.rotation = Quaternion.Euler(x, y, z);
        _state = GameState.OutOfControl;
    }

    public void Die() {
        
        if (_state == GameState.Destroyed) {
            return;
        }

        Instantiate(explosionEffect, planeRigidBody.transform.position, Quaternion.identity);
        _state = GameState.Destroyed;
        
        // hide the plane
        GetComponent<MeshRenderer>().enabled = false;
        
        // remove particles
        _plane.StopSmoke();
        
        // TODO: debris
    }

    private void DisplayPlanePartBreakage(PlanePart part)
    {
        var breakage = part.Breakage;

        int smokeSources;

        if (breakage > 0.7) {
            smokeSources = 3;
        } else if (breakage > 0.3) {
            smokeSources = 2;
        } else if (breakage > 0.1) {
            smokeSources = 1;
        } else {
            smokeSources = 0;
        }
        
        var planeTransform = planeRigidBody.transform;
        if (part.BreakageParticles.Count < smokeSources)
        {
            Vector3 planeOffset = part.ParticleOffset.x * planeTransform.right +
                                  part.ParticleOffset.y * planeTransform.up +
                                  part.ParticleOffset.z * planeTransform.forward;
            
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.3f, 0.3f),
                UnityEngine.Random.Range(-0.3f, 0.3f),
                UnityEngine.Random.Range(-0.3f, 0.3f)
            );
            
            Quaternion backwardLook = Quaternion.LookRotation(planeRigidBody.transform.forward * -1);
                
            var smokeItem = Instantiate(smokeEffect, planeRigidBody.transform.position + planeOffset, backwardLook);
            
            part.BreakageParticles.Add(smokeItem);
            part.RandomOffsets.Add(randomOffset);
        }
        
        // move particles with plane
        foreach (var particle in part.BreakageParticles)
        {
            Vector3 randomOffset = part.RandomOffsets[part.BreakageParticles.IndexOf(particle)];
            
            Vector3 planeOffset = (part.ParticleOffset.x + randomOffset.x) * planeTransform.right +
                                  (part.ParticleOffset.y + randomOffset.y) * planeTransform.up +
                                  (part.ParticleOffset.z + randomOffset.z) * planeTransform.forward;
            
            particle.transform.position = planeRigidBody.transform.position + planeOffset;
        }
    }
    
    private IEnumerable<PlanePartHitbox> GetHitPart(Collision collisionInfo) {
        foreach(Transform planeTransform in planeRigidBody.gameObject.transform)
        {   
            var planeChild = planeTransform.gameObject;
            
            var hitbox = planeChild.gameObject.GetComponent<PlanePartHitbox>();

            if (hitbox == null)
            {
                continue;
            }

            var colliderOfPlane = planeChild.GetComponent<Collider>();

            if (!planeChild.gameObject.activeSelf) {
                continue;
            }

            foreach (var contactPoint in collisionInfo.contacts)
            {
                var bounds = colliderOfPlane.bounds;
                var distanceToHit = bounds.center - contactPoint.point;
                var boundingBoxSize = bounds.size;
                
                if (!(Mathf.Abs(distanceToHit.x) < boundingBoxSize.x)) continue;
                if (!(Mathf.Abs(distanceToHit.y) < boundingBoxSize.y)) continue;
                if (!(Mathf.Abs(distanceToHit.z) < boundingBoxSize.z)) continue;
                
                yield return hitbox;
                break;
            }
        }
    }

    private void OnCollisionStay(Collision collisionInfo) {
        foreach (var hitbox in GetHitPart(collisionInfo)) {
            HandlePlanePartCollision(hitbox.type, collisionInfo);
        }
    }

    private void OnCollisionEnter(Collision collisionInfo)
    {
        var tags = collisionInfo.gameObject.GetComponent<TagHolder>();

        if (tags.HasTag(SharedTag.MainObjective)) {
            Debug.Log("Win");
            return;
        }

        if (_state == GameState.OutOfControl) {
            Die();
            return;
        }
        
        // here we only handle rocket damage
        if (!tags.HasTag(SharedTag.Rocket)) {
            return;
        }

        bool isMain = tags.HasTag(SharedTag.MainRocket);

        foreach (var hitbox in GetHitPart(collisionInfo)) {
            HandlePlanePartRocketHit(hitbox.type, isMain);
        }
    }

    private void HandlePlanePartRocketHit(PlanePartType type, bool isMain) {

        if (isMain) {
            LooseControl();
            return;
        }
        _plane[type].Breakage += 0.4f;
    }

    private void TryActivateFlares()
    {
        if (!_planeRecharge.TryAbility("Flares", FlaresCooldownMs)) {
            return;
        }
        
        var planeTransform = planeRigidBody.transform;
        var planePos = planeTransform.position;
        var right = planeTransform.right;
        var planeLeftWingPos = planePos + right * 2;
        var planeRightWingPos = planePos - right * 2;
                
        Instantiate(flaresEffect, planePos, planeRigidBody.rotation * Quaternion.Euler(15, 0, 0));
        Instantiate(flaresEffect, planePos, planeRigidBody.rotation * Quaternion.Euler(0, 30, 0));
        Instantiate(flaresEffect, planePos, planeRigidBody.rotation * Quaternion.Euler(0, -30, 0));
        Instantiate(flaresEffect, planeLeftWingPos, planeRigidBody.rotation * Quaternion.Euler(-15, 45, 0));
        Instantiate(flaresEffect, planeRightWingPos, planeRigidBody.rotation * Quaternion.Euler(-15, -45, 0));
    }

    private void TryLaunchRocket() {
        
        // prevent launching new rocket while old one is still flying
        if (_rocket != null && !_rocket.IsDestroyed()) {
            return;
        }

        _rocket = null;
        
        if (_rocketsLeftOnBoard <= 0) {
            return;
        }

        _rocketsLeftOnBoard -= 1;
        
        var planeTransform = planeRigidBody.transform;
        
        var spawnPos = planeTransform.position + planeTransform.up * -3 + planeTransform.forward * 10;
        _rocket = Instantiate(rocketPrefab, spawnPos, planeTransform.rotation);
        
        // set tag
        _rocket.gameObject.tag = "PlayerRocket";
        
        var rocketBase = _rocket.GetComponent<RocketBehaviour>();
        var playerControl = _rocket.GetComponent<RocketPlayerControl>();
        var aiControl = _rocket.GetComponent<RocketAiControl>();
        
        // rocket hit something
        rocketBase.OnRocketDestroyed += (collidedWith, _) => { 
            collidedWith.GetComponent<TagHolder>().HasTag(SharedTag.AirDefence).Then(()=>Destroy(collidedWith));
        };
        
        rocketBase.SetMaxSpeed(70);
        rocketBase.SetSpeed(planeRigidBody.velocity.magnitude);
        playerControl.SetSteeringSens(120);
        
        playerControl.enabled = true;
        aiControl.enabled = false;
    }

    private void Update()
    {
        if (_state == GameState.OutOfControl) {
            AnimateLostControl();
            return;
        }

        // change camera view
        if (Input.GetKeyDown(KeyCode.C)) {
            _cameraView = _cameraView == CameraView.Back ? CameraView.Front : CameraView.Back;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryActivateFlares();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TryLaunchRocket();
        }

        DisplayPlanePartBreakage(_plane[PlanePartType.LeftWing]);
        DisplayPlanePartBreakage(_plane[PlanePartType.RightWing]);
        DisplayPlanePartBreakage(_plane[PlanePartType.Tail]);
        DisplayPlanePartBreakage(_plane[PlanePartType.Engine]);
    }

    private void UpdateThrust()
    {
        Vector3 thrustVector;
        
        //Thrust change mostly for debug
        if (Input.GetButton("Jump"))
        {
            thrustVector = planeRigidBody.transform.forward * 10f;
        }
        else
        {
            thrustVector = planeRigidBody.transform.forward * (MaxSpeed);
        }

        breakageText.text = $"Left wing HP: {_plane[PlanePartType.LeftWing].Health}\n" +
                            $"Right wing HP: {_plane[PlanePartType.RightWing].Health}\n" +
                            $"Tail HP: {_plane[PlanePartType.Tail].Health}\n" +
                            $"Engine HP: {_plane[PlanePartType.Engine].Health}\n" +
                            $"Cockpit HP: {_plane[PlanePartType.Cockpit].Health}\n";
        
        // project plane vertical vector on world vertical vector
        Vector3 planeVerticalVector = planeRigidBody.transform.up;
        Vector3 worldVerticalVector = Vector3.up;
        Vector3 planeVerticalVectorProjected = Vector3.Project(planeVerticalVector, worldVerticalVector);
        float verticalSpeedCut = planeVerticalVectorProjected.magnitude;
        
        float engineFunction = _plane.Engine.Health;
        float liftWithoutEngine = 1 - EngineLiftFactor;
        float wingLiftFactor = _plane.LeftWing.Health * _plane.RightWing.Health;
        
        Vector3 fullLiftVector = -Physics.gravity * verticalSpeedCut;
        
        // compute lift vector depending on engine and wing health
        Vector3 liftVector = (fullLiftVector * (liftWithoutEngine * wingLiftFactor)) +
                             (fullLiftVector * (EngineLiftFactor * engineFunction));
        
        // I have no fucking idea why force has to be divided by two. Seems to work though
        Vector3 finalVelocity = thrustVector * planeRigidBody.mass / 2 + liftVector * planeRigidBody.mass;
        
        planeRigidBody.AddForce(finalVelocity, ForceMode.Force);

        speedText.text = $"Speed: {planeRigidBody.velocity.magnitude}\n" +
                         $"{planeRigidBody.velocity}\n" +
                         $"Lift: {verticalSpeedCut}";
    }

    //FixedUpdate is called zero, one or multipe times per frame
    private void FixedUpdate()
    {
        if (_state != GameState.Playing) {
            return;
        }
        PlaneSteering();
        UpdateThrust();
        CameraFollow();
    }

    private void HandlePlanePartCollision(PlanePartType planePart, Collision collision)
    {
        var other = collision.gameObject;
        
        if (_state != GameState.Playing) 
            return;
        
        if (other.CompareTag("WantedTarget"))
        {
            Die();
            // TODO:
            // Win, hook up to something here
            return;
        }
        
        // hitting your head hard is not pleasant by any means
        if (planePart == PlanePartType.Cockpit && _plane.Cockpit.Breakage > 0.3) {
            Die();
            // TODO:
            // Loose, hook up to something here
            return;
        }
        
        // damage plane part
        _plane[planePart].Breakage += 0.01f;
        
        // too broken to stay alive
        if (_plane.Sum(part => part.Breakage) >= 2) {
            Die();
            // TODO:
            // Loose, hook up to something here
        }
    }

    private void CameraFollow()
    {
        // set camera position
        var planeTransform = planeRigidBody.transform;
        var planeRotation = planeTransform.rotation;
        
        var distance = 30;
        const int height = 5;
        
        var view = Quaternion.Euler(0, 0, 0);
        if (_cameraView == CameraView.Front)
        {
            distance = -distance;
            view = Quaternion.Euler(0, 180, 0);
        }

        mainCamera.transform.position = planeTransform.position - planeTransform.forward * distance + planeTransform.up * height;
        
        // set camera rotation slowly over time to follow plane, but ignore roll
        Quaternion planeRotationFixed = Quaternion.Euler(
            planeRotation.eulerAngles.x, 
            planeRotation.eulerAngles.y, 
            0
        );
        planeRotationFixed *= view;
        mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, planeRotationFixed, 0.125f);
    }

    private void PlaneSteering()
    {
        rotationText.text = $"Rotation:\nUp: {Math.Round(planeRigidBody.rotation.eulerAngles.x, 2)}\n" +
                            $"Left: {Math.Round(planeRigidBody.rotation.eulerAngles.y, 2)}\n" +
                            $"Roll: {Math.Round(planeRigidBody.rotation.eulerAngles.z, 2)}";
        
        float vert = Input.GetAxisRaw("Vertical") * SteeringVSens;
        float hor = Input.GetAxisRaw("Horizontal") * SteeringHSens;
        float roll = Input.GetAxisRaw("Roll") * SteeringHSens;
        
        

        Vector3 targetTorque = new Vector3(-vert, hor, roll);
        
        // plane starts spinning horizontally a bit when tail is broken
        targetTorque += new Vector3(0, _plane.Tail.Breakage * vert * BrokenTailSlide, 0);

        float zTorque = (_plane.LeftWing.Health - _plane.RightWing.Health) * 10;
        float wingBreakageRoll = Math.Abs(_plane.LeftWing.Health - _plane.RightWing.Health);
        
        var signedRoll = planeRigidBody.transform.rotation.eulerAngles.z;
        var planeRoll = signedRoll > 180 ? 360 - signedRoll : signedRoll;
        
        if (planeRoll < wingBreakageRoll * 30) {
            targetTorque.z += zTorque;
        }
        
        planeRigidBody.AddRelativeTorque(targetTorque, ForceMode.Force);

        // get plane torque in local space
        Vector3 torque = planeRigidBody.transform.InverseTransformDirection(planeRigidBody.angularVelocity);
        torqueText.text = "Torque:\n" +
                          $"Up: {Math.Round(torque.x, 2)}\n" +
                          $"Left: {Math.Round(torque.y, 2)}\n" +
                          $"Roll: {Math.Round(torque.z, 2)}";
    }

    public bool HasTriggeredFlares()
    {
        return !_planeRecharge.IsRecharged("Flares");
    }
}

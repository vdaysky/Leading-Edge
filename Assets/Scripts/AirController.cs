using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using util;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

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
    
    [SerializeField] private TextMeshProUGUI altitudeText;
    
    [SerializeField] private Texture2D rocketIconTexture;
    
    [SerializeField] private Canvas canvas;
    
    [SerializeField] private GameObject crosshair;

    [SerializeField] private GameObject radar;

    [SerializeField] private RawImage leftWingIcon;
    
    [SerializeField] private RawImage rightWingIcon;
    
    [SerializeField] private RawImage tailIcon;
    
    [SerializeField] private RawImage engineIcon;
    
    [SerializeField] private RawImage cockpitIcon;

    [SerializeField] private RawImage planeIcon;

    [SerializeField] private Texture2D enemyIconTexture;

    [SerializeField] private Texture2D airDefenceIconTexture;

    
    [SerializeField] private Canvas inclineCanvas;
    
    [SerializeField] private Texture2D inclineTexture;

    [SerializeField] private TMP_FontAsset font;
    


    private CameraView _cameraView = CameraView.Back;
    private readonly Plane _plane = new();
    private GameState _state = GameState.Playing;
    private Dictionary<GameObject, GameObject> _objetcsAndIcons = new Dictionary<GameObject, GameObject>();

    private const float MaxSpeed = 52f;
    private const float SteeringVSens = 52;
    private const float SteeringHSens = 70;
    private const long FlaresCooldownMs = 10000;
    private const float EngineLiftFactor = 0.3f;
    private const float BrokenTailSlide = 1.2f;
    private const float RadarZoomIn = 0.1f;



    private int _rocketsLeftOnBoard = 5;
    private GameObject _rocket;
    private readonly Recharge _planeRecharge = new();
    private readonly List<GameObject> _rocketIcons = new();
    private readonly List<GameObject> _inclineItems = new();

    private void Start()
    {
        InitUi();
        
        planeRigidBody.maxDepenetrationVelocity = 0.01f;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void InitUi() {
        // create rocket icons
        for (int i = 0; i < _rocketsLeftOnBoard; i++) {
            GameObject imgObject = new GameObject("RocketIcon#" + i);
            
            RectTransform trans = imgObject.AddComponent<RectTransform>();
            trans.transform.SetParent(canvas.transform); // setting parent
            
            trans.localScale = Vector3.one;

            trans.anchorMin = new Vector2(0.8f, 0);
            trans.anchorMax = new Vector2(0.8f, 0);
            
            trans.anchoredPosition = new Vector2(i * -18 * 2, 96); // setting position, will be on center
            trans.sizeDelta = new Vector2(18, 96); // custom size

            RawImage image = imgObject.AddComponent<RawImage>();
            Texture2D tex = rocketIconTexture;
            image.texture = tex;
            imgObject.transform.SetParent(canvas.transform);
            _rocketIcons.Add(imgObject);
        }
        
        // add incline bars for each 10 degrees
        for (int i = -18; i < 18; i++)
        {
            GameObject imgObject = new GameObject("Incline#" + i);
            
            RectTransform trans = imgObject.AddComponent<RectTransform>();
            trans.transform.SetParent(inclineCanvas.transform);
            
            trans.localScale = Vector3.one;

            trans.anchorMin = new Vector2(0.5f, 0.5f);
            trans.anchorMax = new Vector2(0.5f, 0.5f);
            
            trans.sizeDelta = new Vector2(512, 64);
            
            trans.anchoredPosition = new Vector2(0, i * 100);

            RawImage image = imgObject.AddComponent<RawImage>();
            Texture2D tex = inclineTexture;
            image.texture = tex;
            imgObject.transform.SetParent(inclineCanvas.transform);
            
            // add text inside
            GameObject textObject = new GameObject("InclineText#" + i);
            RectTransform textTrans = textObject.AddComponent<RectTransform>();
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = (i * 10).ToString();
            text.fontSize = 18;
            textTrans.transform.SetParent(imgObject.transform);
            
            // set text position
            textTrans.localScale = Vector3.one;
            textTrans.sizeDelta = new Vector2(100, 40);
            textTrans.anchorMin = new Vector2(0.5f, 0);
            textTrans.anchorMax = new Vector2(0.5f, 0);
            textTrans.anchoredPosition = new Vector2(0, 0);
            
            _inclineItems.Add(imgObject);
        }
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

    private void UpdateUi()
    {
        
        var pitch = planeRigidBody.transform.rotation.eulerAngles.x;
        if (pitch > 180) {
            pitch -= 360;
        }
        
        crosshair.transform.localRotation = Quaternion.Euler(0, 0, planeRigidBody.transform.rotation.eulerAngles.z);
        
        speedText.text = planeRigidBody.velocity.magnitude.ToString("F0"); // units per second
        altitudeText.text = planeRigidBody.transform.position.y.ToString("F0");
        
        // update plane part icons
        leftWingIcon.color = new Color32((byte)(255 * _plane.LeftWing.Health), (byte)(255 * _plane.LeftWing.Health), (byte)(255 * _plane.LeftWing.Health), 255); 
        rightWingIcon.color = new Color32((byte)(255 * _plane.RightWing.Health), (byte)(255 * _plane.RightWing.Health), (byte)(255 * _plane.RightWing.Health), 255);
        tailIcon.color = new Color32((byte)(255 * _plane.Tail.Health), (byte)(255 * _plane.Tail.Health), (byte)(255 * _plane.Tail.Health), 255);
        engineIcon.color = new Color32((byte)(255 * _plane.Engine.Health), (byte)(255 * _plane.Engine.Health), (byte)(255 * _plane.Engine.Health), 255);
        cockpitIcon.color = new Color32((byte)(255 * _plane.Cockpit.Health), (byte)(255 * _plane.Cockpit.Health), (byte)(255 * _plane.Cockpit.Health), 255);
        

        // set plane rotation to yaw
        planeIcon.transform.localRotation = Quaternion.Euler(0, 0, -planeRigidBody.transform.rotation.eulerAngles.y);

        //create all radar objects
        foreach(GameObject radarObj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (!_objetcsAndIcons.ContainsKey(radarObj))
            {
                TagHolder objectTag = radarObj.transform.gameObject.GetComponent<TagHolder>();
                if (objectTag != null)
                {
                    if (objectTag.HasTag(SharedTag.MainRocket))
                    {
                        GameObject imgObject = new GameObject("MainRocketIcon");

                        imgObject.transform.SetParent(radar.transform);
                        RawImage image = imgObject.AddComponent<RawImage>();
                        image.texture = enemyIconTexture;
                        image.color = new Color32(247, 43, 43, 255);
                        RectTransform imgTransform = imgObject.transform.GetComponent<RectTransform>();
                        imgTransform.sizeDelta = new Vector2(30f, 30f);
                        imgTransform.localScale = new Vector3(1f, 1f, 1f);

                        _objetcsAndIcons.Add(radarObj, imgObject);
                    }
                    else if (objectTag.HasTag(SharedTag.PlayerRocket))
                    {
                        GameObject imgObject = new GameObject("PlayerRocketIcon");

                        imgObject.transform.SetParent(radar.transform);
                        RawImage image = imgObject.AddComponent<RawImage>();
                        image.texture = enemyIconTexture;
                        image.color = new Color32(56, 166, 239, 255);
                        RectTransform imgTransform = imgObject.transform.GetComponent<RectTransform>();
                        imgTransform.sizeDelta = new Vector2(30f, 30f);
                        imgTransform.localScale = new Vector3(1f, 1f, 1f);

                        _objetcsAndIcons.Add(radarObj, imgObject);
                    }
                    else if (objectTag.HasTag(SharedTag.Rocket))
                    {
                        GameObject imgObject = new GameObject("RocketIcon");

                        imgObject.transform.SetParent(radar.transform);
                        RawImage image = imgObject.AddComponent<RawImage>();
                        image.texture = enemyIconTexture;
                        image.color = new Color32(247, 145, 43, 255);
                        RectTransform imgTransform = imgObject.transform.GetComponent<RectTransform>();
                        imgTransform.sizeDelta = new Vector2(30f, 30f);
                        imgTransform.localScale = new Vector3(1f, 1f, 1f);

                        _objetcsAndIcons.Add(radarObj, imgObject);
                    }
                    else if (objectTag.HasTag(SharedTag.MainObjective))
                    {
                        GameObject imgObject = new GameObject("MainTargetIcon");

                        imgObject.transform.SetParent(canvas.transform);
                        RawImage imgTarget = imgObject.AddComponent<RawImage>();
                        imgTarget.texture = enemyIconTexture;
                        imgTarget.color = new Color32(232, 206, 36, 255);
                        RectTransform imgTransform = imgObject.transform.GetComponent<RectTransform>();
                        imgTransform.sizeDelta = new Vector2(20f, 20f);
                        imgTransform.localScale = new Vector3(2f, 2f, 2f);
                        imgTransform.anchorMin = new Vector2(0f, 0f);
                        imgTransform.anchorMax = new Vector2(0f, 0f);

                        _objetcsAndIcons.Add(radarObj, imgObject);
                    }
                    else if (objectTag.HasTag(SharedTag.AirDefence))
                    {
                        GameObject imgObject = new GameObject("AirDefenceIcon");

                        imgObject.transform.SetParent(radar.transform);
                        RawImage image = imgObject.AddComponent<RawImage>();
                        image.texture = airDefenceIconTexture;
                        image.color = new Color32(247, 145, 43, 255);
                        RectTransform imgTransform = imgObject.transform.GetComponent<RectTransform>();
                        imgTransform.sizeDelta = new Vector2(50f, 50f);
                        imgTransform.localScale = new Vector3(1f, 1f, 1f);

                        _objetcsAndIcons.Add(radarObj, imgObject);

                        GameObject imgRadius = new GameObject("AirDefenceRadiusIcon");
                        imgRadius.transform.SetParent(imgObject.transform);
                        image = imgRadius.AddComponent<RawImage>();
                        image.texture = enemyIconTexture;
                        image.color = new Color32(247, 145, 43, 100);
                        imgTransform = imgRadius.transform.GetComponent<RectTransform>();
                        imgTransform.sizeDelta = new Vector2(1f, 1f);
                        imgTransform.localScale = new Vector3(2f, 2f, 2f);
                    }
                }
            }
        }

        //delete icons of non-existent objects
        for (int i = _objetcsAndIcons.Count - 1; i >= 0; i--)
        {
            KeyValuePair<GameObject, GameObject> entry = _objetcsAndIcons.ElementAt(i);
            if (entry.Key == null)
            {
                Destroy(entry.Value);
                _objetcsAndIcons.Remove(entry.Key);
            }
        }

        foreach (KeyValuePair<GameObject, GameObject> entry in _objetcsAndIcons)
        {
            if(entry.Key.GetComponent<TagHolder>().HasTag(SharedTag.MainObjective))
            {
                //change mainObjective position
                Vector2 offset = new Vector3(-(planeRigidBody.position.x - entry.Key.transform.position.x),
                -(planeRigidBody.position.z - entry.Key.transform.position.z)) * RadarZoomIn;
                offset = offset.normalized * Mathf.Clamp(offset.magnitude, 0f, 190f);
                entry.Value.transform.GetComponent<RectTransform>().anchoredPosition = planeIcon.transform.GetComponent<RectTransform>().anchoredPosition + offset;
            }
            else
            {
                //change position of other objects
                Vector3 offset = new Vector3(-(planeRigidBody.position.x - entry.Key.transform.position.x),
                -(planeRigidBody.position.z - entry.Key.transform.position.z),
                0f) * RadarZoomIn;
                entry.Value.transform.GetComponent<RectTransform>().anchoredPosition = offset;
            }

            //update size of AirDefence area
            if (entry.Value.transform.childCount > 0)
            {
                AirDefenceController adController = entry.Key.transform.gameObject.GetComponent<AirDefenceController>();
                if (adController != null)
                {
                    float hightDiff = transform.position.y - entry.Key.transform.position.y;
                    float range = adController.GetRange();
                    float size = MathF.Sqrt((range * range) - (hightDiff * hightDiff));
                    entry.Value.transform.GetChild(0).transform.GetComponent<RectTransform>().sizeDelta = new Vector2(size * RadarZoomIn, size * RadarZoomIn);
                }
            }
        }

        // move inclines up and down with pitch, accounting for item index
        for (int i = - _inclineItems.Count / 2; i < _inclineItems.Count / 2; i++)
        {
            var item = _inclineItems[_inclineItems.Count / 2 + i];
            // Yes, I am fucking going to call it 36 times a tick. So what?
            var itemTrans = item.GetComponent<RectTransform>();
            itemTrans.anchoredPosition = new Vector2(0, (i + pitch / 10) * 100);
        }
        
        // rotate incline canvas on screen
        inclineCanvas.transform.localRotation = Quaternion.Euler(0, 0, planeRigidBody.transform.rotation.eulerAngles.z);
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
        _rocketIcons[_rocketsLeftOnBoard].SetActive(false);                
        
        var planeTransform = planeRigidBody.transform;
        
        var spawnPos = planeTransform.position + planeTransform.up * -3 + planeTransform.forward * 10;
        _rocket = Instantiate(rocketPrefab, spawnPos, planeTransform.rotation);

        // set tag
        //_rocket.gameObject.tag = "Player";
        _rocket.gameObject.GetComponent<TagHolder>().AddTag(SharedTag.PlayerRocket);

        var rocketBase = _rocket.GetComponent<RocketBehaviour>();
        var playerControl = _rocket.GetComponent<RocketPlayerControl>();
        var aiControl = _rocket.GetComponent<RocketAiControl>();
        
        // rocket hit something
        rocketBase.OnRocketDestroyed += (collidedWith, _) => { 
            collidedWith.GetComponent<TagHolder>().HasTag(SharedTag.AirDefence).Then(()=>Destroy(collidedWith));
        };
        
        rocketBase.SetMaxSpeed(70);
        rocketBase.SetSpeed(planeRigidBody.velocity.magnitude);
        playerControl.SetSteeringSens(180);
        
        playerControl.enabled = true;
        aiControl.enabled = false;
    }

    private void Update()
    {

        UpdateUi();
        
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
    }

    public bool HasTriggeredFlares()
    {
        return !_planeRecharge.IsRecharged("Flares");
    }
}

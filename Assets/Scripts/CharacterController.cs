using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// Note to Matt Doucette, this character controller was used from the super character controller asset pack on unity :)

namespace SUPERCharacter {
    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(CapsuleCollider))][AddComponentMenu("SUPER Character/SUPER Character Controller")]
    public class SUPERCharacterAIO : MonoBehaviour {
        #region Variables

        public bool controllerPaused = false;

        #region Camera Settings
        [Header("Camera Settings")]
        //
        //Public
        //
        //Both
        public Camera playerCamera;
        public bool enableCameraControl = true, lockAndHideMouse = true, autoGenerateCrosshair = true, showCrosshairIn3rdPerson = false, drawPrimitiveUI = false;
        public Sprite crosshairSprite;
        public PerspectiveModes cameraPerspective = PerspectiveModes._1stPerson;
        //use mouse wheel to switch modes. (too close will set it to fps mode and attempting to zoom out from fps will switch to tps mode)
        public bool automaticallySwitchPerspective = true;
#if ENABLE_INPUT_SYSTEM
    public Key perspectiveSwitchingKey = Key.Q;
#else
        public KeyCode perspectiveSwitchingKey_L = KeyCode.None;
#endif

        public MouseInputInversionModes mouseInputInversion;
        public float Sensitivity = 8;
        public float rotationWeight = 4;
        public float verticalRotationRange = 170.0f;
        public float standingEyeHeight = 0.8f;
        public float crouchingEyeHeight = 0.25f;

        //First person
        public ViewInputModes viewInputMethods;
        public float FOVKickAmount = 10;
        public float FOVSensitivityMultiplier = 0.74f;

        //Third Person
        public bool rotateCharacterToCameraForward = false;
        public float maxCameraDistance = 8;
        public LayerMask cameraObstructionIgnore = -1;
        public float cameraZoomSensitivity = 5;
        public float bodyCatchupSpeed = 2.5f;
        public float inputResponseFiltering = 2.5f;



        //
        //Internal
        //

        //Both
        Vector2 MouseXY;
        Vector2 viewRotVelRef;
        bool isInFirstPerson, isInThirdPerson, perspecTog;
        bool setInitialRot = true;
        Vector3 initialRot;
        Image crosshairImg;
        Image stamMeter, stamMeterBG;
        Image statsPanel, statsPanelBG;
        Image HealthMeter, HydrationMeter, HungerMeter;
        Vector2 normalMeterSizeDelta = new Vector2(175, 12), normalStamMeterSizeDelta = new Vector2(330, 5);
        float internalEyeHeight;

        //First Person
        float initialCameraFOV, FOVKickVelRef, currentFOVMod;

        //Third Person
        float mouseScrollWheel, maxCameraDistInternal, currentCameraZ, cameraZRef;
        Vector3 headPos, headRot, currentCameraPos, cameraPosVelRef;
        Quaternion quatHeadRot;
        Ray cameraObstCheck;
        RaycastHit cameraObstResult;
        [Space(20)]
        #endregion

        #region Movement
        [Header("Movement Settings")]

        //
        //Public
        //
        public bool enableMovementControl = true;

        //Walking/Sprinting/Crouching
        [Range(1.0f, 650.0f)] public float walkingSpeed = 140, sprintingSpeed = 260, crouchingSpeed = 45;
        [Range(1.0f, 400.0f)] public float decelerationSpeed = 240;
#if ENABLE_INPUT_SYSTEM
    public Key sprintKey = Key.LeftShift, crouchKey = Key.LeftCtrl, slideKey = Key.V;
#else
        public KeyCode sprintKey_L = KeyCode.LeftShift, crouchKey_L = KeyCode.LeftControl, slideKey_L = KeyCode.V;
#endif
        public bool canSprint = true, isSprinting, toggleSprint, sprintOverride, canCrouch = true, isCrouching, toggleCrouch, crouchOverride, isIdle;
        public Stances currentStance = Stances.Standing;
        public float stanceTransitionSpeed = 5.0f, crouchingHeight = 0.80f;
        public GroundSpeedProfiles currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
        public LayerMask whatIsGround = -1;

        //Slope affectors
        public float hardSlopeLimit = 70, slopeInfluenceOnSpeed = 1, maxStairRise = 0.25f, stepUpSpeed = 0.2f;

        //Jumping
        public bool canJump = true, holdJump = false, jumpEnhancements = true, Jumped;
#if ENABLE_INPUT_SYSTEM
        public Key jumpKey = Key.Space;
#else
        public KeyCode jumpKey_L = KeyCode.Space;
#endif
        [Range(1.0f, 650.0f)] public float jumpPower = 40;
        [Range(0.0f, 1.0f)] public float airControlFactor = 1;
        public float decentMultiplier = 2.5f, tapJumpMultiplier = 2.1f;
        float jumpBlankingPeriod;

        //Sliding
        public bool isSliding, canSlide = true;
        public float slidingDeceleration = 150.0f, slidingTransitionSpeed = 4, maxFlatSlideDistance = 10;


        //
        //Internal
        //

        //Walking/Sprinting/Crouching
        public GroundInfo currentGroundInfo = new GroundInfo();
        float standingHeight;
        float currentGroundSpeed;
        Vector3 InputDir;
        float HeadRotDirForInput;
        Vector2 MovInput;
        Vector2 MovInput_Smoothed;
        Vector2 _2DVelocity;
        float _2DVelocityMag, speedToVelocityRatio;
        PhysicMaterial _ZeroFriction, _MaxFriction;
        CapsuleCollider capsule;
        Rigidbody p_Rigidbody;
        bool crouchInput_Momentary, crouchInput_FrameOf, sprintInput_FrameOf, sprintInput_Momentary, slideInput_FrameOf, slideInput_Momentary;
        bool changingStances = false;

        //Slope Affectors

        //Jumping
        bool jumpInput_Momentary, jumpInput_FrameOf;

        //Sliding
        Vector3 cachedDirPreSlide, cachedPosPreSlide;



        [Space(20)]
        #endregion

        #region Parkour
#if SAIO_ENABLE_PARKOUR

    //
    //Public
    //

    //Vaulting
    public bool canVault = true, isVaulting, autoVaultWhenSpringing;
#if ENABLE_INPUT_SYSTEM
    public Key VaultKey = Key.E;
#else
    public KeyCode VaultKey_L = KeyCode.E;
#endif
    public string vaultObjectTag = "Vault Obj";
    public float vaultSpeed = 7.5f, maxVaultDepth = 1.5f, maxVaultHeight = 0.75f;


    //
    //Internal
    //

    //Vaulting
    RaycastHit VC_Stage1, VC_Stage2, VC_Stage3, VC_Stage4;
    Vector3 vaultForwardVec;
    bool vaultInput;

    //All
#endif
        private bool doingPosInterp, doingCamInterp;
        #endregion

        #region Stamina System
        //Public
        public bool enableStaminaSystem = true, jumpingDepletesStamina = true;
        [Range(0.0f, 250.0f)] public float Stamina = 50.0f, currentStaminaLevel = 0, s_minimumStaminaToSprint = 5.0f, s_depletionSpeed = 2.0f, s_regenerationSpeed = 1.2f, s_JumpStaminaDepletion = 5.0f;

        //Internal
        bool staminaIsChanging;
        bool ignoreStamina = false;
        #endregion

        #region Footstep System
        [Header("Footstep System")]
        public bool enableFootstepSounds = true;
        public FootstepTriggeringMode footstepTriggeringMode = FootstepTriggeringMode.calculatedTiming;
        [Range(0.0f, 1.0f)] public float stepTiming = 0.15f;
        public List<GroundMaterialProfile> footstepSoundSet = new List<GroundMaterialProfile>();
        bool shouldCalculateFootstepTriggers = true;
        float StepCycle = 0;
        AudioSource playerAudioSource;
        List<AudioClip> currentClipSet = new List<AudioClip>();
        [Space(18)]
        #endregion

        #region  Headbob
        //
        //Public
        //
        public bool enableHeadbob = true;
        [Range(1.0f, 5.0f)] public float headbobSpeed = 0.5f, headbobPower = 0.25f;
        [Range(0.0f, 3.0f)] public float ZTilt = 3;

        //
        //Internal
        //
        bool shouldCalculateHeadbob;
        Vector3 headbobCameraPosition;
        float headbobCyclePosition, headbobWarmUp;

        #endregion

        #region  Survival Stats
        //
        //Public
        //
        public bool enableSurvivalStats = true;
        public SurvivalStats defaultSurvivalStats = new SurvivalStats();
        public float statTickRate = 6.0f, hungerDepletionRate = 0.06f, hydrationDepletionRate = 0.14f;
        public SurvivalStats currentSurvivalStats = new SurvivalStats();

        //
        //Internal
        //
        float StatTickTimer;
        #endregion

        #region Interactable
#if ENABLE_INPUT_SYSTEM

    //
    //Public
    //
    public Key interactKey = Key.E;
#else
        public KeyCode interactKey_L = KeyCode.E;
#endif
        public float interactRange = 4;
        public LayerMask interactableLayer = -1;
        //
        //Internal
        //
        bool interactInput;
        #endregion

        #region Collectables
        #endregion

        #region Animation
        //
        //Pulbic
        //

        //Firstperson
        public Animator _1stPersonCharacterAnimator;
        //ThirdPerson
        public Animator _3rdPersonCharacterAnimator;
        public string a_velocity, a_2DVelocity, a_Grounded, a_Idle, a_Jumped, a_Sliding, a_Sprinting, a_Crouching;
        public bool stickRendererToCapsuleBottom = true;

        #endregion

        [Space(18)]
        public bool enableGroundingDebugging = false, enableMovementDebugging = false, enableMouseAndCameraDebugging = false, enableVaultDebugging = false;
        #endregion
        void Start() {



            #region Camera
            maxCameraDistInternal = maxCameraDistance;
            initialCameraFOV = playerCamera.fieldOfView;
            headbobCameraPosition = Vector3.up * standingEyeHeight;
            internalEyeHeight = standingEyeHeight;
            if (lockAndHideMouse) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (autoGenerateCrosshair || drawPrimitiveUI) {
                Canvas canvas = playerCamera.gameObject.GetComponentInChildren<Canvas>();
                if (canvas == null) { canvas = new GameObject("AutoCrosshair").AddComponent<Canvas>(); }
                canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.pixelPerfect = true;
                canvas.transform.SetParent(playerCamera.transform);
                canvas.transform.position = Vector3.zero;
                if (autoGenerateCrosshair && crosshairSprite) {
                    crosshairImg = new GameObject("Crosshair").AddComponent<Image>();
                    crosshairImg.sprite = crosshairSprite;
                    crosshairImg.rectTransform.sizeDelta = new Vector2(25, 25);
                    crosshairImg.transform.SetParent(canvas.transform);
                    crosshairImg.transform.position = Vector3.zero;
                    crosshairImg.raycastTarget = false;
                }
                if (drawPrimitiveUI) {
                    //Stam Meter BG
                    stamMeterBG = new GameObject("Stam BG").AddComponent<Image>();
                    stamMeterBG.rectTransform.sizeDelta = normalStamMeterSizeDelta;
                    stamMeterBG.transform.SetParent(canvas.transform);
                    stamMeterBG.rectTransform.anchorMin = new Vector2(0.5f, 0);
                    stamMeterBG.rectTransform.anchorMax = new Vector2(0.5f, 0);
                    stamMeterBG.rectTransform.anchoredPosition = new Vector2(0, 22);
                    stamMeterBG.color = Color.gray;
                    stamMeterBG.gameObject.SetActive(enableStaminaSystem);
                    //Stam Meter
                    stamMeter = new GameObject("Stam Meter").AddComponent<Image>();
                    stamMeter.rectTransform.sizeDelta = normalStamMeterSizeDelta;
                    stamMeter.transform.SetParent(canvas.transform);
                    stamMeter.rectTransform.anchorMin = new Vector2(0.5f, 0);
                    stamMeter.rectTransform.anchorMax = new Vector2(0.5f, 0);
                    stamMeter.rectTransform.anchoredPosition = new Vector2(0, 22);
                    stamMeter.color = Color.white;
                    stamMeter.gameObject.SetActive(enableStaminaSystem);
                    //Stats Panel
                    statsPanel = new GameObject("Stats Panel").AddComponent<Image>();
                    statsPanel.rectTransform.sizeDelta = new Vector2(3, 45);
                    statsPanel.transform.SetParent(canvas.transform);
                    statsPanel.rectTransform.anchorMin = new Vector2(0, 0);
                    statsPanel.rectTransform.anchorMax = new Vector2(0, 0);
                    statsPanel.rectTransform.anchoredPosition = new Vector2(12, 33);
                    statsPanel.color = Color.clear;
                    statsPanel.gameObject.SetActive(enableSurvivalStats);
                    //Stats Panel BG
                    statsPanelBG = new GameObject("Stats Panel BG").AddComponent<Image>();
                    statsPanelBG.rectTransform.sizeDelta = new Vector2(175, 45);
                    statsPanelBG.transform.SetParent(statsPanel.transform);
                    statsPanelBG.rectTransform.anchorMin = new Vector2(0, 0);
                    statsPanelBG.rectTransform.anchorMax = new Vector2(1, 0);
                    statsPanelBG.rectTransform.anchoredPosition = new Vector2(87, 22);
                    statsPanelBG.color = Color.white * 0.5f;
                    //Health Meter
                    HealthMeter = new GameObject("Health Meter").AddComponent<Image>();
                    HealthMeter.rectTransform.sizeDelta = normalMeterSizeDelta;
                    HealthMeter.transform.SetParent(statsPanel.transform);
                    HealthMeter.rectTransform.anchorMin = new Vector2(0, 0);
                    HealthMeter.rectTransform.anchorMax = new Vector2(1, 0);
                    HealthMeter.rectTransform.anchoredPosition = new Vector2(87, 6);
                    HealthMeter.color = new Color32(211, 0, 0, 255);
                    //Hydration Meter
                    HydrationMeter = new GameObject("Hydration Meter").AddComponent<Image>();
                    HydrationMeter.rectTransform.sizeDelta = normalMeterSizeDelta;
                    HydrationMeter.transform.SetParent(statsPanel.transform);
                    HydrationMeter.rectTransform.anchorMin = new Vector2(0, 0);
                    HydrationMeter.rectTransform.anchorMax = new Vector2(1, 0);
                    HydrationMeter.rectTransform.anchoredPosition = new Vector2(87, 22);
                    HydrationMeter.color = new Color32(0, 194, 255, 255);
                    //Hunger Meter
                    HungerMeter = new GameObject("Hunger Meter").AddComponent<Image>();
                    HungerMeter.rectTransform.sizeDelta = normalMeterSizeDelta;
                    HungerMeter.transform.SetParent(statsPanel.transform);
                    HungerMeter.rectTransform.anchorMin = new Vector2(0, 0);
                    HungerMeter.rectTransform.anchorMax = new Vector2(1, 0);
                    HungerMeter.rectTransform.anchoredPosition = new Vector2(87, 38);
                    HungerMeter.color = new Color32(142, 54, 0, 255);

                }
            }
            if (cameraPerspective == PerspectiveModes._3rdPerson && !showCrosshairIn3rdPerson) {
                crosshairImg?.gameObject.SetActive(false);
            }
            initialRot = transform.localEulerAngles;
            #endregion

            #region Movement
            p_Rigidbody = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            standingHeight = capsule.height;
            currentGroundSpeed = walkingSpeed;
            _ZeroFriction = new PhysicMaterial("Zero_Friction");
            _ZeroFriction.dynamicFriction = 0f;
            _ZeroFriction.staticFriction = 0;
            _ZeroFriction.frictionCombine = PhysicMaterialCombine.Minimum;
            _ZeroFriction.bounceCombine = PhysicMaterialCombine.Minimum;
            _MaxFriction = new PhysicMaterial("Max_Friction");
            _MaxFriction.dynamicFriction = 1;
            _MaxFriction.staticFriction = 1;
            _MaxFriction.frictionCombine = PhysicMaterialCombine.Maximum;
            _MaxFriction.bounceCombine = PhysicMaterialCombine.Average;
            #endregion

            #region Stamina System
            currentStaminaLevel = Stamina;
            #endregion

            #region Footstep
            playerAudioSource = GetComponent<AudioSource>();
            #endregion

        }
        void Update() {
            if (!controllerPaused) {
                #region Input
#if ENABLE_INPUT_SYSTEM
            MouseXY.x = Mouse.current.delta.y.ReadValue()/50;
            MouseXY.y = Mouse.current.delta.x.ReadValue()/50;
            
            mouseScrollWheel = Mouse.current.scroll.y.ReadValue()/1000;
            if(perspectiveSwitchingKey!=Key.None)perspecTog = Keyboard.current[perspectiveSwitchingKey].wasPressedThisFrame;
            if(interactKey!=Key.None)interactInput = Keyboard.current[interactKey].wasPressedThisFrame;
            //movement

             if(jumpKey!=Key.None)jumpInput_Momentary =  Keyboard.current[jumpKey].isPressed;
             if(jumpKey!=Key.None)jumpInput_FrameOf =  Keyboard.current[jumpKey].wasPressedThisFrame;

             if(crouchKey!=Key.None){
                crouchInput_Momentary =  Keyboard.current[crouchKey].isPressed;
                crouchInput_FrameOf = Keyboard.current[crouchKey].wasPressedThisFrame;
             }
             if(sprintKey!=Key.None){
                sprintInput_Momentary = Keyboard.current[sprintKey].isPressed;
                sprintInput_FrameOf = Keyboard.current[sprintKey].wasPressedThisFrame;
             }
             if(slideKey != Key.None){
                slideInput_Momentary = Keyboard.current[slideKey].isPressed;
                slideInput_FrameOf = Keyboard.current[slideKey].wasPressedThisFrame;
             }
#if SAIO_ENABLE_PARKOUR
            vaultInput = Keyboard.current[VaultKey].isPressed;
#endif
            MovInput.x = Keyboard.current.aKey.isPressed ? -1 : Keyboard.current.dKey.isPressed ? 1 : 0;
            MovInput.y = Keyboard.current.wKey.isPressed ? 1 : Keyboard.current.sKey.isPressed ? -1 : 0;
#else
                //camera
                MouseXY.x = Input.GetAxis("Mouse Y");
                MouseXY.y = Input.GetAxis("Mouse X");
                mouseScrollWheel = Input.GetAxis("Mouse ScrollWheel");
                perspecTog = Input.GetKeyDown(perspectiveSwitchingKey_L);
                interactInput = Input.GetKeyDown(interactKey_L);
                //movement

                jumpInput_Momentary = Input.GetKey(jumpKey_L);
                jumpInput_FrameOf = Input.GetKeyDown(jumpKey_L);
                crouchInput_Momentary = Input.GetKey(crouchKey_L);
                crouchInput_FrameOf = Input.GetKeyDown(crouchKey_L);
                sprintInput_Momentary = Input.GetKey(sprintKey_L);
                sprintInput_FrameOf = Input.GetKeyDown(sprintKey_L);
                slideInput_Momentary = Input.GetKey(slideKey_L);
                slideInput_FrameOf = Input.GetKeyDown(slideKey_L);
#if SAIO_ENABLE_PARKOUR

            vaultInput = Input.GetKeyDown(VaultKey_L);
#endif
                MovInput = Vector2.up * Input.GetAxisRaw("Vertical") + Vector2.right * Input.GetAxisRaw("Horizontal");
#endif
                #endregion

                #region Camera
                if (enableCameraControl) {
                    switch (cameraPerspective) {
                        case PerspectiveModes._1stPerson: {
                                //This is called in FixedUpdate for the 3rd person mode
                                //RotateView(MouseXY, Sensitivity, rotationWeight);
                                if (!isInFirstPerson) { ChangePerspective(PerspectiveModes._1stPerson); }
                                if (perspecTog || (automaticallySwitchPerspective && mouseScrollWheel < 0)) { ChangePerspective(PerspectiveModes._3rdPerson); }
                                HeadbobCycleCalculator();
                                FOVKick();
                            } break;

                        case PerspectiveModes._3rdPerson: {
                                //  UpdateCameraPosition_3rdPerson();
                                if (!isInThirdPerson) { ChangePerspective(PerspectiveModes._3rdPerson); }
                                if (perspecTog || (automaticallySwitchPerspective && maxCameraDistInternal == 0 && currentCameraZ == 0)) { ChangePerspective(PerspectiveModes._1stPerson); }
                                maxCameraDistInternal = Mathf.Clamp(maxCameraDistInternal - (mouseScrollWheel * (cameraZoomSensitivity * 2)), automaticallySwitchPerspective ? 0 : (capsule.radius * 2), maxCameraDistance);
                            } break;
                    }


                    if (setInitialRot) {
                        setInitialRot = false;
                        RotateView(initialRot, false);
                        InputDir = transform.forward;
                    }
                }
                if (drawPrimitiveUI) {
                    if (enableSurvivalStats) {
                        if (!statsPanel.gameObject.activeSelf) statsPanel.gameObject.SetActive(true);

                        HealthMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 12, normalMeterSizeDelta, (currentSurvivalStats.Health / defaultSurvivalStats.Health));
                        HydrationMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 12, normalMeterSizeDelta, (currentSurvivalStats.Hydration / defaultSurvivalStats.Hydration));
                        HungerMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 12, normalMeterSizeDelta, (currentSurvivalStats.Hunger / defaultSurvivalStats.Hunger));
                    } else {
                        if (statsPanel.gameObject.activeSelf) statsPanel.gameObject.SetActive(false);

                    }
                    if (enableStaminaSystem) {
                        if (!stamMeterBG.gameObject.activeSelf) stamMeterBG.gameObject.SetActive(true);
                        if (!stamMeter.gameObject.activeSelf) stamMeter.gameObject.SetActive(true);
                        if (staminaIsChanging) {
                            if (stamMeter.color != Color.white) {
                                stamMeterBG.color = Vector4.MoveTowards(stamMeterBG.color, new Vector4(0, 0, 0, 0.5f), 0.15f);
                                stamMeter.color = Vector4.MoveTowards(stamMeter.color, new Vector4(1, 1, 1, 1), 0.15f);
                            }
                            stamMeter.rectTransform.sizeDelta = Vector2.Lerp(Vector2.up * 5, normalStamMeterSizeDelta, (currentStaminaLevel / Stamina));
                        } else {
                            if (stamMeter.color != Color.clear) {
                                stamMeterBG.color = Vector4.MoveTowards(stamMeterBG.color, new Vector4(0, 0, 0, 0), 0.15f);
                                stamMeter.color = Vector4.MoveTowards(stamMeter.color, new Vector4(0, 0, 0, 0), 0.15f);
                            }
                        }
                    } else {
                        if (stamMeterBG.gameObject.activeSelf) stamMeterBG.gameObject.SetActive(false);
                        if (stamMeter.gameObject.activeSelf) stamMeter.gameObject.SetActive(false);
                    }
                }

                if (currentStance == Stances.Standing && !changingStances) {
                    internalEyeHeight = standingEyeHeight;
                }
                #endregion

                #region Movement
                if (cameraPerspective == PerspectiveModes._3rdPerson) {
                    HeadRotDirForInput = Mathf.MoveTowardsAngle(HeadRotDirForInput, headRot.y, bodyCatchupSpeed * (1 + Time.deltaTime));
                    MovInput_Smoothed = Vector2.MoveTowards(MovInput_Smoothed, MovInput, inputResponseFiltering * (1 + Time.deltaTime));
                }
                InputDir = cameraPerspective == PerspectiveModes._1stPerson ? Vector3.ClampMagnitude((transform.forward * MovInput.y + transform.right * (viewInputMethods == ViewInputModes.Traditional ? MovInput.x : 0)), 1) : Quaternion.AngleAxis(HeadRotDirForInput, Vector3.up) * (Vector3.ClampMagnitude((Vector3.forward * MovInput_Smoothed.y + Vector3.right * MovInput_Smoothed.x), 1));
                GroundMovementSpeedUpdate();
                if (canJump && (holdJump ? jumpInput_Momentary : jumpInput_FrameOf)) { Jump(jumpPower); }
                #endregion

                #region Stamina system
                if (enableStaminaSystem) { CalculateStamina(); }
                #endregion

                #region Footstep
                CalculateFootstepTriggers();
                #endregion

                #region Survival Stats
                if (enableSurvivalStats && Time.time > StatTickTimer) {
                    TickStats();
                }
                #endregion

                #region Interaction
                if (interactInput) {
                    TryInteract();
                }
                #endregion
            } else {
                jumpInput_FrameOf = false;
                jumpInput_Momentary = false;
            }
            #region Animation
            UpdateAnimationTriggers(controllerPaused);
            #endregion
        }
        void FixedUpdate() {
            if (!controllerPaused) {



                #region Movement
                if (enableMovementControl) {
                    GetGroundInfo();
                    MovePlayer(InputDir, currentGroundSpeed);

                    if (isSliding) { Slide(); }
                }
                #endregion

                #region Camera
                RotateView(MouseXY, Sensitivity, rotationWeight);
                if (cameraPerspective == PerspectiveModes._3rdPerson) {
                    UpdateBodyRotation_3rdPerson();
                    UpdateCameraPosition_3rdPerson();
                }

                #endregion
            }
        }
        

        #region Camera Functions
        void RotateView(Vector2 yawPitchInput, float inputSensitivity, float cameraWeight) {

            switch (viewInputMethods) {

                case ViewInputModes.Traditional: {
                        yawPitchInput.x *= ((mouseInputInversion == MouseInputInversionModes.X || mouseInputInversion == MouseInputInversionModes.Both) ? 1 : -1);
                        yawPitchInput.y *= ((mouseInputInversion == MouseInputInversionModes.Y || mouseInputInversion == MouseInputInversionModes.Both) ? -1 : 1);
                        float maxDelta = Mathf.Min(5, (26 - cameraWeight)) * 360;
                        switch (cameraPerspective) {
                            case PerspectiveModes._1stPerson: {
                                    Vector2 targetAngles = ((Vector2.right * playerCamera.transform.localEulerAngles.x) + (Vector2.up * p_Rigidbody.rotation.eulerAngles.y));
                                    float fovMod = FOVSensitivityMultiplier > 0 && playerCamera.fieldOfView <= initialCameraFOV ? ((initialCameraFOV - playerCamera.fieldOfView) * (FOVSensitivityMultiplier / 10)) + 1 : 1;
                                    targetAngles = Vector2.SmoothDamp(targetAngles, targetAngles + (yawPitchInput * (((inputSensitivity * 5) / fovMod))), ref viewRotVelRef, (Mathf.Pow(cameraWeight * fovMod, 2)) * Time.fixedDeltaTime, maxDelta, Time.fixedDeltaTime);

                                    targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
                                    targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                                    playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward * (enableHeadbob ? headbobCameraPosition.z : 0));
                                    p_Rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * targetAngles.y));

                                    //p_Rigidbody.rotation = ;
                                    //transform.localEulerAngles = (Vector3.up*targetAngles.y);
                                } break;

                            case PerspectiveModes._3rdPerson: {

                                    headPos = transform.position + Vector3.up * standingEyeHeight;
                                    quatHeadRot = Quaternion.Euler(headRot);
                                    headRot = Vector3.SmoothDamp(headRot, headRot + ((Vector3)yawPitchInput * (inputSensitivity * 5)), ref cameraPosVelRef, (Mathf.Pow(cameraWeight, 2)) * Time.fixedDeltaTime, maxDelta, Time.fixedDeltaTime);
                                    headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
                                    headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
                                    headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);


                                } break;

                        }

                    } break;

                case ViewInputModes.Retro: {
                        yawPitchInput = Vector2.up * (Input.GetAxis("Horizontal") * ((mouseInputInversion == MouseInputInversionModes.Y || mouseInputInversion == MouseInputInversionModes.Both) ? -1 : 1));
                        Vector2 targetAngles = ((Vector2.right * playerCamera.transform.localEulerAngles.x) + (Vector2.up * transform.localEulerAngles.y));
                        float fovMod = FOVSensitivityMultiplier > 0 && playerCamera.fieldOfView <= initialCameraFOV ? ((initialCameraFOV - playerCamera.fieldOfView) * (FOVSensitivityMultiplier / 10)) + 1 : 1;
                        targetAngles = targetAngles + (yawPitchInput * ((inputSensitivity / fovMod)));
                        targetAngles.x = 0;
                        playerCamera.transform.localEulerAngles = (Vector3.right * targetAngles.x) + (Vector3.forward * (enableHeadbob ? headbobCameraPosition.z : 0));
                        transform.localEulerAngles = (Vector3.up * targetAngles.y);
                    } break;
            }

        }
        public void RotateView(Vector3 AbsoluteEulerAngles, bool SmoothRotation) {

            switch (cameraPerspective) {

                case (PerspectiveModes._1stPerson): {
                        AbsoluteEulerAngles.x += AbsoluteEulerAngles.x > 180 ? -360 : AbsoluteEulerAngles.x < -180 ? 360 : 0;
                        AbsoluteEulerAngles.x = Mathf.Clamp(AbsoluteEulerAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);


                        if (SmoothRotation) {
                            IEnumerator SmoothRot() {
                                doingCamInterp = true;
                                Vector3 refVec = Vector3.zero, targetAngles = (Vector3.right * playerCamera.transform.localEulerAngles.x) + Vector3.up * transform.eulerAngles.y;
                                while (Vector3.Distance(targetAngles, AbsoluteEulerAngles) > 0.1f) {
                                    targetAngles = Vector3.SmoothDamp(targetAngles, AbsoluteEulerAngles, ref refVec, 25 * Time.deltaTime);
                                    targetAngles.x += targetAngles.x > 180 ? -360 : targetAngles.x < -180 ? 360 : 0;
                                    targetAngles.x = Mathf.Clamp(targetAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                                    playerCamera.transform.localEulerAngles = Vector3.right * targetAngles.x;
                                    transform.eulerAngles = Vector3.up * targetAngles.y;
                                    yield return null;
                                }
                                doingCamInterp = false;
                            }
                            StopCoroutine("SmoothRot");
                            StartCoroutine(SmoothRot());
                        } else {
                            playerCamera.transform.eulerAngles = Vector3.right * AbsoluteEulerAngles.x;
                            transform.eulerAngles = (Vector3.up * AbsoluteEulerAngles.y) + (Vector3.forward * AbsoluteEulerAngles.z);
                        }
                    } break;

                case (PerspectiveModes._3rdPerson): {
                        if (SmoothRotation) {
                            AbsoluteEulerAngles.y += AbsoluteEulerAngles.y > 180 ? -360 : AbsoluteEulerAngles.y < -180 ? 360 : 0;
                            AbsoluteEulerAngles.x += AbsoluteEulerAngles.x > 180 ? -360 : AbsoluteEulerAngles.x < -180 ? 360 : 0;
                            AbsoluteEulerAngles.x = Mathf.Clamp(AbsoluteEulerAngles.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                            IEnumerator SmoothRot() {
                                doingCamInterp = true;
                                Vector3 refVec = Vector3.zero;
                                while (Vector3.Distance(headRot, AbsoluteEulerAngles) > 0.1f) {
                                    headPos = p_Rigidbody.position + Vector3.up * standingEyeHeight;
                                    quatHeadRot = Quaternion.Euler(headRot);
                                    headRot = Vector3.SmoothDamp(headRot, AbsoluteEulerAngles, ref refVec, 25 * Time.deltaTime);
                                    headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
                                    headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
                                    headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                                    yield return null;
                                }
                                doingCamInterp = false;
                            }
                            StopCoroutine("SmoothRot");
                            StartCoroutine(SmoothRot());
                        }
                        else {
                            headRot = AbsoluteEulerAngles;
                            headRot.y += headRot.y > 180 ? -360 : headRot.y < -180 ? 360 : 0;
                            headRot.x += headRot.x > 180 ? -360 : headRot.x < -180 ? 360 : 0;
                            headRot.x = Mathf.Clamp(headRot.x, -0.5f * verticalRotationRange, 0.5f * verticalRotationRange);
                            quatHeadRot = Quaternion.Euler(headRot);
                            if (doingCamInterp) { }
                        }
                    } break;
            }
        }
        public void ChangePerspective(PerspectiveModes newPerspective = PerspectiveModes._1stPerson) {
            switch (newPerspective) {
                case PerspectiveModes._1stPerson: {
                        StopCoroutine("SmoothRot");
                        isInThirdPerson = false;
                        isInFirstPerson = true;
                        transform.eulerAngles = Vector3.up * headRot.y;
                        playerCamera.transform.localPosition = Vector3.up * standingEyeHeight;
                        playerCamera.transform.localEulerAngles = (Vector2)playerCamera.transform.localEulerAngles;
                        cameraPerspective = newPerspective;
                        if (_3rdPersonCharacterAnimator) {
                            _3rdPersonCharacterAnimator.gameObject.SetActive(false);
                        }
                        if (_1stPersonCharacterAnimator) {
                            _1stPersonCharacterAnimator.gameObject.SetActive(true);
                        }
                        if (crosshairImg && autoGenerateCrosshair) {
                            crosshairImg.gameObject.SetActive(true);
                        }
                    } break;

                case PerspectiveModes._3rdPerson: {
                        StopCoroutine("SmoothRot");
                        isInThirdPerson = true;
                        isInFirstPerson = false;
                        playerCamera.fieldOfView = initialCameraFOV;
                        maxCameraDistInternal = maxCameraDistInternal == 0 ? capsule.radius * 2 : maxCameraDistInternal;
                        currentCameraZ = -(maxCameraDistInternal * 0.85f);
                        playerCamera.transform.localEulerAngles = (Vector2)playerCamera.transform.localEulerAngles;
                        headRot.y = transform.eulerAngles.y;
                        headRot.x = playerCamera.transform.eulerAngles.x;
                        cameraPerspective = newPerspective;
                        if (_3rdPersonCharacterAnimator) {
                            _3rdPersonCharacterAnimator.gameObject.SetActive(true);
                        }
                        if (_1stPersonCharacterAnimator) {
                            _1stPersonCharacterAnimator.gameObject.SetActive(false);
                        }
                        if (crosshairImg && autoGenerateCrosshair) {
                            if (!showCrosshairIn3rdPerson) {
                                crosshairImg.gameObject.SetActive(false);
                            } else {
                                crosshairImg.gameObject.SetActive(true);
                            }
                        }
                    } break;
            }
        }
        void FOVKick() {
            if (cameraPerspective == PerspectiveModes._1stPerson && FOVKickAmount > 0) {
                currentFOVMod = (!isIdle && isSprinting) ? initialCameraFOV + (FOVKickAmount * ((sprintingSpeed / walkingSpeed) - 1)) : initialCameraFOV;
                if (!Mathf.Approximately(playerCamera.fieldOfView, currentFOVMod) && playerCamera.fieldOfView >= initialCameraFOV) {
                    playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, currentFOVMod, ref FOVKickVelRef, Time.deltaTime, 50);
                }
            }
        }
        void HeadbobCycleCalculator() {
            if (enableHeadbob) {
                if (!isIdle && currentGroundInfo.isGettingGroundInfo && !isSliding) {
                    headbobWarmUp = Mathf.MoveTowards(headbobWarmUp, 1, Time.deltaTime * 5);
                    headbobCyclePosition += (_2DVelocity.magnitude) * (Time.deltaTime * (headbobSpeed / 10));

                    headbobCameraPosition.x = (((Mathf.Sin(Mathf.PI * (2 * headbobCyclePosition + 0.5f))) * (headbobPower / 50))) * headbobWarmUp;
                    headbobCameraPosition.y = ((Mathf.Abs((((Mathf.Sin(Mathf.PI * (2 * headbobCyclePosition))) * 0.75f)) * (headbobPower / 50))) * headbobWarmUp) + internalEyeHeight;
                    headbobCameraPosition.z = ((Mathf.Sin(Mathf.PI * (2 * headbobCyclePosition))) * (ZTilt / 3)) * headbobWarmUp;
                } else {
                    headbobCameraPosition = Vector3.MoveTowards(headbobCameraPosition, Vector3.up * internalEyeHeight, Time.deltaTime / (headbobPower * 0.3f));
                    headbobWarmUp = 0.1f;
                }
                playerCamera.transform.localPosition = (Vector2)headbobCameraPosition;
                if (StepCycle > (headbobCyclePosition * 3)) { StepCycle = headbobCyclePosition + 0.5f; }
            }
        }
        void UpdateCameraPosition_3rdPerson() {

            //Camera Obstacle Check
            cameraObstCheck = new Ray(headPos + (quatHeadRot * (Vector3.forward * capsule.radius)), quatHeadRot * -Vector3.forward);
            if (Physics.SphereCast(cameraObstCheck, 0.5f, out cameraObstResult, maxCameraDistInternal, cameraObstructionIgnore, QueryTriggerInteraction.Ignore)) {
                currentCameraZ = -(Vector3.Distance(headPos, cameraObstResult.point) * 0.9f);

            } else {
                currentCameraZ = Mathf.SmoothDamp(currentCameraZ, -(maxCameraDistInternal * 0.85f), ref cameraZRef, Time.deltaTime, 10, Time.fixedDeltaTime);
            }

            //Debugging
            if (enableMouseAndCameraDebugging) {
                Debug.Log(headRot);
                Debug.DrawRay(cameraObstCheck.origin, cameraObstCheck.direction * maxCameraDistance, Color.red);
                Debug.DrawRay(cameraObstCheck.origin, cameraObstCheck.direction * -currentCameraZ, Color.green);
            }
            currentCameraPos = headPos + (quatHeadRot * (Vector3.forward * currentCameraZ));
            playerCamera.transform.position = currentCameraPos;
            playerCamera.transform.rotation = quatHeadRot;
        }

        void UpdateBodyRotation_3rdPerson() {
            //if is moving, rotate capsule to match camera forward   //change button down to bool of isFiring or isTargeting
            if (!isIdle && !isSliding && currentGroundInfo.isGettingGroundInfo) {
                transform.rotation = (Quaternion.Euler(0, Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, (Mathf.Atan2(InputDir.x, InputDir.z) * Mathf.Rad2Deg), 10), 0));
                //transform.rotation = Quaternion.Euler(0,Mathf.MoveTowardsAngle(transform.eulerAngles.y,(Mathf.Atan2(InputDir.x,InputDir.z)*Mathf.Rad2Deg),2.5f), 0);
            } else if (isSliding) {
                transform.localRotation = (Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, (Mathf.Atan2(p_Rigidbody.velocity.x, p_Rigidbody.velocity.z) * Mathf.Rad2Deg), 10)));
            } else if (!currentGroundInfo.isGettingGroundInfo && rotateCharacterToCameraForward) {
                transform.localRotation = (Quaternion.Euler(Vector3.up * Mathf.MoveTowardsAngle(p_Rigidbody.rotation.eulerAngles.y, headRot.y, 10)));
            }
        }
        #endregion

        #region Movement Functions
        void MovePlayer(Vector3 Direction, float Speed) {
            // GroundInfo gI = GetGroundInfo();
            isIdle = Direction.normalized.magnitude <= 0;
            _2DVelocity = Vector2.right * p_Rigidbody.velocity.x + Vector2.up * p_Rigidbody.velocity.z;
            speedToVelocityRatio = (Mathf.Lerp(0, 2, Mathf.InverseLerp(0, (sprintingSpeed / 50), _2DVelocity.magnitude)));
            _2DVelocityMag = Mathf.Clamp((walkingSpeed / 50) / _2DVelocity.magnitude, 0f, 2f);


            //Movement
            if ((currentGroundInfo.isGettingGroundInfo) && !Jumped && !isSliding && !doingPosInterp)
            {
                //Deceleration
                if (Direction.magnitude == 0 && p_Rigidbody.velocity.normalized.magnitude > 0.1f) {
                    p_Rigidbody.AddForce(-new Vector3(p_Rigidbody.velocity.x, currentGroundInfo.isInContactWithGround ? p_Rigidbody.velocity.y - Physics.gravity.y : 0, p_Rigidbody.velocity.z) * (decelerationSpeed * Time.fixedDeltaTime), ForceMode.Force);
                }
                //normal speed
                else if ((currentGroundInfo.isGettingGroundInfo) && currentGroundInfo.groundAngle < hardSlopeLimit && currentGroundInfo.groundAngle_Raw < hardSlopeLimit) {
                    p_Rigidbody.velocity = (Vector3.MoveTowards(p_Rigidbody.velocity, Vector3.ClampMagnitude(((Direction) * ((Speed) * Time.fixedDeltaTime)) + (Vector3.down), Speed / 50), 1));
                }
                capsule.sharedMaterial = InputDir.magnitude > 0 ? _ZeroFriction : _MaxFriction;
            }
            //Sliding
            else if (isSliding) {
                p_Rigidbody.AddForce(-(p_Rigidbody.velocity - Physics.gravity) * (slidingDeceleration * Time.fixedDeltaTime), ForceMode.Force);
            }

            //Air Control
            else if (!currentGroundInfo.isGettingGroundInfo) {
                p_Rigidbody.AddForce((((Direction * (walkingSpeed)) * Time.fixedDeltaTime) * airControlFactor * 5) * currentGroundInfo.groundAngleMultiplier_Inverse_persistent, ForceMode.Acceleration);
                p_Rigidbody.velocity = Vector3.ClampMagnitude((Vector3.right * p_Rigidbody.velocity.x + Vector3.forward * p_Rigidbody.velocity.z), (walkingSpeed / 50)) + (Vector3.up * p_Rigidbody.velocity.y);
                if (!currentGroundInfo.potentialStair && jumpEnhancements) {
                    if (p_Rigidbody.velocity.y < 0 && p_Rigidbody.velocity.y > Physics.gravity.y * 1.5f) {
                        p_Rigidbody.velocity += Vector3.up * (Physics.gravity.y * (decentMultiplier) * Time.fixedDeltaTime);
                    } else if (p_Rigidbody.velocity.y > 0 && !jumpInput_Momentary) {
                        p_Rigidbody.velocity += Vector3.up * (Physics.gravity.y * (tapJumpMultiplier - 1) * Time.fixedDeltaTime);
                    }
                }
            }


        }
        void Jump(float Force) {
            if ((currentGroundInfo.isInContactWithGround) &&
                (currentGroundInfo.groundAngle < hardSlopeLimit) &&
                ((enableStaminaSystem && jumpingDepletesStamina) ? currentStaminaLevel > s_JumpStaminaDepletion * 1.2f : true) &&
                (Time.time > (jumpBlankingPeriod + 0.1f)) &&
                (currentStance == Stances.Standing && !Jumped)) {

                Jumped = true;
                p_Rigidbody.velocity = (Vector3.right * p_Rigidbody.velocity.x) + (Vector3.forward * p_Rigidbody.velocity.z);
                p_Rigidbody.AddForce(Vector3.up * (Force / 10), ForceMode.Impulse);
                if (enableStaminaSystem && jumpingDepletesStamina) {
                    InstantStaminaReduction(s_JumpStaminaDepletion);
                }
                capsule.sharedMaterial = _ZeroFriction;
                jumpBlankingPeriod = Time.time;
            }
        }
        public void DoJump(float Force = 10.0f) {
            if (
                (Time.time > (jumpBlankingPeriod + 0.1f)) &&
                (currentStance == Stances.Standing)) {
                Jumped = true;
                p_Rigidbody.velocity = (Vector3.right * p_Rigidbody.velocity.x) + (Vector3.forward * p_Rigidbody.velocity.z);
                p_Rigidbody.AddForce(Vector3.up * (Force / 10), ForceMode.Impulse);
                if (enableStaminaSystem && jumpingDepletesStamina) {
                    InstantStaminaReduction(s_JumpStaminaDepletion);
                }
                capsule.sharedMaterial = _ZeroFriction;
                jumpBlankingPeriod = Time.time;
            }
        }
        void Slide() {
            if (!isSliding) {
                if (currentGroundInfo.isInContactWithGround) {
                    //do debug print
                    if (enableMovementDebugging) { print("Starting Slide."); }
                    p_Rigidbody.AddForce((transform.forward * ((sprintingSpeed)) + (Vector3.up * currentGroundInfo.groundInfluenceDirection.y)), ForceMode.Force);
                    cachedDirPreSlide = transform.forward;
                    cachedPosPreSlide = transform.position;
                    capsule.sharedMaterial = _ZeroFriction;
                    StartCoroutine(ApplyStance(slidingTransitionSpeed, Stances.Crouching));
                    isSliding = true;
                }
            } else if (slideInput_Momentary) {
                if (enableMovementDebugging) { print("Continuing Slide."); }
                if (Vector3.Distance(transform.position, cachedPosPreSlide) < maxFlatSlideDistance) { p_Rigidbody.AddForce(cachedDirPreSlide * (sprintingSpeed / 50), ForceMode.Force); }
                if (p_Rigidbody.velocity.magnitude > sprintingSpeed / 50) { p_Rigidbody.velocity = p_Rigidbody.velocity.normalized * (sprintingSpeed / 50); }
                else if (p_Rigidbody.velocity.magnitude < (crouchingSpeed / 25)) {
                    if (enableMovementDebugging) { print("Slide too slow, ending slide into crouch."); }
                    //capsule.sharedMaterial = _MaxFrix;
                    isSliding = false;
                    isSprinting = false;
                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                    currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                }
            } else {
                if (OverheadCheck()) {
                    if (p_Rigidbody.velocity.magnitude > (walkingSpeed / 50)) {
                        if (enableMovementDebugging) { print("Key realeased, ending slide into a sprint."); }
                        isSliding = false;
                        StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                        currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                    } else {
                        if (enableMovementDebugging) { print("Key realeased, ending slide into a walk."); }
                        isSliding = false;
                        StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                        currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                    }
                } else {
                    if (enableMovementDebugging) { print("Key realeased but there is an obstruction. Ending slide into crouch."); }
                    isSliding = false;
                    isSprinting = false;
                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                    currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                }

            }
        }
        void GetGroundInfo() {
            //to Get if we're actually touching ground.
            //to act as a normal and point buffer.
            currentGroundInfo.groundFromSweep = null;

            currentGroundInfo.groundFromSweep = Physics.SphereCastAll(transform.position, capsule.radius - 0.001f, Vector3.down, ((capsule.height / 2)) - (capsule.radius / 2), whatIsGround);
            currentGroundInfo.isInContactWithGround = Physics.Raycast(transform.position, Vector3.down, out currentGroundInfo.groundFromRay, (capsule.height / 2) + 0.25f, whatIsGround);

            if (Jumped && (Physics.Raycast(transform.position, Vector3.down, (capsule.height / 2) + 0.1f, whatIsGround) || Physics.CheckSphere(transform.position - (Vector3.up * ((capsule.height / 2) - (capsule.radius - 0.05f))), capsule.radius, whatIsGround)) && Time.time > (jumpBlankingPeriod + 0.1f)) {
                Jumped = false;
            }

            //if(Result.isGrounded){
            if (currentGroundInfo.groundFromSweep != null && currentGroundInfo.groundFromSweep.Length != 0) {
                currentGroundInfo.isGettingGroundInfo = true;
                currentGroundInfo.groundNormals_lowgrade.Clear();
                currentGroundInfo.groundNormals_highgrade.Clear();
                foreach (RaycastHit hit in currentGroundInfo.groundFromSweep) {
                    if (hit.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Angle(hit.normal, Vector3.up) < hardSlopeLimit) {
                        currentGroundInfo.groundNormals_lowgrade.Add(hit.normal);
                    } else {
                        currentGroundInfo.groundNormals_highgrade.Add(hit.normal);
                    }
                }
                if (currentGroundInfo.groundNormals_lowgrade.Any()) {
                    currentGroundInfo.groundNormal_Averaged = Average(currentGroundInfo.groundNormals_lowgrade);
                } else {
                    currentGroundInfo.groundNormal_Averaged = Average(currentGroundInfo.groundNormals_highgrade);
                }
                currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromSweep.Average(x => (x.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Angle(x.normal, Vector3.up) < hardSlopeLimit) ? x.point.y : currentGroundInfo.groundFromRay.point.y); //Mathf.MoveTowards(currentGroundInfo.groundRawYPosition, currentGroundInfo.groundFromSweep.Average(x=> (x.point.y > currentGroundInfo.groundFromRay.point.y && Vector3.Dot(x.normal,Vector3.up)<-0.25f) ? x.point.y :  currentGroundInfo.groundFromRay.point.y),Time.deltaTime*2);

            } else {
                currentGroundInfo.isGettingGroundInfo = false;
                currentGroundInfo.groundNormal_Averaged = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundNormal_Raw = currentGroundInfo.groundFromRay.normal;
                currentGroundInfo.groundRawYPosition = currentGroundInfo.groundFromRay.point.y;
            }

            if (currentGroundInfo.isGettingGroundInfo) { currentGroundInfo.groundAngleMultiplier_Inverse_persistent = currentGroundInfo.groundAngleMultiplier_Inverse; }
            //{
            currentGroundInfo.groundInfluenceDirection = Vector3.MoveTowards(currentGroundInfo.groundInfluenceDirection, Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.Cross(currentGroundInfo.groundNormal_Averaged, Vector3.up)).normalized, 2 * Time.fixedDeltaTime);
            currentGroundInfo.groundInfluenceDirection.y = 0;
            currentGroundInfo.groundAngle = Vector3.Angle(currentGroundInfo.groundNormal_Averaged, Vector3.up);
            currentGroundInfo.groundAngle_Raw = Vector3.Angle(currentGroundInfo.groundNormal_Raw, Vector3.up);
            currentGroundInfo.groundAngleMultiplier_Inverse = ((currentGroundInfo.groundAngle - 90) * -1) / 90;
            currentGroundInfo.groundAngleMultiplier = ((currentGroundInfo.groundAngle)) / 90;
            //
            currentGroundInfo.groundTag = currentGroundInfo.isInContactWithGround ? currentGroundInfo.groundFromRay.transform.tag : string.Empty;
            if (Physics.Raycast(transform.position + (Vector3.down * ((capsule.height * 0.5f) - 0.1f)), InputDir, out currentGroundInfo.stairCheck_RiserCheck, capsule.radius + 0.1f, whatIsGround)) {
                if (Physics.Raycast(currentGroundInfo.stairCheck_RiserCheck.point + (currentGroundInfo.stairCheck_RiserCheck.normal * -0.05f) + Vector3.up, Vector3.down, out currentGroundInfo.stairCheck_HeightCheck, 1.1f)) {
                    if (!Physics.Raycast(transform.position + (Vector3.down * ((capsule.height * 0.5f) - maxStairRise)) + InputDir * (capsule.radius - 0.05f), InputDir, 0.2f, whatIsGround)) {
                        if (!isIdle && currentGroundInfo.stairCheck_HeightCheck.point.y > (currentGroundInfo.stairCheck_RiserCheck.point.y + 0.025f) /* Vector3.Angle(currentGroundInfo.groundFromRay.normal, Vector3.up)<5 */ && Vector3.Angle(currentGroundInfo.groundNormal_Averaged, currentGroundInfo.stairCheck_RiserCheck.normal) > 0.5f) {
                            p_Rigidbody.position -= Vector3.up * -0.1f;
                            currentGroundInfo.potentialStair = true;
                        }
                    } else { currentGroundInfo.potentialStair = false; }
                }
            } else { currentGroundInfo.potentialStair = false; }


            currentGroundInfo.playerGroundPosition = Mathf.MoveTowards(currentGroundInfo.playerGroundPosition, currentGroundInfo.groundRawYPosition + (capsule.height / 2) + 0.01f, 0.05f);
            //}

            if (currentGroundInfo.isInContactWithGround && enableFootstepSounds && shouldCalculateFootstepTriggers) {
                if (currentGroundInfo.groundFromRay.collider is TerrainCollider) {
                    currentGroundInfo.groundMaterial = null;
                    currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                    currentGroundInfo.currentTerrain = currentGroundInfo.groundFromRay.transform.GetComponent<Terrain>();
                    if (currentGroundInfo.currentTerrain) {
                        Vector2 XZ = (Vector2.right * (((transform.position.x - currentGroundInfo.currentTerrain.transform.position.x) / currentGroundInfo.currentTerrain.terrainData.size.x)) * currentGroundInfo.currentTerrain.terrainData.alphamapWidth) + (Vector2.up * (((transform.position.z - currentGroundInfo.currentTerrain.transform.position.z) / currentGroundInfo.currentTerrain.terrainData.size.z)) * currentGroundInfo.currentTerrain.terrainData.alphamapHeight);
                        float[,,] aMap = currentGroundInfo.currentTerrain.terrainData.GetAlphamaps((int)XZ.x, (int)XZ.y, 1, 1);
                        for (int i = 0; i < aMap.Length; i++) {
                            if (aMap[0, 0, i] == 1) {
                                currentGroundInfo.groundLayer = currentGroundInfo.currentTerrain.terrainData.terrainLayers[i];
                                break;
                            }
                        }
                    } else { currentGroundInfo.groundLayer = null; }
                } else {
                    currentGroundInfo.groundLayer = null;
                    currentGroundInfo.groundPhysicMaterial = currentGroundInfo.groundFromRay.collider.sharedMaterial;
                    currentGroundInfo.currentMesh = currentGroundInfo.groundFromRay.transform.GetComponent<MeshFilter>().sharedMesh;
                    if (currentGroundInfo.currentMesh && currentGroundInfo.currentMesh.isReadable) {
                        int limit = currentGroundInfo.groundFromRay.triangleIndex * 3, submesh;
                        for (submesh = 0; submesh < currentGroundInfo.currentMesh.subMeshCount; submesh++) {
                            int indices = currentGroundInfo.currentMesh.GetTriangles(submesh).Length;
                            if (indices > limit) { break; }
                            limit -= indices;
                        }
                        currentGroundInfo.groundMaterial = currentGroundInfo.groundFromRay.transform.GetComponent<Renderer>().sharedMaterials[submesh];
                    } else { currentGroundInfo.groundMaterial = currentGroundInfo.groundFromRay.collider.GetComponent<MeshRenderer>().sharedMaterial; }
                }
            } else { currentGroundInfo.groundMaterial = null; currentGroundInfo.groundLayer = null; currentGroundInfo.groundPhysicMaterial = null; }
#if UNITY_EDITOR
            if (enableGroundingDebugging) {
                print("Grounded: " + currentGroundInfo.isInContactWithGround + ", Ground Hits: " + currentGroundInfo.groundFromSweep.Length + ", Ground Angle: " + currentGroundInfo.groundAngle.ToString("0.00") + ", Ground Multi: " + currentGroundInfo.groundAngleMultiplier.ToString("0.00") + ", Ground Multi Inverse: " + currentGroundInfo.groundAngleMultiplier_Inverse.ToString("0.00"));
                print("Ground mesh readable for dynamic foot steps: " + currentGroundInfo.currentMesh?.isReadable);
                Debug.DrawRay(transform.position, Vector3.down * ((capsule.height / 2) + 0.1f), Color.green);
                Debug.DrawRay(transform.position, currentGroundInfo.groundInfluenceDirection, Color.magenta);
                Debug.DrawRay(transform.position + (Vector3.down * ((capsule.height * 0.5f) - 0.05f)) + InputDir * (capsule.radius - 0.05f), InputDir * (capsule.radius + 0.1f), Color.cyan);
                Debug.DrawRay(transform.position + (Vector3.down * ((capsule.height * 0.5f) - 0.5f)) + InputDir * (capsule.radius - 0.05f), InputDir * (capsule.radius + 0.3f), new Color(0, .2f, 1, 1));
            }
#endif
        }
        void GroundMovementSpeedUpdate() {
#if SAIO_ENABLE_PARKOUR
        if(!isVaulting)
#endif
            {
                switch (currentGroundMovementSpeed) {
                    case GroundSpeedProfiles.Walking: {
                            if (isCrouching || isSprinting) {
                                isSprinting = false;
                                isCrouching = false;
                                currentGroundSpeed = walkingSpeed;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                            }
#if SAIO_ENABLE_PARKOUR
                    if(vaultInput && canVault){VaultCheck();}
#endif
                            //check for state change call
                            if ((canCrouch && crouchInput_FrameOf) || crouchOverride) {
                                isCrouching = true;
                                isSprinting = false;
                                currentGroundSpeed = crouchingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                                break;
                            } else if ((canSprint && sprintInput_FrameOf && ((enableStaminaSystem && jumpingDepletesStamina) ? currentStaminaLevel > s_minimumStaminaToSprint : true) && (enableSurvivalStats ? (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving) : true)) || sprintOverride) {
                                isCrouching = false;
                                isSprinting = true;
                                currentGroundSpeed = sprintingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                            }
                            break;
                        }

                    case GroundSpeedProfiles.Crouching: {
                            if (!isCrouching) {
                                isCrouching = true;
                                isSprinting = false;
                                currentGroundSpeed = crouchingSpeed;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                            }


                            //check for state change call
                            if ((toggleCrouch ? crouchInput_FrameOf : !crouchInput_Momentary) && !crouchOverride && OverheadCheck()) {
                                isCrouching = false;
                                isSprinting = false;
                                currentGroundSpeed = walkingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                                break;
                            } else if (((canSprint && sprintInput_FrameOf && ((enableStaminaSystem && jumpingDepletesStamina) ? currentStaminaLevel > s_minimumStaminaToSprint : true) && (enableSurvivalStats ? (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving) : true)) || sprintOverride) && OverheadCheck()) {
                                isCrouching = false;
                                isSprinting = true;
                                currentGroundSpeed = sprintingSpeed;
                                currentGroundMovementSpeed = GroundSpeedProfiles.Sprinting;
                                StopCoroutine("ApplyStance");
                                StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                            }
                            break;
                        }

                    case GroundSpeedProfiles.Sprinting: {
                            //if(!isIdle)
                            {
                                if (!isSprinting) {
                                    isCrouching = false;
                                    isSprinting = true;
                                    currentGroundSpeed = sprintingSpeed;
                                    StopCoroutine("ApplyStance");
                                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                                }
#if SAIO_ENABLE_PARKOUR
                        if((vaultInput || autoVaultWhenSpringing) && canVault){VaultCheck();}
#endif
                                //check for state change call
                                if (canSlide && !isIdle && slideInput_FrameOf && currentGroundInfo.isInContactWithGround) {
                                    Slide();
                                    currentGroundMovementSpeed = GroundSpeedProfiles.Sliding;
                                    break;
                                }


                                else if ((canCrouch && crouchInput_FrameOf) || crouchOverride) {
                                    isCrouching = true;
                                    isSprinting = false;
                                    currentGroundSpeed = crouchingSpeed;
                                    currentGroundMovementSpeed = GroundSpeedProfiles.Crouching;
                                    StopCoroutine("ApplyStance");
                                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Crouching));
                                    break;
                                    //Can't leave sprint in toggle sprint.
                                } else if ((toggleSprint ? sprintInput_FrameOf : !sprintInput_Momentary) && !sprintOverride) {
                                    isCrouching = false;
                                    isSprinting = false;
                                    currentGroundSpeed = walkingSpeed;
                                    currentGroundMovementSpeed = GroundSpeedProfiles.Walking;
                                    StopCoroutine("ApplyStance");
                                    StartCoroutine(ApplyStance(stanceTransitionSpeed, Stances.Standing));
                                }
                                break;
                            }
                        }
                    case GroundSpeedProfiles.Sliding: {
                        } break;
                }
            }
        }
        IEnumerator ApplyStance(float smoothSpeed, Stances newStance) {
            currentStance = newStance;
            float targetCapsuleHeight = currentStance == Stances.Standing ? standingHeight : crouchingHeight;
            float targetEyeHeight = currentStance == Stances.Standing ? standingEyeHeight : crouchingEyeHeight;
            while (!Mathf.Approximately(capsule.height, targetCapsuleHeight)) {
                changingStances = true;
                capsule.height = (smoothSpeed > 0 ? Mathf.MoveTowards(capsule.height, targetCapsuleHeight, stanceTransitionSpeed * Time.fixedDeltaTime) : targetCapsuleHeight);
                internalEyeHeight = (smoothSpeed > 0 ? Mathf.MoveTowards(internalEyeHeight, targetEyeHeight, stanceTransitionSpeed * Time.fixedDeltaTime) : targetCapsuleHeight);

                if (currentStance == Stances.Crouching && currentGroundInfo.isGettingGroundInfo) {
                    p_Rigidbody.velocity = p_Rigidbody.velocity + (Vector3.down * 2);
                    if (enableMovementDebugging) { print("Applying Stance and applying down force "); }
                }
                yield return new WaitForFixedUpdate();
            }
            changingStances = false;
            yield return null;
        }
        bool OverheadCheck() {    //Returns true when there is no obstruction.
            bool result = false;
            if (Physics.Raycast(transform.position, Vector3.up, standingHeight - (capsule.height / 2), whatIsGround)) { result = true; }
            return !result;
        }
        Vector3 Average(List<Vector3> vectors) {
            Vector3 returnVal = default(Vector3);
            vectors.ForEach(x => { returnVal += x; });
            returnVal /= vectors.Count();
            return returnVal;
        }

        #endregion

        #region Stamina System
        private void CalculateStamina() {
            if (isSprinting && !ignoreStamina && !isIdle) {
                if (currentStaminaLevel != 0) {
                    currentStaminaLevel = Mathf.MoveTowards(currentStaminaLevel, 0, s_depletionSpeed * Time.deltaTime);
                } else if (!isSliding) { currentGroundMovementSpeed = GroundSpeedProfiles.Walking; }
                staminaIsChanging = true;
            }
            else if (currentStaminaLevel != Stamina && !ignoreStamina && (enableSurvivalStats ? (!currentSurvivalStats.isDehydrated && !currentSurvivalStats.isStarving) : true)) {
                currentStaminaLevel = Mathf.MoveTowards(currentStaminaLevel, Stamina, s_regenerationSpeed * Time.deltaTime);
                staminaIsChanging = true;
            } else {
                staminaIsChanging = false;
            }
        }
        public void InstantStaminaReduction(float Reduction) {
            if (!ignoreStamina && enableStaminaSystem) { currentStaminaLevel = Mathf.Clamp(currentStaminaLevel -= Reduction, 0, Stamina); }
        }
        #endregion

        #region Footstep System
        void CalculateFootstepTriggers() {
            if (enableFootstepSounds && footstepTriggeringMode == FootstepTriggeringMode.calculatedTiming && shouldCalculateFootstepTriggers) {
                if (_2DVelocity.magnitude > (currentGroundSpeed / 100) && !isIdle) {
                    if (cameraPerspective == PerspectiveModes._1stPerson) {
                        if ((enableHeadbob ? headbobCyclePosition : Time.time) > StepCycle && currentGroundInfo.isGettingGroundInfo && !isSliding) {
                            //print("Steped");
                            CallFootstepClip();
                            StepCycle = enableHeadbob ? (headbobCyclePosition + 0.5f) : (Time.time + ((stepTiming * _2DVelocityMag) * 2));
                        }
                    } else {
                        if (Time.time > StepCycle && currentGroundInfo.isGettingGroundInfo && !isSliding) {
                            //print("Steped");
                            CallFootstepClip();
                            StepCycle = (Time.time + ((stepTiming * _2DVelocityMag) * 2));
                        }
                    }
                }
            }
        }
        public void CallFootstepClip() {
            if (playerAudioSource) {
                if (enableFootstepSounds && footstepSoundSet.Any()) {
                    for (int i = 0; i < footstepSoundSet.Count(); i++) {

                        if (footstepSoundSet[i].profileTriggerType == MatProfileType.Material) {
                            if (footstepSoundSet[i]._Materials.Contains(currentGroundInfo.groundMaterial)) {
                                currentClipSet = footstepSoundSet[i].footstepClips;
                                break;
                            } else if (i == footstepSoundSet.Count - 1) {
                                currentClipSet = null;
                            }
                        }

                        else if (footstepSoundSet[i].profileTriggerType == MatProfileType.physicMaterial) {
                            if (footstepSoundSet[i]._physicMaterials.Contains(currentGroundInfo.groundPhysicMaterial)) {
                                currentClipSet = footstepSoundSet[i].footstepClips;
                                break;
                            } else if (i == footstepSoundSet.Count - 1) {
                                currentClipSet = null;
                            }
                        }

                        else if (footstepSoundSet[i].profileTriggerType == MatProfileType.terrainLayer) {
                            if (footstepSoundSet[i]._Layers.Contains(currentGroundInfo.groundLayer)) {
                                currentClipSet = footstepSoundSet[i].footstepClips;
                                break;
                            } else if (i == footstepSoundSet.Count - 1) {
                                currentClipSet = null;
                            }
                        }
                    }

                    if (currentClipSet != null && currentClipSet.Any()) {
                        playerAudioSource.PlayOneShot(currentClipSet[Random.Range(0, currentClipSet.Count())]);
                    }
                }
            }
        }
        #endregion

        #region Parkour Functions
#if SAIO_ENABLE_PARKOUR
    void VaultCheck(){
        if(!isVaulting){
            if(enableVaultDebugging){ Debug.DrawRay(transform.position-(Vector3.up*(capsule.height/4)), transform.forward*(capsule.radius*2), Color.blue,120);}
            if(Physics.Raycast(transform.position-(Vector3.up*(capsule.height/4)), transform.forward,out VC_Stage1,capsule.radius*2) && VC_Stage1.transform.CompareTag(vaultObjectTag)){
                float vaultObjAngle = Mathf.Acos(Vector3.Dot(Vector3.up,(Quaternion.LookRotation(VC_Stage1.normal,Vector3.up)*Vector3.up))) * Mathf.Rad2Deg;

                if(enableVaultDebugging) {Debug.DrawRay((VC_Stage1.normal*-0.05f)+(VC_Stage1.point+((Quaternion.LookRotation(VC_Stage1.normal,Vector3.up)*Vector3.up)*(maxVaultHeight))), -(Quaternion.LookRotation(VC_Stage1.normal,Vector3.up)*Vector3.up)*(capsule.height),Color.cyan,120);}
                if(Physics.Raycast((VC_Stage1.normal*-0.05f)+(VC_Stage1.point+((Quaternion.LookRotation(VC_Stage1.normal,Vector3.up)*Vector3.up)*(maxVaultHeight))), -(Quaternion.LookRotation(VC_Stage1.normal,Vector3.up)*Vector3.up), out VC_Stage2,capsule.height) && VC_Stage2.transform == VC_Stage1.transform && VC_Stage2.point.y <= currentGroundInfo.groundRawYPosition+maxVaultHeight+vaultObjAngle){
                    vaultForwardVec = -VC_Stage1.normal;

                    if(enableVaultDebugging) {Debug.DrawLine(VC_Stage2.point+(vaultForwardVec*maxVaultDepth)-(Vector3.up*0.01f), (VC_Stage2.point- (Vector3.up*.01f)), Color.red,120   );}
                    if(Physics.Linecast((VC_Stage2.point+(vaultForwardVec*maxVaultDepth))-(Vector3.up*0.01f), VC_Stage2.point - (Vector3.up*0.01f),out VC_Stage3)){
                        Ray vc4 = new Ray(VC_Stage3.point+(vaultForwardVec*(capsule.radius+(vaultObjAngle*0.01f))),Vector3.down);
                        if(enableVaultDebugging){ Debug.DrawRay(vc4.origin, vc4.direction,Color.green,120);}
                        Physics.SphereCast(vc4,capsule.radius,out VC_Stage4,maxVaultHeight+(capsule.height/2));
                        Vector3 proposedPos = ((Vector3.right*vc4.origin.x)+(Vector3.up*(VC_Stage4.point.y+(capsule.height/2)+0.01f))+(Vector3.forward*vc4.origin.z)) + (VC_Stage3.normal*0.02f);

                        if(VC_Stage4.collider && !Physics.CheckCapsule(proposedPos-(Vector3.up*((capsule.height/2)-capsule.radius)), proposedPos+(Vector3.up*((capsule.height/2)-capsule.radius)),capsule.radius)){
                            isVaulting = true;
                            StopCoroutine("PositionInterp");
                            StartCoroutine(PositionInterp(proposedPos, vaultSpeed));

                        }else if(enableVaultDebugging){Debug.Log("Cannot Vault this Object. Sufficient space/ground was not found on the other side of the vault object.");}
                    }else if(enableVaultDebugging){Debug.Log("Cannot Vault this object. Object is too deep or there is an obstruction on the other side.");}
                }if(enableVaultDebugging){Debug.Log("Vault Object is too high or there is something ontop of the object that is not marked as vaultable.");}

            }

        }else if(!doingPosInterp){
            isVaulting = false;
        }
    }
    
    IEnumerator PositionInterp(Vector3 pos, float speed){
        doingPosInterp = true;
        Vector3 vel = p_Rigidbody.velocity;
        p_Rigidbody.useGravity = false;
        p_Rigidbody.velocity = Vector3.zero;
        capsule.enabled = false;
        while(Vector3.Distance(p_Rigidbody.position, pos)>0.01f){
            p_Rigidbody.velocity = Vector3.zero;
            p_Rigidbody.position = (Vector3.MoveTowards(p_Rigidbody.position, pos,speed*Time.fixedDeltaTime));
            yield return new WaitForFixedUpdate();
        }
        capsule.enabled = true;
        p_Rigidbody.useGravity = true;
        p_Rigidbody.velocity = vel;
        doingPosInterp = false;
        if(isVaulting){VaultCheck();}
    }
#endif
        #endregion

        #region Survival Stat Functions
        public void TickStats() {
            if (currentSurvivalStats.Hunger > 0) {
                currentSurvivalStats.Hunger = Mathf.Clamp(currentSurvivalStats.Hunger - (hungerDepletionRate + (isSprinting && !isIdle ? 0.1f : 0)), 0, defaultSurvivalStats.Hunger);
                currentSurvivalStats.isStarving = (currentSurvivalStats.Hunger < (defaultSurvivalStats.Hunger / 10));
            }
            if (currentSurvivalStats.Hydration > 0) {
                currentSurvivalStats.Hydration = Mathf.Clamp(currentSurvivalStats.Hydration - (hydrationDepletionRate + (isSprinting && !isIdle ? 0.1f : 0)), 0, defaultSurvivalStats.Hydration);
                currentSurvivalStats.isDehydrated = (currentSurvivalStats.Hydration < (defaultSurvivalStats.Hydration / 8));
            }
            currentSurvivalStats.hasLowHealth = (currentSurvivalStats.Health < (defaultSurvivalStats.Health / 10));

            StatTickTimer = Time.time + (60 / statTickRate);
        }
        public void ImmediateStateChange(float Amount, StatSelector Stat = StatSelector.Health) {
            switch (Stat) {
                case StatSelector.Health: {
                        currentSurvivalStats.Health = Mathf.Clamp(currentSurvivalStats.Health + Amount, 0, defaultSurvivalStats.Health);
                        currentSurvivalStats.hasLowHealth = (currentSurvivalStats.Health < (defaultSurvivalStats.Health / 10));

                    } break;

                case StatSelector.Hunger: {
                        currentSurvivalStats.Hunger = Mathf.Clamp(currentSurvivalStats.Hunger + Amount, 0, defaultSurvivalStats.Hunger);
                        currentSurvivalStats.isStarving = (currentSurvivalStats.Hunger < (defaultSurvivalStats.Hunger / 10));
                    } break;

                case StatSelector.Hydration: {
                        currentSurvivalStats.Hydration = Mathf.Clamp(currentSurvivalStats.Hydration + Amount, 0, defaultSurvivalStats.Hydration);
                        currentSurvivalStats.isDehydrated = (currentSurvivalStats.Hydration < (defaultSurvivalStats.Hydration / 8));
                    } break;
            }
        }
        public void LevelUpStat(float newMaxStatLevel, StatSelector Stat = StatSelector.Health, bool Refill = true) {
            switch (Stat) {
                case StatSelector.Health: {
                        defaultSurvivalStats.Health = Mathf.Clamp(newMaxStatLevel, 0, newMaxStatLevel); ;
                        if (Refill) { currentSurvivalStats.Health = Mathf.Clamp(newMaxStatLevel, 0, newMaxStatLevel); }
                        currentSurvivalStats.hasLowHealth = (currentSurvivalStats.Health < (defaultSurvivalStats.Health / 10));

                    } break;
                case StatSelector.Hunger: {
                        defaultSurvivalStats.Hunger = Mathf.Clamp(newMaxStatLevel, 0, newMaxStatLevel); ;
                        if (Refill) { currentSurvivalStats.Hunger = Mathf.Clamp(newMaxStatLevel, 0, newMaxStatLevel); }
                        currentSurvivalStats.isStarving = (currentSurvivalStats.Hunger < (defaultSurvivalStats.Hunger / 10));

                    } break;
                case StatSelector.Hydration: {
                        defaultSurvivalStats.Hydration = Mathf.Clamp(newMaxStatLevel, 0, newMaxStatLevel); ;
                        if (Refill) { currentSurvivalStats.Hydration = Mathf.Clamp(newMaxStatLevel, 0, newMaxStatLevel); }
                        currentSurvivalStats.isDehydrated = (currentSurvivalStats.Hydration < (defaultSurvivalStats.Hydration / 8));

                    } break;
            }
        }

        #endregion

        #region Animator Update
        void UpdateAnimationTriggers(bool zeroOut = false) {
            switch (cameraPerspective) {
                case PerspectiveModes._1stPerson: {
                        if (_1stPersonCharacterAnimator) {
                            //Setup Fistperson animation triggers here.

                        }
                    } break;

                case PerspectiveModes._3rdPerson: {
                        if (_3rdPersonCharacterAnimator) {
                            if (stickRendererToCapsuleBottom) {
                                _3rdPersonCharacterAnimator.transform.position = (Vector3.right * _3rdPersonCharacterAnimator.transform.position.x) + (Vector3.up * (transform.position.y - (capsule.height / 2))) + (Vector3.forward * _3rdPersonCharacterAnimator.transform.position.z);
                            }
                            if (!zeroOut) {
                                //Setup Thirdperson animation triggers here.
                                if (a_velocity != "") {
                                    _3rdPersonCharacterAnimator.SetFloat(a_velocity, p_Rigidbody.velocity.sqrMagnitude);
                                }
                                if (a_2DVelocity != "") {
                                    _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, _2DVelocity.magnitude);
                                }
                                if (a_Idle != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Idle, isIdle);
                                }
                                if (a_Sprinting != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sprinting, isSprinting);
                                }
                                if (a_Crouching != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Crouching, isCrouching);
                                }
                                if (a_Sliding != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sliding, isSliding);
                                }
                                if (a_Jumped != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Jumped, Jumped);
                                }
                                if (a_Grounded != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Grounded, currentGroundInfo.isInContactWithGround);
                                }
                            } else {
                                if (a_velocity != "") {
                                    _3rdPersonCharacterAnimator.SetFloat(a_velocity, 0);
                                }
                                if (a_2DVelocity != "") {
                                    _3rdPersonCharacterAnimator.SetFloat(a_2DVelocity, 0);
                                }
                                if (a_Idle != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Idle, true);
                                }
                                if (a_Sprinting != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sprinting, false);
                                }
                                if (a_Crouching != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Crouching, false);
                                }
                                if (a_Sliding != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Sliding, false);
                                }
                                if (a_Jumped != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Jumped, false);
                                }
                                if (a_Grounded != "") {
                                    _3rdPersonCharacterAnimator.SetBool(a_Grounded, true);
                                }
                            }

                        }

                    } break;
            }
        }
        #endregion

        #region Interactables
        public bool TryInteract() {
            if (cameraPerspective == PerspectiveModes._3rdPerson) {
                Collider[] cols = Physics.OverlapBox(transform.position + (transform.forward * (interactRange / 2)), Vector3.one * (interactRange / 2), transform.rotation, interactableLayer, QueryTriggerInteraction.Ignore);
                IInteractable interactable = null;
                float lastColestDist = 100;
                foreach (Collider c in cols) {
                    IInteractable i = c.GetComponent<IInteractable>();
                    if (i != null) {
                        float d = Vector3.Distance(transform.position, c.transform.position);
                        if (d < lastColestDist) {
                            lastColestDist = d;
                            interactable = i;
                        }
                    }
                }
                return ((interactable != null) ? interactable.Interact() : false);

            } else {
                RaycastHit h;
                if (Physics.SphereCast(playerCamera.transform.position, 0.25f, playerCamera.transform.forward, out h, interactRange, interactableLayer, QueryTriggerInteraction.Ignore)) {
                    IInteractable i = h.collider.GetComponent<IInteractable>();
                    if (i != null) {
                        return i.Interact();
                    }
                }
            }
            return false;
        }
        #endregion

        #region Gizmos
#if UNITY_EDITOR
        private void OnDrawGizmos() {
            if (enableGroundingDebugging) {
                if (Application.isPlaying) {

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(transform.position - (Vector3.up * ((capsule.height / 2) - (capsule.radius + 0.1f))), capsule.radius);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position - (Vector3.up * ((capsule.height / 2) - (capsule.radius - 0.5f))), capsule.radius);
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(new Vector3(transform.position.x, currentGroundInfo.playerGroundPosition, transform.position.z), 0.05f);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(new Vector3(transform.position.x, currentGroundInfo.groundRawYPosition, transform.position.z), 0.05f);
                    Gizmos.color = Color.green;

                }

            }

#if SAIO_ENABLE_PARKOUR
        if(enableVaultDebugging &&Application.isPlaying){
            Gizmos.DrawWireSphere(VC_Stage3.point+(vaultForwardVec*(capsule.radius)),capsule.radius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(VC_Stage4.point,capsule.radius);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(((Vector3.right*(VC_Stage3.point+(vaultForwardVec*(capsule.radius))).x)+(Vector3.up*(VC_Stage4.point.y+(capsule.height/2)+0.01f))+(Vector3.forward*(VC_Stage3.point+(vaultForwardVec*(capsule.radius))).z)),capsule.radius);
        }
#endif
        }
#endif
        #endregion

        public void PausePlayer(PauseModes pauseMode) {
            controllerPaused = true;
            switch (pauseMode) {
                case PauseModes.MakeKinematic: {
                        p_Rigidbody.isKinematic = true;
                    } break;

                case PauseModes.FreezeInPlace: {
                        p_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    } break;

                case PauseModes.BlockInputOnly: {

                    } break;
            }

            p_Rigidbody.velocity = Vector3.zero;
            InputDir = Vector2.zero;
            MovInput = Vector2.zero;
            MovInput_Smoothed = Vector2.zero;
            capsule.sharedMaterial = _MaxFriction;

            UpdateAnimationTriggers(true);
            if (a_velocity != "") {
                _3rdPersonCharacterAnimator.SetFloat(a_velocity, 0);
            }
        }
        public void UnpausePlayer(float delay = 0) {
            if (delay == 0) {
                controllerPaused = false;
                p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                p_Rigidbody.isKinematic = false;
            }
            else {
                StartCoroutine(UnpausePlayerI(delay));
            }
        }
        IEnumerator UnpausePlayerI(float delay) {
            yield return new WaitForSecondsRealtime(delay);
            controllerPaused = false;
            p_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            p_Rigidbody.isKinematic = false;
        }

    }


    #region Classes and Enums
    [System.Serializable]
    public class GroundInfo {
        public bool isInContactWithGround, isGettingGroundInfo, potentialStair;
        public float groundAngleMultiplier_Inverse = 1, groundAngleMultiplier_Inverse_persistent = 1, groundAngleMultiplier = 0, groundAngle, groundAngle_Raw, playerGroundPosition, groundRawYPosition;
        public Vector3 groundInfluenceDirection, groundNormal_Averaged, groundNormal_Raw;
        public List<Vector3> groundNormals_lowgrade = new List<Vector3>(), groundNormals_highgrade;
        public string groundTag;
        public Material groundMaterial;
        public TerrainLayer groundLayer;
        public PhysicMaterial groundPhysicMaterial;
        internal Terrain currentTerrain;
        internal Mesh currentMesh;
        internal RaycastHit groundFromRay, stairCheck_RiserCheck, stairCheck_HeightCheck;
        internal RaycastHit[] groundFromSweep;


    }
    [System.Serializable]
    public class GroundMaterialProfile {
        public MatProfileType profileTriggerType = MatProfileType.Material;
        public List<Material> _Materials;
        public List<PhysicMaterial> _physicMaterials;
        public List<TerrainLayer> _Layers;
        public List<AudioClip> footstepClips = new List<AudioClip>();
    }
    [System.Serializable]
    public class SurvivalStats {
        public float Health = 250.0f, Hunger = 100.0f, Hydration = 100f;
        public bool hasLowHealth, isStarving, isDehydrated;
    }
    public enum StatSelector { Health, Hunger, Hydration }
    public enum MatProfileType { Material, terrainLayer, physicMaterial }
    public enum FootstepTriggeringMode { calculatedTiming, calledFromAnimations }
    public enum PerspectiveModes { _1stPerson, _3rdPerson }
    public enum ViewInputModes { Traditional, Retro }
    public enum MouseInputInversionModes { None, X, Y, Both }
    public enum GroundSpeedProfiles { Crouching, Walking, Sprinting, Sliding }
    public enum Stances { Standing, Crouching }
    public enum PauseModes { MakeKinematic, FreezeInPlace, BlockInputOnly }
    #endregion

    #region Interfaces
    public interface IInteractable {
        bool Interact();
    }
}
#endregion







using System.Collections.Generic;
using GameCamp.Game.Combat.Projectiles;
using GameCamp.Game.Core;
using GameCamp.Game.Data;
using GameCamp.Game.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCamp.Game.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private PlayerStatRuntime statRuntime = new();
        [SerializeField] private ProjectilePool projectilePool;
        [SerializeField] private Transform defaultMuzzle;

        [Header("Movement")]
        [SerializeField] private bool enableHorizontalDragMove = true;
        [SerializeField] private float minX = -3.5f;
        [SerializeField] private float maxX = 3.5f;

        [Header("Animation")]
        [SerializeField] private Animator animator;

        private readonly List<WeaponModuleBase> activeWeapons = new();
        private readonly Dictionary<int, WeaponModuleBase> weaponById = new();
        private readonly HashSet<WeaponType> ownedWeaponTypes = new();

        private static readonly int IdleStateHash = Animator.StringToHash("Idle");
        private static readonly int WalkStateHash = Animator.StringToHash("Walk");

        private GameFlowController gameFlowController;
        private bool isGameplayPlaying = true;
        private Camera cachedCamera;
        private float dragOffsetX;
        private bool isDragging;
        private bool isWalkingAnimation;

        private void Awake()
        {
            if (defaultMuzzle == null)
            {
                defaultMuzzle = transform;
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            cachedCamera = Camera.main;
        }

        private void OnDestroy()
        {
            if (gameFlowController != null)
            {
                gameFlowController.OnStateChanged -= HandleGameFlowStateChanged;
            }
        }

        public void SetGameFlowController(GameFlowController controller)
        {
            if (gameFlowController == controller)
            {
                return;
            }

            if (gameFlowController != null)
            {
                gameFlowController.OnStateChanged -= HandleGameFlowStateChanged;
            }

            gameFlowController = controller;

            if (gameFlowController == null)
            {
                Debug.LogError($"{nameof(PlayerController)} requires {nameof(GameFlowController)} dependency.", this);
                isGameplayPlaying = false;
                return;
            }

            gameFlowController.OnStateChanged += HandleGameFlowStateChanged;
            HandleGameFlowStateChanged(gameFlowController.State);
        }

        public void SetProjectilePool(ProjectilePool pool)
        {
            projectilePool = pool;
        }

        private void HandleGameFlowStateChanged(GameFlowController.FlowState state)
        {
            isGameplayPlaying = state == GameFlowController.FlowState.Playing;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            statRuntime?.Tick(dt);

            if (IsGameplayPaused())
            {
                isDragging = false;
                SetMoveAnimation(false);
                return;
            }

            if (enableHorizontalDragMove)
            {
                HandleHorizontalDragMove();
                SetMoveAnimation(isDragging);
            }
            else
            {
                SetMoveAnimation(false);
            }

            for (int i = 0; i < activeWeapons.Count; i++)
            {
                activeWeapons[i].Tick(dt);
            }
        }

        public bool GrantWeapon(WeaponDataSO weaponData)
        {
            if (weaponData == null)
            {
                return false;
            }

            if (weaponById.ContainsKey(weaponData.WeaponId))
            {
                ownedWeaponTypes.Add(weaponData.WeaponKind);
                return true;
            }

            if (projectilePool == null)
            {
                Debug.LogError($"{nameof(PlayerController)} requires {nameof(projectilePool)}.", this);
                return false;
            }

            WeaponModuleBase weaponRuntime = WeaponRuntimeFactory.Create(weaponData);
            if (weaponRuntime == null)
            {
                Debug.LogWarning($"No runtime found for weapon kind: {weaponData.WeaponKind}");
                return false;
            }

            var context = new PlayerWeaponContext(this, statRuntime, projectilePool, defaultMuzzle);
            weaponRuntime.Initialize(context);

            weaponById.Add(weaponData.WeaponId, weaponRuntime);
            activeWeapons.Add(weaponRuntime);
            ownedWeaponTypes.Add(weaponData.WeaponKind);
            return true;
        }

        public void EnsureOwnedWeaponType(WeaponType weaponType)
        {
            if (weaponType == WeaponType.Common)
            {
                return;
            }

            ownedWeaponTypes.Add(weaponType);
        }

        public bool HasWeaponType(WeaponType weaponType)
        {
            if (weaponType == WeaponType.Common)
            {
                return true;
            }

            return ownedWeaponTypes.Contains(weaponType);
        }

        public int AddStatModifier(PlayerStatModifier modifier)
        {
            return statRuntime != null ? statRuntime.AddCommonModifier(modifier) : -1;
        }

        public int AddWeaponStatModifier(WeaponStatModifier modifier)
        {
            return statRuntime != null ? statRuntime.AddWeaponModifier(modifier) : -1;
        }

        private bool IsGameplayPaused()
        {
            return !isGameplayPlaying;
        }

        private void HandleHorizontalDragMove()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
                if (cachedCamera == null)
                {
                    return;
                }
            }

            if (TryGetPointerDownScreenPosition(out Vector2 downPos))
            {
                if (TryScreenToWorldX(downPos, out float pointerWorldX))
                {
                    dragOffsetX = transform.position.x - pointerWorldX;
                    isDragging = true;
                }
            }

            if (isDragging && TryGetPointerHeldScreenPosition(out Vector2 heldPos))
            {
                if (TryScreenToWorldX(heldPos, out float pointerWorldX))
                {
                    float targetX = Mathf.Clamp(pointerWorldX + dragOffsetX, minX, maxX);
                    Vector3 pos = transform.position;
                    pos.x = targetX;
                    transform.position = pos;
                }
            }
            else if (isDragging && !IsAnyPointerPressed())
            {
                isDragging = false;
            }

            if (TryGetPointerUp())
            {
                isDragging = false;
            }
        }

        private bool TryScreenToWorldX(Vector2 screenPos, out float worldX)
        {
            worldX = transform.position.x;
            float cameraToPlayerZ = transform.position.z - cachedCamera.transform.position.z;
            Vector3 world = cachedCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cameraToPlayerZ));
            worldX = world.x;
            return true;
        }

        private static bool TryGetPointerDownScreenPosition(out Vector2 screenPos)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                screenPos = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            screenPos = default;
            return false;
        }

        private static bool TryGetPointerHeldScreenPosition(out Vector2 screenPos)
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                screenPos = touchscreen.primaryTouch.position.ReadValue();
                return true;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                screenPos = mouse.position.ReadValue();
                return true;
            }

            screenPos = default;
            return false;
        }

        private static bool TryGetPointerUp()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasReleasedThisFrame)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasReleasedThisFrame;
        }

        private static bool IsAnyPointerPressed()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
            {
                return true;
            }

            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
        }

        private void SetMoveAnimation(bool isWalking)
        {
            if (animator == null || isWalkingAnimation == isWalking)
            {
                return;
            }

            animator.Play(isWalking ? WalkStateHash : IdleStateHash, 0);
            isWalkingAnimation = isWalking;
        }
    }
}

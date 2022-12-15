using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace SniperFlee
{
    public enum CharacterStance { Standing, Crouching, Proning }

    public class CharacterController : MonoBehaviour
    {
        [Header("Speed [Normal Sprint]")]
        [SerializeField] private Vector2 _standingSpeed = new Vector2(0, 0);
        [SerializeField] private Vector2 _crouchingSpeed = new Vector2(0, 0);
        [SerializeField] private Vector2 _proningSpeed = new Vector2(0, 0);

        [Header("Capsule [Radius Height YOffset]")]
        [SerializeField] private Vector3 _standingCapsule = Vector3.zero;
        [SerializeField] private Vector3 _crouchingCapsule = Vector3.zero;
        [SerializeField] private Vector3 _proningCapsule = Vector3.zero;

        [Header("Sharpness")]
        [SerializeField] private float _moveSharpness = 10f;
        [SerializeField] private float _standingRotationSharpness = 10f;
        [SerializeField] private float _crouchingRotationSharpness = 10f;
        [SerializeField] private float _proningRotationSharpness = 10f;

        #region ANIMATOR_STATE_NAMES
        private const string _standToCrouch = "Base Layer.Base_Crouching";
        private const string _standToProne  = "Base Layer.Stand_to_Prone";
        private const string _crouchToStand = "Base Layer.Base_Standing";
        private const string _crouchToProne = "Base Layer.Crouch_to_Prone";
        private const string _proneToStand  = "Base Layer.Prone_to_Stand";
        private const string _proneToCrouch = "Base Layer.Prone_to_Crouch";
        private const string _standToDeath  = "Base Layer.Standing_Death";
        private const string _crouchToDeath = "Base Layer.Crouch_Death";
        private const string _proneToDeath  = "Base Layer.Prone_Death";
        #endregion

        private PlayerInput         _inputs;
        private CameraController    _cameraController;
        private Animator            _animator;
        private CapsuleCollider     _capsuleCollider;
        
        private CharacterStance _stance;
        private LayerMask       _layerMask;
        private Collider[]      _obstructions = new Collider[8];

        private float _runSpeed;
        private float _sprintSpeed;
        private float _rotationSharpness;

        private float       _targetSpeed;
        private Quaternion  _targetRotation;

        private float       _newSpeed;
        private Vector3     _newVelocity;
        private Quaternion  _newRotation;

        [Header("Player Stats")]
        public float _health = 100f;
        [SerializeField] private Image _damageScreen;
        [SerializeField] private Image _redCornersScreen;

        private Color _alphaColor;
        private Color _alphaRedCorners;
        private bool _isDead = false;

        private void Start()
        {
            _animator = GetComponent<Animator>();
            _capsuleCollider = GetComponent<CapsuleCollider>();
            _inputs = GetComponent<PlayerInput>();
            _cameraController = GetComponent<CameraController>();

            _runSpeed = _standingSpeed.x;
            _sprintSpeed = _standingSpeed.y;
            _rotationSharpness = _standingRotationSharpness;
            _stance = CharacterStance.Standing;
            SetCapsuleDimensions(_standingCapsule);

            int mask = 0;
            for(int i = 0; i < 32; i++)
            {
                if(!(Physics.GetIgnoreLayerCollision(gameObject.layer, i)))
                {
                    mask |= 1 << i;
                }
            }
            _layerMask = mask;

            _alphaColor = _damageScreen.color;
            _alphaRedCorners = _redCornersScreen.color;

            _animator.applyRootMotion = false;
        }

        private void Update()
        {
            Vector3 moveInputVector = new Vector3(_inputs.MoveAxisRight, 0, _inputs.MoveAxisForward).normalized;
            Vector3 cameraPlanarDirection = _cameraController._cameraPlanarDirection;
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection);
            moveInputVector = cameraPlanarRotation * moveInputVector;

            if(_inputs.Sprint.Pressed())
            {
                _targetSpeed = moveInputVector != Vector3.zero ? _sprintSpeed : 0;
            }
            else
            {
                _targetSpeed = moveInputVector != Vector3.zero ? _runSpeed : 0;
            }
            _newSpeed = Mathf.Lerp(_newSpeed, _targetSpeed, Time.deltaTime * _moveSharpness);

            _newVelocity = moveInputVector * _targetSpeed;
            transform.Translate(_newVelocity * Time.deltaTime, Space.World);

            if (_targetSpeed != 0)
            {
                _targetRotation = Quaternion.LookRotation(moveInputVector);
                _newRotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _rotationSharpness);
                transform.rotation = _newRotation;
            }

            _animator.SetFloat("Forward", _newSpeed);

            if(_redCornersScreen != null)
            {
                if(_alphaRedCorners.a > 0f && !_isDead)
                {
                    _alphaRedCorners.a -= 0.01f;
                    _redCornersScreen.color = _alphaRedCorners;
                }
            }
        }

        private void LateUpdate()
        {
            switch(_stance)
            {
                case CharacterStance.Standing:
                    if(_inputs.Crouch.PressedDown()) { RequestStanceChange(CharacterStance.Crouching); }
                    else if(_inputs.Prone.PressedDown()) { RequestStanceChange(CharacterStance.Proning); }
                    break;

                case CharacterStance.Crouching:
                    if (_inputs.Crouch.PressedDown()) { RequestStanceChange(CharacterStance.Standing); }
                    else if (_inputs.Prone.PressedDown()) { RequestStanceChange(CharacterStance.Proning); }
                    break;

                case CharacterStance.Proning:
                    if (_inputs.Crouch.PressedDown()) { RequestStanceChange(CharacterStance.Crouching); }
                    else if (_inputs.Prone.PressedDown()) { RequestStanceChange(CharacterStance.Standing); }
                    break;
            }
        }

        public bool RequestStanceChange(CharacterStance newStance)
        {
            if (_stance == newStance)
                return true;

            switch(_stance)
            {
                case CharacterStance.Standing:
                    if(newStance == CharacterStance.Crouching)
                    {
                        if (!CharacterOverlap(_crouchingCapsule))
                        {
                            _runSpeed = _crouchingSpeed.x;
                            _sprintSpeed = _crouchingSpeed.y;
                            _rotationSharpness = _crouchingRotationSharpness;
                            _stance = newStance;

                            _animator.CrossFadeInFixedTime(_standToCrouch, 0.5f);
                            SetCapsuleDimensions(_crouchingCapsule);
                            return true;
                        }
                    }

                    else if (newStance == CharacterStance.Proning)
                    {
                        if (!CharacterOverlap(_proningCapsule))
                        {
                            _runSpeed = _proningSpeed.x;
                            _sprintSpeed = _proningSpeed.y;
                            _rotationSharpness = _proningRotationSharpness;
                            _stance = newStance;

                            _animator.CrossFadeInFixedTime(_standToProne, 0.25f);
                            SetCapsuleDimensions(_proningCapsule);
                            return true;
                        }
                    }
                    break;

                case CharacterStance.Crouching:
                    if (newStance == CharacterStance.Standing)
                    {
                        if (!CharacterOverlap(_standingCapsule))
                        {
                            _runSpeed = _standingSpeed.x;
                            _sprintSpeed = _standingSpeed.y;
                            _rotationSharpness = _standingRotationSharpness;
                            _stance = newStance;

                            _animator.CrossFadeInFixedTime(_crouchToStand, 0.5f);
                            SetCapsuleDimensions(_standingCapsule);
                            return true;
                        }
                    }

                    else if (newStance == CharacterStance.Proning)
                    {
                        if (!CharacterOverlap(_proningCapsule))
                        {
                            _runSpeed = _proningSpeed.x;
                            _sprintSpeed = _proningSpeed.y;
                            _rotationSharpness = _proningRotationSharpness;
                            _stance = newStance;

                            _animator.CrossFadeInFixedTime(_crouchToProne, 0.25f);
                            SetCapsuleDimensions(_proningCapsule);
                            return true;
                        }
                    }
                    break;

                case CharacterStance.Proning:
                    if (newStance == CharacterStance.Standing)
                    {
                        if (!CharacterOverlap(_standingCapsule))
                        {
                            _runSpeed = _standingSpeed.x;
                            _sprintSpeed = _standingSpeed.y;
                            _rotationSharpness = _standingRotationSharpness;
                            _stance = newStance;

                            _animator.CrossFadeInFixedTime(_proneToStand, 0.25f);
                            SetCapsuleDimensions(_standingCapsule);
                            return true;
                        }
                    }

                    else if (newStance == CharacterStance.Crouching)
                    {
                        if (!CharacterOverlap(_crouchingCapsule))
                        {
                            _runSpeed = _crouchingSpeed.x;
                            _sprintSpeed = _crouchingSpeed.y;
                            _rotationSharpness = _crouchingRotationSharpness;
                            _stance = newStance;

                            _animator.CrossFadeInFixedTime(_proneToCrouch, 0.25f);
                            SetCapsuleDimensions(_crouchingCapsule);
                            return true;
                        }
                    }
                    break;
            }
            return false;
        }

        private bool CharacterOverlap(Vector3 capsuleDimensions)
        {
            float radius = capsuleDimensions.x;
            float height = capsuleDimensions.y;
            Vector3 center = new Vector3(_capsuleCollider.center.x, capsuleDimensions.z, _capsuleCollider.center.z);

            Vector3 point0;
            Vector3 point1;

            if(height < radius * 2)
            {
                point0 = transform.position + center;
                point1 = transform.position + center;
            }
            else
            {
                point0 = transform.position + center + (transform.up * (height * 0.5f - radius));
                point1 = transform.position + center - (transform.up * (height * 0.5f - radius));
            }

            radius = radius - 0.1f;

            int numOverlaps = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, _obstructions, _layerMask);
            
            for(int i = 0; i < numOverlaps; i++)
            {
                if (_obstructions[i] == _capsuleCollider)
                    numOverlaps--;
            }

            return numOverlaps > 0;
        }

        private void SetCapsuleDimensions(Vector3 capsuleDimensions)
        {
            _capsuleCollider.center = new Vector3(_capsuleCollider.center.x, capsuleDimensions.z, _capsuleCollider.center.z);
            _capsuleCollider.radius = capsuleDimensions.x;
            _capsuleCollider.height = capsuleDimensions.y;
        }

        public bool IsDead()
        {
            return _isDead;
        }

        public void TakeDamage(float damage)
        {
            _health -= damage;

            if (_health <= 0f)
            {
                switch (_stance)
                {
                    case CharacterStance.Standing:
                        _animator.CrossFadeInFixedTime(_standToDeath, 0.25f);
                        break;
                    case CharacterStance.Crouching:
                        _animator.CrossFadeInFixedTime(_crouchToDeath, 0.2f);
                        break;
                    case CharacterStance.Proning:
                        _animator.CrossFadeInFixedTime(_proneToDeath, 0.25f);
                        break;
                }

                _isDead = true;
                _alphaRedCorners.a = 0.9f;
                _redCornersScreen.color = _alphaRedCorners;
                Debug.Log("Oh No!! I DIED");

                StartCoroutine(DeadTransitionCR());
            }
            else
            {
                Debug.Log("I'm taking damage oh shit!");
                _alphaColor.a += 0.33f;
                _damageScreen.color = _alphaColor;
                _alphaRedCorners.a = 0.8f;
                _redCornersScreen.color = _alphaRedCorners;
            }
        }

        IEnumerator DeadTransitionCR()
        {
            yield return new WaitForSeconds(2f);

            Cursor.lockState = CursorLockMode.None;
            Initiate.Fade("DeadScene", Color.black, 0.5f);
        }
    }
}

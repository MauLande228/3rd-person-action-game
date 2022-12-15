using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SniperFlee
{
    public class CameraController : MonoBehaviour
    {
        [Header ("Framing")]
        [SerializeField] private Camera _camera = null;
        [SerializeField] private Transform _followTransform = null;
        [SerializeField] private Vector3 _framing = new Vector3(0, 0, 0);

        [Header("Distance")]
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _defaultDistance = 5f;
        [SerializeField] private float _minDistance = 0f;
        [SerializeField] private float _maxDistance = 10f;

        [Header("Rotation")]
        [SerializeField] private bool _invertX = false;
        [SerializeField] private bool _invertY = false;
        [SerializeField] private float _mouseSensitivity = 1f;
        [SerializeField] private float _rotationSharpness = 25f;
        [SerializeField] private float _defaultVerticalAngle = 20f;
        [SerializeField] [Range(-90, 90)] private float _minVerticalAngle = -90.0f;
        [SerializeField] [Range(-90, 90)] private float _maxVerticalAngle =  90.0f;

        [Header("Obstruction")]
        [SerializeField] private float _checkRadius = 0.2f;
        [SerializeField] private LayerMask _obstructionLayers = -1;
        private List<Collider> _ignoreColliders = new List<Collider>();

        private Vector3 _planarDirection;
        private Quaternion _targetRotation;
        private float _targetVerticalAngle;
        private float _targetDistance;
        private Vector3 _targetPosition;

        private Vector3 _newPosition;
        private Quaternion _newRotation;

        public Vector3 _cameraPlanarDirection { get => _planarDirection; }

        private void OnValidate()
        {
            _defaultDistance = Mathf.Clamp(_defaultDistance, _minDistance, _maxDistance);
            _defaultVerticalAngle = Mathf.Clamp(_defaultVerticalAngle, _minVerticalAngle, _maxVerticalAngle);
        }

        private void Start()
        {
            _ignoreColliders.AddRange(GetComponentsInChildren<Collider>());
            var triggerZone = GameObject.FindWithTag("Goal");
            if (triggerZone != null)
                _ignoreColliders.Add(triggerZone.GetComponent<BoxCollider>());
                

            _planarDirection = _followTransform.forward;
            _targetDistance = _defaultDistance;
            _targetVerticalAngle = _defaultVerticalAngle;

            _targetRotation = Quaternion.LookRotation(_planarDirection) * Quaternion.Euler(_targetVerticalAngle, 0, 0);
            _targetPosition = _followTransform.position - (_targetRotation * Vector3.forward) * _targetDistance;

            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            #region MOUSE INPUT HANDLING
            float mouseX = PlayerInput.MouseXInput * _mouseSensitivity;
            float mouseY = -PlayerInput.MouseYInput * _mouseSensitivity;
            float zoom = -PlayerInput.MouseScrollInput * _zoomSpeed;

            if (_invertX) { mouseX *= -1f; }
            if (_invertY) { mouseX *= -1f; }

            Vector3 focusPosition = _followTransform.position + _camera.transform.TransformDirection(_framing);

            _planarDirection = Quaternion.Euler(0, mouseX, 0) * _planarDirection;
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle + mouseY, _minVerticalAngle, _maxVerticalAngle);
            _targetDistance = Mathf.Clamp(_targetDistance + zoom, _minDistance, _maxDistance);

            float smallestDistance = _targetDistance;
            RaycastHit[] hits = Physics.SphereCastAll(
                focusPosition,
                _checkRadius,
                _targetRotation * -Vector3.forward,
                _targetDistance,
                _obstructionLayers);

            if(hits.Length > 0)
            {
                foreach(RaycastHit hit in hits)
                {
                    if(!_ignoreColliders.Contains(hit.collider))
                    {
                        if(hit.distance < smallestDistance)
                        {
                            smallestDistance = hit.distance;
                        }
                    }
                }
            }

            _targetRotation = Quaternion.LookRotation(_planarDirection) * Quaternion.Euler(_targetVerticalAngle, 0, 0);
            _targetPosition = focusPosition - (_targetRotation * Vector3.forward) * smallestDistance;

            _newRotation = Quaternion.Slerp(_camera.transform.rotation, _targetRotation, Time.deltaTime * _rotationSharpness);
            _newPosition = Vector3.Lerp(_camera.transform.position, _targetPosition, Time.deltaTime * _rotationSharpness);

            _camera.transform.rotation = _newRotation;
            _camera.transform.position = _newPosition;
            #endregion
        }
    }
}

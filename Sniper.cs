using SniperFlee;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Sniper : MonoBehaviour
{
    [SerializeField] private float _damage = 34f;
    [SerializeField] private float _range = 1000f;
    [SerializeField] private GameObject _characterTarget;
    [SerializeField] private GameObject _hitEffect;

    private PlayerInput _inputs;
    private SniperFlee.CharacterController _characterController;
    private LensFlareComponentSRP _sniperLensFlare;
    private AudioSource _shotSound;
    private Vector3 _targetPosition;
    private bool _bIsHidden = true;
    private bool _bIsMoving = false;
    private bool _bSniperBusy = false;

    private void Start()
    {
        _targetPosition = _characterTarget.transform.position;

        _characterController = _characterTarget.GetComponent<SniperFlee.CharacterController>();

        _inputs = _characterTarget.GetComponent<PlayerInput>();
        if (_inputs != null)
            Debug.Log("The game object asociated with this Sniper does not own a PlayerInput component");

        _sniperLensFlare = GetComponent<LensFlareComponentSRP>();
        if (_sniperLensFlare != null)
            Debug.Log("The sniper owns a Lens flare SRP component");

        _shotSound = GetComponent<AudioSource>();

        _sniperLensFlare.enabled = false;

        InvokeRepeating("Shoot", 1f, 2f);
        InvokeRepeating("EnableFreeRoam", 0.1f, 5f);
    }

    private void Update()
    {
        _targetPosition = _characterTarget.transform.position;
        _targetPosition.y = _targetPosition.y + 0.6f;
        _targetPosition = (_targetPosition - transform.position).normalized;

        Ray ray = new Ray(transform.position, _targetPosition);
        if(Physics.Raycast(ray, out RaycastHit hit, _range))
        {   
            if(hit.transform.gameObject.name == "Ch35_nonPBR")
                _bIsHidden = false;
            else
                _bIsHidden = true;
        }

        if (_inputs.MoveAxisForward != 0 || _inputs.MoveAxisRight != 0)
            _bIsMoving = true;
        else
            _bIsMoving = false;
    }

    private void Shoot()
    {
        if ((!_bIsHidden || _bIsMoving) && _bSniperBusy && !_characterController.IsDead())
        {
            _shotSound.Play();

            Ray ray = new Ray(transform.position, _targetPosition);

            if (Physics.Raycast(ray, out RaycastHit hit, _range))
            {

                if (hit.transform.gameObject.name == "Ch35_nonPBR")
                    _characterController.TakeDamage(_damage);

                GameObject go = Instantiate(_hitEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(go, 2f);
            }
        }
    }

    private void EnableFreeRoam()
    {
        _bSniperBusy = !_bSniperBusy;
        _sniperLensFlare.enabled = _bSniperBusy;
    }

}

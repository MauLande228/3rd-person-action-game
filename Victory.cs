using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Victory : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Cursor.lockState = CursorLockMode.None;
            StartCoroutine(WinTransitionCR());

        }

        IEnumerator WinTransitionCR()
        {
            yield return new WaitForSeconds(2f);
            Initiate.Fade("VictoryScene", Color.black, 1f);
        }
    }
}

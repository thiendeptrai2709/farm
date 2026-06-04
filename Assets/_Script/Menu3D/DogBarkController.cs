using System.Collections;
using UnityEngine;

public class DogBarkController : MonoBehaviour
{
    public string barkSoundName = "Dog_Bark";
    public float minWaitTime = 15f;
    public float maxWaitTime = 40f;

    private void Start()
    {
        StartCoroutine(RandomBarkRoutine());
    }

    private IEnumerator RandomBarkRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(barkSoundName, transform.position);
            }
        }
    }
}
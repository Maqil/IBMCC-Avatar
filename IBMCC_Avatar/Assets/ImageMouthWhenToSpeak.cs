using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageMouthWhenToSpeak : MonoBehaviour
{
    Animator associatedAnimator;
    // Start is called before the first frame update
    void Start()
    {
        associatedAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(DialogueManager.instance.speaking)
        {
            associatedAnimator.SetBool("Talking",true);
        }
        else
        {
            associatedAnimator.SetBool("Talking",false);
        }
    }
}

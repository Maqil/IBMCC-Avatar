using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextBox_WriteAsItGoes : MonoBehaviour
{
    TextMeshProUGUI associatedText;
    bool LaunchedSynth = false;
    // Start is called before the first frame update
    void Start()
    {
      associatedText = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!DialogueManager.instance.finishedSpeaking)
        {
            if(!LaunchedSynth && LoginWrapper.wrapper.ttsStarted)
            {
                LaunchedSynth = true;
                LoginWrapper.wrapper.SynthesizeSentence(DialogueManager.instance.line);
            }
            if(!DialogueManager.instance.speaking && LoginWrapper.wrapper.PermitTextAdvanceSynth() 
            && LoginWrapper.wrapper.tAStarted)
            {
                StartCoroutine(Speak());
            }
        }
        else
        {
            //Debug.Log("Line Read Successfully");
            if(!string.IsNullOrEmpty(LoginWrapper.wrapper.line))
            {
                if (DialogueManager.instance.line != LoginWrapper.wrapper.line)
                    {
                        DialogueManager.instance.line = LoginWrapper.wrapper.line;
                    }
            }
            if(!LoginWrapper.wrapper.listening && !LoginWrapper.wrapper.finishedListening)
            {
                LoginWrapper.wrapper.ListentoUser();
                    
            }
            if(LoginWrapper.wrapper.assDelivered)
            {
                    associatedText.text = "";
                    DialogueManager.instance.finishedSpeaking = false;
                    LoginWrapper.wrapper.assDelivered = false;
                    LoginWrapper.wrapper.finishedListening = false;
                if(LoginWrapper.wrapper.goodbye)
                {
                    StartCoroutine(QuitApp());
                }                
            }
        }
    }
    
    IEnumerator Speak()
    {
        DialogueManager.instance.speaking = true;
        AudioSource tempAudio = GameObject.FindGameObjectWithTag("Avatar").GetComponent<AudioSource>();
        if(tempAudio.clip != null)
        {
            tempAudio.Play();
            yield return new WaitForSeconds(tempAudio.clip.length);
            DialogueManager.instance.speaking = false;
            DialogueManager.instance.finishedSpeaking = true;
            LaunchedSynth = false;
        }
        else
        {
            DialogueManager.instance.speaking = false;
            LaunchedSynth = false;
        }
        
    }
    IEnumerator QuitApp()
    {
        //Debug.Log("Quiting");
        yield return new WaitForSeconds(1f);
        Application.Quit();
    }
}

using System.Collections;
using System.Collections.Generic;
using IBM.Cloud.SDK;
using IBM.Cloud.SDK.Utilities;
using UnityEngine;
using IBM.Watson.TextToSpeech.V1;
using IBM.Watson.ToneAnalyzer.V3;
using IBM.Watson.ToneAnalyzer.V3.Model;
using System.IO;
using System.Text.RegularExpressions;
using IBM.Watson.SpeechToText.V1;
using IBM.Watson.Assistant.V2;
using IBM.Watson.Assistant.V2.Model;
using IBM.Cloud.SDK.DataTypes;
using IBM.Watsson.Examples;
using System;
using System.Net.Http;
using System.Text;
using UnityEngine.Networking;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

public class LoginWrapper : MonoBehaviour
{
    // Wrapper Singleton
    static public LoginWrapper wrapper;

    //Services
    TextToSpeechService tts;
    ToneAnalyzerService tas;
    SpeechToTextService stt;
    AssistantService ass;
    

    //Info used to create services
    string versionDate = "2016-05-19";
    string versionDateAssistant = "2019-02-28";
    string DialogueHash = "temp";
    string assistantID = "d9189588-6f94-47a7-bb10-63c2d7dc1181";
    string assistantSessionID;

    //These are useful sometimes
    public AudioClip loadableClip;
    AudioSource associatedSource;

    //string for the Speech to Text
    public string STTOutput = "temp";

    //bools to show that some things are up.
    public bool ttsStarted { get; private set;}  = false;
    public bool tAStarted { get; private set;}  = false;
    public bool sttStarted { get; private set;}  = false;
    public bool assStarted { get; private set;}  = false;
    bool ttsSynthesing = false;
    public bool sttTextualizing { get; private set;}  = false;

    //Tones used
    public ToneAnalysis activeAnalysis;
    List<string> tones = new List<string>() {
        "emotion"
    };
    public float emotionScore;
    public string activeFile;
    public string line;

    //Bools for the assistant
    public bool assDelivered = false;
    public bool goodbye = false;
    MessageInput assMessageInput = new MessageInput()
            {
                Options = new MessageInputOptions()
                {
                    ReturnContext = true
                }

            };
    public MessageContext testContext;
    //Speech to Text related information
    public string lineSTT;
    public string savedLineSTT;
    public ExampleStreaming STTstreamer;
    public Dictionary<string, byte[]> voiceLines;
    public bool listening = false;
    public bool finishedListening = false;
    public float cooldownSTT = 0f;
    public float limitSTT = 2f;


void Start()
{
    if(wrapper == null)
    {
        wrapper = this;
    }
    else if (wrapper != this)
    {
        Destroy(this);
    }
    LogSystem.InstallDefaultReactors();
    Runnable.Run(SetUpTTS());
    Runnable.Run(SetUpToneAnalyser());
    Runnable.Run(SetUpSpeechToText());
    Runnable.Run(SetUpAssistant());
    associatedSource = GameObject.FindGameObjectWithTag("Avatar").GetComponent<AudioSource>();
    if(File.Exists(Application.persistentDataPath + "/voiceLines.eld"))
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/voiceLines.eld", FileMode.Open);
        voiceLines = (Dictionary<string,byte[]>)bf.Deserialize(file);
        file.Close();
    }else{
        voiceLines = new Dictionary<string, byte[]>();
    }
    
}

//This is related to the Speech to Text
void Update()
{
    if(listening)
    {
        //Verify if the 2 condition to stop listening are there
        if(cooldownSTT >= limitSTT && !String.IsNullOrEmpty(savedLineSTT))
        {
            STTstreamer.StopRecording();
            SendLineAssistant(lineSTT.Trim(Environment.NewLine.ToCharArray()));
            savedLineSTT = "";
            lineSTT = "";
            listening = false;
            finishedListening = true;
        }//Add 1 (time period) if the 2 texts are equal
        if(lineSTT == savedLineSTT)
        {
            cooldownSTT += Time.deltaTime;
        }else//Otherwise, reset time and copy the line from the STT to the line saved.
        {
            cooldownSTT = 0f;
            savedLineSTT = lineSTT;
        }
    }
}
IEnumerator SetUpTTS()
{
    //  Create IAM token options and supply the apikey. IamUrl is the URL used to get the 
    //  authorization token using the IamApiKey. It defaults to https://iam.cloud.ibm.com/identity/token
    TokenOptions iamTokenOptions = new TokenOptions()
    {
        IamApiKey = "q0pbfjAdkvRwz_qFv3mbFvSZ4a4xA9--nlPPhtyF5D7-"
    };
    //  Create credentials using the IAM token options
    Credentials credentials = new Credentials(iamTokenOptions, "https://gateway-lon.watsonplatform.net/text-to-speech/api");
    while (!credentials.HasIamTokenData())
        yield return null;

    tts = new TextToSpeechService(credentials);
    ttsStarted = true;
    //assistant.ListWorkspaces(callback: OnListWorkspaces);
    //Debug.Log(tts.ListVoices(OnSuccess).ToString());
}

IEnumerator SetUpToneAnalyser()
{
    TokenOptions iamTokenOptions = new TokenOptions()
    {
        IamApiKey = "GZCuLVAYf_CWIPKZTs0zyqn4dg0nw9KePmYGjPEeIzS3"
    };

    Credentials credentials = new Credentials(iamTokenOptions, "https://gateway-lon.watsonplatform.net/tone-analyzer/api");
    while (!credentials.HasIamTokenData())
        yield return null;

    tas = new ToneAnalyzerService(versionDate,credentials);
    tAStarted = true;

}

IEnumerator SetUpAssistant()
{
    TokenOptions iamTokenOptions = new TokenOptions()
        {
            IamApiKey = "PG_NxLAWuKYyr2OfIsu91wTsWbHg43Je19YrEuORUFXT"
        };

    Credentials credentials = new Credentials(iamTokenOptions, "https://gateway-lon.watsonplatform.net/assistant/api");
    while (!credentials.HasIamTokenData())
        yield return null;
    
    ass = new AssistantService(versionDateAssistant,credentials);
    assStarted = true;
    ass.CreateSession(OnSessionCreationResponse,"d9189588-6f94-47a7-bb10-63c2d7dc1181");
}

IEnumerator SetUpSpeechToText()
{
        TokenOptions iamTokenOptions = new TokenOptions()
    {
        IamApiKey = "liOt3C4cqS86vauIsjDd11uAny_ezrjp2tarts5LYrmi"
    };

    Credentials credentials = new Credentials(iamTokenOptions,"https://gateway-lon.watsonplatform.net/speech-to-text/api");
    while (!credentials.HasIamTokenData())
        yield return null;
    
    stt = new SpeechToTextService(credentials);
    sttStarted = true;
    //Set up the Example Streamer using the created STTAssistant
    STTstreamer = GameObject.FindGameObjectWithTag("Streamer").GetComponent<ExampleStreaming>();
    STTstreamer.SetServiceFromWrapper(stt);

}
//Basic Callback. shows the response and nothing more
private void OnSuccess<T>(DetailedResponse<T> resp, IBMError error)
{
    Log.Debug("ExampleCallback.OnSuccess()", "Response received: {0}", resp.Response);
}
//writes the soundfile gotten from the Text To Speech then plays it.
private void OnSynthesize(DetailedResponse<byte[]> resp, IBMError error)
{
    if(!voiceLines.ContainsKey(DialogueHash))
    {
        voiceLines.Add(DialogueHash,resp.Result);
    }
    else{
        Debug.LogWarning("This is not normal, the program shouldn't get here if the hash exists.");
    }
    //associatedSource.PlayOneShot(Resources.Load<AudioClip>("Sounds/" + fileNameSaveFile));
            BinaryFormatter bf = new BinaryFormatter();
            Debug.Log("DataPath: " + Application.persistentDataPath);
            FileStream file = File.Create(Application.persistentDataPath + "/voiceLines.eld");
            bf.Serialize(file, voiceLines);
            file.Close();
            associatedSource.clip = WaveFile.ParseWAV(DialogueHash,resp.Result);
            ttsSynthesing = false;
}
//Shows the result of the Speech To Text (Unused)
/* 
private void OnTextify(DetailedResponse<string> resp, IBMError error)
{
    sttTextualizing = true;
    STTOutput = resp.Result;
    sttTextualizing = false;
}
*/
//calculates & saves the Tone Analysis.
private void OnToneAnalysis(DetailedResponse<ToneAnalysis> resp,IBMError error)
{
    activeAnalysis = resp.Result;
    if (resp.Result.DocumentTone.ToneCategories != null)
    {
        float sadnessScore = 0;
        float happinessScore = 0;
            for(int j=0;j<resp.Result.DocumentTone.ToneCategories[0].Tones.Count;j++)
            {
                switch(resp.Result.DocumentTone.ToneCategories[0].Tones[j].ToneName)
                {
                    case "Sadness":
                    sadnessScore = (float)resp.Result.DocumentTone.ToneCategories[0].Tones[j].Score;
                    break;
                    case "Joy":
                    happinessScore = (float)resp.Result.DocumentTone.ToneCategories[0].Tones[j].Score;
                    break;
                }
            }
        emotionScore = happinessScore - sadnessScore;
        PlayerPrefs.SetFloat(activeFile,emotionScore);
    }
    else
    {
        UnityEngine.Debug.Log("Error, something happened.");
    }
}
//Synthesizes the sentence
public void SynthesizeSentence(string sentence)
{
    activeFile = Regex.Replace(sentence,@"[^a-zA-Z0-9 -]","").ToLower();
    activeFile = Regex.Replace(activeFile, @"\s+", string.Empty);
    DialogueHash = activeFile;
    using (MD5 md5Hash = MD5.Create())
            {
                DialogueHash = GetMd5Hash(md5Hash,DialogueHash);
            }
    ttsSynthesing = true;
    if(voiceLines.ContainsKey(DialogueHash))
        {
            associatedSource.clip = WaveFile.ParseWAV(DialogueHash,voiceLines[DialogueHash]);
            ttsSynthesing = false;
            //associatedSource.PlayOneShot(Resources.Load<AudioClip>("Sounds/" + fileNameSaveFile));
        }
    else
        {
            tts.Synthesize(OnSynthesize,sentence,"en-US_AllisonV3Voice",null,"audio/wav");
        }
            if(PlayerPrefs.HasKey(activeFile))
    {
        emotionScore = PlayerPrefs.GetFloat(activeFile);
    }else
        {
            ToneInput tempInput = new ToneInput();
            tempInput.Text = sentence;
            tas.Tone(OnToneAnalysis,tempInput,false,tones);
        }
}
//Condition to Advance in the Text Script
public bool PermitTextAdvanceSynth()
{
    if(ttsStarted && !ttsSynthesing && tAStarted)
        return true;
    else
        return false;
}
//Another condition to advance in the Text Script.
public bool PermitTextAdvanceSTT()
{
 if (STTstreamer.Active)
    return false;
 else
    return true;
}
//Function that calls the Tone Analyzer
public void ReturnReaction(string text)
{
    if(tAStarted)
    {
        ToneInput tempInput = new ToneInput();
        tempInput.Text = text;
        tas.Tone(OnToneAnalysis,tempInput,false,tones);
    }
}

//Self Explanatory
public void ListentoUser()
{
    listening = true;
    STTstreamer.StartRecording();
}

//Unused
public void OnRecognize(SpeechRecognitionEvent result)
{
    if (result != null && result.results.Length > 0)
            {
                foreach (var res in result.results)
                {
                    string text = string.Format("{0}\n", res.alternatives[0].transcript);
                    //Log.Debug("ExampleStreaming.OnRecognize()", text);
                }

            }
}

//Calls the Assistant.
public void SendLineAssistant(string intline)
{
    assMessageInput.Text = intline;
    ass.Message(OnMessage,assistantID,assistantSessionID,assMessageInput);
}

//Called after the Assistant is called.
public void OnMessage(DetailedResponse<MessageResponse> response, IBMError error)
{
    //Debug.Log("number of internal stuff > " + response.Result.Output.Generic.Count);
    //Debug.Log("Text Embedded: " + response.Result.Output.Generic[0].Text);
    if(response.Result.Output.Intents.Count > 0)
        if(response.Result.Output.Intents[0].Intent == "Goodbye")
        {
            goodbye = true;
        }
    line = response.Result.Output.Generic[0].Text;
    assDelivered = true;
}

//Parses out the JSON if received.
private string ParseText(string text)
{
    if (text.Contains("["))
        {
            string[] splittext = text.Split('[');
            UnityEngine.Debug.Log(splittext[1]);
            //model = new HeartDiseaseModel(Newtonsoft.Json.JsonConvert.DeserializeObject<HeartDiseaseContext>(splittext[1].Replace("]","")));
            return splittext[0];
        }
    else
    {
        return text;
    }
}

// https://stackoverflow.com/questions/16078254/create-audioclip-from-byte
private float[] ConvertByteToFloatAudio(byte[] array) 
            {
                float[] floatArr = new float[array.Length / 4];
                for (int i = 0; i < floatArr.Length; i++) 
                {
                    if (BitConverter.IsLittleEndian) 
                        Array.Reverse(array, i * 4, 4);
                    floatArr[i] = BitConverter.ToSingle(array, i * 4) / 0x80000000;
                }
                return floatArr;
            }
//https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.md5
static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
/* private void ContactHeartDiseaseModel(HeartDiseaseModel model)
    {
    try {
         ProcessStartInfo process = new ProcessStartInfo("C:\\Windows\\system32\\cmd.exe","curl -k -X POST \\ https://169.51.49.149:31843/dmodel/v1/heart/pyscript/reglog/score \\ -H 'Authorization: Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6Im1vdWFkIiwicGFja2FnZU5hbWUiOiJIZWFsdGhjYXJlIiwicGFja2FnZVJvdXRlIjoiaGVhcnQiLCJpYXQiOjE1NjI1NzkzMDB9.nJOhhpNIGNhWBXSF2q8cSeahbzX_dq4Wpgv_5XJl4htH1m9mUwCqCspetA-9VFWIXeslkAG-_t9iWW4zJ9C2lyuXVQwTE4nr7qseFIg42HtYUKnkzRLmBTPV4QbKl6NXtAZqkRPa65CTBaLZZk7uCuXBlZJU3xdrLEa8S8is3Xzz6Y5Fbm_SRC9MGNlFV0GjF8do9Nsl1RMKXeeRV3QGZfxe6lU5kH94aPnLCB875W79ihdDYMBFOiyFuH1h_S5wGY4NJJpM5GD9ZHk8kb5aY9DRTZ0GtctVsokyejUZBlWKyR2lUSVlPKU-bQM273G7B8D2AOsH7W1P9g8gO26smQ' \\ -H 'Cache-Control: no-cache' \\ -H 'Content-Type: application/json' \\ -d '" + "{\"args\":{\"input_json\":" +Newtonsoft.Json.JsonConvert.SerializeObject(model) + "}}'");
         process.CreateNoWindow = true;
         process.UseShellExecute = false;
         process.RedirectStandardError = true;
         Process myProcess = Process.Start(process);
         myProcess.WaitForExit();
         int ExitCode = myProcess.ExitCode;
         string output = myProcess.StandardError.ReadToEnd();
         UnityEngine.Debug.Log(ExitCode);
         UnityEngine.Debug.Log(output);
         } catch (Exception e){
             print(e);        
         }
    }*/

/* private async System.Threading.Tasks.Task ContactHeartDiseaseModelAsync(HeartDiseaseModel mode)
{
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = (requestMessage, certificate, chain, policyErrors) => true; 
    using (var httpClient = new HttpClient(handler))
    {
        using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://169.51.49.149:31843/dmodel/v1/test/pyscript/reglog/score"))
        {
            request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VybmFtZSI6Im1vdWFkIiwicGFja2FnZU5hbWUiOiJIZWFsdGhjYXJlIiwicGFja2FnZVJvdXRlIjoidGVzdCIsImlhdCI6MTU2MDc4MDYwNX0.kNHGHn8ELuHc3ix1kgDXINoz-gKszbbU8JngdlCsPBEQXuBK-yBOAoXIJPEs5zRxj2yb8CURZb6BCMMxgcVtQZpVPRFOODo4okiUhl_ioGgpZ6KQ7951u6FgY4gDqvbB5EZzYYAAgzhvWmOjCOYBVhpQbS7jU_3kh9T8zApRGWdlDNtkTRxzMRfP8-p0kk-BwmibAOxIuC4MuSsJ_P3xd5UMKTVIi47DDRS1kMZHlhxu2xEOy5KzqiKDoyW_MC6dEFuCTb3mAwCO12RBrXeaC0iBFOoKAqnxXjc7jVeT61jK4uZbRqFmnYA4V6Ls0oFV29jE5_M5_Xvo43W88T0vSQ"); 

            request.Content = new StringContent("{\"args\":{\"input_json\":" + Newtonsoft.Json.JsonConvert.SerializeObject(mode) + "}}", Encoding.UTF8, "application/json"); 

            var response = await httpClient.SendAsync(request);
            Debug.Log(response);
            modelResponse = response.ToString();
            obtainedModelResponse = true;
        }
    }
}*/


public void OnSessionCreationResponse(DetailedResponse<SessionResponse> response, IBMError error)
{
    assistantSessionID = response.Result.SessionId;
}
}





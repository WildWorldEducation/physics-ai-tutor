using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.Networking;

public class TTSResponseBehaviour : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;
    public TextAsset awsSecret;

    public AnimationBehaviour animationBehaviour;
    void Start()
    {
        string text = awsSecret.text;
    }

    public async void Response(string chatGPTResponse)
    {
        // Get a key using another account, not root.
        var credentials = new BasicAWSCredentials(accessKey: "AKIA6RPUV6MBZWYENGMV", secretKey: awsSecret.text);
        var client = new AmazonPollyClient(credentials, Amazon.RegionEndpoint.USEast1);

        var request = new SynthesizeSpeechRequest()
        {
            Text = chatGPTResponse,
            Engine = Engine.Neural,
            VoiceId = VoiceId.Ruth,
            OutputFormat = OutputFormat.Mp3
        };

        var response = await client.SynthesizeSpeechAsync(request);

        WriteIntoFile(response.AudioStream);

        using (var www = UnityWebRequestMultimedia.GetAudioClip(uri: $"{Application.persistentDataPath}/audio.mp3", AudioType.MPEG))
        {
            var op = www.SendWebRequest();

            while (!op.isDone)
            {
                Task.Yield();
            }

            var clip = DownloadHandlerAudioClip.GetContent(www);

            // Start the talking animation.
            animationBehaviour.TalkingAnimation();

            audioSource.clip = clip;
            audioSource.Play();

            // Stop the talking animation.
            StartCoroutine(IdleAnimation());
        }
    }

    private void WriteIntoFile(Stream stream)
    {
        using (var fileStream = new FileStream(path: $"{Application.persistentDataPath}/audio.mp3", FileMode.Create))
        {
            byte[] buffer = new byte[4 * 1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, offset: 0, count: buffer.Length)) > 0)
            {
                fileStream.Write(buffer, offset: 0, count: bytesRead);
            }
        }
    }

    private IEnumerator IdleAnimation()
    {
        // Wait until the clip is finished.
        yield return new WaitForSeconds(audioSource.clip.length);
        // Stop the talking animation.
        animationBehaviour.IdleAnimation();
    }
}

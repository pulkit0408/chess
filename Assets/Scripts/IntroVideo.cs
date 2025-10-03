using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Drag your VideoPlayer here in the Inspector
     // Name of the scene to load after the video

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished; // Subscribe to the event
            videoPlayer.Play(); // Start playing the video
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        // Load the next scene after the video finishes
        SceneManager.LoadScene("start");
    }

}


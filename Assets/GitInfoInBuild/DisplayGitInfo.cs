using FyndReality.Util.Git;
using UnityEngine;

public class DisplayGitInfo : MonoBehaviour
{
    private string gitHash;
    private string gitStatus;

    private void Start()
    {
        gitHash = Git.HashShort;
        gitStatus = Git.Status;
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 20), gitHash);

        //Git status is clipped if it's larger then size of label
        GUI.Label(new Rect(10, 30, 400, 400), gitStatus);
    }
}
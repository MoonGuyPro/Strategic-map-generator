using UnityEditor;
using UnityEditor.SceneManagement;

public static class BatchRunner
{
    public static void RunSim()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
    }
}
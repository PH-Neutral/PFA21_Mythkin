using UnityEngine.SceneManagement;

public static class LoadingData {
    public static int sceneToLoadIndex;

    public static void LoadScene(int index) {
        sceneToLoadIndex = index;
        SceneManager.LoadScene("Loading");
    }
}
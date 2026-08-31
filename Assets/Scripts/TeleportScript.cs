using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("TOUCHING");
        SceneManager.LoadScene(1);
    }
}

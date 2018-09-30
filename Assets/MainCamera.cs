using UnityEngine;
using System.Collections;

public class MainCamera : MonoBehaviour
{
    private static MainCamera _Instance = null;
    public static MainCamera Instance
    {
        get
        {
            if (_Instance == null) _Instance = GameObject.FindObjectOfType<MainCamera>();
            return _Instance;
        }
    }

    public static Shader MainShader { get { return Instance._MainShader; } }
    public Shader _MainShader = null;

	// Use this for initialization
	void Start ()
    {
        // set w/h to screen res
        Camera cam = GetComponent<Camera>();
        //camera.transform.position = new Vector3(-0.5f, 0.5f);
        cam.orthographicSize = Screen.height / 2;
        /*camera.transform.Translate((float)Screen.width / 2 / 100, (float)Screen.height / 2 / 100, 0, Space.World);
        camera.projectionMatrix *= Matrix4x4.Scale(new Vector3(100, -100, 1));*/
        cam.transform.Translate((float)Screen.width / 2, (float)Screen.height / 2, 0, Space.World);
        cam.projectionMatrix *= Matrix4x4.Scale(new Vector3(1, -1, 1));
        Debug.LogFormat("{0}x{1}", Screen.width, Screen.height);
    }
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}

using UnityEngine;
using System.Collections;

public class sphere_calibration : MonoBehaviour {
    public GameObject sphere;
    public Vector3[] calibration_points;
    public int point;
	// Use this for initialization
	void Start () {
	    calibration_points = new Vector3[]{new Vector3(0.0f, 0.01f, 0.0f),
                                            new Vector3(0.5f, 0.01f, -0.3f),
                                            new Vector3(-0.5f, 0.01f, -0.3f),
                                            new Vector3(-0.5f, 0.01f, 0.3f),
                                            new Vector3(0.5f, 0.01f, 0.3f)};
        point = 1;
        //sphere.transform.position = calibration_points[1];
    }

    public void next_point()
    {
        point++;
        if (point < calibration_points.Length)
        {
            Debug.Log("Proximo punto");
            Debug.Log(point);
            Debug.Log(calibration_points[point]);
            sphere.transform.position = calibration_points[point];
        }
    }
	// Update is called once per frame
	void Update () {
	
	}
}

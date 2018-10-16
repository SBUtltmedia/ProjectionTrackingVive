using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class createCube : MonoBehaviour {

    public GameObject cube;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKeyDown("c"))
        {
            var tracker = GameObject.Find("Model");
            Vector3 trackerPos = tracker.transform.position;
            Instantiate(cube, trackerPos, Quaternion.identity);
        }
	}
}

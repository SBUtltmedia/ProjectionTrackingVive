﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO: refactor so _height - pos.y is abstracted away

public class vertices : MonoBehaviour
{
    public Texture2D _crosshairTexture;
    public Camera _mainCamera;

    private Calibration _calibrator;
    private int _minNumberOfPoints = 7;
    private List<Vector3> _objectPositions = new List<Vector3>();
    private List<Vector3> _imagePositions = new List<Vector3>();
    private bool _occludeWorld;
    private int _width;
    private int _height;
    private double _reprojectionError;
    private int? _dragging;
    private bool _calibrating;

    private static int IMAGE_POINT_MARKER_SIZE = 5;
    private static int IMAGE_POINT_HIGHLIGHTED_SIZE = 10;
    private static string CALIB_SPHERE_TAG = "CalibrationSphere";

    public GameObject mycube;

    void Start()
    {
        _occludeWorld = false;
        _reprojectionError = 0.0;
        _height = (int)Screen.height;
        _width = (int)Screen.width;
        _calibrator = new Calibration(_mainCamera);
        _calibrating = true;

    }

    void OnGUI()
    {

        if (!_calibrating)
            return;

        if (_occludeWorld)
            OccludeScreen();

        _imagePositions.ForEach(delegate (Vector3 position) { Debug.Log(position); DrawCrosshairImage(position, IMAGE_POINT_MARKER_SIZE); });
        if (ImagePointHighlighted())
            DrawCrosshairImage(_imagePositions[ImagePointMouseHooveringOver().Value], IMAGE_POINT_HIGHLIGHTED_SIZE);

        GUI.Box(new Rect(10, 10, 150, 90), "# points: " + _imagePositions.Count.ToString() + "\nReProj error: " + string.Format("{0:f2}", _reprojectionError));
    }

    private void DrawCrosshairImage(Vector3 position, int imagePointMarkerWidth)
    {

        GUI.DrawTexture(new Rect(position.x - imagePointMarkerWidth, position.y - imagePointMarkerWidth, imagePointMarkerWidth * 2, imagePointMarkerWidth * 2), _crosshairTexture);
    }

    private void OccludeScreen()
    {
        Texture2D blackTexture = new Texture2D(1, 1);
        blackTexture.SetPixel(0, 0, Color.black);
        GUI.DrawTexture(new Rect(0, 0, _width, _height), blackTexture);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleCalibrating();
            makeSpheres();
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (ImagePointHighlighted())
            {

                _dragging = ImagePointMouseHooveringOver();
            }
            else
            {
                //Debug.Log(_imagePositions.Count);
                //Debug.Log(_objectPositions.Count);
                if (_imagePositions.Count < _objectPositions.Count)
                {
                   // Debug.Log("hell");
                    // We have the same number of object and image positions, so we are starting a new correspondence. First is the object position
             
                    // we already have an object position, now we collect the 2D correspondence
                    CaptureImagePoint();
                    TriggerCalibration();
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _dragging = null;
            TriggerCalibration();
        }

        if (_dragging != null)
        {
            var pos = Input.mousePosition;
            pos.y = _height - pos.y;
            _imagePositions[_dragging.Value] = pos;
        }
    }

   private void makeSpheres()
    {
        Debug.Log("local:" + mycube.transform.position);

         Mesh mf = mycube.GetComponent<MeshFilter>().mesh;

         Vector3[] vertices = mf.vertices.Distinct().ToArray();
        int p = 0;
        //// Debug.Log(vertices.Length);
         while (p < vertices.Length)
        {

        //     Debug.Log("local:" + mycube.transform.position);
             Vector3 point = transform.TransformPoint(vertices[p]);
            Debug.Log("world:"+point);
        //     //  Debug.Log(transform.TransformPoint(vertices[p]));
            CreateSphereAt(point);
             _objectPositions.Add(point);
        //    // _occludeWorld = true;
          p++;
         }

    }


    private void ToggleCalibrating()
    {
        _calibrating = !_calibrating;

        foreach (GameObject sphere in GameObject.FindGameObjectsWithTag(CALIB_SPHERE_TAG))
        {
            var renderer = sphere.GetComponent<Renderer>();
            renderer.enabled = _calibrating;
        }
    }

    private bool ImagePointHighlighted()
    {
        return ImagePointMouseHooveringOver() != null;
    }

    private int? ImagePointMouseHooveringOver()
    {
        Vector3 pos = Input.mousePosition;
        int? imagePosMatch = null;
        for (int i = 0; i < _imagePositions.Count; i++)
        {
            Vector3 imagePos = _imagePositions[i];
            if (Mathf.Abs(imagePos.x - pos.x) + Mathf.Abs(imagePos.y - (_height - pos.y)) < 5)
            {
                imagePosMatch = i;
            }
        }
        return imagePosMatch;
    }

    private void CaptureWorldPoint()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit3d;
        if (Physics.Raycast(ray, out hit3d))
        {
            CreateSphereAt(hit3d.point);
            _objectPositions.Add(hit3d.point);
            _occludeWorld = true;
        }
    }

    private void CaptureImagePoint()
    {
        Vector3 pos = Input.mousePosition;
        pos.y = _height - pos.y; // note the screen pos starts bottom left. We want top left origin
        _imagePositions.Add(pos);
        _occludeWorld = false;
    }

    private static void CreateSphereAt(Vector3 point)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position = point;
        sphere.tag = CALIB_SPHERE_TAG;
        sphere.transform.localScale = new Vector3(0.1F, 0.1F, 0.1F);
    }

    private void TriggerCalibration()
    {
        if (_imagePositions.Count < _minNumberOfPoints)
            return;
        if (_imagePositions.Count != _objectPositions.Count)
            return;

        _reprojectionError = _calibrator.calibrateFromCorrespondences(_imagePositions, _objectPositions);
    }

    public List<Vector3> GetImagePoints()
    {
        return _imagePositions;
    }

    public List<Vector3> GetWorldPoints()
    {
        return _objectPositions;
    }

    public void ReplayRecordedPoints(List<Vector3> worldPoints, List<Vector3> imgPoints)
    {
        _imagePositions = imgPoints;
        _objectPositions = worldPoints;
        foreach (var point in worldPoints)
            CreateSphereAt(point);
        TriggerCalibration();
    }
}
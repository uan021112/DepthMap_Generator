using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class DepthMapGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        InitPath();
        _idx = 0;
        GetFrameNum();
    }

    // Update is called once per frame
    void Update()
    {
        if (_idx < _frameNum)
        {
            GetFilePath();
            RotateModel();
            PlaceRGBImage();
            SaveDepthMap();
            _idx++;
        }
    }

    #region DepthManager
    public Camera _cam;
    public GameObject _model;
    public RawImage _rawImage;

    private int _idx;
    private int _frameNum;

    private void GetFrameNum()
    {
        _frameNum = Directory.GetFiles(_colorPath).Length;
    }

    private void GetFilePath()
    {
        GetColorFilePath();
        GetDepthFilePath();
        GetPoseFilePath();
    }
    private void RotateModel()
    {
        Matrix4x4 poseMatrix = ReadPose(_poseFilePath);
        Pose pose = TransferMatrixToPose(poseMatrix);
        _model.transform.position = pose.position;
        _model.transform.rotation = pose.rotation;

        _model.transform.RotateAround(_cam.transform.position, _cam.transform.right, 180f);
        _model.transform.RotateAround(_cam.transform.position, _cam.transform.forward, 90f);
        _model.transform.Rotate(new Vector3(180, 0, 0));
    }

    private void PlaceRGBImage()
    {
        byte[] imageData = File.ReadAllBytes(_colorFilePath);
        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(imageData); 
        _rawImage.texture = texture;
    }

    private void SaveDepthMap()
    {
        int width = 1440,
            height = 1920;
        RenderTexture rt = new RenderTexture(width, height, 0);

        _cam.targetTexture = rt;
        RenderTexture.active = rt;
        _cam.Render();
        Texture2D screenShot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenShot.Apply();

        byte[] bytes = screenShot.EncodeToJPG();
        File.WriteAllBytes(_depthFilePath, bytes);

        _cam.targetTexture = null;
        RenderTexture.active = null;
    }
    #endregion

    #region ModelManager
    private Matrix4x4 ReadPose(string poseFilePath)
    {
        //
        Matrix4x4 matrix = new Matrix4x4(); 
        using (StreamReader reader = new StreamReader(poseFilePath))
        {
            for (int i = 0; i < 4; i++)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < 4; j++)
                {
                    matrix[i, j] = float.Parse(values[j]);
                }
            }
        }
        return matrix;     
    }

    private Pose TransferMatrixToPose(Matrix4x4 rtM)
    {
        Matrix4x4 rtM_inverse = rtM.inverse;
        Vector3 position = GetPosition(rtM_inverse);
        position.x *= -1;

        Vector3 v = new Vector3();
        Quaternion q = rtM_inverse.rotation;

        v = q.eulerAngles;
        //v.x = 180.0f - v.x;
        //v.z *= 1;

        v.x *= -1;
        v.y = 180.0f - v.y;
        v.z = 180.0f + v.z;
        q = Quaternion.Euler(v);

        Quaternion rotation = q;

        return new Pose(position, rotation);
    }

    private Vector3 GetPosition(Matrix4x4 matrix)
    {
        var x = matrix.m03;
        var y = matrix.m13;
        var z = matrix.m23;
        return new Vector3(x, y, z);
    }
    #endregion

    #region PathManager
    public string _workspacePath;

    private string _colorPath;
    private string _depthPath;
    private string _posePath;

    private string _colorFilePath;
    private string _depthFilePath;
    private string _poseFilePath;

    private void InitPath()
    {
        _colorPath = Path.Combine(_workspacePath, "color");
        _depthPath = Path.Combine(_workspacePath, "depth");
        _posePath = Path.Combine(_workspacePath, "pose");
    }

    private void GetColorFilePath()
    {
        _colorFilePath = Path.Combine(_colorPath, $"frame-{_idx:D6}.color.jpg");
    }

    private void GetDepthFilePath()
    {
        _depthFilePath = Path.Combine(_depthPath, $"frame-{_idx:D6}.depth.jpg");
    }

    private void GetPoseFilePath()
    {
        _poseFilePath = Path.Combine(_posePath, $"frame-{_idx:D6}.pose.txt");
    }
    #endregion

}

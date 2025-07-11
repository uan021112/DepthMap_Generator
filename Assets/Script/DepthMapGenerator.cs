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
            if (_isRotateModel)
            {
                RotateModel();
            }
            else
            {
                RotateCamera();
            }
            PlaceRGBImage();
            SaveDepthMap();
            _idx++;
        }
    }

    #region DepthManager
    public Camera _cam;
    public GameObject _model;
    public RawImage _rawImage;
    public bool _isRotateModel = true;

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

    private void RotateCamera()
    {
        Matrix4x4 mPose_c2w = ReadPose(_poseFilePath);
        Pose pose = TransferMatrixToPose(mPose_c2w);
        _cam.transform.rotation = pose.rotation;
        _cam.transform.position = pose.position;
    }

    private void RotateModel()
    {
        Matrix4x4 mPose_w2c = ReadPose(_poseFilePath).inverse;
        Pose pose = TransferMatrixToPose(mPose_w2c);
        _model.transform.rotation = pose.rotation;
        _model.transform.position = pose.position;
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
        Vector3 position = GetPosition(rtM);
        Quaternion rotation = rtM.rotation;
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

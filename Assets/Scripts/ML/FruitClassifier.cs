using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TensorFlowLite;
using UnityEngine;

public sealed class FruitClassifier : IDisposable
{
    public const float ConfidenceThreshold = 0.55f;
    private const int ImageSize = 224;
    private Interpreter interpreter;
    private readonly string[] labels;

    public FruitClassifier(byte[] modelBytes, string labelText)
    {
        labels = ParseLabels(labelText);
        if (labels.Length == 0) throw new InvalidDataException("Fruit model label list is empty.");
        if (modelBytes == null || modelBytes.Length == 0) throw new InvalidDataException("Fruit TFLite model asset is empty.");
        // This TFLite package's one-argument constructor dereferences the nullable options parameter.
        // Passing options explicitly avoids that package bug.
        var candidate = new Interpreter(modelBytes, new InterpreterOptions());
        try
        {
            var input = candidate.GetInputTensorInfo(0);
            var output = candidate.GetOutputTensorInfo(0);
            if (input.shape.Length != 4 || input.shape[0] != 1 || input.shape[1] != ImageSize || input.shape[2] != ImageSize || input.shape[3] != 3)
                throw new InvalidDataException("Unsupported TFLite input shape: " + string.Join("x", input.shape));
            if (input.type != Interpreter.DataType.Float32)
                throw new InvalidDataException("Expected Float32 input tensor; model reports " + input.type);
            if (output.shape.Length < 2 || output.shape[output.shape.Length - 1] != labels.Length)
                throw new InvalidDataException("Output class count does not match labels. Shape=" + string.Join("x", output.shape) + ", labels=" + labels.Length);
            if (output.type != Interpreter.DataType.Float32)
                throw new InvalidDataException("Expected Float32 output tensor; model reports " + output.type);
            candidate.AllocateTensors();
            interpreter = candidate;
            Debug.Log($"TFLite ready. input={string.Join("x", input.shape)} {input.type}, output={string.Join("x", output.shape)} {output.type}, classes={labels.Length}");
        }
        catch { candidate.Dispose(); throw; }
    }

    public (string label, float confidence) Classify(Texture2D image)
    {
        if (interpreter == null) throw new ObjectDisposedException(nameof(FruitClassifier));
        var pixels = image.GetPixels32();
        var input = new float[ImageSize * ImageSize * 3];
        var side = Mathf.Min(image.width, image.height);
        var offsetX = (image.width - side) / 2f;
        var offsetY = (image.height - side) / 2f;
        for (var y = 0; y < ImageSize; y++)
        for (var x = 0; x < ImageSize; x++)
        {
            var sx = Mathf.Clamp(Mathf.FloorToInt(offsetX + (x + .5f) * side / ImageSize), 0, image.width - 1);
            var sy = Mathf.Clamp(Mathf.FloorToInt(offsetY + (y + .5f) * side / ImageSize), 0, image.height - 1);
            var p = pixels[sy * image.width + sx];
            var i = (y * ImageSize + x) * 3;
            input[i] = (p.r - 127.5f) / 127.5f;
            input[i + 1] = (p.g - 127.5f) / 127.5f;
            input[i + 2] = (p.b - 127.5f) / 127.5f;
        }
        interpreter.SetInputTensorData(0, input);
        interpreter.Invoke();
        var outputInfo = interpreter.GetOutputTensorInfo(0);
        var output = new float[outputInfo.shape.Aggregate(1, (a, b) => a * b)];
        interpreter.GetOutputTensorData(0, output);
        var best = Array.IndexOf(output, output.Max());
        return (labels[best], output[best]);
    }

    private static string[] ParseLabels(string text) => (text ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
        .Select(line => { var parts = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries); return parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : (parts.Length == 1 ? parts[0] : ""); })
        .Where(label => !string.IsNullOrWhiteSpace(label)).ToArray();

    public void Dispose() { interpreter?.Dispose(); interpreter = null; }
}

public sealed class FruitScanService : MonoBehaviour
{
    public static FruitScanService Instance { get; private set; }
    [SerializeField] private TextAsset labelsAsset;
    private FruitClassifier classifier;
    private WebCamTexture cameraTexture;
    private Texture2D lastCapturedFrame;
    private int lastCaptureRotationAngle;
    private int cameraGeneration;
    public bool LastCaptureMirrored { get; private set; }
    public bool CameraRunning => cameraTexture != null && cameraTexture.isPlaying;

    private async void Awake()
    {
        Instance = this;
        if (labelsAsset == null) labelsAsset = Resources.Load<TextAsset>("labels");
        if (labelsAsset == null) { Debug.LogError("Missing Assets/Resources/labels.txt"); return; }
        var request = Resources.LoadAsync<TextAsset>("fruit_model");
        while (!request.isDone) await Task.Yield();
        if (request.asset is TextAsset model)
        {
            try { classifier = new FruitClassifier(model.bytes, labelsAsset.text); }
            catch (Exception e) { Debug.LogError("TFLite model initialization failed: " + e); }
        }
        else Debug.LogError("Missing Assets/Resources/fruit_model.tflite");
    }

    public async Task<bool> StartCamera()
    {
        if (CameraRunning) return true;
        var generation = ++cameraGeneration;
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) await Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (this == null || generation != cameraGeneration) return false;
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam)) return false;
        var devices = WebCamTexture.devices;
        if (devices.Length == 0) return false;
        var device = devices.FirstOrDefault(d => d.isFrontFacing == false);
        if (string.IsNullOrEmpty(device.name)) device = devices[0];
        cameraTexture = new WebCamTexture(device.name, 1280, 720, 30);
        cameraTexture.Play();
        for (var i = 0; i < 40; i++)
        {
            if (this == null || generation != cameraGeneration || cameraTexture == null) return false;
            if (cameraTexture.didUpdateThisFrame && cameraTexture.width > 16) return true;
            await Task.Delay(50);
        }
        if (generation == cameraGeneration) StopCamera();
        return false;
    }

    public Texture CameraFeed => cameraTexture;
    public WebCamTexture CameraTexture => cameraTexture;
    public Texture2D LastCapturedFrame => lastCapturedFrame;
    public int LastCaptureRotationAngle => lastCaptureRotationAngle;
    public (FruitData fruit, float confidence, string error) CaptureAndClassify()
    {
        if (!CameraRunning) return (null, 0, "Camera is not ready.");
        if (classifier == null) return (null, 0, "TFLite model is unavailable. Check the Unity Console.");
        Texture2D frame = null;
        try
        {
            frame = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGB24, false);
            frame.SetPixels32(cameraTexture.GetPixels32()); frame.Apply(false, false);
            var (label, confidence) = classifier.Classify(frame);
            var fruit = FruitDatabase.Instance.FindByLabel(label);
            if (fruit == null) return (null, confidence, "Model label has no fruit record: " + label);
            if (confidence < FruitClassifier.ConfidenceThreshold) return (null, confidence, "ผลไม้ที่พบยังไม่ชัดเจน");
            LocalRepository.AddScan(fruit, confidence);
            if (lastCapturedFrame != null) Destroy(lastCapturedFrame);
            lastCapturedFrame = frame;
            lastCaptureRotationAngle = cameraTexture.videoRotationAngle;
            LastCaptureMirrored = cameraTexture.videoVerticallyMirrored;
            frame = null;
            return (fruit, confidence, null);
        }
        catch (Exception e) { Debug.LogException(e); return (null, 0, "Inference failed: " + e.Message); }
        finally { if (frame != null) Destroy(frame); }
    }

    public void StopCamera() { cameraGeneration++; if (cameraTexture != null) { cameraTexture.Stop(); Destroy(cameraTexture); cameraTexture = null; } }
    private void OnDestroy() { StopCamera(); classifier?.Dispose(); if (lastCapturedFrame != null) Destroy(lastCapturedFrame); if (Instance == this) Instance = null; }
}

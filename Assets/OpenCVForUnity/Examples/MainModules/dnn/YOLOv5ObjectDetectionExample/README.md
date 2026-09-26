# YOLOv5 Object Detection Example — Supported Models

> **Model files:** YOLOv5 models are not included in OpenCV for Unity due to licensing. Download pre-exported models or export your own ONNX files, then place them under `Assets/StreamingAssets/OpenCVForUnityExamples/dnn/yolov5`. Sample assets and setup notes: [YOLOv5WithOpenCVForUnityExample](https://github.com/EnoxSoftware/YOLOv5WithOpenCVForUnityExample).

This example and `YOLOv5ObjectDetector` also work with **YOLOv6** and **YOLOv7** object-detection models exported for OpenCV DNN with the same input/output shapes as YOLOv5. See [YOLOv5WithOpenCVForUnityExample](https://github.com/EnoxSoftware/YOLOv5WithOpenCVForUnityExample).

## Contents

- [YOLOv5](#yolov5)
- [YOLOv6](#yolov6)
- [YOLOv7](#yolov7)

---

## YOLOv5

### References

- Source repository url: https://github.com/ultralytics/yolov5

### Export to ONNX

- YOLOv5_export_to_OpenCVDNN_ONNX.ipynb

### Convert ONNX to Sentis format

If you want to use the model with Unity Sentis, convert the `.onnx` file to `.sentis` format in the Unity Editor.

1. Place the `.onnx` file in a folder **outside `StreamingAssets`**, for example:
   `Assets/Models/yolov5/model.onnx`
2. In the Unity Editor, select the `.onnx` file and make sure it is recognized by the **Sentis ONNX Model Importer**.
3. Open the **Sentis Inspector** for the imported ONNX model.
4. Click the **Serialize to StreamingAssets** button.
5. Sentis will generate a `.sentis` file directly under the `StreamingAssets` folder.
6. Move the generated `.sentis` file from `StreamingAssets` to the same folder as the original `.onnx` file:
   `Assets/StreamingAssets/OpenCVForUnityExamples/dnn/yolov5`
7. Copy or move the original `.onnx` file to the same folder as the `.sentis` file.

The resulting folder should look like:

```text
Assets/
└── StreamingAssets/
    └── OpenCVForUnityExamples/
        └── dnn/
            └── yolov5/
                ├── model.onnx
                └── model.sentis
```

**Note:** Do not place the `.onnx` file directly under `StreamingAssets` before conversion. The Sentis ONNX Model Importer does not process ONNX files located in `StreamingAssets` in the required way.

### Training resources

- [Train Custom Data](https://github.com/ultralytics/yolov5/wiki/Train-Custom-Data)
- [How to Train a YOLOv5 Model On a Custom Dataset](https://blog.roboflow.com/how-to-train-yolov5-on-a-custom-dataset/)

---

## YOLOv6

### References

- Source repository url: https://github.com/meituan/YOLOv6

### Export to ONNX

- YOLOv6_export_to_OpenCVDNN_ONNX.ipynb

### Training resources

- [How to Train a YOLOv6 Model on a Custom Dataset](https://blog.roboflow.com/how-to-train-yolov6-on-a-custom-dataset/)

---

## YOLOv7

### References

- Source repository url: https://github.com/WongKinYiu/yolov7

### Export to ONNX

- YOLOv7_export_to_OpenCVDNN_ONNX.ipynb

### Training resources

- [How to Train a YOLOv7 Model on a Custom Dataset](https://blog.roboflow.com/yolov7-custom-dataset-training-tutorial/)

# YOLOv8 Instance Segmentation Example — Supported Models

> **Model files:** YOLOv8 models are not included in OpenCV for Unity due to licensing. Download pre-exported models or export your own ONNX/Sentis files, then place them under `Assets/StreamingAssets/OpenCVForUnityExamples/dnn/yolov8`. Sample assets and setup notes: [YOLOv8WithOpenCVForUnityExample](https://github.com/EnoxSoftware/YOLOv8WithOpenCVForUnityExample).

This example and `YOLOv8InstanceSegmenter` also work with **YOLOv5u**, **YOLOv9**, ~~**YOLOv10**~~, **YOLOv11**, and **YOLOv12** instance-segmentation models exported for OpenCV DNN with the same input/output shapes as YOLOv8. See [YOLOv8WithOpenCVForUnityExample](https://github.com/EnoxSoftware/YOLOv8WithOpenCVForUnityExample).

## Contents

- [YOLOv8](#yolov8)
- [YOLOv5u](#yolov5u)
- [YOLOv9](#yolov9)
- [YOLOv10](#yolov10)
- [YOLOv11](#yolov11)
- [YOLOv12](#yolov12)

---

## YOLOv8

### References

- https://github.com/ultralytics/ultralytics
- https://docs.ultralytics.com/tasks/
- https://docs.ultralytics.com/models/yolov8/
- https://github.com/ultralytics/ultralytics/tree/main/examples/YOLOv8-OpenCV-ONNX-Python

### Export to ONNX

- YOLOv8_export_to_OpenCVDNN_ONNX.ipynb

### Convert ONNX to Sentis format

If you want to use the model with Unity Sentis, convert the `.onnx` file to `.sentis` format in the Unity Editor.

1. Place the `.onnx` file in a folder **outside `StreamingAssets`**, for example:
   `Assets/Models/yolov8/model.onnx`
2. In the Unity Editor, select the `.onnx` file and make sure it is recognized by the **Sentis ONNX Model Importer**.
3. Open the **Sentis Inspector** for the imported ONNX model.
4. Click the **Serialize to StreamingAssets** button.
5. Sentis will generate a `.sentis` file directly under the `StreamingAssets` folder.
6. Move the generated `.sentis` file from `StreamingAssets` to the same folder as the original `.onnx` file:
   `Assets/StreamingAssets/OpenCVForUnityExamples/dnn/yolov8`
7. Copy or move the original `.onnx` file to the same folder as the `.sentis` file.

The resulting folder should look like:

```text
Assets/
└── StreamingAssets/
    └── OpenCVForUnityExamples/
        └── dnn/
            └── yolov8/
                ├── model.onnx
                └── model.sentis
```

**Note:** Do not place the `.onnx` file directly under `StreamingAssets` before conversion. The Sentis ONNX Model Importer does not process ONNX files located in `StreamingAssets` in the required way.

### Training resources

- [How to Train YOLOv8 Object Detection on a Custom Dataset](https://github.com/roboflow/notebooks/blob/main/notebooks/train-yolov8-object-detection-on-custom-dataset.ipynb)
- [YOLOv8 Train Custom Dataset: Train Your Object Detection Model](https://yolov8.org/yolov8-train-custom-dataset-train/)
- [Training custom datasets with Ultralytics YOLOv8 in Google Colab](https://www.ultralytics.com/blog/training-custom-datasets-with-ultralytics-yolov8-in-google-colab)
- [How to Train YOLOv8 Instance Segmentation on a Custom Dataset](https://blog.roboflow.com/how-to-train-a-yolov8-classification-model/)
- [How to Train an Ultralytics YOLOv8 Classification Model](https://blog.roboflow.com/how-to-train-a-yolov8-classification-model/)
- [How to Train a Custom Ultralytics YOLOv8 Pose Estimation Model](https://blog.roboflow.com/train-a-custom-yolov8-pose-estimation-model/)
- [How to Train an Ultralytics YOLOv8 Oriented Bounding Box (OBB) Model](https://blog.roboflow.com/train-yolov8-obb-model/)

---

## YOLOv5u

### References

- https://github.com/ultralytics/ultralytics
- https://docs.ultralytics.com/tasks/
- https://docs.ultralytics.com/models/yolov5/

### Export to ONNX

- YOLOv5u_export_to_OpenCVDNN_ONNX.ipynb

---

## YOLOv9

### References

- https://github.com/ultralytics/ultralytics
- https://docs.ultralytics.com/tasks/
- https://docs.ultralytics.com/models/yolov9/

### Export to ONNX

- YOLOv9_export_to_OpenCVDNN_ONNX.ipynb

### Training resources

- [How to Train YOLOv9 on a Custom Dataset](https://blog.roboflow.com/train-yolov9-model/)

---

## YOLOv10

### References

- https://github.com/ultralytics/ultralytics
- https://docs.ultralytics.com/tasks/
- https://docs.ultralytics.com/models/yolov10/

### Export to ONNX

- YOLOv10_export_to_OpenCVDNN_ONNX.ipynb

### Training resources

- [How to Train a YOLOv10 Model on a Custom Dataset](https://blog.roboflow.com/yolov10-how-to-train/)

---

## YOLOv11

### References

- https://github.com/ultralytics/ultralytics
- https://docs.ultralytics.com/tasks/
- https://docs.ultralytics.com/models/yolov11/

### Export to ONNX

- YOLOv11_export_to_OpenCVDNN_ONNX.ipynb

### Training resources

- [How to Train a YOLOv11 Object Detection Model on a Custom Dataset](https://blog.roboflow.com/yolov11-how-to-train-custom-data/)
- [How to Train YOLOv11 Instance Segmentation on a Custom Dataset](https://blog.roboflow.com/train-yolov11-instance-segmentation/)

---

## YOLOv12

### References

- https://github.com/ultralytics/ultralytics
- https://docs.ultralytics.com/tasks/
- https://docs.ultralytics.com/models/yolov12/

### Export to ONNX

- YOLOv12_export_to_OpenCVDNN_ONNX.ipynb

### Training resources

- [How to Train a YOLOv12 Object Detection Model on a Custom Dataset](https://blog.roboflow.com/train-yolov12-model/)

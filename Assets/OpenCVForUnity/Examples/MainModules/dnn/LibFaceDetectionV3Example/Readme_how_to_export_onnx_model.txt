#
# How to export onnx model
#

1. Install PyTorch >= v1.0.0 following official instruction.

python                    3.7.7
pytorch                   1.4.0


2. Clone this repository. We will call the cloned directory as $TRAIN_ROOT.

git clone https://github.com/ShiqiYu/libfacedetection.train


3. Copy the export_onnx.py to $TRAIN_ROOT folder.


4. Run the export_onnx.py.

cd $TRAIN_ROOT
python export_onnx.py


5. Copy the yunet_final.onnx to Assets/StreamingAssets/dnn folder in your project.
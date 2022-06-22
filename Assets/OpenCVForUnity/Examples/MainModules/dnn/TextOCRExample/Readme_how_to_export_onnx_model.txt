#
# How to export onnx model (Text recognition model)
#

1. Install PyTorch >= v1.0.0 following official instruction.

python                    3.7.7
pytorch                   1.4.0


2. Clone this repository. We will call the cloned directory as $TRAIN_ROOT.

git clone https://github.com/meijieru/crnn.pytorch


3. Download a pretrained model.
   Put the downloaded model file crnn.pth into directory $TRAIN_ROOT/data/. (See README in repository)

https://pan.baidu.com/s/1pLbeCND
or
https://www.dropbox.com/s/dboqjk20qjkpta3/crnn.pth?dl=0


4. Copy the export_onnx.py to $TRAIN_ROOT folder.


5. Run the export_onnx.py.

cd $TRAIN_ROOT
python export_onnx.py


6. Copy the crnn.onnx to Assets/StreamingAssets/dnn folder in your project.
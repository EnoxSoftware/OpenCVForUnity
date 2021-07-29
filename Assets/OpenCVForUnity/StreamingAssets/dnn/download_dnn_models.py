#!/usr/bin/env python

from __future__ import print_function
import hashlib
import os
import sys
import tarfile
import zipfile
import requests

if sys.version_info[0] < 3:
    from urllib2 import urlopen
else:
    from urllib.request import urlopen


class Model:
    MB = 1024*1024
    BUFSIZE = 10*MB

    def __init__(self, **kwargs):
        self.name = kwargs.pop('name')
        self.url = kwargs.pop('url', None)
        self.downloader = kwargs.pop('downloader', None)
        self.filename = kwargs.pop('filename')
        self.sha = kwargs.pop('sha', None)
        self.archive = kwargs.pop('archive', None)
        self.member = kwargs.pop('member', None)
        self.delete = kwargs.pop('delete', None)

    def __str__(self):
        return 'Model <{}>'.format(self.name)

    def printRequest(self, r):
        def getMB(r):
            d = dict(r.info())
            for c in ['content-length', 'Content-Length']:
                if c in d:
                    return int(d[c]) / self.MB
            return '<unknown>'
        print('  {} {} [{} Mb]'.format(r.getcode(), r.msg, getMB(r)))

    def verify(self):
        if not self.sha:
            return False
        print('  expect {}'.format(self.sha))
        sha = hashlib.sha1()
        try:
            with open(self.filename, 'rb') as f:
                while True:
                    buf = f.read(self.BUFSIZE)
                    if not buf:
                        break
                    sha.update(buf)
            print('  actual {}'.format(sha.hexdigest()))
            return self.sha == sha.hexdigest()
        except Exception as e:
            print('  catch {}'.format(e))

    def get(self):
                
        if self.delete:
            if os.path.isfile(self.delete):
                os.remove(self.delete)
                print('  delete {}'.format(self.delete))
            return True

        if self.verify():
            print('  hash match - skipping')
            return True

        basedir = os.path.dirname(self.filename)
        if basedir and not os.path.exists(basedir):
            print('  creating directory: ' + basedir)
            os.makedirs(basedir, exist_ok=True)

        if self.archive or self.member:
            assert(self.archive and self.member)
            print('  hash check failed - extracting')
            print('  get {}'.format(self.member))
            self.extract()
        elif self.url:
            print('  hash check failed - downloading')
            print('  get {}'.format(self.url))
            self.download()
        else:
            assert self.downloader
            print('  hash check failed - downloading')
            sz = self.downloader(self.filename)
            print('  size = %.2f Mb' % (sz / (1024.0 * 1024)))

        print(' done')
        print(' file {}'.format(self.filename))
        return self.verify()

    def download(self):
        try:
            r = urlopen(self.url, timeout=60)
            self.printRequest(r)
            self.save(r)
        except Exception as e:
            print('  catch {}'.format(e))

    def extract(self):
        if zipfile.is_zipfile(self.archive):
            try:
                with zipfile.ZipFile(self.archive, 'r') as f:
                    assert self.member in f.namelist()
                    f.extract(self.member)
            except Exception as e:
                print('  catch {}'.format(e))
        else:
            try:
                with tarfile.open(self.archive) as f:
                    assert self.member in f.getnames()
                    self.save(f.extractfile(self.member))
            except Exception as e:
                print('  catch {}'.format(e))

    def save(self, r):
        with open(self.filename, 'wb') as f:
            print('  progress ', end='')
            sys.stdout.flush()
            while True:
                buf = r.read(self.BUFSIZE)
                if not buf:
                    break
                f.write(buf)
                print('>', end='')
                sys.stdout.flush()


def GDrive(gid):
    def download_gdrive(dst):
        session = requests.Session()  # re-use cookies

        URL = "https://docs.google.com/uc?export=download"
        response = session.get(URL, params = { 'id' : gid }, stream = True)

        def get_confirm_token(response):  # in case of large files
            for key, value in response.cookies.items():
                if key.startswith('download_warning'):
                    return value
            return None
        token = get_confirm_token(response)

        if token:
            params = { 'id' : gid, 'confirm' : token }
            response = session.get(URL, params = params, stream = True)

        BUFSIZE = 1024 * 1024
        PROGRESS_SIZE = 10 * 1024 * 1024

        sz = 0
        progress_sz = PROGRESS_SIZE
        with open(dst, "wb") as f:
            for chunk in response.iter_content(BUFSIZE):
                if not chunk:
                    continue  # keep-alive

                f.write(chunk)
                sz += len(chunk)
                if sz >= progress_sz:
                    progress_sz += PROGRESS_SIZE
                    print('>', end='')
                    sys.stdout.flush()
        print('')
        return sz
    return download_gdrive


models = [
    # ColorizationExample : # https://github.com/richzhang/colorization/
    Model(
        name='ColorizationExample',
        url='https://github.com/richzhang/colorization/raw/caffe/demo/imgs/ansel_adams3.jpg',
        sha='ca4af64f5cd4adc180d167f09f617319871d4608',
        filename='ansel_adams3.jpg'),
    Model(
        name='ColorizationExample',
        url='http://eecs.berkeley.edu/~rich.zhang/projects/2016_colorization/files/demo_v2/colorization_release_v2.caffemodel',
        sha='21e61293a3fa6747308171c11b6dd18a68a26e7f',
        filename='colorization_release_v2.caffemodel'),
    Model(
        name='ColorizationExample',
        url='https://github.com/richzhang/colorization/raw/caffe/models/colorization_deploy_v2.prototxt',
        sha='f528334e386a69cbaaf237a7611d833bef8e5219',
        filename='colorization_deploy_v2.prototxt'),


    # DaSiamRPNTrackerExample : # https://github.com/opencv/opencv/blob/master/samples/dnn/dasiamrpn_tracker.py
    Model(
        name='DaSiamRPNTrackerExample',
        url='https://www.dropbox.com/s/rr1lk9355vzolqv/dasiamrpn_model.onnx?dl=1',
        sha='91b774fce7df4c0e4918469f0f482d9a27d0e2d4',
        filename='dasiamrpn_model.onnx'),
    Model(
        name='DaSiamRPNTrackerExample',
        url='https://www.dropbox.com/s/999cqx5zrfi7w4p/dasiamrpn_kernel_r1.onnx?dl=1',
        sha='bb64620a54348657133eb28be2d3a2a8c76b84b3',
        filename='dasiamrpn_kernel_r1.onnx'),
    Model(
        name='DaSiamRPNTrackerExample',
        url='https://www.dropbox.com/s/qvmtszx5h339a0w/dasiamrpn_kernel_cls1.onnx?dl=1',
        sha='e9ccd270ce8059bdf7ed0d1845c03ef4a951ee0f',
        filename='dasiamrpn_kernel_cls1.onnx'),


    # FastNeuralStyleTransferExample : # https://github.com/jcjohnson/fast-neural-style
    Model(
        name='FastNeuralStyleTransferExample',
        url='https://cs.stanford.edu/people/jcjohns/fast-neural-style/models/instance_norm/mosaic.t7',
        sha='f4d3e2a5e3060b3c39a9648ad009de3e09cd0001',
        filename='mosaic.t7'),


    # LibFaceDetectionV2Example : # https://github.com/ShiqiYu/libfacedetection/
    Model(
        name='LibFaceDetectionV2Example',
        url='https://github.com/ShiqiYu/libfacedetection/raw/96a7cc0bbfcf05bac17c2df52bee0e8ba6c72964/models/caffe/yufacedetectnet-open-v2.caffemodel',
        sha='3716bd48064f7f56410470f875c9a5c6264ca31c',
        filename='yufacedetectnet-open-v2.caffemodel'),
    Model(
        name='LibFaceDetectionV2Example',
        url='https://github.com/ShiqiYu/libfacedetection/raw/96a7cc0bbfcf05bac17c2df52bee0e8ba6c72964/models/caffe/yufacedetectnet-open-v2.prototxt',
        sha='d8afd224aa1cc41c922075dc95f67852848352c0',
        filename='yufacedetectnet-open-v2.prototxt'),


    # LibFaceDetectionV3Example : # https://github.com/ShiqiYu/libfacedetection.train
    Model(
        name='LibFaceDetectionV3Example',
        url='https://github.com/ShiqiYu/libfacedetection.train/raw/73957ce7f04c0cd4a9f2bf0a7ad4be8fec1da222/tasks/task1/onnx/YuFaceDetectNet.onnx',
        sha='decd9c9b4e2154dca44140dcd04b65af6ea452c5',
        filename='YuFaceDetectNet.onnx'),


    # MaskRCNNExample : # https://github.com/opencv/opencv/blob/master/samples/dnn/mask_rcnn.py
    Model(
        name='MaskRCNNExample',
        url='https://github.com/chuanqi305/MobileNet-SSD/raw/master/images/004545.jpg',
        sha='2b0c65f59a9f9071f1e7de452f0c2004e8d55b7b',
        filename='004545.jpg'),
    Model(
        name='MaskRCNNExample',
        url='http://download.tensorflow.org/models/object_detection/mask_rcnn_inception_v2_coco_2018_01_28.tar.gz',
        sha='f8a920756744d0f7ee812b3ec2474979f74ab40c',
        filename='mask_rcnn_inception_v2_coco_2018_01_28.tar.gz'),
    Model(
        name='MaskRCNNExample',
        archive='mask_rcnn_inception_v2_coco_2018_01_28.tar.gz',
        member='mask_rcnn_inception_v2_coco_2018_01_28/frozen_inference_graph.pb',
        sha='c8adff66a1e23e607f57cf1a7cfabad0faa371f9',
        filename='mask_rcnn_inception_v2_coco_2018_01_28.pb'),
    Model(
        name='MaskRCNNExample',
        filename='mask_rcnn_inception_v2_coco_2018_01_28.tar.gz',
        delete='mask_rcnn_inception_v2_coco_2018_01_28.tar.gz'),
    Model(
        name='MaskRCNNExample',
        url='https://github.com/opencv/opencv_extra/raw/master/testdata/dnn/mask_rcnn_inception_v2_coco_2018_01_28.pbtxt',
        sha='31208559adc3c8ae210db9961e89558b6715ffc8',
        filename='mask_rcnn_inception_v2_coco_2018_01_28.pbtxt'),
    Model(
        name='MaskRCNNExample',
        url='https://github.com/amikelive/coco-labels/raw/master/coco-labels-paper.txt',
        sha='0885b1279d53eb26cd181fa7b9eb8e167526fa16',
        filename='coco-labels-paper.txt'),


    #  MobileNetSSDExample : # https://github.com/chuanqi305/MobileNet-SSD/
    Model(
        name='MobileNetSSDExample',
        url='https://github.com/chuanqi305/MobileNet-SSD/raw/master/images/004545.jpg',
        sha='2b0c65f59a9f9071f1e7de452f0c2004e8d55b7b',
        filename='004545.jpg'),
    Model(
        name='MobileNetSSDExample',
        downloader=GDrive('0B3gersZ2cHIxRm5PMWRoTkdHdHc'),
        sha='994d30a8afaa9e754d17d2373b2d62a7dfbaaf7a',
        filename='MobileNetSSD_deploy.caffemodel'),
    Model(
        name='MobileNetSSDExample',
        url='https://github.com/chuanqi305/MobileNet-SSD/raw/f5d072ccc7e3dcddaa830e9805da4bf1000b2836/MobileNetSSD_deploy.prototxt',
        sha='d77c9cf09619470d49b82a9dd18704813a2043cd',
        filename='MobileNetSSD_deploy.prototxt'),


    # OpenPoseExample : # https://github.com/opencv/opencv/blob/master/samples/dnn/openpose.py
    Model(
        name='OpenPoseExample',
        url='https://github.com/CMU-Perceptual-Computing-Lab/openpose/raw/master/examples/media/COCO_val2014_000000000589.jpg',
        sha='e078403d1a09d0ef392c29741cb0c358c3a76322',
        filename='COCO_val2014_000000000589.jpg'),
    Model(
        name='OpenPoseExample',
        url='http://posefs1.perception.cs.cmu.edu/OpenPose/models/pose/mpi/pose_iter_160000.caffemodel',
        sha='a344f4da6b52892e44a0ca8a4c68ee605fc611cf',
        filename='pose_iter_160000.caffemodel'),
    Model(
        name='OpenPoseExample',
        url='https://github.com/opencv/opencv_extra/raw/master/testdata/dnn/openpose_pose_mpi_faster_4_stages.prototxt',
        sha='ed939bc1107ee9eea41190ff00113ea986a9eca5',
        filename='openpose_pose_mpi_faster_4_stages.prototxt'),
    Model(
        name='OpenPoseExample',
        url='http://posefs1.perception.cs.cmu.edu/OpenPose/models/pose/coco/pose_iter_440000.caffemodel',
        sha='ac7e97da66f3ab8169af2e601384c144e23a95c1',
        filename='pose_iter_440000.caffemodel'),
    Model(
        name='OpenPoseExample',
        url='https://github.com/opencv/opencv_extra/raw/master/testdata/dnn/openpose_pose_coco.prototxt',
        sha='98da0ee763e78e3772d4c542d648d2b762945547',
        filename='openpose_pose_coco.prototxt'),
    Model(
        name='OpenPoseExample',
        url='https://github.com/ortegatron/hand_detector_train/raw/master/images/hand_synth_sample2.jpg',
        sha='7ca198504278c8f20662da3339db17e34a08205c',
        filename='hand_synth_sample2.jpg'),
    Model(
        name='OpenPoseExample',
        url='http://posefs1.perception.cs.cmu.edu/OpenPose/models/hand/pose_iter_102000.caffemodel',
        sha='faa68886705093aa7d7de5869cd5ff3d464b29e1',
        filename='pose_iter_102000.caffemodel'),
    Model(
        name='OpenPoseExample',
        url='https://github.com/CMU-Perceptual-Computing-Lab/openpose/raw/master/models/hand/pose_deploy.prototxt',
        sha='2727821d99810812e19a33246c628ee7661fad8f',
        filename='pose_deploy.prototxt'),


    # ResnetSSDFaceDetectionExample :
    Model(
        name='ResnetSSDFaceDetectionExample',
        url='https://github.com/opencv/opencv_3rdparty/raw/dnn_samples_face_detector_20170830/res10_300x300_ssd_iter_140000.caffemodel',
        sha='15aa726b4d46d9f023526d85537db81cbc8dd566',
        filename='res10_300x300_ssd_iter_140000.caffemodel'),
    Model(
        name='ResnetSSDFaceDetectionExample',
        url='https://github.com/opencv/opencv/raw/master/samples/dnn/face_detector/deploy.prototxt',
        sha='7e5cc2cefc23908176a73f58c9b0ea7e5c74db2d',
        filename='deploy.prototxt'),


    #  TensorflowInceptionExample : # https://github.com/opencv/opencv/blob/master/samples/dnn/tf_inception.cpp
    Model(
        name='TensorflowInceptionExample',
        url='https://storage.googleapis.com/download.tensorflow.org/models/inception5h.zip',
        sha='e3b84c7e240ce8025b30d868f5e840b4bba9761d',
        filename='inception5h.zip'),
        
    Model(
        name='TensorflowInceptionExample',
        archive='inception5h.zip',
        member='tensorflow_inception_graph.pb',
        sha='c8a5a000ee8d8dd75886f152a50a9c5b53d726a5',
        filename='tensorflow_inception_graph.pb'),
    Model(
        name='TensorflowInceptionExample',
        archive='inception5h.zip',
        member='imagenet_comp_graph_label_strings.txt',
        sha='5897b4e765c5ce9c66d570102d0317219d033995',
        filename='imagenet_comp_graph_label_strings.txt'),
    Model(
        name='TensorflowInceptionExample',
        filename='inception5h.zip',
        delete='inception5h.zip'),


    # TextOCRExample : # https://github.com/opencv/opencv/blob/master/samples/dnn/text_detection.cpp
    Model(
        name='TextOCRExample',
        url='https://www.dropbox.com/s/r2ingd0l3zt8hxs/frozen_east_text_detection.tar.gz?dl=1',
        sha='3ca8233d6edd748f7ed23246c8ca24cbf696bb94',
        filename='frozen_east_text_detection.tar.gz'),
    Model(
        name='TextOCRExample',
        archive='frozen_east_text_detection.tar.gz',
        member='frozen_east_text_detection.pb',
        sha='fffabf5ac36f37bddf68e34e84b45f5c4247ed06',
        filename='frozen_east_text_detection.pb'),
    Model(
        name='TextOCRExample',
        filename='frozen_east_text_detection.tar.gz',
        delete='frozen_east_text_detection.tar.gz'),


    # YoloObjectDetectionExample :  # https://github.com/opencv/opencv/issues/17148
    Model(
        name='YoloObjectDetectionExample',
        url='https://github.com/pjreddie/darknet/raw/master/data/person.jpg',
        sha='19281b65c5bd43381dfe04e637e78d0cf0b05cbe',
        filename='person.jpg'),
    Model(
        name='YoloObjectDetectionExample',
        url='https://github.com/AlexeyAB/darknet/raw/master/cfg/yolov4-tiny.cfg',
        sha='b161c2b0984b0c3b466c04b0d6cb3e52f06d93dd',
        filename='yolov4-tiny.cfg'),
    Model(
        name='YoloObjectDetectionExample',
        url='https://github.com/AlexeyAB/darknet/releases/download/darknet_yolo_v4_pre/yolov4-tiny.weights',
        sha='451caaab22fb9831aa1a5ee9b5ba74a35ffa5dcb',
        filename='yolov4-tiny.weights'),
    Model(
        name='YoloObjectDetectionExample',
        url='https://github.com/pjreddie/darknet/raw/master/data/coco.names',
        sha='b769c7d769385f7640be484dd9c7537b6fb2f35e',
        filename='coco.names'),
]

# Note: models will be downloaded to current working directory
#       expected working directory is <testdata>/dnn
if __name__ == '__main__':

    selected_model_name = None
    if len(sys.argv) > 1:
        selected_model_name = sys.argv[1]
        print('Model: ' + selected_model_name)

    failedModels = []
    for m in models:
        print(m)
        if selected_model_name is not None and not m.name.startswith(selected_model_name):
            continue
        if not m.get():
            failedModels.append(m.filename)

    if failedModels:
        print("Following models have not been downloaded:")
        for f in failedModels:
            print("* {}".format(f))
        exit(15)

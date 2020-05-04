#!/usr/bin/python3
from __future__ import print_function

import os
import sys
import torch
import argparse
from torch.autograd import Variable

sys.path.append(os.getcwd() + '/tasks/task1')
from yufacedetectnet import YuFaceDetectNet


parser = argparse.ArgumentParser(description='Face and Landmark Detection')

parser.add_argument('-m', '--trained_model', default='tasks/task1/weights/yunet_final.pth',
                    type=str, help='Trained state_dict file path to open')
parser.add_argument('-o', '--output', default='yunet_final.onnx', type=str, help='')


args = parser.parse_args()

def check_keys(model, pretrained_state_dict):
    ckpt_keys = set(pretrained_state_dict.keys())
    model_keys = set(model.state_dict().keys())
    used_pretrained_keys = model_keys & ckpt_keys
    unused_pretrained_keys = ckpt_keys - model_keys
    missing_keys = model_keys - ckpt_keys
    print('Missing keys:{}'.format(len(missing_keys)))
    print('Unused checkpoint keys:{}'.format(len(unused_pretrained_keys)))
    print('Used keys:{}'.format(len(used_pretrained_keys)))
    assert len(used_pretrained_keys) > 0, 'load NONE from pretrained checkpoint'
    return True

def remove_prefix(state_dict, prefix):
    ''' Old style model is stored with all names of parameters sharing common prefix 'module.' '''
    print('remove prefix \'{}\''.format(prefix))
    f = lambda x: x.split(prefix, 1)[-1] if x.startswith(prefix) else x
    return {f(key): value for key, value in state_dict.items()}

def load_model(model, pretrained_path, load_to_cpu):
    print('Loading pretrained model from {}'.format(pretrained_path))
    if load_to_cpu:
        pretrained_dict = torch.load(pretrained_path, map_location=lambda storage, loc: storage)
    else:
        device = torch.cuda.current_device()
        pretrained_dict = torch.load(pretrained_path, map_location=lambda storage, loc: storage.cuda(device))
    if "state_dict" in pretrained_dict.keys():
        pretrained_dict = remove_prefix(pretrained_dict['state_dict'], 'module.')
    else:
        pretrained_dict = remove_prefix(pretrained_dict, 'module.')
    check_keys(model, pretrained_dict)
    model.load_state_dict(pretrained_dict, strict=False)
    return model


if __name__ == '__main__':

    torch.set_grad_enabled(False)

    # net and model
    net = YuFaceDetectNet(phase='test', size=None )    # initialize detector
    net = load_model(net, args.trained_model, True)

    net.eval()

    print('Finished loading model!')

    img = Variable(torch.randn(1, 3, 240, 320), requires_grad=True)

    loc, conf = net(img)  # forward pass
    

    # Export the onnx model
    # default export
    torch.onnx.export(net,               # model being run
                  img,                         # model input (or a tuple for multiple inputs)
                  args.output,   # where to save the model (can be a file or file-like object)
                  export_params=True,        # store the trained parameter weights inside the model file
                  opset_version=10,          # the ONNX version to export the model to
                  do_constant_folding=True,  # whether to execute constant folding for optimization
                  input_names = ['input'],   # the model's input names
                  output_names = ['output'])



    '''
    # export with `dynamic_axes`
    torch.onnx.export(net,               # model being run
                  img,                         # model input (or a tuple for multiple inputs)
                  "yunet_final_dynamic.onnx",   # where to save the model (can be a file or file-like object)
                  export_params=True,        # store the trained parameter weights inside the model file
                  opset_version=10,          # the ONNX version to export the model to
                  do_constant_folding=True,  # whether to execute constant folding for optimization
                  input_names = ['input'],   # the model's input names
                  output_names = ['output'], # the model's output names
                  dynamic_axes={'input' : {0 : 'batch_size'},    # variable lenght axes
                                'output' : {0 : 'batch_size'}})
    '''
    
    print('Finished exporting onnx model to', args.output)


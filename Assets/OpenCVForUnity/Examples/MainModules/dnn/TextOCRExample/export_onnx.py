import torch
import models.crnn as crnn

model = crnn.CRNN(32, 1, 37, 256)
model.load_state_dict(torch.load('./data/crnn.pth'))
dummy_input = torch.randn(1, 1, 32, 100)
torch.onnx.export(model, dummy_input, "crnn.onnx", verbose=True)

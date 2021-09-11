import os
from os.path import join

TRAIN_DIR = 'temp_online_ckpt'
TRAINING_DATA_PATH = 'imgs'
TRAINING_SCENE = '00002'
GPU_ID = '0'
IMG_TYPE = 'png'
OUTPUT_DIR = 'output'

proj_path = 'src\\image-processing\\panoramic-reflection-removal'
model_path = 'C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\models\\panoramic-reflection-removal\\ckpt_decomposition_reflection'

# os.system(f'py -3.6 train_reflection_online.py --train_dir {TRAIN_DIR} --training_data_path {TRAINING_DATA_PATH} --training_scene {TRAINING_SCENE} --GPU_ID {GPU_ID}')
os.system(f'py -3.6 test_reflection.py --test_dataset_name {TRAINING_DATA_PATH}/{TRAINING_SCENE} --img_type {IMG_TYPE} --ckpt_path {model_path}\\model.ckpt-1000 --output_dir {OUTPUT_DIR}')

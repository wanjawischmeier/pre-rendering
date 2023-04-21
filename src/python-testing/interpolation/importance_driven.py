import numpy as np
from PIL import Image

def generate_weighted_samples(image_path, weight_map_path, num_samples):
    input_image = Image.open(image_path)
    input_pixels = input_image.load()

    weight_map = Image.open(weight_map_path).convert("L")
    weight_pixels = weight_map.load()

    patch_size = int(np.sqrt(input_image.size[0] * input_image.size[1] / num_samples))

    output_image = Image.new(input_image.mode, input_image.size)
    output_pixels = output_image.load()

    x_min, y_min = patch_size // 2, patch_size // 2
    x_max, y_max = input_image.size[0] - patch_size // 2, input_image.size[1] - patch_size // 2

    for i in range(num_samples):
        # Choose random point in image
        x = np.random.randint(x_min, x_max)
        y = np.random.randint(y_min, y_max)

        # Sample weight map at point
        weight = weight_pixels[x, y] / 255

        # Sample patch at point
        patch = np.array([input_pixels[i, j] for j in range(y-patch_size//2, y+patch_size//2+1) for i in range(x-patch_size//2, x+patch_size//2+1)])

        # Calculate weighted average of patch
        output_pixels[x, y] = tuple((patch * weight).mean(axis=0).round().astype(np.uint8))

    return output_image


path = 'images\\importance-based\\'

# Load the input texture and weight map
input_texture = Image.open(path + 'coords.png')
weight_map = Image.open(path + 'weights.png').convert('L')

# Generate the output texture
output_texture = generate_weighted_samples(path + 'coords.png', path + 'weights.png', 400000)
# output_texture = generate_texture(input_texture, weight_map, patch_size=5, output_size=(512, 512))

# Save the output texture
output_texture.save(path + 'output_texture.jpg')
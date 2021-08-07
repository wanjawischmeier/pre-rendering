from zipfile import ZIP_DEFLATED, ZipFile
from os import walk, getcwd
from os.path import dirname, join, splitext

def zipdir(path, target):
    ziph = ZipFile(target, 'w', ZIP_DEFLATED)
    for root, dirs, files in walk(path):
        for file in files:
            if splitext(file)[1] == ".py":
                ziph.write(join(root, file), join("source", file))
            else:
                ziph.write(join(root, file), join("source\\icons", file))

build_path = join(dirname(__file__), "source")
target_path = join(getcwd(), "builds\\blender-plugin")

highest_version = 0
for root, dirs, files in walk(target_path):
    for file in files:
        version = float(splitext(file)[0].split('V')[1].replace('_', '.'))
        if version > highest_version: highest_version = version

target_name = f"PreRenderingV{str(round(highest_version + 0.1, 1)).replace('.', '_')}.zip"

zipdir(build_path, join(target_path, target_name))
print(build_path, join(target_path, target_name))
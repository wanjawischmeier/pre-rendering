from distutils.dir_util import copy_tree
from os.path import dirname, join

src = join(dirname(__file__), "source")
dst = "C:\\Users\\wanja\\AppData\\Roaming\\Blender Foundation\\Blender\\2.92\\scripts\\addons\\source"
copy_tree(src, dst)
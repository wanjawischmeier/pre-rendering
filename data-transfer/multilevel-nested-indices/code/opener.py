import tkinter # for pydroid
from sys import argv
from os.path import isfile, splitext
from subprocess import call
from messaging import *
from time import sleep

arguments = argv

if len(arguments) < 2:
    showerror(Error.ERR_NO_REFERENCE, str(arguments[1:]))
elif not isfile(arguments[1]):
    showerror(Error.ERR_NO_REFERENCE, str(arguments[1:]))
elif splitext(arguments[1])[1] == '.ststext':
    showerror(Error.ERR_INVALID_FILE, splitext(arguments[1])[1])
else:
    path = arguments[1]

    showinfo('File opened: ' + path)
    # img = 'D:\\Pictures\\MeineFotos\\bei_wolf\\Carlow\\IMG_0529.JPG'
    # call(['start', path], shell=True)
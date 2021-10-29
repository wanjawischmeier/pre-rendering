SET source=%1
IF %2 EQU x64 SET source=%source%%2\
SET source=%source:image-decoder\image-decoder=image-decoder%%3

robocopy %source% %4 /E
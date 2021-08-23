import tkinter
import string
import pygame
from pygame.locals import *
import time

step = 1
path = 0
run = True

path = "Img_000"+str(step)+".jpg"
step = step +1
background1 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background2 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background3 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background4 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background5 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background6 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background7 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background8 = pygame.image.load(path)
path = "Img_000"+str(step)+".jpg"
step = step +1
background9 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background10 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background11 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background12 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background13 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background14 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background15 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background16 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background17 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background18 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background19 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background20 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background21 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background22 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background23 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background24 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background25 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background26 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background27 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background28 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background29 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background30 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background31 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background32 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background33 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background34 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background35 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background36 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background37 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background38 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background39 = pygame.image.load(path)
path = "Img_00"+str(step)+".jpg"
step = step +1
background40 = pygame.image.load(path)
path = "Img_0040.jpg"

    
def handleButton(event):
    global screen
    global background
    global b
    global step
    global path

    for i in range(2):
        screen.blit(background1,(0,0))
        background = pygame.image.load(path)
        screen.blit(background1,(0,0))
        pygame.display.update()
        
        screen.blit(background2,(0,0))
        background = pygame.image.load(path)
        screen.blit(background2,(0,0))
        pygame.display.update()

        screen.blit(background3,(0,0))
        background = pygame.image.load(path)
        screen.blit(background3,(0,0))
        pygame.display.update()

        screen.blit(background4,(0,0))
        background = pygame.image.load(path)
        screen.blit(background4,(0,0))
        pygame.display.update()

        screen.blit(background5,(0,0))
        background = pygame.image.load(path)
        screen.blit(background5,(0,0))
        pygame.display.update()

        screen.blit(background6,(0,0))
        background = pygame.image.load(path)
        screen.blit(background6,(0,0))
        pygame.display.update()

        screen.blit(background7,(0,0))
        background = pygame.image.load(path)
        screen.blit(background7,(0,0))
        pygame.display.update()

        screen.blit(background8,(0,0))
        background = pygame.image.load(path)
        screen.blit(background8,(0,0))
        pygame.display.update()

        screen.blit(background9,(0,0))
        background = pygame.image.load(path)
        screen.blit(background9,(0,0))
        pygame.display.update()

        screen.blit(background10,(0,0))
        background = pygame.image.load(path)
        screen.blit(background10,(0,0))
        pygame.display.update()

        screen.blit(background11,(0,0))
        background = pygame.image.load(path)
        screen.blit(background11,(0,0))
        pygame.display.update()

        screen.blit(background12,(0,0))
        background = pygame.image.load(path)
        screen.blit(background12,(0,0))
        pygame.display.update()

        screen.blit(background13,(0,0))
        background = pygame.image.load(path)
        screen.blit(background13,(0,0))
        pygame.display.update()

        screen.blit(background14,(0,0))
        background = pygame.image.load(path)
        screen.blit(background14,(0,0))
        pygame.display.update()

        screen.blit(background15,(0,0))
        background = pygame.image.load(path)
        screen.blit(background15,(0,0))
        pygame.display.update()

        screen.blit(background16,(0,0))
        background = pygame.image.load(path)
        screen.blit(background16,(0,0))
        pygame.display.update()

        screen.blit(background17,(0,0))
        background = pygame.image.load(path)
        screen.blit(background17,(0,0))
        pygame.display.update()

        screen.blit(background18,(0,0))
        background = pygame.image.load(path)
        screen.blit(background18,(0,0))
        pygame.display.update()

        screen.blit(background19,(0,0))
        background = pygame.image.load(path)
        screen.blit(background19,(0,0))
        pygame.display.update()

        screen.blit(background20,(0,0))
        background = pygame.image.load(path)
        screen.blit(background20,(0,0))
        pygame.display.update()

        screen.blit(background21,(0,0))
        background = pygame.image.load(path)
        screen.blit(background21,(0,0))
        pygame.display.update()

        screen.blit(background22,(0,0))
        background = pygame.image.load(path)
        screen.blit(background22,(0,0))
        pygame.display.update()

        screen.blit(background23,(0,0))
        background = pygame.image.load(path)
        screen.blit(background23,(0,0))
        pygame.display.update()

        screen.blit(background24,(0,0))
        background = pygame.image.load(path)
        screen.blit(background24,(0,0))
        pygame.display.update()

        screen.blit(background25,(0,0))
        background = pygame.image.load(path)
        screen.blit(background25,(0,0))
        pygame.display.update()

        screen.blit(background26,(0,0))
        background = pygame.image.load(path)
        screen.blit(background26,(0,0))
        pygame.display.update()

        screen.blit(background27,(0,0))
        background = pygame.image.load(path)
        screen.blit(background27,(0,0))
        pygame.display.update()

        screen.blit(background28,(0,0))
        background = pygame.image.load(path)
        screen.blit(background28,(0,0))
        pygame.display.update()

        screen.blit(background29,(0,0))
        background = pygame.image.load(path)
        screen.blit(background29,(0,0))
        pygame.display.update()

        screen.blit(background30,(0,0))
        background = pygame.image.load(path)
        screen.blit(background30,(0,0))
        pygame.display.update()

        screen.blit(background31,(0,0))
        background = pygame.image.load(path)
        screen.blit(background31,(0,0))
        pygame.display.update()

        screen.blit(background32,(0,0))
        background = pygame.image.load(path)
        screen.blit(background32,(0,0))
        pygame.display.update()

        screen.blit(background33,(0,0))
        background = pygame.image.load(path)
        screen.blit(background33,(0,0))
        pygame.display.update()

        screen.blit(background34,(0,0))
        background = pygame.image.load(path)
        screen.blit(background34,(0,0))
        pygame.display.update()

        screen.blit(background35,(0,0))
        background = pygame.image.load(path)
        screen.blit(background35,(0,0))
        pygame.display.update()

        screen.blit(background36,(0,0))
        background = pygame.image.load(path)
        screen.blit(background36,(0,0))
        pygame.display.update()

        screen.blit(background37,(0,0))
        background = pygame.image.load(path)
        screen.blit(background37,(0,0))
        pygame.display.update()

        screen.blit(background38,(0,0))
        background = pygame.image.load(path)
        screen.blit(background38,(0,0))
        pygame.display.update()

        screen.blit(background39,(0,0))
        background = pygame.image.load(path)
        screen.blit(background39,(0,0))
        pygame.display.update()

        screen.blit(background40,(0,0))
        background = pygame.image.load(path)
        screen.blit(background40,(0,0))
        pygame.display.update()

        

pygame.init()

screen = pygame.display.set_mode((1280,720))
pygame.display.set_caption("Try4")

background = pygame.image.load("Img_0001.jpg")

b = tkinter.Button(text = "Animation starten")
b.pack()
b.bind ("<Button-1>", handleButton)

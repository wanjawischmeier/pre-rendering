from tkinter import Tk, BOTH
from tkinter.ttk import Progressbar as p_bar
from time import sleep

class Progressbar:
    def __init__(self, title: str):
        self.max = 100
        self.min = 0
        self.__raw_value__ = 0
        self.__window__ = Tk()
        self.__window__.title(title)
        self.__bar__ = p_bar(
            self.__window__, 
            mode = 'determinate'
        )
        self.__bar__.pack(fill = BOTH)

        x = (self.__window__.winfo_screenwidth() // 2) - 200
        y = (self.__window__.winfo_screenheight() // 2) -10

        self.__window__.geometry(f'400x20+{x}+{y}')

    def __del__(self):
        try: self.__window__.destroy()
        except: pass

    def close(self): self.__del__()

    def step(self):
        self.__raw_value__ += 1
        self.value = None


    @property
    def value(self):
        """Set the current value of the progressbar. Automaticaly updates the gui of the progressbar.

        Args:
            value (int, optional): The value for the progressbar to be set to. When left empty, the progressbar will just increase by one.
        """
        return self.__raw_value__

    @value.setter
    def value(self, value: int) -> None:
        if value != None: self.__raw_value__ = value
        self.__bar__['value'] = self.__raw_value__ / self.max *100
        self.__bar__.update()

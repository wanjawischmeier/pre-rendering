from tkinter import Tk
from tkinter.messagebox import showwarning as tk_showwarning, showerror as tk_showerror, showinfo as tk_showinfo
from json import loads
from os import getcwd

root = Tk()
root.withdraw()

class Error:
    TITLE               = 'TITLE'
    EXPLANATION         = 'EXPL'
    REASON              = 'REASON'
    FIX                 = 'FIX'
    ERR_NO_REFERENCE    = 'ERR_NO_REFERENCE'
    ERR_INVALID_FILE    = 'ERR_INVALID_FILE'

    error_codes = {}

    
    def loadErrorData() -> dict:
        if Error.error_codes == {}:
            with open(getcwd() + '/error_codes.json', 'r') as file: # use '\\' instead of '/'
                raw = file.read()
                Error.error_codes = loads(raw)

        return Error.error_codes

def showinfo(title: str, message: str) -> None:
    tk_showinfo(
        title, 
        message
    )

def showerror(error_type: str, additional_data: str, exit_after_error: bool = True) -> list:
    error_codes = Error.loadErrorData()

    error = error_codes['errors'][error_type]

    title = error[Error.TITLE]
    title = title.replace('%e', error_type)

    expl = error[Error.EXPLANATION]
    expl = expl.replace('%s', additional_data)

    reason = error[Error.REASON]
    fix = error[Error.FIX]

    tk_showerror(
        title, 
        f'{expl}\n\n{reason}\n\n{fix}\n\nSorry for the inconvenience!'
    )
    
    #if exit_after_error: exit()

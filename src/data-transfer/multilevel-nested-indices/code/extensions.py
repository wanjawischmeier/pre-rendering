import sys, os, traceback, types




def __isUserAdmin():
    if os.name == 'nt':
        import ctypes
        try:
            return ctypes.windll.shell32.IsUserAnAdmin()
        except:
            traceback.print_exc()
            print("Admin check failed, assuming not an admin.")
            return False
    elif os.name == 'posix':
        return os.getuid() == 0
    else:
        raise(RuntimeError, "Unsupported operating system for this module: %s" % (os.name,))




def __runAsAdmin(cmdLine=None, wait=True):

    if os.name != 'nt':
        raise(RuntimeError, "This function is only implemented on Windows.")

    import win32api, win32con, win32event, win32process
    from win32com.shell.shell import ShellExecuteEx
    from win32com.shell import shellcon

    python_exe = sys.executable

    if cmdLine is None:
        cmdLine = [python_exe] + sys.argv
    elif type(cmdLine) not in (types.TupleType,types.ListType):
        raise(ValueError, "cmdLine is not a sequence.")
    cmd = '"%s"' % (cmdLine[0],)
    params = " ".join(['"%s"' % (x,) for x in cmdLine[1:]])
    cmdDir = ''
    showCmd = win32con.SW_SHOWNORMAL
    lpVerb = 'runas'

    try:
        procInfo = ShellExecuteEx(nShow=showCmd,
                                fMask=shellcon.SEE_MASK_NOCLOSEPROCESS,
                                lpVerb=lpVerb,
                                lpFile=cmd,
                                lpParameters=params)
    except:
        return False

    if wait:
        procHandle = procInfo['hProcess']    
        obj = win32event.WaitForSingleObject(procHandle, win32event.INFINITE)
        rc = win32process.GetExitCodeProcess(procHandle)
    else:
        rc = None

    return True


# C:\Users\wanja\Downloads\SteamSetup.exe
# assoc .ststext=Second Test Extension
# ftype Second Test Extension="Path"

def createExtension(extension, extension_label, executable, pathAsArgument=True):
    if not __isUserAdmin(): __runAsAdmin()
    os.system(f'assoc {extension}={extension_label}')
    if pathAsArgument: args = "%1"
    else: args = ""
    os.system(f'ftype {extension_label}="{executable}" "{args}"')

def getExePath(): 
    print(sys.executable)
    return sys.executable


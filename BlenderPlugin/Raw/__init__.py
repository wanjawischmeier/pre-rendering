# Plugin info
bl_info = {
    "name":         "PreRendering",
    "author":       "Wanja Wischmeier",
    "version":      (0, 1),
    "blender":      (2, 80, 0),
    "location":     "Render > PreRender",
    "description":  "Generates a map file from the current scene",
    "warning":      "This is still very experimental, always make sure to save your project first.",
    "doc_url":      "https://sites.google.com/view/prerendering/",
    "category":     "Render"
}


modulesNames = ["setup", "prerender", "methods", "data"]
 
import sys
import importlib
 
modulesFullNames = {}
for currentModuleName in modulesNames:
    modulesFullNames[currentModuleName] = ("{}.{}".format(__name__, currentModuleName))
 
for currentModuleFullName in modulesFullNames.values():
    if currentModuleFullName in sys.modules:
        importlib.reload(sys.modules[currentModuleFullName])
    else:
        globals()[currentModuleFullName] = importlib.import_module(currentModuleFullName)
        setattr(globals()[currentModuleFullName], "modulesNames", modulesFullNames)
 
def register():
    for currentModuleName in modulesFullNames.values():
        if currentModuleName in sys.modules:
            if hasattr(sys.modules[currentModuleName], "register"):
                sys.modules[currentModuleName].register()
 
def unregister():
    for currentModuleName in modulesFullNames.values():
        if currentModuleName in sys.modules:
            if hasattr(sys.modules[currentModuleName], "unregister"):
                sys.modules[currentModuleName].unregister()
 

if __name__ == "__main__":
    register()

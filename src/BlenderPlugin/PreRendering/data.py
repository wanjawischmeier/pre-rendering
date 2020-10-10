cache = {
    "setup":    False,
    "camera":   None,
    "width":    0
}

qualitys = [
    ("low",     "Low quality (720p)",       "Low filesize and fast reading, but may look bad"),
    ("medium",  "Medium quality (1080p)",   "Propably the best option for large maps"),
    ("high",    "High quality (2k)",        "Will result in a large map file, but capture the most detail"),
    ("ultra",   "Very high quality (4k)",   "Very large filesize, only for very good PC's")
]

resolutions = {
    "low":      (1280, 720),
    "medium":   (1920, 1080),
    "high":     (3840, 2160),
    "ultra":    (7680, 4320)
}
resolution_default = resolutions.get("medium")
def setRenderSettings(scene: object, camera: object, resolution: tuple, frame_end: int) -> None:
    camera.data.type = 'PANO'
    camera.data.cycles.panorama_type = 'EQUIRECTANGULAR'
    scene.render.engine = 'CYCLES'
    scene.render.filepath = "C://tmp/map"
    scene.render.image_settings.file_format = 'FFMPEG'
    scene.render.ffmpeg.format = 'MPEG4'
    scene.render.fps = 30
    scene.render.resolution_x = resolution[0]
    scene.render.resolution_y = resolution[1]
    scene.frame_end = frame_end
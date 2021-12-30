d=bpy.data.objects['Domain']
b=bpy.data.objects['ChunkBounds']
p=bpy.data.objects['ChunkPosition']
t=bpy.data.objects['Text']

cW=d['chunkWidth']
cC=d['chunkColumns']
cR=d['chunkRows']
total=(cW**2)*cC*cR

def u(s):
    t.data.body=f"""
frame={s.frame_current}
pos={p.location[:2]}
cPos={[round(i/cW) for i in b.location[:2]]}
cIndex={round(b.location[0]/cW)+round(b.location[1]/cW)*cC}
mIndex={floor(s.frame_current/total)}
    """

bpy.app.handlers.frame_change_post.clear()
bpy.app.handlers.frame_change_post.append(u)
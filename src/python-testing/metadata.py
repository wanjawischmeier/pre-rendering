from tinytag import TinyTag

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\cycles\\chunk_debugger.mp4"
tags = TinyTag.get(path)

TinyTag._set_field(tags, "artist", "loltest")
tags.artist
class Configuration:
    nclip: float
    fclip: float
    blockWidth: float
    blockHeight: float
    chunkWidth: int
    chunkColumns: int
    chunkRows: int
    channelBlocks: int

c = Configuration()
c.nclip = 10
c.fclip = 100
c.blockWidth = 1.2
c.blockHeight = 1.4
c.chunkWidth = 4
c.chunkColumns = 5
c.chunkRows = 5
c.channelBlocks = 3

print(c.__dict__)
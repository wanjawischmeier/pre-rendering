frame = 0           # a
chunkWidth = 4
chunkColumns = 5
chunkRows = 5
domainLocation = 0
domainScale = 0

x = ((frame%(chunkColumns*chunkRows*(chunkWidth**2)))-((frame%(chunkColumns*chunkRows*(chunkWidth**2)))%(chunkWidth**2)))/(chunkWidth**2)%chunkColumns*chunkWidth*(domainScale/chunkColumns/chunkWidth)+(-domainScale/2+domainLocation)+((frame%(chunkColumns*chunkRows*(chunkWidth**2)))%(chunkWidth**2))%chunkWidth*(domainScale/chunkColumns/chunkWidth)
y = ((frame%(chunkColumns*chunkRows*(chunkWidth**2)))-(frame%(chunkColumns*chunkRows*(chunkWidth**2)))%((chunkWidth**2)*chunkColumns))/((chunkWidth**2)*chunkColumns)*chunkWidth*(domainScale/chunkRows/chunkWidth)+(-domainScale/2+domainLocation)+(((frame%(chunkColumns*chunkRows*(chunkWidth**2)))%(chunkWidth**2))-((frame%(chunkColumns*chunkRows*(chunkWidth**2)))%(chunkWidth**2))%chunkWidth)/chunkWidth*(domainScale/chunkRows/chunkWidth)    
function setup() {
  cnv = createCanvas(windowWidth, windowHeight-25);
  cnv.mousePressed(loop);
  cnv.mouseReleased(noLoop);
  cnv.mouseWheel(newBlock);
  
  noLoop();
  
  bg_col = color(190, 199, 199);
  blue_col = color(18, 19, 23);
  black_col = color(6, 17, 28);
  
  blockSize = createSlider(10, 100, 40);
  chunkWidth = createSlider(1, 20, 4);
  createButton('Reload').mousePressed(reload);
  
  blockSize.mousePressed(reload);
  blockSize.mouseMoved(conditionalReload);
  
  chunkWidth.mousePressed(reload);
  chunkWidth.mouseMoved(conditionalReload);
  
  reload();
}

function draw() {
  newBlock();
}

function conditionalReload() {
  if (mouseIsPressed) reload();
}

function reload() {
  noLoop();
  bg_col.setAlpha(255);
  background(bg_col);
  bg_col.setAlpha(1);
  
  calculateChunkConstants();

  stroke(black_col);
  noFill();
  rect(offsetX, offsetY, chunkColumnsP, chunkRowsP, blockSize.value()/10);
  
  square(offsetX, offsetY, blockSize.value() * chunkWidth.value(), blockSize.value()/10);
  
  noStroke();
  fill(blue_col);
  square(offsetX, offsetY, blockSize.value(), blockSize.value()/10);
}

function newBlock() {
  background(bg_col);
  
  pos = getWorldPosition(frames, chunkWidth.value());
  
  square(
    pos[0]*blockSize.value()+offsetX,
    pos[1]*blockSize.value()%chunkRowsP+offsetY,
    blockSize.value(),
    blockSize.value()/10
  );
  
  frames++;
}

function calculateChunkConstants() {
  // How many positions fit in each chunk
  chunkSize = (chunkWidth.value()**2);
  
  // How many columns and rows of columns fit on the screen
  chunkColumns = floor(width / blockSize.value() / chunkWidth.value());
  chunkRows = floor(height / blockSize.value() / chunkWidth.value());
  
  // How many pixels these columns and rows are contained in
  chunkColumnsP = chunkColumns * blockSize.value() * chunkWidth.value();
  chunkRowsP = chunkRows * blockSize.value() * chunkWidth.value();
  
  // Center the display rect
  offsetX = (width - chunkColumnsP) / 2;
  offsetY = (height - chunkRowsP) / 2;
  
  // How many positions fit in each row
  rowSize = chunkSize * chunkColumns;
  frames = 0;
}

function getWorldPosition(index, chunkWidthV) {
  // Index of the position inside the chunk
  // (0 <= chunkIndex <= chunkSize)
  chunkIndex = index%chunkSize;
  
  // Index of the current row
  // (0 <= rowIndex <= rowSize)
  rowIndex = index%rowSize;
  
  // Position of the chunk
  chunkIndexX = (index-chunkIndex)/chunkSize%chunkColumns*chunkWidthV;
  chunkIndexY = (index-rowIndex)/rowSize*chunkWidthV;
  
  // Position relative to chunk
  x = chunkIndex%chunkWidthV;
  y = (chunkIndex-x)/chunkWidthV;
  
  // Add chunk offset
  x += chunkIndexX;
  y += chunkIndexY;
    
  return [x, y];
}

function getChunkBasedIndex(x, y) {
  
}
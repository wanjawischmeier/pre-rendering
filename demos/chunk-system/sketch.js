let blockWidth = 40;
let chunkWidth = 4;

function setup() {
  cnv = createCanvas(windowWidth, windowHeight-28);
  cnv.mousePressed(loopIfInBounds);
  cnv.mouseReleased(noLoopIfInBounds);
  cnv.mouseWheel(newBlock);
    
  bg_col = color(190, 199, 199);
  blue_col = color(18, 19, 23);
  black_col = color(6, 17, 28);
  
  strokeWeight(4);
  textAlign(CENTER);
  noLoop();
  
  bg_col.setAlpha(255);
  background(bg_col);
  bg_col.setAlpha(1);
  
  calculateChunkConstants();
  drawHeader();

  stroke(black_col);
  noFill();
  rect(offsetX, offsetY, chunkColumnsP, chunkRowsP, blockWidth/10);
  
  square(offsetX, offsetY, blockWidth * chunkWidth, blockWidth/10);
  
  noStroke();
  fill(blue_col);
  square(offsetX, offsetY, blockWidth, blockWidth/10);
  
  i0 = 12
  i1 = 43
}

function draw() {
  drawHeader();
  
  if (mouseIsPressed) {
    if (inBounds(mouseX, mouseY)) {
      cPos = getCoordinateSystemPosition(mouseX, mouseY);
      frames = getChunkIndex(cPos[0], cPos[1]);
      
      newBlock();
    }
  } else {
   newBlock();
  }
}

function drawHeader() {
  textSize(32);
  text('Chunk System Testing', width/2, offsetY/2);
  textSize(16);
  text(`blockWidth = ${blockWidth}\tchunkWidth = ${chunkWidth}`, width/2, offsetY/1.2);
}

function loopIfInBounds() {
  if (inBounds(mouseX, mouseY)) {
    loop();
  }
}

function noLoopIfInBounds() {
  if (inBounds(mouseX, mouseY)) {
    noLoop();
  }
}

function keyPressed() {
  if (isLooping()) {
    noLoop();
  } else {
    loop(); 
  }
}

function newBlock() {
  background(bg_col);
  
  pos = getWorldPosition(frames);
  
  square(
    pos[0]*blockWidth+offsetX,
    pos[1]*blockWidth%chunkRowsP+offsetY,
    blockWidth,
    blockWidth/10
  );
  
  frames++;
}

function calculateChunkConstants() {
  // How many positions fit in each chunk
  chunkSize = (chunkWidth**2);
  
  // How many columns and rows of columns fit on the screen
  chunkColumns = floor(width / blockWidth / chunkWidth);
  chunkRows = floor(height / blockWidth / chunkWidth);
  
  // How many pixels these columns and rows are contained in
  chunkColumnsP = chunkColumns * blockWidth * chunkWidth;
  chunkRowsP = chunkRows * blockWidth * chunkWidth;
  
  // Center the display rect
  offsetX = (width - chunkColumnsP) / 2;
  offsetY = (height - chunkRowsP) / 4 * 3;
  
  // How many positions fit in each row
  rowSize = chunkSize * chunkColumns;
  frames = 0;
}

function inBounds(x, y) {
  return x > offsetX && y > offsetY && x < width-offsetX && y < height-offsetY;
}

function getCoordinateSystemPosition(x, y) {
  return [
    floor((x-offsetX)/blockWidth),
    floor((y-offsetY)/blockWidth)
  ];
}

function getIndex(x, y, w) {
  return x + y * w;
}

function getChunkIndex(x, y) {
  // Coordinates of the chunk
  chunkCoordinateX = floor(x/chunkWidth);
  chunkCoordinateY = floor(y/chunkWidth);
  
  // Draw chunk outline
  noFill();
  stroke(blue_col);
  square(
    chunkCoordinateX*chunkWidth*blockWidth + offsetX,
    chunkCoordinateY*chunkWidth*blockWidth + offsetY,
    chunkWidth*blockWidth
  );
  noStroke();
  fill(blue_col);
  
  // Coordinates relative to the chunk
  chunkPositionX = x%chunkWidth;
  chunkPositionY = y%chunkWidth;
  
  // Index of the chunk
  chunkIndex = getIndex(chunkCoordinateX, chunkCoordinateY, chunkColumns);

  // Total chunk index
  chunkIndex *= chunkWidth**2;

  // Index inside the chunk
  positionIndex = getIndex(chunkPositionX, chunkPositionY, chunkWidth);

  return chunkIndex + positionIndex;
}

function getWorldPosition(index) {
  // Index of the position inside the chunk
  // (0 <= chunkIndex <= chunkSize)
  chunkIndex = index%chunkSize;
  
  // Index of the current row
  // (0 <= rowIndex <= rowSize)
  rowIndex = index%rowSize;
  
  // Position of the chunk
  chunkIndexX = (index-chunkIndex)/chunkSize%chunkColumns*chunkWidth;
  chunkIndexY = (index-rowIndex)/rowSize*chunkWidth;
  
  // Position relative to chunk
  x = chunkIndex%chunkWidth;
  y = (chunkIndex-x)/chunkWidth;
  
  // Add chunk offset
  x += chunkIndexX;
  y += chunkIndexY;
    
  return [x, y];
}
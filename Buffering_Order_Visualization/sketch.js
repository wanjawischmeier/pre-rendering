let colSize;
let rowSize;
let cCol;
let cRow;
let colWidth = 20;
let rowHeight = 20;
let bufferSize;
let colSteps;
let slider;
let frame = 0;

function setup() {
  createCanvas(windowWidth -4, windowHeight -40);
  // frameRate(10);
  // fullscreen();
  
  init();
}

function init() {
  slider = createSlider(2, 45, colWidth);
  slider.size(width, 10);
  slider.mouseReleased(valueChanged);
  
  colSize = floor(width / colWidth);
  rowSize = floor(height / rowHeight);
  
  cCol = floor((width /2) / colWidth);
  cRow = floor((height /2) / rowHeight);
  
  background(250);
  if (colWidth < 5) noStroke();
  else stroke(1);
  
  for (col = 0; col < colSize; col++) {
    for (row = 0; row < rowSize; row++) {
      if (col == cCol && row == cRow) fill(100, 200, 250);
      else noFill();
      
      rect(col * colWidth, row * rowHeight, colWidth, rowHeight);
    }
  }
  
  if (colSize < rowSize) bufferSize = floor(colSize /2);
  else bufferSize = floor(rowSize /2);
  colSteps = floor(255 / bufferSize);
  
  loop();
}

function draw() {
  if (frame < bufferSize) {
    let w = frame;
    
    fill(100, 200, (bufferSize - w) * colSteps, 140);
  
    for (i = -w; i < w; i++) {
      rect((cCol + i) * colWidth, (cRow + w) * rowHeight, colWidth, rowHeight);
      rect((cCol - i) * colWidth, (cRow - w) * rowHeight, colWidth, rowHeight);
      rect((cCol + w) * colWidth, (cRow - i) * rowHeight, colWidth, rowHeight);
      rect((cCol - w) * colWidth, (cRow + i) * rowHeight, colWidth, rowHeight);
    }
  }
  else {
    noLoop();
  }
  
  frame++;
}

function valueChanged() {
  frame = 0;
  
  colWidth = slider.value();
  rowHeight = slider.value();
  
  slider.remove();
    
  init();
}
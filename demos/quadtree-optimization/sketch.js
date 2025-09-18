let pointSize = 10;
let points = 20;
let gridSize = 4;
let gridWidth = 4;
let gridCol = 200;
let cellSize;
let tree = {};
let treeSize = 0;

function setup() {
  let windowSize = windowWidth < windowHeight ? windowWidth : windowHeight;
  cellSize = windowSize / gridSize;
  createCanvas(windowSize, windowSize);
  noLoop();
  textAlign(CENTER, CENTER);
  textSize(25);
  
  stroke(gridCol);
  strokeWeight(gridWidth);
  noFill();
  
  for (let i = 0; i < gridSize; i++) {
    for (let j = 0; j < gridSize; j++) {
      square(i * cellSize, j * cellSize, cellSize);
    }
  }
  
  strokeWeight(pointSize);
  
  for (let i = 0; i < points; i++) {
    let p = new Point(round(random(width)), round(random(height)));
    noStroke();
    iterate(p, cellSize);
  }
  
  showCounter();
}

function mousePressed() {
  let p = new Point(mouseX, mouseY);
  noStroke();
  iterate(p, cellSize);
  showCounter();
}

function showCounter() {
  fill(255);
  noStroke();
  rect(gridWidth / 2, gridWidth / 2, cellSize - gridWidth, 50);
  fill(0);
  text(treeSize, gridWidth / 2, gridWidth / 2, cellSize - gridWidth, 50);
}

function alphaSquare(x, y, w, col) {
  fill(255);
  square(x, y, w);
  fill(col);
  square(x, y, w);
}

class Point {
  constructor(x, y) {
    this.x = x;
    this.y = y;
    this.col = color(random(255), random(255), random(255));
    treeSize += 1;
    
    stroke(this.col);
    point(this.x, this.y);
  }
}

function getMap(x, y) {
  if (tree[x]) {
    let p = tree[x][y];
    if (p) {
      return p;
    }
  }
}

function setMap(x, y, p) {
  if (!tree[x]) tree[x] = {};
  tree[x][y] = p;
}

function iterate(p, size) {
  p.col.setAlpha(50);
  
  let bx = p.x - p.x % size;
  let by = p.y - p.y % size;
  let pp = getMap(bx, by);
  if (pp) {
    size /= 2;
    let cx = p.x - p.x % size;
    let cy = p.y - p.y % size;
    alphaSquare(cx, cy, size, p.col);
    
    let dx = pp.x - pp.x % size;
    let dy = pp.y - pp.y % size;
    alphaSquare(dx, dy, size, p.col);
    
    stroke(p.col);
    point(p.x, p.y);
    stroke(pp.col);
    point(pp.x, pp.y);
    noStroke();
    
    iterate(p, size);
  }
  
  setMap(bx, by, p);
  fill(p.col);
  point(bx, by);
  square(bx, by, size);
}
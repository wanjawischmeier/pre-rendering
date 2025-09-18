let n = 16*16;
let points = [];
let tiles = [];
let tileSize = 50;
let tileCols, tileRows;

function pointInTriangle(px, py, a, b, c) {
  let v0x = c.x - a.x;
  let v0y = c.y - a.y;
  let v1x = b.x - a.x;
  let v1y = b.y - a.y;
  let v2x = px - a.x;
  let v2y = py - a.y;

  let dot00 = v0x * v0x + v0y * v0y;
  let dot01 = v0x * v1x + v0y * v1y;
  let dot02 = v0x * v2x + v0y * v2y;
  let dot11 = v1x * v1x + v1y * v1y;
  let dot12 = v1x * v2x + v1y * v2y;

  let denom = dot00 * dot11 - dot01 * dot01;
  if (denom === 0) return false;

  let invDenom = 1 / denom;
  let u = (dot11 * dot02 - dot01 * dot12) * invDenom;
  let v = (dot00 * dot12 - dot01 * dot02) * invDenom;

  return (u >= 0) && (v >= 0) && (u + v <= 1);
}


function findTriangleUnderMouse(neighborIndices) {
  let gridCols = ceil(sqrt(points.length)); // original grid width
  let gridRows = gridCols; // assuming square-ish grid

  for (let i of neighborIndices) {
    let xi = i % gridCols;
    let yi = Math.floor(i / gridCols);

    // Wrapped neighbor positions
    let rightXi = (xi + 1) % gridCols;
    let belowYi = (yi + 1) % gridRows;

    let rightIdx = yi * gridCols + rightXi;
    let belowIdx = belowYi * gridCols + xi;
    let bottomRightIdx = belowYi * gridCols + rightXi;

    // Triangle A: topLeft → bottomLeft → topRight
    let topLeft = points[i];
    let topRight = points[rightIdx];
    let bottomLeft = points[belowIdx];
    let bottomRight = points[bottomRightIdx];
    
    // Compute AABB of the full cell
    let minX = Math.min(topLeft.x, topRight.x, bottomLeft.x, bottomRight.x);
    let maxX = Math.max(topLeft.x, topRight.x, bottomLeft.x, bottomRight.x);
    let minY = Math.min(topLeft.y, topRight.y, bottomLeft.y, bottomRight.y);
    let maxY = Math.max(topLeft.y, topRight.y, bottomLeft.y, bottomRight.y);

    // Fast AABB check
    if (mouseX < minX || mouseX > maxX || mouseY < minY || mouseY > maxY) {
      continue;
    }
    
    if (pointInTriangle(mouseX, mouseY, topLeft, bottomLeft, topRight)) {
      return [topLeft, bottomLeft, topRight];
    }

    // Triangle B: bottomLeft → topRight → bottomRight
    if (pointInTriangle(mouseX, mouseY, bottomLeft, topRight, bottomRight)) {
      return [bottomLeft, topRight, bottomRight];
    }
  }

  return null;
}


function setup() {
  createCanvas(500, 500);
  pixelDensity(1);

  // Initialize points on a grid with jitter
  let gridSize = ceil(sqrt(n));
  let jitterX = width / gridSize / 2;
  for (let i = 0; i < n; i++) {
    let xi = i % gridSize;
    let yi = floor(i / gridSize);
    let x = map(xi, 0, gridSize - 1, 0, width) + random(-jitterX, jitterX);
    let y = map(yi, 0, gridSize - 1, 0, height) + random(-3, 3);
    let value = round(random(0, 1)) * 255;
    points.push({ x, y, value });
  }

  // Initialize tile grid
  tileCols = ceil(width / tileSize);
  tileRows = ceil(height / tileSize);

  for (let i = 0; i < tileCols; i++) {
    tiles[i] = [];
    for (let j = 0; j < tileRows; j++) {
      tiles[i][j] = {
        x: i * tileSize,
        y: j * tileSize,
        points: []
      };
    }
  }

  // Assign points to tiles
  for (let i = 0; i < points.length; i++) {
    let pt = points[i];
    let tx = floor(pt.x / tileSize);
    let ty = floor(pt.y / tileSize);

    tx = constrain(tx, 0, tileCols - 1);
    ty = constrain(ty, 0, tileRows - 1);
    tiles[tx][ty].points.push(i);
  }
}

function draw() {
  background(150);
  strokeWeight(6);

  // Draw all points in white
  for (let pt of points) {
    stroke(pt.value);
    point(pt.x, pt.y);
  }

  // Hover tile highlight and neighbor logic
  let hx = floor(mouseX / tileSize);
  let hy = floor(mouseY / tileSize);

  if (hx >= 0 && hy >= 0) {
    noFill();
    strokeWeight(4);

    // Collect and draw neighbor tiles (wrap around)
    stroke(200, 100, 0);
    let neighborIndices = [];
    for (let dx = -1; dx <= 1; dx++) {
      for (let dy = -1; dy <= 1; dy++) {
        let nx = (hx + dx + tileCols) % tileCols;
        let ny = (hy + dy + tileRows) % tileRows;
        neighborIndices.push(...tiles[nx][ny].points);
        rect(nx * tileSize, ny * tileSize, tileSize, tileSize);
      }
    }
    
    // Draw hover tile border
    stroke(0, 255, 0);
    rect(hx * tileSize, hy * tileSize, tileSize, tileSize);

    // Draw neighbor points in red
    for (let idx of neighborIndices) {
      let pt = points[idx];
      stroke(255, 0, 0);
      point(pt.x, pt.y);
    }
    
    let tri = findTriangleUnderMouse(neighborIndices);
    if (tri) {
      noStroke();
      fill(0, 255, 255, 80);
      triangle(tri[0].x, tri[0].y, tri[1].x, tri[1].y, tri[2].x, tri[2].y);
    }
  }
}

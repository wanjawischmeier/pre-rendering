let tileSize = 120;
let gridCols = 4;
let gridRows = 5;
let pointsPerTile = 2;
let searchStepsPerTileSqrt = 30;
let epsilon = 6;

let tiles = [];

function initTiles() {
  for (let y = 0; y < gridRows; y++) {
    for (let x = 0; x < gridCols; x++) {
      let tileX = x * tileSize;
      let tileY = y * tileSize;

      // Generate random points within the tile
      let points = [];
      for (let i = 0; i < pointsPerTile; i++) {
        let px = random(tileX, tileX + tileSize);
        let py = random(tileY, tileY + tileSize);
        points.push([createVector(px, py), color(random(255), random(255), random(255))]);
      }

      tiles.push({
        x: tileX,
        y: tileY,
        points: points
      });
    }
  }
}

function findThreeClosestPoints(target, pointArray) {
  // Start with max distance and dummy points
  let closest = [
    { dist: Infinity, point: null },
    { dist: Infinity, point: null },
    { dist: Infinity, point: null }
  ];

  for (let i = 0; i < pointArray.length; i++) {
    let p = pointArray[i];
    let d = dist(target.x, target.y, p[0].x, p[0].y);

    // Insert in the right spot if closer than any current
    if (d < closest[0].dist) {
      // Shift down
      closest[2] = closest[1];
      closest[1] = closest[0];
      closest[0] = { dist: d, point: p };
    } else if (d < closest[1].dist) {
      closest[2] = closest[1];
      closest[1] = { dist: d, point: p };
    } else if (d < closest[2].dist) {
      closest[2] = { dist: d, point: p };
    }
  }

  return closest;
}

function getThreePointDistanceDifference(target, points) {
  let d_a = dist(target.x, target.y, points[0].point[0].x, points[0].point[0].y);
  let d_b = dist(target.x, target.y, points[1].point[0].x, points[1].point[0].y);
  let d_c = dist(target.x, target.y, points[2].point[0].x, points[2].point[0].y);
  
  let dx_ab = abs(d_a - d_b);
  let dx_bc = abs(d_b - d_c);
  let dx_ac = abs(d_a - d_c);
  return (dx_ab + dx_bc + dx_ac);
  /*
  let distances = [d_a, d_b, d_c];

  // Find the max and min distances
  let maxDist = Math.max(...distances);
  let minDist = Math.min(...distances);

  // Return the difference — how far off they are from being equal
  return maxDist - minDist;
  */
}

function getThreePointTotalDistance(target, points) {
  return (
    dist(target.x, target.y, points[0].point[0].x, points[0].point[0].y) +
    dist(target.x, target.y, points[1].point[0].x, points[1].point[0].y) +
    dist(target.x, target.y, points[2].point[0].x, points[2].point[0].y)
  );
}



function setup() {
  createCanvas(windowWidth, windowHeight);
  initTiles();
}

function draw() {
  background(255);
  strokeWeight(1);
  
  for (let tile of tiles) {
    // Draw tile
    noFill();
    stroke(100);
    rect(tile.x, tile.y, tileSize, tileSize);

    // Draw points in tile
    noStroke();      
    for (let p of tile.points) {
      fill(p[1]);
      circle(p[0].x, p[0].y, 8);
    }
  }
  
  let cx = floor(mouseX / tileSize);
  let cy = floor(mouseY / tileSize);
  if (mouseX < 25 || mouseY < 25 || cx >= gridCols || cy >= gridRows)
    return;

  let lx = (cx - 1 + gridCols) % gridCols;
  let rx = (cx + 1) % gridCols;
  let ty = (cy - 1 + gridRows) % gridRows;
  let by = (cy + 1) % gridRows;
    
  let tileCenter = tiles[cx + cy * gridCols];
  let tileTopLeft = tiles[lx + ty * gridCols];
  let tileTopCenter = tiles[cx + ty * gridCols];
  let tileTopRight = tiles[rx + ty * gridCols];
  let tileCenterLeft = tiles[lx + cy * gridCols];
  let tileCenterRight = tiles[rx + cy * gridCols];
  let tileBottomLeft = tiles[lx + by * gridCols];
  let tileBottomCenter = tiles[cx + by * gridCols];
  let tileBottomRight = tiles[rx + by * gridCols];
  let allPoints = [...tileCenter.points, ...tileTopLeft.points, ...tileTopCenter.points, ...tileTopRight.points, ...tileCenterLeft.points, ...tileCenterRight.points, ...tileBottomLeft.points, ...tileBottomCenter.points, ...tileBottomRight.points];
  
  // Draw neighbors
  noFill();
  strokeWeight(2);
  stroke(200, 150, 0);
  rect(tileTopLeft.x, tileTopLeft.y, tileSize, tileSize);
  rect(tileTopCenter.x, tileTopCenter.y, tileSize, tileSize);
  rect(tileTopRight.x, tileTopRight.y, tileSize, tileSize);
  rect(tileCenterLeft.x, tileCenterLeft.y, tileSize, tileSize);
  rect(tileCenterRight.x, tileCenterRight.y, tileSize, tileSize);
  rect(tileBottomLeft.x, tileBottomLeft.y, tileSize, tileSize);
  rect(tileBottomCenter.x, tileBottomCenter.y, tileSize, tileSize);
  rect(tileBottomRight.x, tileBottomRight.y, tileSize, tileSize);
  
  // Draw center tile
  stroke(100, 200, 0);
  rect(tileCenter.x, tileCenter.y, tileSize, tileSize);
  
  let stepSize = tileSize / searchStepsPerTileSqrt;
  for (let y = tileCenter.y; y <= tileCenter.y + tileSize; y += stepSize) {
    for (let x = tileCenter.x; x <= tileCenter.x + tileSize; x += stepSize) {
      let pCenter = createVector(x, y);

      // Cardinal neighbors
      let pLeft   = createVector(max(tileCenter.x, x - stepSize), y);
      let pRight  = createVector(min(tileCenter.x + tileSize, x + stepSize), y);
      let pTop    = createVector(x, max(tileCenter.y, y - stepSize));
      let pBottom = createVector(x, min(tileCenter.y + tileSize, y + stepSize));

      // Diagonal (corner) neighbors
      let pTopLeft     = createVector(pLeft.x,  pTop.y);
      let pTopRight    = createVector(pRight.x, pTop.y);
      let pBottomLeft  = createVector(pLeft.x,  pBottom.y);
      let pBottomRight = createVector(pRight.x, pBottom.y);

      // Find closest 3 points for each probe
      let closestCenter      = findThreeClosestPoints(pCenter,      allPoints);
      let closestLeft        = findThreeClosestPoints(pLeft,        allPoints);
      let closestRight       = findThreeClosestPoints(pRight,       allPoints);
      let closestTop         = findThreeClosestPoints(pTop,         allPoints);
      let closestBottom      = findThreeClosestPoints(pBottom,      allPoints);
      let closestTopLeft     = findThreeClosestPoints(pTopLeft,     allPoints);
      let closestTopRight    = findThreeClosestPoints(pTopRight,    allPoints);
      let closestBottomLeft  = findThreeClosestPoints(pBottomLeft,  allPoints);
      let closestBottomRight = findThreeClosestPoints(pBottomRight, allPoints);

      // Get distance deviation from equidistantness
      let dxCenter      = getThreePointDistanceDifference(pCenter,      closestCenter);
      let dxLeft        = getThreePointDistanceDifference(pLeft,        closestLeft);
      let dxRight       = getThreePointDistanceDifference(pRight,       closestRight);
      let dxTop         = getThreePointDistanceDifference(pTop,         closestTop);
      let dxBottom      = getThreePointDistanceDifference(pBottom,      closestBottom);
      let dxTopLeft     = getThreePointDistanceDifference(pTopLeft,     closestTopLeft);
      let dxTopRight    = getThreePointDistanceDifference(pTopRight,    closestTopRight);
      let dxBottomLeft  = getThreePointDistanceDifference(pBottomLeft,  closestBottomLeft);
      let dxBottomRight = getThreePointDistanceDifference(pBottomRight, closestBottomRight);

      // Visualize main probe
      noStroke();
      // fill(closestCenter[0].point[1]); // green intensity based on equidistant-ness
      fill(0, dxCenter * 4, 0); // green intensity based on equidistant-ness
      circle(x, y, 6);

      // Check if center is a local minimum among all 8 neighbors
      if (
        dxCenter < dxLeft &&
        dxCenter < dxRight &&
        dxCenter < dxTop &&
        dxCenter < dxBottom &&
        dxCenter < dxTopLeft &&
        dxCenter < dxTopRight &&
        dxCenter < dxBottomLeft &&
        dxCenter < dxBottomRight
      ) {
        fill(250, 200, 0); // highlight as local Voronoi vertex candidate
        circle(x, y, 6);
      }
    }
  }
}

let points = [];
let selectedPoint = null;
const radius = 10;

function setup() {
  createCanvas(600, 600);
  // Initialize 4 points (v0 to v3)
  points.push(createVector(200, 200));
  points.push(createVector(300, 200));
  points.push(createVector(300, 300));
  points.push(createVector(200, 300));
}

function draw() {
  background(240);
  stroke(0);
  fill(255);

  // Draw quad
  beginShape();
  for (let pt of points) {
    vertex(pt.x, pt.y);
  }
  endShape(CLOSE);

  // Draw control points
  for (let pt of points) {
    fill(255);
    stroke(0);
    ellipse(pt.x, pt.y, radius * 2);
  }

  // Compute bounding box (symmetric around v0)
  let v0 = points[0];
  let minXY = v0.copy();
  let maxXY = v0.copy();

  for (let pt of points) {
    minXY = vectorMin(minXY, pt);
    maxXY = vectorMax(maxXY, pt);
  }

  let toMax = vectorAbs(p5.Vector.sub(maxXY, v0));
  let toMin = vectorAbs(p5.Vector.sub(minXY, v0));
  let offset = createVector(max(toMax.x, toMin.x), max(toMax.y, toMin.y));

  let boxMin = p5.Vector.sub(v0, offset);
  let boxMax = p5.Vector.add(v0, offset);

  // Draw bounding box
  noFill();
  stroke(255, 0, 0);
  strokeWeight(2);
  rectMode(CORNERS);
  rect(boxMin.x, boxMin.y, boxMax.x, boxMax.y);

  // Label
  noStroke();
  fill(0);
  text("Red box: symmetric bounding box around v0", 10, height - 10);
}

function mousePressed() {
  for (let pt of points) {
    if (dist(mouseX, mouseY, pt.x, pt.y) < radius) {
      selectedPoint = pt;
      break;
    }
  }
}

function mouseDragged() {
  if (selectedPoint) {
    selectedPoint.set(mouseX, mouseY);
  }
}

function mouseReleased() {
  selectedPoint = null;
}

// Utility functions
function vectorMin(a, b) {
  return createVector(min(a.x, b.x), min(a.y, b.y));
}

function vectorMax(a, b) {
  return createVector(max(a.x, b.x), max(a.y, b.y));
}

function vectorAbs(v) {
  return createVector(abs(v.x), abs(v.y));
}

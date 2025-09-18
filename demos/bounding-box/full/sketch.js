let points = [];
let selectedPoint = null;
const radius = 10;

function setup() {
  createCanvas(windowWidth, windowHeight);
  points = [
    createVector(200, 200),
    createVector(300, 200),
    createVector(300, 300),
    createVector(200, 300)
  ];
}

function draw() {
  background(240);
  stroke(0);
  fill(255);

  // Draw quad
  beginShape();
  for (let pt of points) vertex(pt.x, pt.y);
  endShape(CLOSE);

  // Draw points
  for (let pt of points) {
    ellipse(pt.x, pt.y, radius * 2);
  }

  // Compute cheap AABB bounding float3
  let v0 = points[0];
  let d1 = p5.Vector.sub(points[1], v0);
  let d2 = p5.Vector.sub(points[2], v0);
  let d3 = p5.Vector.sub(points[3], v0);

  let minX = min(0, d1.x, d2.x, d3.x);
  let maxX = max(0, d1.x, d2.x, d3.x);
  let minY = min(0, d1.y, d2.y, d3.y);
  let maxY = max(0, d1.y, d2.y, d3.y);

  let maxOffset = createVector(maxX, maxY);
  let minOffset = max(-minX, -minY);

  // Reconstruct bounding box corners
  let boxMin = createVector(v0.x - minOffset, v0.y - minOffset);
  let boxMax = createVector(v0.x + maxOffset.x, v0.y + maxOffset.y);

  // Draw bounding box
  noFill();
  stroke(255, 0, 0);
  strokeWeight(2);
  rectMode(CORNERS);
  rect(boxMin.x, boxMin.y, boxMax.x, boxMax.y);

  // Label
  noStroke();
  fill(0);
  text("Red box: simple axis-aligned bounding box around v0", 10, height - 10);
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
  if (selectedPoint) selectedPoint.set(mouseX, mouseY);
}

function mouseReleased() {
  selectedPoint = null;
}

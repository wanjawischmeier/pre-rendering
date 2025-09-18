let points = [];
let selectedPoint = null;
const radius = 10;

function setup() {
  createCanvas(600, 600);
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

  // Compute bounding float3
  let v0 = points[0];
  let center = createVector(0, 0);
  for (let pt of points) center.add(pt);
  center.div(4);

  let dir = p5.Vector.sub(center, v0).normalize();
  let maxProj = 0;
  for (let pt of points) {
    let delta = p5.Vector.sub(pt, v0);
    let proj = abs(delta.dot(dir));
    maxProj = max(maxProj, proj);
  }

  // Visualize
  let end = p5.Vector.add(v0, p5.Vector.mult(dir, maxProj));
  stroke(255, 0, 0);
  strokeWeight(2);
  line(v0.x, v0.y, end.x, end.y);

  noStroke();
  fill(0);
  text(`Direction: (${dir.x.toFixed(2)}, ${dir.y.toFixed(2)})`, 10, height - 30);
  text(`Magnitude: ${maxProj.toFixed(2)}`, 10, height - 15);
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

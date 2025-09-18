let points = [];
let n = 500; // number of random points
let k = 12;
let k2 = 50;
let isIterative = true;

function getNClosest(targetPoint, pointsArray, n) {
  // Clone the array and sort it based on distance to the target point
  let sorted = pointsArray.slice().sort((a, b) => {
    let d1 = dist(targetPoint.x, targetPoint.y, a.x, a.y);
    let d2 = dist(targetPoint.x, targetPoint.y, b.x, b.y);
    return d1 - d2;
  });

  // Return the first n points (excluding the target point if it's in the list)
  return sorted.filter(p => p !== targetPoint).slice(0, n);
}

function getDirectionalClosest(targetPoint, pointsArray, n, sectorCount = 4) {
  let sectors = Array(sectorCount).fill(null);
  let sectorAngles = TWO_PI / sectorCount;

  for (let p of pointsArray) {
    if (p === targetPoint) continue;

    let dx = p.x - targetPoint.x;
    let dy = p.y - targetPoint.y;
    let angle = atan2(dy, dx);
    if (angle < 0) angle += TWO_PI;

    let sectorIndex = floor(angle / sectorAngles);
    let current = sectors[sectorIndex];

    let d = dist(targetPoint.x, targetPoint.y, p.x, p.y);
    if (!current || d < current.dist) {
      sectors[sectorIndex] = { point: p, dist: d };
    }
  }

  // Extract points from sectors
  let selected = sectors
    .filter(s => s !== null)
    .map(s => s.point);

  // If we still need more, fill with globally closest (excluding already selected)
  if (selected.length < n) {
    let extras = pointsArray.slice().filter(p =>
      p !== targetPoint && !selected.includes(p)
    ).sort((a, b) => {
      let d1 = dist(targetPoint.x, targetPoint.y, a.x, a.y);
      let d2 = dist(targetPoint.x, targetPoint.y, b.x, b.y);
      return d1 - d2;
    }).slice(0, n - selected.length);

    selected = selected.concat(extras);
  }

  return selected;
}

function getNClosestInQuadrants(targetPoint, pointsArray, n) {
  const quadrantBuckets = [[], [], [], []]; // Q1: +x,+y, Q2: -x,+y, Q3: -x,-y, Q4: +x,-y

  for (let p of pointsArray) {
    if (p === targetPoint) continue;
    let dx = p.x - targetPoint.x;
    let dy = p.y - targetPoint.y;

    if (dx >= 0 && dy >= 0) quadrantBuckets[0].push(p);      // Q1
    else if (dx < 0 && dy >= 0) quadrantBuckets[1].push(p);   // Q2
    else if (dx < 0 && dy < 0) quadrantBuckets[2].push(p);    // Q3
    else if (dx >= 0 && dy < 0) quadrantBuckets[3].push(p);   // Q4
  }

  let perQuadrant = floor(n / 4);
  let selected = [];

  for (let i = 0; i < 4; i++) {
    let sorted = quadrantBuckets[i].slice().sort((a, b) => {
      let d1 = dist(targetPoint.x, targetPoint.y, a.x, a.y);
      let d2 = dist(targetPoint.x, targetPoint.y, b.x, b.y);
      return d1 - d2;
    });

    selected.push(...sorted.slice(0, perQuadrant));
  }

  // Fill remaining with global closest (if any slots left)
  if (selected.length < n) {
    let extras = pointsArray
      .filter(p => p !== targetPoint && !selected.includes(p))
      .sort((a, b) =>
        dist(targetPoint.x, targetPoint.y, a.x, a.y) -
        dist(targetPoint.x, targetPoint.y, b.x, b.y)
      )
      .slice(0, n - selected.length);

    selected = selected.concat(extras);
  }

  return selected;
}



// Check if point p is inside triangle a, b, c using barycentric coords
function pointInTriangle(p, a, b, c) {
  let v0 = p5.Vector.sub(c, a);
  let v1 = p5.Vector.sub(b, a);
  let v2 = p5.Vector.sub(p, a);

  let d00 = v0.dot(v0);
  let d01 = v0.dot(v1);
  let d02 = v0.dot(v2);
  let d11 = v1.dot(v1);
  let d12 = v1.dot(v2);

  let denom = d00 * d11 - d01 * d01;
  if (denom === 0) return false;

  let u = (d11 * d02 - d01 * d12) / denom;
  let v = (d00 * d12 - d01 * d02) / denom;

  return u >= 0 && v >= 0 && u + v <= 1;
}

// Check if test point lies inside circumcircle of triangle a, b, c
function inCircumcircle(test, a, b, c, debug) {
  // Translate points to origin (relative to a)
  let ax = a.x, ay = a.y;
  let bx = b.x, by = b.y;
  let cx = c.x, cy = c.y;

  // Midpoints of AB and AC
  let d = 2 * (ax*(by - cy) + bx*(cy - ay) + cx*(ay - by));
  if (d === 0) return false; // Degenerate triangle

  let ux = ((ax*ax + ay*ay)*(by - cy) + (bx*bx + by*by)*(cy - ay) + (cx*cx + cy*cy)*(ay - by)) / d;
  let uy = ((ax*ax + ay*ay)*(cx - bx) + (bx*bx + by*by)*(ax - cx) + (cx*cx + cy*cy)*(bx - ax)) / d;

  let center = createVector(ux, uy);
  let radius = dist(center.x, center.y, ax, ay);
  let dTest = dist(center.x, center.y, test.x, test.y);
  
  if (debug) {
    // === Draw circle ===
    stroke(150, 100);
    strokeWeight(1);
    ellipse(center.x, center.y, radius * 2);   
  }
  
  return [dTest < radius, center, radius];
}


// Main function: returns array of 3 vectors (triangle that contains x, y and passes Delaunay)
function findDelaunayTriangle(p, vertices, debug) {
  let iters = 0;
  
  for (let i = 0; i < vertices.length; i++) {    
    let a = vertices[i];
    for (let j = i + 1; j < vertices.length; j++) {
      let b = vertices[j];
      for (let k = j + 1; k < vertices.length; k++) {
        let c = vertices[k];

        if (!pointInTriangle(p, a, b, c)) continue;

        let isDelaunay = true;
        let cc = inCircumcircle(a, a, b, c, false);
        
        for (let m = 0; m < vertices.length; m++) {
          if (m === i || m === j || m === k) continue;
          if (inCircumcircle(vertices[m], a, b, c, false)[0]) {
            if (!isIterative && false) {
              stroke(200, 200, 200);
              strokeWeight(1);
              triangle(a.x, a.y, b.x, b.y, c.x, c.y);
              strokeWeight(10);
              point(vertices[m].x, vertices[m].y);
            }
            
            isDelaunay = false;
            break;
          }
          
          iters++;
        }

        if (isDelaunay) {
          if (debug) {
            // === Draw circle ===
            stroke(150, 100);
            strokeWeight(1);
            ellipse(cc[1].x, cc[1].y, cc[2] * 2);
          }

          print("Found in " + iters);
          return [a, b, c];
        }
      }
    }
  }
  // print(del.length);
  print("Failed in " + iters);
  return null; // No valid triangle found
}

function drawTriangle(tri, col) {
  fill(col);
  stroke(0);
  triangle(
    tri[0].x, tri[0].y,
    tri[1].x, tri[1].y,
    tri[2].x, tri[2].y
  );
}

function areTrianglesEqual(t1, t2) {
  // Check if both triangles contain the same 3 vertices (order-independent)
  for (let v1 of t1) {
    let match = t2.some(v2 => v1.x === v2.x && v1.y === v2.y);
    if (!match) return false;
  }
  return true;
}



function setup() {
  createCanvas(windowWidth, windowHeight);
  background(255);

  // Generate and store random points
  for (let i = 0; i < n; i++) {
    let x = random(width);
    let y = random(height);
    points.push(createVector(x, y));
  }

  // Draw the points
  stroke(0);
  strokeWeight(5);
  for (let pt of points) {
    point(pt.x, pt.y);
  }
}

function draw() {
  if (!isIterative) {
    background(255);

    // Draw the points
    stroke(0);
    strokeWeight(5);
    for (let pt of points) {
      point(pt.x, pt.y);
    }
  }
  
  let mouse;
  if (mouseX == 0) {
    mouse = createVector(random(width), random(height));
  } else {
    mouse = createVector(mouseX, mouseY);
  }
  let closest = getNClosestInQuadrants(mouse, points, k);
  let closest2 = getNClosest(mouse, points, k2);
  
  if (!isIterative) {
    stroke(200, 100, 50);
    strokeWeight(5);
    for (let pt of closest) {
      point(pt.x, pt.y);
    }

    stroke(200, 0, 0);
  }
  
  noFill();
  strokeWeight(1);
  let tri = findDelaunayTriangle(mouse, closest, mouseX != 0);
  let tri2 = findDelaunayTriangle(mouse, closest2, false);
  
  if (!tri && tri2) {
    // Case a: first triangle is null, second exists
    drawTriangle(tri2, color(255, 0, 255)); // pink
  } else if (tri && tri2 && !areTrianglesEqual(tri, tri2)) {
    // Case b: both exist and are different
    drawTriangle(tri2, color(255, 0, 0)); // red
  }
  
  if (tri == null) return;
  
  stroke(50, 200, 50);
  triangle(tri[0].x, tri[0].y, tri[1].x, tri[1].y, tri[2].x, tri[2].y);
}